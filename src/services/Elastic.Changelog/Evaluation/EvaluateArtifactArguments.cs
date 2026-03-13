// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Evaluation;

/// <summary>Arguments for the changelog evaluate-artifact command (commit workflow).</summary>
public record EvaluateArtifactArguments
{
	public required string MetadataPath { get; init; }
	public bool CommentOnly { get; init; }
	public required string Owner { get; init; }
	public required string Repo { get; init; }
}
