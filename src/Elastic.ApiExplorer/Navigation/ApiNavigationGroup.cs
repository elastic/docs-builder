// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.ApiExplorer.Navigation;


public class ApiGroupNavigationItem : IGroupNavigationItem
{
	public ApiGroupNavigationItem(int depth, IGroupNavigationItem? parent, IGroupNavigationItem? root)
	{
		Parent = parent;
		Depth = depth;
		//Current = group.Current;
		NavigationRoot = root ?? this;
		Id = NavigationRoot.Id;
	}

	public IGroupNavigationItem NavigationRoot { get; }
	public string Id { get; }
	public IGroupNavigationItem? Parent { get; set; }
	public int Depth { get; }
	public IPageInformation? Current { get; }
	public IPageInformation? Index { get; set; }
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; set; } = [];
}

