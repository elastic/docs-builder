// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

public class NavigationViewModel
{
	public required string Title { get; init; }
	public required string TitleUrl { get; init; }
	public required INodeNavigationItem<INavigationModel, INavigationItem> Tree { get; init; }
	public required bool IsPrimaryNavEnabled { get; init; }
	public required bool IsGlobalAssemblyBuild { get; init; }
	public required IEnumerable<INodeNavigationItem<INavigationModel, INavigationItem>> TopLevelItems { get; init; }

	/// controls whether to split the navigation tree automatically
	public required bool IsUsingNavigationDropdown { get; init; }

	// How many levels to render. -1 means all
	public required int MaxLevel { get; init; }
}
