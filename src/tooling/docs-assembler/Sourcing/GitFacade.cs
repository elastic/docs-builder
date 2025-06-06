// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling.ExternalCommands;

namespace Documentation.Assembler.Sourcing;


public interface IGitRepository
{
	void Init();
	string GetCurrentCommit();
	void GitAddOrigin(string origin);
	bool IsInitialized();
	void Pull(string branch);
	void Fetch(string reference);
	void EnableSparseCheckout(string folder);
	void DisableSparseCheckout();
	void Checkout(string reference);
}

// This git repository implementation is optimized for pull and fetching single commits.
// It uses `git pull --depth 1` and `git fetch --depth 1` to minimize the amount of data transferred.
public class SingleCommitOptimizedGitRepository(DiagnosticsCollector collector, IDirectoryInfo workingDirectory) : ExternalCommandExecutor(collector, workingDirectory), IGitRepository
{
	private static readonly Dictionary<string, string> EnvironmentVars = new()
	{
		// Disable git editor prompts:
		// There are cases where `git pull` would prompt for an editor to write a commit message.
		// This env variable prevents that.
		{ "GIT_EDITOR", "true" }
	};

	public string GetCurrentCommit() => Capture("git", "rev-parse", "HEAD");

	public void Init() => ExecIn(EnvironmentVars, "git", "init");
	public bool IsInitialized() => Directory.Exists(Path.Combine(WorkingDirectory.FullName, ".git"));
	public void Pull(string branch) => ExecIn(EnvironmentVars, "git", "pull", "--depth", "1", "--allow-unrelated-histories", "--no-ff", "origin", branch);
	public void Fetch(string reference) => ExecIn(EnvironmentVars, "git", "fetch", "--no-tags", "--prune", "--no-recurse-submodules", "--depth", "1", "origin", reference);
	public void EnableSparseCheckout(string folder) => ExecIn(EnvironmentVars, "git", "sparse-checkout", "set", folder);
	public void DisableSparseCheckout() => ExecIn(EnvironmentVars, "git", "sparse-checkout", "disable");
	public void Checkout(string reference) => ExecIn(EnvironmentVars, "git", "checkout", "--force", reference);

	public void GitAddOrigin(string origin) => ExecIn(EnvironmentVars, "git", "remote", "add", "origin", origin);
}
