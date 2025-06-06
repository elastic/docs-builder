// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

public interface INavigationScope
{
	INodeNavigationItem NavigationRoot { get; }
}

public interface INavigationItem : INavigationScope
{
	INodeNavigationItem? Parent { get; set; }
}

public interface ILeafNavigationItem : INavigationItem
{
	IPageInformation Current { get; }
}

// TODO make generic TINdex and TNavigationItem
public interface INodeNavigationItem : INavigationItem
{
	int Depth { get; }
	string Id { get; }
	IPageInformation Index { get; }
	IReadOnlyCollection<INavigationItem> NavigationItems { get; }
}

public interface IPageInformation : INavigationScope
{
	string Url { get; }
	string NavigationTitle { get; }

	//TODO investigate if this is needed, only used by breadcrumbs to deduplicate
	string CrossLink { get; }
}

