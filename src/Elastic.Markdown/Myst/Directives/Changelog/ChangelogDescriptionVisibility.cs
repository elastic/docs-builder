// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Changelog;

/// <summary>
/// Controls changelog entry description (body text) rendering for the {changelog} directive.
/// Combinable flags: at most one base token (<see cref="Auto"/>, <see cref="KeepDescriptions"/>, or
/// <see cref="HideDescriptions"/>) plus optional overlays (<see cref="KeepHighlightDescriptions"/>,
/// <see cref="KeepFeatureDescriptions"/>).
/// </summary>
[Flags]
public enum ChangelogDescriptionVisibility
{
	None = 0,

	/// <summary>
	/// Hide record descriptions when the bundle has only public constituent repos (per assembler.yml);
	/// show when any constituent is private. With no private repos configured, hides descriptions everywhere.
	/// </summary>
	Auto = 1,

	/// <summary>
	/// Always render record descriptions when present in source YAML.
	/// </summary>
	KeepDescriptions = 1 << 1,

	/// <summary>
	/// Never render record descriptions (including dropdown authoring placeholders), except where an overlay applies.
	/// </summary>
	HideDescriptions = 1 << 2,

	/// <summary>
	/// Always render record descriptions in the Highlights section. Combine with other tokens to control remaining sections.
	/// When used alone, descriptions are hidden everywhere except Highlights. When <c>:highlights:</c> is omitted, the
	/// Highlights overlay has no visible effect.
	/// </summary>
	KeepHighlightDescriptions = 1 << 3,

	/// <summary>
	/// Always render record descriptions in the Features section. Combine with other tokens to control remaining sections.
	/// When used alone, descriptions are hidden everywhere except Features.
	/// </summary>
	KeepFeatureDescriptions = 1 << 4
}
