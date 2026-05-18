// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// Lightweight data carrier for a navigation section, used by the rendering layer
/// to drive the secondary nav bar tabs and resolve which sidebar to show.
/// </summary>
public record NavigationSection(
	string Id,
	string Label,
	string Url,
	bool Isolated,
	IReadOnlyList<INavigationItem> NavigationItems
);

/// <summary>
/// A nav island nested within a parent section. When a page belongs to an island,
/// the sidebar shows only the island's tree with a back arrow to the parent section.
/// </summary>
public record NavigationIsland(
	string Id,
	string Label,
	string Url,
	string SourceTocRootId,
	NavigationSection ParentSection,
	IReadOnlyList<INavigationItem> NavigationItems
);
