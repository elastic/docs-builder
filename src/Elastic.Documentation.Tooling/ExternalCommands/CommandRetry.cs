// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ProcNet;

namespace Elastic.Documentation.ExternalCommands;

/// <param name="MaxAttempts">Total attempts, including the first.</param>
/// <param name="BaseDelay">Delay before the second attempt. Each later attempt doubles it.</param>
/// <param name="AttemptTimeout">Per-attempt timeout. Overrides the executor-level timeout when set.</param>
public readonly record struct RetryPolicy(int MaxAttempts, TimeSpan BaseDelay, TimeSpan? AttemptTimeout = null)
{
	public static RetryPolicy None { get; } = new(1, TimeSpan.Zero);

	public TimeSpan DelayBeforeAttempt(int attempt) => attempt <= 1 ? TimeSpan.Zero : BaseDelay * Math.Pow(2, attempt - 2);
}

/// <summary>Outcome of a single command attempt.</summary>
/// <param name="ExitCode">The process exit code, or -1 when the attempt threw (e.g. ProcNet timeout).</param>
/// <param name="Exception">The exception that ended the attempt, or null when it exited cleanly.</param>
public readonly record struct CommandFailure(int ExitCode, Exception? Exception)
{
	public override string ToString() => Exception is not null ? $"exit {ExitCode}: {Exception.Message}" : $"exit {ExitCode}";
}

/// <summary>
/// Low-level retry primitive used by <see cref="ExternalCommandExecutor"/>.
/// Treats a <see cref="ProcExecException"/> (per-attempt ProcNet timeout) as a retryable failure
/// rather than an unhandled exception, so the retry loop always engages on transient network stalls.
/// </summary>
public static class CommandRetry
{
	/// <summary>
	/// Runs <paramref name="invoke"/> up to <see cref="RetryPolicy.MaxAttempts"/> times.
	/// Returns <c>null</c> on success (exit code 0); returns the last <see cref="CommandFailure"/> when
	/// all attempts are exhausted.
	/// </summary>
	/// <param name="policy">Retry configuration.</param>
	/// <param name="invoke">Factory that runs the command and returns its exit code.</param>
	/// <param name="delay">Called with the computed back-off interval before each retry (not called before attempt 1).</param>
	/// <param name="onRetry">Called after each failed attempt that has a retry remaining.</param>
	public static CommandFailure? Invoke(RetryPolicy policy, Func<int> invoke, Action<TimeSpan> delay, Action<CommandFailure> onRetry)
	{
		CommandFailure last = default;
		for (var attempt = 1; attempt <= policy.MaxAttempts; attempt++)
		{
			if (attempt > 1)
				delay(policy.DelayBeforeAttempt(attempt));

			int exitCode;
			Exception? caught = null;
			try
			{
				exitCode = invoke();
			}
			catch (ProcExecException ex)
			{
				// ProcNet throws on per-attempt timeout instead of returning a non-zero exit code.
				// We catch it here so the retry loop always sees a failure as a failure, not as
				// an unhandled escape that bypasses the retry budget.
				exitCode = -1;
				caught = ex;
			}

			if (exitCode == 0)
				return null;

			last = new CommandFailure(exitCode, caught);
			if (attempt < policy.MaxAttempts)
				onRetry(last);
		}

		return last;
	}
}

/// <summary>Shared git timeout constants.</summary>
public static class GitTimeouts
{
	/// <summary>
	/// Default per-attempt timeout applied to network git operations in CI.
	/// Returns <c>null</c> (no timeout) outside CI so local first-clones are not killed.
	/// </summary>
	public static TimeSpan? CiDefault => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ? null : TimeSpan.FromMinutes(10);
}
