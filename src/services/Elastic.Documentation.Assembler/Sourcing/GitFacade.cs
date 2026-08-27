// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ExternalCommands;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Sourcing;

public interface IGitRepository
{
	void Init();
	string GetCurrentCommit();
	void GitAddOrigin(string origin);
	bool IsInitialized();
	void Pull(string branch);
	void Fetch(string reference);
	void EnableSparseCheckout(string[] folders);
	void DisableSparseCheckout();
	void Checkout(string reference);
}

// This git repository implementation is optimized for pull and fetching single commits.
// It uses `git pull --depth 1` and `git fetch --depth 1` to minimize the amount of data transferred.
public class SingleCommitOptimizedGitRepository(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IDirectoryInfo workingDirectory,
	TimeSpan? cloneTimeout = null
) : ExternalCommandExecutor(collector, workingDirectory), IGitRepository
{
	private static readonly Dictionary<string, string> EnvironmentVars = new()
	{
		// Disable git editor prompts:
		// There are cases where `git pull` would prompt for an editor to write a commit message.
		// This env variable prevents that.
		{
			"GIT_EDITOR",
			"true"
		}
	};

	// Network-bound operations retry up to 3 times with exponential back-off.
	// Each attempt is bounded by the per-repo clone_timeout (default: CI ? 10min : unbounded).
	// CloneRef wraps Fetch with up to 3 wipe-and-reclone passes, so the realistic worst case is
	// 3 × 3 = 9 fetch invocations, each bounded by the per-attempt timeout.
	private readonly RetryPolicy _networkRetry = new(
		MaxAttempts: 3,
		BaseDelay: TimeSpan.FromSeconds(5),
		AttemptTimeout: cloneTimeout ?? GitTimeouts.CiDefault
	);

	/// <inheritdoc />
	protected override ILogger Logger { get; } = logFactory.CreateLogger<SingleCommitOptimizedGitRepository>();

	public string GetCurrentCommit() => Capture("git", "rev-parse", "HEAD");

	public void Init() => ExecIn(EnvironmentVars, "git", "init");
	public bool IsInitialized() => Directory.Exists(Path.Join(WorkingDirectory.FullName, ".git"));
	public void Pull(string branch) =>
		_ =
			ExecInWithRetry(
				EnvironmentVars,
				_networkRetry,
				"git",
				"pull",
				"--depth",
				"1",
				"--allow-unrelated-histories",
				"--no-ff",
				"origin",
				branch
			);
	public void Fetch(string reference) =>
		_ =
			ExecInWithRetry(
				EnvironmentVars,
				_networkRetry,
				"git",
				"fetch",
				"--no-tags",
				"--prune",
				"--no-recurse-submodules",
				"--depth",
				"1",
				"origin",
				reference
			);
	public void EnableSparseCheckout(string[] folders) =>
		ExecIn(EnvironmentVars, "git", ["sparse-checkout", "set", "--no-cone", .. folders]);

	public void DisableSparseCheckout() => ExecIn(EnvironmentVars, "git", "sparse-checkout", "disable");
	public void Checkout(string reference) => ExecIn(EnvironmentVars, "git", "checkout", "--force", reference);

	public void GitAddOrigin(string origin) => ExecIn(EnvironmentVars, "git", "remote", "add", "origin", origin);
}
