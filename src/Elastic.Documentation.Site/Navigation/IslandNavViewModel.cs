// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Site.Navigation;

public class IslandNavViewModel
{
	public required string BackLinkUrl { get; init; }
	public required string BackLinkTitle { get; init; }
	public required string ListingRootUrl { get; init; }
	public required string ListingRootTitle { get; init; }
	public required IReadOnlyList<IslandNavGroup> Groups { get; init; }
	public ListingVisual Visual { get; init; }
}

public record IslandNavGroup(string Title, string Url, IReadOnlyList<IslandNavPage> Pages);
public record IslandNavPage(string Title, string Url);
