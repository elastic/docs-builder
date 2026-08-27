// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Exporters.GitDiff;

internal enum SourceFileChangeType
{
	Added,
	Modified,
	Deleted,
	Renamed
}

internal record SourceFileChange(
	string Path,
	SourceFileChangeType ChangeType,
	string? NewPath = null
);

internal record ChangedFileSourceResult(
	string Base,
	IReadOnlyList<SourceFileChange> Changes
);
