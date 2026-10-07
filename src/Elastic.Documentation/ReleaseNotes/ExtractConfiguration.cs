// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Configuration for extraction of release notes and issues from PR descriptions.
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> <c>ChangelogConfigurationLoader</c> in <c>Elastic.Documentation.Configuration</c> (<c>Load</c>, <c>LoadRequired</c>, <c>LoadForProfileMode</c>), reading <c>changelog.yml</c> (the <c>extract</c> block).</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new ExtractConfiguration { ... }</c>) and <c>with</c> for variations.</para>
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
