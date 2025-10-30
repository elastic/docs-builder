// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.Documentation.Navigation.Assembler;

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
	public TModel Model { get; } = model;

	private string? _homeProviderCache;
	private string? _urlCache;

	/// <inheritdoc />
	public string Url
	{
		get
		{
			if (_homeProviderCache is not null && _homeProviderCache == args.HomeAccessor.HomeProvider.Id && _urlCache is not null)
				return _urlCache;


			_homeProviderCache = args.HomeAccessor.HomeProvider.Id;

			_urlCache = DetermineUrl();
			return _urlCache;

			string DetermineUrl()
			{
				var rootUrl = args.HomeAccessor.HomeProvider.PathPrefix.TrimEnd('/');
				var relativeToContainer = args.HomeAccessor.HomeProvider.NavigationRoot.Parent is SiteNavigation;

				// Remove extension while preserving the directory path
				var relativePath = relativeToContainer ? args.RelativePathToTableOfContents : args.RelativePathToDocumentationSet;
				var path = relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
					? relativePath[..^3]  // Remove last 3 characters (.md)
					: relativePath;

				// If a path ends with /index or is just index, omit it from the URL
				if (path.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
					path = path[..^6]; // Remove "/index"
				else if (path.Equals("index", StringComparison.OrdinalIgnoreCase))
					return string.IsNullOrEmpty(rootUrl) ? "/" : $"{rootUrl}/";

				if (string.IsNullOrEmpty(path))
					return string.IsNullOrEmpty(rootUrl) ? "/" : $"{rootUrl}/";

				return $"{rootUrl}/{path.TrimEnd('/')}/";
			}
		}
	}

	/// <inheritdoc />
	public bool Hidden { get; } = args.Hidden;

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
