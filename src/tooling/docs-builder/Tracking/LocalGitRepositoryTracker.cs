// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling.ExternalCommands;
namespace Documentation.Builder.Tracking;

public partial class LocalGitRepositoryTracker(DiagnosticsCollector collector, IDirectoryInfo workingDirectory) : ExternalCommandExecutor(collector, workingDirectory), IRepositoryTracker
{
	public IEnumerable<string> GetChangedFiles(string lookupPath)
	{
		var defaultBranch = GetDefaultBranch();
		var commitChanges = CaptureMultiple("git", "diff", "--name-status", $"{defaultBranch}...HEAD", "--", $"./{lookupPath}");
		var localChanges = CaptureMultiple("git", "status", "--porcelain");
		List<string> output = [
			.. commitChanges
			.Where(line => line.StartsWith('R') || line.StartsWith('D') || line.StartsWith('A'))
			.Select(line => line.Split('\t')[1]),
			.. localChanges
			.Select(x => x.TrimStart())
			.Where(line => line.StartsWith('R') || line.StartsWith('D') || line.StartsWith("A ", StringComparison.Ordinal) || line.StartsWith("??"))
			.Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1])
		];
		return output.Where(line => line.StartsWith(lookupPath));
	}

	private string GetDefaultBranch()
	{
		if (!Capture(true, "git", "merge-base", "-a", "HEAD", "main").StartsWith("fatal", StringComparison.InvariantCulture))
			return "main";
		if (!Capture(true, "git", "merge-base", "-a", "HEAD", "master").StartsWith("fatal", StringComparison.InvariantCulture))
			return "master";
		return Capture("git", "symbolic-ref", "refs/remotes/origin/HEAD").Split('/').Last();
	}
}
