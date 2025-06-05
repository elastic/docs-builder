// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.ApiExplorer.Navigation;


public class ApiGroupNavigationItem(int depth, ApiNavigationGroup group) : IGroupNavigationItem
{
	public INavigationGroup NavigationRoot { get; } = group;
	public string Id { get; } = group.Id;
	public INavigationItem? Parent { get; set; } = group.Parent;
	public int Depth { get; } = depth;
	public IPageInformation? Current { get; } = group.Current;
	public IPageInformation? Index { get; }
	public IReadOnlyCollection<INavigationItem> NavigationItems => group.NavigationItems;
	public INavigationGroup Group { get; } = group;
}

public class ApiNavigationGroup : INavigationGroup
{
	public INavigationGroup NavigationRoot { get; }
	public string Id { get; }
	public INavigationItem? Parent { get; set; }
	public int Depth { get; }
	public IPageInformation? Current { get; }
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }
	public string? IndexFileName { get; }
	public IGroupNavigationItem GroupNavigationItem { get; set; }

	public ApiNavigationGroup()
	{
		NavigationRoot = this;
		Depth = 0;
		Id = ShortId.Create("");
		Current = null;
		NavigationItems = [];
		IndexFileName = null;
		GroupNavigationItem = new ApiGroupNavigationItem(Depth, this);
	}
}
