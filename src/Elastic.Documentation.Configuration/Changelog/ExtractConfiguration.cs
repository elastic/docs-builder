// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Configuration for extraction of release notes and issues from PR descriptions.
/// </summary>
public record ExtractConfiguration
{
	/// <summary>
	/// Whether to extract release notes from PR descriptions by default.
	/// Defaults to true. Can be overridden by CLI --no-extract-release-notes.
	/// </summary>
	public bool ReleaseNotes { get; init; } = true;

	/// <summary>
	/// Whether to extract linked issues from PR body by default.
	/// Defaults to true. Looks for patterns like "Fixes #123", "Closes #456", etc.
	/// </summary>
	public bool Issues { get; init; } = true;
}
