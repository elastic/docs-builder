// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer;

public class ApiIndexLeafNavigation<TModel>(
	TModel model, string url, string navigationTitle,
	IRootNavigationItem<INavigationModel, INavigationItem> rootNavigation,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent = null
) : ILeafNavigationItem<TModel>
	where TModel : IApiModel
{
	/// <inheritdoc />
	public string Url { get; } = url;

	/// <inheritdoc />
	public string NavigationTitle { get; } = navigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = rootNavigation;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden { get; }

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public TModel Model { get; } = model;
}
