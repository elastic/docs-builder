// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Isolated;

public record VirtualFileNavigationArgs(
	string RelativePath,
	bool Hidden,
	int NavigationIndex,
	int Depth,
	INodeNavigationItem<INavigationModel, INavigationItem>? Parent,
	IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot,
	IPathPrefixProvider PrefixProvider,
	IReadOnlyCollection<INavigationItem> NavigationItems
);

/// Represents a file navigation item that defines children which are not part of the file tree.
public class VirtualFileNavigation<TModel>(TModel model, IFileInfo fileInfo, VirtualFileNavigationArgs args)
	: INodeNavigationItem<TModel, INavigationItem>
	where TModel : IDocumentationFile
{
	/// <inheritdoc />
	public string Url => Index.Url;

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; init; } = args.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = args.Parent;

	/// <inheritdoc />
	public bool Hidden { get; init; } = args.Hidden;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink { get; }

	/// <inheritdoc />
	public int Depth { get; init; } = args.Depth;

	/// <inheritdoc />
	public string Id { get; } = ShortId.Create(args.RelativePath);

	/// <inheritdoc />
	public ILeafNavigationItem<TModel> Index { get; init; } =
		new FileNavigationLeaf<TModel>(model, fileInfo, new FileNavigationArgs(args.RelativePath, args.Hidden, args.NavigationIndex, args.Parent, args.NavigationRoot, args.PrefixProvider));

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; init; } = args.NavigationItems;
}
