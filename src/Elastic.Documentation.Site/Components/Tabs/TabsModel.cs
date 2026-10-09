// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.Tabs;

public sealed record TabSetModel
{
	/// <summary>The rendered tab items.</summary>
	public required IHtmlContent Items { get; init; }
}

public sealed record TabItemModel
{
	public required string Title { get; init; }
	public required IHtmlContent Body { get; init; }
	public required int SetIndex { get; init; }
	public required int Index { get; init; }

	/// <summary>Tabs with the same group and key stay in sync across the page and across page loads.</summary>
	public string? SyncKey { get; init; }

	public string? SyncGroup { get; init; }

	public string Id => $"tabs-item-{SetIndex}-{Index}";
}
