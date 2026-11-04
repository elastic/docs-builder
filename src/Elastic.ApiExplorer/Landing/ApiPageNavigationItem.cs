// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Landing;

/// <summary>The sidebar entry of one API page: the members every page entry shares. Each page type decides its URL.</summary>
public abstract class ApiPageNavigationItem<TModel>(
	string url,
	string navigationTitle,
	TModel model,
	IRootNavigationItem<INavigationModel, INavigationItem> root,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent
) : ILeafNavigationItem<TModel> where TModel : INavigationModel
{
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = root;
	public string Id { get; } = ShortId.Create(url);
	public TModel Model { get; } = model;
	public string Url { get; } = url;
	public bool Hidden { get; init; }
	public string NavigationTitle { get; } = navigationTitle;
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;
	public int NavigationIndex { get; set; }
}
