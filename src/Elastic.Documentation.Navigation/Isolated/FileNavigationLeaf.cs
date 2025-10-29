// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;

namespace Elastic.Documentation.Navigation.Isolated;

public record FileNavigationArgs(
	string RelativePathToDocumentationSet,
	string RelativePathToTableOfContents,
	bool Hidden,
	int NavigationIndex,
	INodeNavigationItem<INavigationModel, INavigationItem>? Parent,
	INavigationHomeAccessor HomeAccessor
);

[DebuggerDisplay("{Url}")]
public class FileNavigationLeaf<TModel>(TModel model, IFileInfo fileInfo, FileNavigationArgs args) : ILeafNavigationItem<TModel>
	where TModel : IDocumentationFile
{
	public IFileInfo FileInfo { get; } = fileInfo;

	/// <inheritdoc />
	public TModel Model { get; init; } = model;

	/// <inheritdoc />
	public string Url
	{
		get
		{
			var rootUrl = args.HomeAccessor.HomeProvider.PathPrefix.TrimEnd('/');
			// Remove extension while preserving the directory path
			var relativePath = args.RelativePathToDocumentationSet;
			var path = relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
				? relativePath[..^3]  // Remove last 3 characters (.md)
				: relativePath;

			// If a path ends with /index or is just index, omit it from the URL
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
	public bool Hidden { get; init; } = args.Hidden;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => args.HomeAccessor.HomeProvider.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = args.Parent;

	/// <inheritdoc />
	public string NavigationTitle => Model.NavigationTitle;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink { get; }
}
