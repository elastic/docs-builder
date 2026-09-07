// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Codex.Sourcing;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ExternalCommands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Navigation.Tests.Codex;

public class CodexGitRepositoryTests
{
	[Fact]
	public void Fetch_WhenGitFails_ThrowsWithoutLeavingTheFailureAsAnUncaughtJobError()
	{
		var gitCollector = new DiagnosticsCollector([]);
		var git = CreateGit(gitCollector, ExitCode(128), ExitCode(128), ExitCode(128));

		var act = () => git.Fetch("abc123");

		act.Should().Throw<InvalidOperationException>().WithMessage("git fetch failed for 'abc123'");
		gitCollector.Errors.Should().Be(1);
	}

	[Fact]
	public void Checkout_WhenGitFails_ThrowsAfterRecordingTheFailureOnTheGitCollector()
	{
		var gitCollector = new DiagnosticsCollector([]);
		var git = CreateGit(gitCollector, ExitCode(1));

		var act = () => git.Checkout("FETCH_HEAD");

		act.Should().Throw<InvalidOperationException>().WithMessage("git checkout failed for 'FETCH_HEAD'");
		gitCollector.Errors.Should().Be(1);
	}

	private static Func<int> ExitCode(int code) => () => code;

	private static ScriptedCodexGitRepository CreateGit(IDiagnosticsCollector gitCollector, params Func<int>[] steps)
	{
		var fileSystem = new MockFileSystem();
		var workingDirectory = fileSystem.DirectoryInfo.New("/tmp/clone/repo");
		workingDirectory.Create();
		return new ScriptedCodexGitRepository(gitCollector, workingDirectory, steps);
	}

	private sealed class ScriptedCodexGitRepository(
		IDiagnosticsCollector collector,
		IDirectoryInfo workingDirectory,
		Func<int>[] steps
	) : CodexGitRepository(NullLoggerFactory.Instance, collector, workingDirectory)
	{
		private int _callCount;

		protected override int ExecInCore(
			Dictionary<string, string> environmentVars,
			TimeSpan? attemptTimeout,
			string binary,
			params string[] args
		)
		{
			if (_callCount >= steps.Length)
				throw new InvalidOperationException($"Unexpected invocation {_callCount + 1}");
			return steps[_callCount++]();
		}

		protected override void DelayBeforeRetry(TimeSpan delay)
		{
			// Tests must not wait on the production 5s fetch back-off.
		}
	}
}
