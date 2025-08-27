// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Documentation.Builder.Tracking;

public enum GitChangeType
{
	Added,
	Modified,
	Deleted,
	Renamed,
	Untracked,
	Other
}

public record GitChange(string FilePath, GitChangeType ChangeType);
public record RenamedGitChange(string OldFilePath, string NewFilePath, GitChangeType ChangeType) : GitChange(OldFilePath, ChangeType);

public interface IRepositoryTracker
{
	IReadOnlyCollection<GitChange> GetChangedFiles();
}
