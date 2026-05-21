// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;

namespace Elastic.Documentation.Site.Navigation;

public class NavigationTreeItem
{
	public required int Level { get; init; }
	//public required MarkdownFile CurrentDocument { get; init; }
	public required INodeNavigationItem<INavigationModel, INavigationItem> SubTree { get; init; }
	public required bool IsPrimaryNavEnabled { get; init; }
	public required bool IsGlobalAssemblyBuild { get; init; }
	public required string RootNavigationId { get; set; }
	public required IHtmxAttributeProvider Htmx { get; init; }
	public string NavV2LocationPath { get; init; } = "";

	/// <summary>True when this subtree renders inside an island (entry-point + descendants).
	/// Links here must swap the full page chrome so clicking from a parent section's sidebar
	/// switches into the island's focused sidebar.</summary>
	public bool IsIslandSubtree { get; init; }
}
