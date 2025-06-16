// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling.ExternalCommands;

namespace Documentation.Builder.Tracking;

public record GitChange(string FilePath, GitChangeType ChangeType);
public record RenamedGitChange(string OldFilePath, string NewFilePath, GitChangeType ChangeType) : GitChange(OldFilePath, ChangeType);

public class LocalGitRepositoryTracker(DiagnosticsCollector collector, IDirectoryInfo workingDirectory) : ExternalCommandExecutor(collector, workingDirectory), IRepositoryTracker
{
	public IEnumerable<GitChange> GetChangedFiles(string lookupPath)
	{
		var defaultBranch = GetDefaultBranch();
		var commitChanges = CaptureMultiple("git", "diff", "--name-status", $"{defaultBranch}...HEAD", "--", $"./{lookupPath}");
		var localChanges = CaptureMultiple("git", "status", "--porcelain");
		_ = Capture("git", "stash", "push", "--", $"./{lookupPath}");
		var localUnstagedChanges = CaptureMultiple("git", "stash", "show", "--name-status", "-u");
		_ = Capture("git", "stash", "pop");

		return [.. GetCommitChanges(commitChanges), .. GetLocalChanges(localChanges), .. GetCommitChanges(localUnstagedChanges)];
	}

	private string GetDefaultBranch()
	{
		if (!Capture(true, "git", "merge-base", "-a", "HEAD", "main").StartsWith("fatal", StringComparison.InvariantCulture))
			return "main";
		if (!Capture(true, "git", "merge-base", "-a", "HEAD", "master").StartsWith("fatal", StringComparison.InvariantCulture))
			return "master";
		return Capture("git", "symbolic-ref", "refs/remotes/origin/HEAD").Split('/').Last();
	}

	private static IEnumerable<GitChange> GetCommitChanges(string[] changes)
	{
		foreach (var change in changes)
		{
			var parts = change.AsSpan().TrimStart();
			if (parts.Length < 2)
				continue;

			var changeType = parts[0] switch
			{
				'A' => GitChangeType.Added,
				'M' => GitChangeType.Modified,
				'D' => GitChangeType.Deleted,
				'R' => GitChangeType.Renamed,
				_ => GitChangeType.Other
			};

			yield return new GitChange(change.Split('\t')[1], changeType);
		}
	}

	private static IEnumerable<GitChange> GetLocalChanges(string[] changes)
	{
		foreach (var change in changes)
		{
			var changeStatusCode = change.AsSpan();
			if (changeStatusCode.Length < 2)
				continue;

			var changeType = (changeStatusCode[0], changeStatusCode[1]) switch
			{
				('R', _) or (_, 'R') => GitChangeType.Renamed,
				('D', _) or (_, 'D') when changeStatusCode[0] != 'A' => GitChangeType.Deleted,
				('?', '?') => GitChangeType.Untracked,
				('A', _) or (_, 'A') => GitChangeType.Added,
				('M', _) or (_, 'M') => GitChangeType.Modified,
				_ => GitChangeType.Other
			};

			var changeParts = change.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			yield return changeType switch
			{
				GitChangeType.Renamed => new RenamedGitChange(changeParts[1], changeParts[3], changeType),
				_ => new GitChange(changeParts[1], changeType)
			};
		}
	}
}
