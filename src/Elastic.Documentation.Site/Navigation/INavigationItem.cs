// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

public interface INavigationScope
{
	IGroupNavigationItem NavigationRoot { get; }
}

public interface INavigationItem : INavigationScope
{
	string Id { get; }
	IGroupNavigationItem? Parent { get; set; }
	int Depth { get; }
	//TODO not nullable
	IPageInformation? Current { get; }
}

public interface IGroupNavigationItem : INavigationItem
{
	IPageInformation? Index { get; }
	IReadOnlyCollection<INavigationItem> NavigationItems { get; }
}

public interface IPageInformation : INavigationScope
{
	string Url { get; }
	string NavigationTitle { get; }

	//TODO investigate if this is needed, only used by breadcrumbs to deduplicate
	string CrossLink { get; }
}

