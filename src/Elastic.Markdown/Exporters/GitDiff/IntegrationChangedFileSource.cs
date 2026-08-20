// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;

namespace Elastic.Markdown.Exporters.GitDiff;

internal static class IntegrationChangedFileSource
{
	public static bool HasFileList(IEnvironmentVariables environment) =>
		HasValue(environment, "ADDED_FILES")
		|| HasValue(environment, "MODIFIED_FILES")
		|| HasValue(environment, "DELETED_FILES")
		|| HasValue(environment, "RENAMED_FILES");

	public static ChangedFileSourceResult GetChanges(string docsetPrefix, IEnvironmentVariables environment, string diffBase)
	{
		var changes = new List<SourceFileChange>();

		AddChanges(environment.GetEnvironmentVariable("DELETED_FILES"), SourceFileChangeType.Deleted, docsetPrefix, changes);
		AddChanges(environment.GetEnvironmentVariable("ADDED_FILES"), SourceFileChangeType.Added, docsetPrefix, changes);
		AddChanges(environment.GetEnvironmentVariable("MODIFIED_FILES"), SourceFileChangeType.Modified, docsetPrefix, changes);
		AddRenames(environment.GetEnvironmentVariable("RENAMED_FILES"), docsetPrefix, changes);

		return new ChangedFileSourceResult(diffBase, changes);
	}

	private static bool HasValue(IEnvironmentVariables environment, string name) =>
		!string.IsNullOrWhiteSpace(environment.GetEnvironmentVariable(name));

	private static void AddChanges(string? raw, SourceFileChangeType changeType, string docsetPrefix, List<SourceFileChange> changes)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return;

		foreach (var file in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (GitDiffPathNormalization.TryToDocsetRelative(file, docsetPrefix, out _))
				changes.Add(new SourceFileChange(file, changeType));
		}
	}

	private static void AddRenames(string? raw, string docsetPrefix, List<SourceFileChange> changes)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return;

		foreach (var pair in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			var parts = pair.Split(':', 2);
			if (parts.Length != 2)
				continue;

			if (!GitDiffPathNormalization.TryToDocsetRelative(parts[0], docsetPrefix, out _)
				&& !GitDiffPathNormalization.TryToDocsetRelative(parts[1], docsetPrefix, out _))
				continue;

			changes.Add(new SourceFileChange(parts[0], SourceFileChangeType.Renamed, parts[1]));
		}
	}
}
