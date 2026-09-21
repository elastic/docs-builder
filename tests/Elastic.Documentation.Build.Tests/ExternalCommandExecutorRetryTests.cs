// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ExternalCommands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcNet;

namespace Elastic.Documentation.Build.Tests;

public class ExternalCommandExecutorRetryTests
{
	private static readonly RetryPolicy FiveAttempts = new(MaxAttempts: 5, BaseDelay: TimeSpan.FromSeconds(1));

	[Fact]
	public void ExecInWithRetry_SucceedsOnFirstAttempt_EmitsNoErrors()
	{
		var executor = CreateExecutor(ExitCode(0));

		var succeeded = executor.Retry(FiveAttempts);

		succeeded.Should().BeTrue();
		executor.CallCount.Should().Be(1);
		executor.Diagnostics.Errors.Should().Be(0);
		executor.RecordedDelays.Should().BeEmpty();
	}

	[Fact]
	public void ExecInWithRetry_SucceedsAfterTransientFailures_EmitsNoErrors()
	{
		var executor = CreateExecutor(ExitCode(1), ExitCode(1), ExitCode(0));

		var succeeded = executor.Retry(FiveAttempts);

		succeeded.Should().BeTrue();
		executor.CallCount.Should().Be(3);
		executor.Diagnostics.Errors.Should().Be(0);
		executor.RecordedDelays.Should().Equal(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
	}

	[Fact]
	public void ExecInWithRetry_ExhaustsAllAttempts_EmitsExactlyOneError()
	{
		var executor = CreateExecutor(ExitCode(1), ExitCode(1), ExitCode(1), ExitCode(1), ExitCode(1));

		var succeeded = executor.Retry(FiveAttempts);

		succeeded.Should().BeFalse();
		executor.CallCount.Should().Be(5);
		executor.Diagnostics.Errors.Should().Be(1);
		executor
			.RecordedDelays
			.Should()
			.Equal(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8));
	}

	[Fact]
	public void ExecInWithRetry_WithCustomPolicy_HonoursAttemptsAndBaseDelay()
	{
		var executor = CreateExecutor(ExitCode(1), ExitCode(1), ExitCode(1));

		var succeeded = executor.Retry(new RetryPolicy(MaxAttempts: 3, BaseDelay: TimeSpan.FromSeconds(2)));

		succeeded.Should().BeFalse();
		executor.CallCount.Should().Be(3);
		executor.Diagnostics.Errors.Should().Be(1);
		executor.RecordedDelays.Should().Equal(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
	}

	[Fact]
	public void ExecIn_WhenCommandFails_EmitsOneErrorWithoutRetrying()
	{
		var executor = CreateExecutor(ExitCode(1));

		executor.ExecOnce();

		executor.CallCount.Should().Be(1);
		executor.Diagnostics.Errors.Should().Be(1);
		executor.RecordedDelays.Should().BeEmpty();
	}

	[Fact]
	public void ExecInWithRetry_WhenAttemptTimesOut_RetriesAndSucceeds()
	{
		// First attempt simulates a ProcNet per-attempt timeout; second succeeds.
		var executor = CreateExecutor(Timeout("10 minutes"), ExitCode(0));

		var succeeded = executor.Retry(FiveAttempts);

		succeeded.Should().BeTrue();
		executor.CallCount.Should().Be(2);
		executor.Diagnostics.Errors.Should().Be(0);
		// One delay recorded between the timed-out attempt and the successful retry.
		executor.RecordedDelays.Should().HaveCount(1);
	}

	[Fact]
	public void ExecInWithRetry_WhenEveryAttemptTimesOut_EmitsExactlyOneError()
	{
		var policy = new RetryPolicy(MaxAttempts: 3, BaseDelay: TimeSpan.FromSeconds(1));
		var executor = CreateExecutor(Timeout("10 minutes"), Timeout("10 minutes"), Timeout("10 minutes"));

		var succeeded = executor.Retry(policy);

		succeeded.Should().BeFalse();
		executor.CallCount.Should().Be(3);
		executor.Diagnostics.Errors.Should().Be(1);
	}

	[Fact]
	public void ExecInWithRetry_OnRetry_CallsOnBeforeRetryOncePerRetryNotBeforeFirstAttempt()
	{
		var executor = CreateExecutor(ExitCode(1), ExitCode(1), ExitCode(0));

		var succeeded = executor.Retry(FiveAttempts);

		succeeded.Should().BeTrue();
		executor.OnBeforeRetryCallCount.Should().Be(2);
	}

	[Fact]
	public void ExecInWithRetry_OnFirstAttemptSuccess_OnBeforeRetryNeverCalled()
	{
		var executor = CreateExecutor(ExitCode(0));

		executor.Retry(FiveAttempts);

		executor.OnBeforeRetryCallCount.Should().Be(0);
	}

	// ── Helpers ──────────────────────────────────────────────────────────────────

	/// <summary>Scripts a normal process exit.</summary>
	private static Func<int> ExitCode(int code) => () => code;

	/// <summary>Scripts a ProcNet per-attempt timeout (throws rather than returning an exit code).</summary>
	private static Func<int> Timeout(string message) => () => throw new ProcExecException($"Timeout {message}");

	private static RetryTestCommandExecutor CreateExecutor(params Func<int>[] steps)
	{
		var fileSystem = new MockFileSystem();
		var workingDirectory = fileSystem.DirectoryInfo.New("/workspace/repo");
		workingDirectory.Create();
		return new RetryTestCommandExecutor(new DiagnosticsCollector([]), workingDirectory, steps);
	}

	private sealed class RetryTestCommandExecutor(
		IDiagnosticsCollector collector,
		IDirectoryInfo workingDirectory,
		Func<int>[] steps
	) : ExternalCommandExecutor(collector, workingDirectory)
	{
		public int CallCount { get; private set; }

		public int OnBeforeRetryCallCount { get; private set; }

		public List<TimeSpan> RecordedDelays { get; } = [];

		public IDiagnosticsCollector Diagnostics => Collector;

		protected override ILogger Logger { get; } = NullLogger.Instance;

		protected override int ExecInCore(
			Dictionary<string, string> environmentVars,
			TimeSpan? attemptTimeout,
			string binary,
			params string[] args
		)
		{
			if (CallCount >= steps.Length)
				throw new InvalidOperationException($"Unexpected invocation {CallCount + 1}, only {steps.Length} steps were scripted");
			return steps[CallCount++](); // may throw ProcExecException
		}

		protected override void OnBeforeRetry() => OnBeforeRetryCallCount++;

		protected override void DelayBeforeRetry(TimeSpan delay) => RecordedDelays.Add(delay);

		public bool Retry(RetryPolicy policy) => ExecInWithRetry([], policy, "git", "fetch", "origin", "main");

		public void ExecOnce() => ExecIn([], "git", "fetch", "origin", "main");
	}
}
