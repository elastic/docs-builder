// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ExternalCommands;
using Microsoft.Extensions.Logging;

namespace Elastic.Codex.Sourcing;

/// <summary>
/// Git repository operations optimized for shallow clones.
/// </summary>
public class CodexGitRepository(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IDirectoryInfo workingDirectory
) : ExternalCommandExecutor(collector, workingDirectory)
{
	/// <inheritdoc />
	protected override ILogger Logger { get; } = logFactory.CreateLogger<CodexGitRepository>();

	private static readonly Dictionary<string, string> EnvironmentVars = new()
	{
		// Disable git editor prompts
		{
			"GIT_EDITOR",
			"true"
		}
	};

	// Network-bound fetch operations retry up to 3 times with exponential back-off.
	// The default CI timeout is shared with the assembler path (10 min per attempt).
	private static readonly RetryPolicy NetworkRetry = new(
		MaxAttempts: 3,
		BaseDelay: TimeSpan.FromSeconds(5),
		AttemptTimeout: GitTimeouts.CiDefault
	);

	protected override void OnBeforeRetry() =>
		GitLocks.ClearStale(
			WorkingDirectory.FileSystem,
			WorkingDirectory.FullName,
			f => Logger.LogWarning("Removed stale git lock file {LockFile}", f)
		);

	public string GetCurrentCommit() => Capture("git", "rev-parse", "HEAD");

	public bool HasHead() => !string.IsNullOrEmpty(CaptureQuiet("git", "rev-parse", "--verify", "HEAD"));

	public void Init() => ExecIn(EnvironmentVars, "git", "init");

	public bool IsInitialized() => Directory.Exists(Path.Join(WorkingDirectory.FullName, ".git"));

	public void Fetch(string reference)
	{
		if (
			!ExecInWithRetry(
				EnvironmentVars,
				NetworkRetry,
				"git",
				"fetch",
				"--no-tags",
				"--prune",
				"--no-recurse-submodules",
				"--depth",
				"1",
				"origin",
				reference
			)
		)
			throw new InvalidOperationException($"git fetch failed for '{reference}'");
	}

	public void EnableSparseCheckout(string[] folders) =>
		ExecIn(EnvironmentVars, "git", ["sparse-checkout", "set", "--no-cone", .. folders]);

	public void Checkout(string reference)
	{
		if (!ExecInWithRetry(EnvironmentVars, RetryPolicy.None, "git", "checkout", "--force", reference))
			throw new InvalidOperationException($"git checkout failed for '{reference}'");
	}

	public void GitAddOrigin(string origin) => ExecIn(EnvironmentVars, "git", "remote", "add", "origin", origin);
}
