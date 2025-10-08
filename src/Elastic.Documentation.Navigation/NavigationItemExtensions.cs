// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation.Isolated;

namespace Elastic.Documentation.Navigation;

public record NotFoundModel(string NavigationTitle) : IDocumentationFile;

public class NotFoundLeafNavigationItem<TModel>(TModel model, INodeNavigationItem<INavigationModel, INavigationItem> parent
)
	: ILeafNavigationItem<TModel>
	where TModel : IDocumentationFile
{
	/// <inheritdoc />
	public string Url => string.Empty;

	/// <inheritdoc />
	public string NavigationTitle => string.Empty;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = parent.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden { get; }

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink { get; }

	/// <inheritdoc />
	public TModel Model { get; } = model;
}

public static class NavigationItemExtensions
{
	public static ILeafNavigationItem<TModel> FindIndex<TModel>(this INodeNavigationItem<INavigationModel, INavigationItem> node, TModel fallback)
		where TModel : IDocumentationFile
	{
		var leaf = node.NavigationItems.OfType<ILeafNavigationItem<TModel>>().FirstOrDefault();
		if (leaf is not null)
			return leaf;

		var nodes = node.NavigationItems.OfType<INodeNavigationItem<TModel, INavigationItem>>().ToList();
		if (nodes.Count == 0)
			return new NotFoundLeafNavigationItem<TModel>(fallback, node);

		return nodes.First().Index;

	}
}
