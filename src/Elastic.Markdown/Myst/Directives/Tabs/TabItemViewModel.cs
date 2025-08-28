// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Components;

namespace Elastic.Markdown.Myst.Directives.Tabs;

public class TabItemViewModel : DirectiveViewModel
{
	public required int Index { get; init; }
	public required int TabSetIndex { get; init; }
	public required string Title { get; init; }
	public required string? SyncKey { get; init; }
	public required string? TabSetGroupKey { get; init; }
	public required ApplicableToViewModel? ApplicableToViewModel { get; init; }
}
