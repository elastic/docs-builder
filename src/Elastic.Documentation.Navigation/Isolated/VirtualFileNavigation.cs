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
	: INodeNavigationItem<TModel, INavigationItem> where TModel : IDocumentationFile
{
	public IFileInfo FileInfo { get; } = fileInfo;

	/// <inheritdoc />
	public string Url
	{
		get
		{
			var rootUrl = args.PrefixProvider.PathPrefix.TrimEnd('/');
			// Remove extension while preserving directory path
			var relativePath = args.RelativePath;
			var path = relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
				? relativePath[..^3]  // Remove last 3 characters (.md)
				: relativePath;

			// If path ends with /index or is just index, omit it from the URL
			if (path.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
				path = path[..^6]; // Remove "/index"
			else if (path.Equals("index", StringComparison.OrdinalIgnoreCase))
				return string.IsNullOrEmpty(rootUrl) ? "/" : rootUrl;

			if (string.IsNullOrEmpty(path))
				return string.IsNullOrEmpty(rootUrl) ? "/" : rootUrl;

			return $"{rootUrl}/{path}";
		}
	}

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
	public TModel Index { get; init; } = model;

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; init; } = args.NavigationItems;
}
