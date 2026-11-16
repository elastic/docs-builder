// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Configuration for extraction of release notes and issues from PR descriptions.
/// </summary>
/// <remarks>
/// Constructed through <c>ChangelogConfiguration</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
public record ExtractConfiguration
{
	/// <summary>
	/// Whether to extract release notes from PR descriptions by default.
	/// Defaults to true. Can be overridden by CLI --no-extract-release-notes.
	/// </summary>
	public bool ReleaseNotes { get; internal init; } = true;

	/// <summary>
	/// Whether to extract linked issues from PR body by default.
	/// Defaults to true. Looks for patterns like "Fixes #123", "Closes #456", etc.
	/// </summary>
	public bool Issues { get; internal init; } = true;

	/// <summary>
	/// Whether to strip square-bracket prefixes from PR titles by default.
	/// Defaults to false. When enabled, titles like "[ES|QL] Fix bug" become "Fix bug",
	/// and a single ASCII hyphen used as a team separator after the prefix is removed when it is followed by whitespace
	/// (for example "[Team] - Do something" becomes "Do something").
	/// Serialized changelog YAML uses quoted scalars when a title still starts with <c>-</c>, <c>*</c>, <c>+</c>, en dash, or em dash so the value is not parsed as a list marker.
	/// Can be overridden by CLI --strip-title-prefix flag.
	/// </summary>
	public bool StripTitlePrefix { get; internal init; }
}
