// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Changelog;

/// <summary>
/// Controls changelog entry description (body text) rendering for the {changelog} directive.
/// Mirrors the structure of <see cref="ChangelogLinkVisibility"/> while using opposite privacy defaults for <see cref="Auto"/>.
/// </summary>
public enum ChangelogDescriptionVisibility
{
	/// <summary>
	/// Hide record descriptions when the bundle has only public constituent repos (per assembler.yml);
	/// show when any constituent is private. With no private repos configured, hides descriptions everywhere.
	/// </summary>
	Auto,

	/// <summary>
	/// Always render record descriptions when present in source YAML.
	/// </summary>
	KeepDescriptions,

	/// <summary>
	/// Never render record descriptions (including dropdown authoring placeholders).
	/// </summary>
	HideDescriptions
}
