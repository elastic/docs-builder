// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using ProcNet;

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
public class SingleCommitOptimizedGitRepository(DiagnosticsCollector collector, IDirectoryInfo workingDirectory) : IGitRepository
{
	public string GetCurrentCommit() => Capture("git", "rev-parse", "HEAD");

	public void Init() => ExecIn("git", "init");
	public bool IsInitialized() => Directory.Exists(Path.Combine(workingDirectory.FullName, ".git"));
	public void Pull(string branch) => ExecIn("git", "pull", "--depth", "1", "--allow-unrelated-histories", "--no-ff", "origin", branch);
	public void Fetch(string reference) => ExecIn("git", "fetch", "--no-tags", "--prune", "--no-recurse-submodules", "--depth", "1", "origin", reference);
	public void EnableSparseCheckout(string folder) => ExecIn("git", "sparse-checkout", "set", folder);
	public void DisableSparseCheckout() => ExecIn("git", "sparse-checkout", "disable");
	public void Checkout(string reference) => ExecIn("git", "checkout", "--force", reference);

	public void GitAddOrigin(string origin) => ExecIn("git", "remote", "add", "origin", origin);

	private void ExecIn(string binary, params string[] args)
	{
		var arguments = new ExecArguments(binary, args)
		{
			WorkingDirectory = workingDirectory.FullName,
			Environment = new Dictionary<string, string>
			{
				// Disable git editor prompts:
				// There are cases where `git pull` would prompt for an editor to write a commit message.
				// This env variable prevents that.
				{ "GIT_EDITOR", "true" }
			},
		};
		var result = Proc.Exec(arguments);
		if (result != 0)
			collector.EmitError("", $"Exit code: {result} while executing {binary} {string.Join(" ", args)} in {workingDirectory}");
	}
	private string Capture(string binary, params string[] args)
	{
		// Try 10 times to capture the output of the command, if it fails, we'll throw an exception on the last try
		Exception? e = null;
		for (var i = 0; i <= 9; i++)
		{
			try
			{
				return CaptureOutput();
			}
			catch (Exception ex)
			{
				if (ex is not null)
					e = ex;
			}
		}

		if (e is not null)
			collector.EmitError("", "failure capturing stdout", e);

		return string.Empty;

		string CaptureOutput()
		{
			var arguments = new StartArguments(binary, args)
			{
				WorkingDirectory = workingDirectory.FullName,
				Timeout = TimeSpan.FromSeconds(3),
				WaitForExit = TimeSpan.FromSeconds(3),
				ConsoleOutWriter = NoopConsoleWriter.Instance
			};
			var result = Proc.Start(arguments);
			var line = result.ExitCode != 0
				? throw new Exception($"Exit code is not 0. Received {result.ExitCode} from {binary}: {workingDirectory}")
				: result.ConsoleOut.FirstOrDefault()?.Line ?? throw new Exception($"No output captured for {binary}: {workingDirectory}");
			return line;
		}
	}
}
