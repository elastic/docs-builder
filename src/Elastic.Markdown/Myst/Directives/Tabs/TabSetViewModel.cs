// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Tabs;

/// <summary>One option in the dropdown rendering of a tab-set.</summary>
/// <param name="Title">Visible tab title, e.g. "Python".</param>
/// <param name="InputId">Id of the radio input the option activates.</param>
/// <param name="SyncKey">`:sync:` value, used to keep tab-sets in the same group aligned.</param>
public readonly record struct TabSetOption(string Title, string InputId, string? SyncKey);

public class TabSetViewModel : DirectiveViewModel
{
	public required bool RenderAsDropdown { get; init; }
	public required int TabSetIndex { get; init; }
	public required string? GroupKey { get; init; }
	public required IReadOnlyList<TabSetOption> Options { get; init; }
}
