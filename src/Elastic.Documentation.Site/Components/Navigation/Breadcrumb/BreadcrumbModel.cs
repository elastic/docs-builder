// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Navigation.Breadcrumb;

public sealed record BreadcrumbItem(string Label, string? Href = null);

public sealed record BreadcrumbModel
{
	/// <summary>Ancestors first. The last item is the current page and renders without a link.</summary>
	public required IReadOnlyList<BreadcrumbItem> Items { get; init; }
}
