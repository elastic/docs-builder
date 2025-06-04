// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

public interface INavigationScope
{
	INavigationGroup NavigationRoot { get; }
}

public interface INavigationItem : INavigationScope
{
	string Id { get; }
	INavigationItem? Parent { get; set; }
	int Depth { get; }
	//TODO not nullable
	IPageInformation? Current { get; }
}

//TODO the difference between this and INavigationGroup smells
public interface IGroupNavigationItem : INavigationItem
{
	IPageInformation? Index { get; }
	IReadOnlyCollection<INavigationItem> NavigationItems { get; }
	INavigationGroup Group { get; }
}




public interface INavigationGroup : INavigationItem
{
	IReadOnlyCollection<INavigationItem> NavigationItems { get; }
	string? IndexFileName { get; }

	IGroupNavigationItem GroupNavigationItem { get; }
}


public interface IPageInformation : INavigationScope
{
	string Url { get; }
	string NavigationTitle { get; }

	//TODO investigate if this is needed, only used by breadcrumbs to deduplicate
	string CrossLink { get; }
}

