// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

public interface INavigationScope<out TRoot>
	where TRoot : IRootNavigationItem<IPageInformation, INavigationItem>
{
	TRoot NavigationRoot { get; }
}

public interface INavigationItem
{
	//TODO the setter smells
	IRootNavigationItem<IPageInformation, INavigationItem>? Parent { get; set; }
}

public interface ILeafNavigationItem<out TCurrent, out TRoot> : INavigationItem, INavigationScope<TRoot>
	where TCurrent : IPageInformation
	where TRoot : IRootNavigationItem<IPageInformation, INavigationItem>
{
	TCurrent Current { get; }
}

public interface IRootNavigationItem<out TIndex, out TNavigation>
	: INavigationItem
	where TIndex : IPageInformation
	where TNavigation : INavigationItem
{
	int Depth { get; }
	string Id { get; }
	TIndex Index { get; }
	IReadOnlyCollection<TNavigation> NavigationItems { get; }
}

public interface INodeNavigationItem<out TIndex, out TNavigation, out TRoot>
	: IRootNavigationItem<TIndex, TNavigation>, INavigationScope<TRoot>
	where TIndex : IPageInformation
	where TNavigation : INavigationItem
	where TRoot : IRootNavigationItem<IPageInformation, INavigationItem>;

public interface IPageInformation : INavigationScope<IRootNavigationItem<IPageInformation, INavigationItem>>
{
	string Url { get; }
	string NavigationTitle { get; }

	//TODO investigate if this is needed, only used by breadcrumbs to deduplicate
	string CrossLink { get; }
}

