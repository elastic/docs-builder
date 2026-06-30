// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Markdown.Myst.Directives.Changelog;

/// <summary>
/// Directive-level rendering options sourced once from <see cref="ChangelogBlock"/>. These values are
/// invariant across every bundle in a single <c>{changelog}</c> render, so they are gathered into one
/// object and threaded through the renderer instead of being passed as individual parameters.
/// </summary>
internal sealed record ChangelogRenderOptions
{
	public required bool Subsections { get; init; }
	public required bool DropdownsEnabled { get; init; }
	public required bool ReleaseDatesEnabled { get; init; }
	public required ChangelogTypeFilter TypeFilter { get; init; }
	public required ChangelogLinkVisibility LinkVisibility { get; init; }
	public required ChangelogDescriptionVisibility DescriptionVisibility { get; init; }
	public required HashSet<string> PrivateRepositories { get; init; }
	public required HashSet<string> HideFeatures { get; init; }
	public PublishBlocker? PublishBlocker { get; init; }
}
