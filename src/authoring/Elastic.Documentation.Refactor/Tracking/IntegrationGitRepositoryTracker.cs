// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Extensions;
using static System.StringSplitOptions;

namespace Elastic.Documentation.Refactor.Tracking;

public class IntegrationGitRepositoryTracker(IDirectoryInfo gitRoot, IDirectoryInfo documentationSourceDirectory) : IRepositoryTracker
{
	public IReadOnlyCollection<GitChange> GetChangedFiles()
	{
		return GetChanges().ToArray();

		IEnumerable<GitChange> GetChanges()
		{
			var deletedFiles = Environment.GetEnvironmentVariable("DELETED_FILES") ?? string.Empty;
			if (!string.IsNullOrEmpty(deletedFiles))
			{
				foreach (var file in deletedFiles.Split(' ', RemoveEmptyEntries).Where(IsUnderDocset))
					yield return new GitChange(file, GitChangeType.Deleted);
			}

			var addedFiles = Environment.GetEnvironmentVariable("ADDED_FILES");
			if (!string.IsNullOrEmpty(addedFiles))
			{
				foreach (var file in addedFiles.Split(' ', RemoveEmptyEntries).Where(IsUnderDocset))
					yield return new GitChange(file, GitChangeType.Added);
			}

			var modifiedFiles = Environment.GetEnvironmentVariable("MODIFIED_FILES");
			if (!string.IsNullOrEmpty(modifiedFiles))
			{
				foreach (var file in modifiedFiles.Split(' ', RemoveEmptyEntries).Where(IsUnderDocset))
					yield return new GitChange(file, GitChangeType.Modified);
			}

			var renamedFiles = Environment.GetEnvironmentVariable("RENAMED_FILES");
			if (!string.IsNullOrEmpty(renamedFiles))
			{
				foreach (var pair in renamedFiles.Split(' ', RemoveEmptyEntries))
				{
					var parts = pair.Split(':');
					if (parts.Length != 2 || !IsUnderDocset(parts[0]))
						continue;
					yield return new RenamedGitChange(parts[0], parts[1], GitChangeType.Renamed);
				}
			}
		}
	}

	private bool IsUnderDocset(string relativePath)
	{
		var fs = gitRoot.FileSystem;
		var normalized = relativePath.Replace('/', fs.Path.DirectorySeparatorChar);
		var file = fs.FileInfo.New(fs.Path.Join(gitRoot.FullName, normalized));
		return file.IsSubPathOf(documentationSourceDirectory);
	}
}
