// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

public interface INavigationScope
{
	INodeNavigationItem<IPageInformation, INavigationItem> NavigationRoot { get; }
}

public interface INavigationItem : INavigationScope
{
	//TODO the setter smells
	INodeNavigationItem<IPageInformation, INavigationItem>? Parent { get; set; }
}

public interface ILeafNavigationItem<out TCurrent> : INavigationItem
	where TCurrent : IPageInformation
{
	TCurrent Current { get; }
}

public interface INodeNavigationItem<out TIndex, out TNavigation>
	: INavigationItem
	where TIndex : IPageInformation
	where TNavigation : INavigationItem
{
	int Depth { get; }
	string Id { get; }
	TIndex Index { get; }
	IReadOnlyCollection<TNavigation> NavigationItems { get; }
}

public interface IPageInformation : INavigationScope
{
	string Url { get; }
	string NavigationTitle { get; }

	//TODO investigate if this is needed, only used by breadcrumbs to deduplicate
	string CrossLink { get; }
}

