// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Documentation.Builder.Tracking;

public class IntegrationGitRepositoryTracker(string lookupPath) : IRepositoryTracker
{
	private string LookupPath { get; } = $"{lookupPath}/";
	public IEnumerable<GitChange> GetChangedFiles()
	{
		var deletedFiles = Environment.GetEnvironmentVariable("DELETED_FILES") ?? string.Empty;
		if (!string.IsNullOrEmpty(deletedFiles))
		{
			foreach (var file in deletedFiles.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(f => f.StartsWith(LookupPath)))
				yield return new GitChange(file, GitChangeType.Deleted);
		}

		var addedFiles = Environment.GetEnvironmentVariable("ADDED_FILES");
		if (!string.IsNullOrEmpty(addedFiles))
		{
			foreach (var file in addedFiles.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(f => f.StartsWith(LookupPath)))
				yield return new GitChange(file, GitChangeType.Added);
		}

		var modifiedFiles = Environment.GetEnvironmentVariable("MODIFIED_FILES");
		if (!string.IsNullOrEmpty(modifiedFiles))
		{
			foreach (var file in modifiedFiles.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(f => f.StartsWith(LookupPath)))
				yield return new GitChange(file, GitChangeType.Modified);
		}

		var renamedFiles = Environment.GetEnvironmentVariable("RENAMED_FILES");
		if (!string.IsNullOrEmpty(renamedFiles))
		{
			foreach (var file in renamedFiles.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(f => f.StartsWith(LookupPath)))
				yield return new RenamedGitChange(string.Empty, file, GitChangeType.Renamed);
		}
	}
}
