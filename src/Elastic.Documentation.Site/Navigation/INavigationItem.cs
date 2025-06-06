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
	string Url { get; }
	string NavigationTitle { get; }

	//TODO the setter smells
	INodeNavigationItem<IPageInformation, INavigationItem>? Parent { get; set; }
}

public interface ILeafNavigationItem<out TModel> : INavigationItem
	where TModel : IPageInformation
{
	TModel Model { get; }
}

public interface INodeNavigationItem<out TIndex, out TChildNavigation>
	: INavigationItem
	where TIndex : IPageInformation
	where TChildNavigation : INavigationItem
{
	int Depth { get; }
	string Id { get; }
	TIndex Index { get; }
	IReadOnlyCollection<TChildNavigation> NavigationItems { get; }
}

public interface IPageInformation : INavigationScope;

