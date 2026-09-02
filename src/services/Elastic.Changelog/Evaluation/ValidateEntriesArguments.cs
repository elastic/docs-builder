// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Evaluation;

/// <summary>Arguments for the <c>changelog validate-entries</c> command.</summary>
public record ValidateEntriesArguments
{
	public required string ConfigFile { get; init; }
	public required string Owner { get; init; }
	public required string Repo { get; init; }
	public required int PrNumber { get; init; }
	public required string[] PrLabels { get; init; }
	/// <summary>Explicit file list; bypasses API discovery when non-null. Null means discover via GitHub API.</summary>
	public string[]? Files { get; init; }

	// PR context — written to decision metadata when running on CI.
	public string HeadRef { get; init; } = "";
	public string HeadSha { get; init; } = "";
	public bool IsFork { get; init; }
	public bool CanCommit { get; init; }
	public bool MaintainerCanModify { get; init; }
	public string? HeadRepo { get; init; }
}
