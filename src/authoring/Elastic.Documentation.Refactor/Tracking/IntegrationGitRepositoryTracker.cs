// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using static System.StringComparison;
using static System.StringSplitOptions;

namespace Elastic.Documentation.Refactor.Tracking;

public class IntegrationGitRepositoryTracker(string lookupPath) : IRepositoryTracker
{
	private string LookupPath { get; } = $"{lookupPath.Trim(['/', '\\'])}/";
	public IReadOnlyCollection<GitChange> GetChangedFiles()
	{
		return GetChanges().ToArray();

		IEnumerable<GitChange> GetChanges()
		{
			var deletedFiles = Environment.GetEnvironmentVariable("DELETED_FILES") ?? string.Empty;
			if (!string.IsNullOrEmpty(deletedFiles))
			{
				foreach (var file in deletedFiles.Split(' ', RemoveEmptyEntries).Where(f => f.StartsWith(LookupPath, OrdinalIgnoreCase)))
					yield return new GitChange(file, GitChangeType.Deleted);
			}

			var addedFiles = Environment.GetEnvironmentVariable("ADDED_FILES");
			if (!string.IsNullOrEmpty(addedFiles))
			{
				foreach (var file in addedFiles.Split(' ', RemoveEmptyEntries).Where(f => f.StartsWith(LookupPath, OrdinalIgnoreCase)))
					yield return new GitChange(file, GitChangeType.Added);
			}

			var modifiedFiles = Environment.GetEnvironmentVariable("MODIFIED_FILES");
			if (!string.IsNullOrEmpty(modifiedFiles))
			{
				foreach (var file in modifiedFiles.Split(' ', RemoveEmptyEntries).Where(f => f.StartsWith(LookupPath, OrdinalIgnoreCase)))
					yield return new GitChange(file, GitChangeType.Modified);
			}

			var renamedFiles = Environment.GetEnvironmentVariable("RENAMED_FILES");
			if (!string.IsNullOrEmpty(renamedFiles))
			{
				foreach (var pair in renamedFiles.Split(' ', RemoveEmptyEntries).Where(f => f.StartsWith(LookupPath, OrdinalIgnoreCase)))
				{
					var parts = pair.Split(':');
					if (parts.Length == 2)
						yield return new RenamedGitChange(parts[0], parts[1], GitChangeType.Renamed);
				}
			}

		}
	}
}
