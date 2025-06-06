// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

public interface INavigationModel;

public interface INavigationItem
{
	string Url { get; }
	string NavigationTitle { get; }
	INodeNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	//TODO the setter smells
	INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
}

public interface ILeafNavigationItem<out TModel> : INavigationItem
	where TModel : INavigationModel
{
	TModel Model { get; }
}

public interface INodeNavigationItem<out TIndex, out TChildNavigation>
	: INavigationItem
	where TIndex : INavigationModel
	where TChildNavigation : INavigationItem
{
	int Depth { get; }
	string Id { get; }
	TIndex Index { get; }
	IReadOnlyCollection<TChildNavigation> NavigationItems { get; }
}

