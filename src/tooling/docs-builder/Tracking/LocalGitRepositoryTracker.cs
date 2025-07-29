// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling.ExternalCommands;

namespace Documentation.Builder.Tracking;

public record FileChange(string FilePath, FileChangeType ChangeType);
public record RenamedFileChange(string OldFilePath, string NewFilePath, FileChangeType ChangeType) : FileChange(OldFilePath, ChangeType);

public interface IChangedFilesTracker
{
	IEnumerable<FileChange> GetChangedFiles();
}

public class LocalGitRepositoryTracker(DiagnosticsCollector collector, BuildContext context)
	: ExternalCommandExecutor(collector, context.DocumentationSourceDirectory), IChangedFilesTracker
{
	public IEnumerable<FileChange> GetChangedFiles()
	{
		var defaultBranch = GetDefaultBranch();
		var commitChanges = CaptureMultiple("git", "diff", "--name-status", $"{defaultBranch}...HEAD", "--", ".");
		var localChanges = CaptureMultiple("git", "status", "--porcelain");
		var stashResult = ExecInSilent([], "git", "stash", "push", "--", ".");

		var localUnstagedChanges = Array.Empty<string>();
		if (stashResult.ExitCode == 0 && stashResult.ConsoleOut.Count > 0 && !stashResult.ConsoleOut[0].Line.StartsWith("No local changes to save"))
		{
			localUnstagedChanges = CaptureMultiple("git", "stash", "show", "--name-status", "-u");
			_ = ExecInSilent([], "git", "stash", "pop");
		}

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

	private static IEnumerable<FileChange> GetCommitChanges(string[] changes)
	{
		foreach (var change in changes)
		{
			var parts = change.AsSpan().TrimStart();
			if (parts.Length < 2)
				continue;

			var changeType = parts[0] switch
			{
				'A' => FileChangeType.Added,
				'M' => FileChangeType.Modified,
				'D' => FileChangeType.Deleted,
				'R' => FileChangeType.Renamed,
				_ => FileChangeType.Other
			};

			yield return new FileChange(change.Split('\t')[1], changeType);
		}
	}

	private static IEnumerable<FileChange> GetLocalChanges(string[] changes)
	{
		foreach (var change in changes)
		{
			var changeStatusCode = change.AsSpan();
			if (changeStatusCode.Length < 2)
				continue;

			var changeType = (changeStatusCode[0], changeStatusCode[1]) switch
			{
				('R', _) or (_, 'R') => FileChangeType.Renamed,
				('D', _) or (_, 'D') when changeStatusCode[0] != 'A' => FileChangeType.Deleted,
				('?', '?') => FileChangeType.Untracked,
				('A', _) or (_, 'A') => FileChangeType.Added,
				('M', _) or (_, 'M') => FileChangeType.Modified,
				_ => FileChangeType.Other
			};

			var changeParts = change.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			yield return changeType switch
			{
				FileChangeType.Renamed => new RenamedFileChange(changeParts[1], changeParts[3], changeType),
				_ => new FileChange(changeParts[1], changeType)
			};
		}
	}
}
