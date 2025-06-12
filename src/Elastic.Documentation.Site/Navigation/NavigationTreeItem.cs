// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

public class NavigationTreeItem
{
	public required int Level { get; init; }
	//public required MarkdownFile CurrentDocument { get; init; }
	public required INodeNavigationItem<INavigationModel, INavigationItem> SubTree { get; init; }
	public required bool IsPrimaryNavEnabled { get; init; }
	public required bool IsGlobalAssemblyBuild { get; init; }
	public required string RootNavigationId { get; set; }
}
