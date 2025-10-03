// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Isolated;

// A model for nodes in the navigation representing directories e.g., sets, toc's and folders.
public record DocumentationDirectory(string NavigationTitle) : IDocumentationFile;

public interface IDocumentationFileFactory<out TModel> where TModel : IDocumentationFile
{
	TModel? TryCreateDocumentationFile(IFileInfo path);
}

public static class DocumentationNavigationFactory
{
	public static ILeafNavigationItem<TModel> CreateFileNavigationLeaf<TModel>(TModel model, FileNavigationArgs args)
		where TModel : IDocumentationFile =>
		new FileNavigationLeaf<TModel>(model, args) { NavigationIndex = args.NavigationIndex };

	public static INodeNavigationItem<TModel, INavigationItem> CreateVirtualFileNavigation<TModel>(TModel model, VirtualFileNavigationArgs args)
		where TModel : IDocumentationFile =>
		new VirtualFileNavigation<TModel>(model, args) { NavigationIndex = args.NavigationIndex };
}

public class DocumentationSetNavigation :
	IRootNavigationItem<IDocumentationFile, INavigationItem>, INavigationPathPrefixProvider, IPathPrefixProvider
{
	private readonly IDocumentationFileFactory<IDocumentationFile> _factory;

	public DocumentationSetNavigation(
		DocumentationSetFile documentationSet,
		IDocumentationSetContext context,
		IDocumentationFileFactory<IDocumentationFile> factory,
		IRootNavigationItem<IDocumentationFile, INavigationItem>? parent = null,
		IRootNavigationItem<IDocumentationFile, INavigationItem>? root = null,
		IPathPrefixProvider? pathPrefixProvider = null
	)
	{
		_factory = factory;
		// Initialize root properties
		NavigationRoot = root ?? this;
		Parent = parent;
		Depth = 0;
		Hidden = false;
		IsCrossLink = false;
		PathPrefixProvider = pathPrefixProvider ?? this;
		_pathPrefix = pathPrefixProvider?.PathPrefix ?? string.Empty;
		Id = ShortId.Create(documentationSet.Project ?? "root");
		Index = new DocumentationDirectory(documentationSet.Project ?? "Documentation");
		IsUsingNavigationDropdown = documentationSet.Features.PrimaryNav ?? false;
		Git = context.Git;
		Identifier = new Uri($"{Git.RepositoryName}://");
		_ = _tableOfContentNodes.TryAdd(Identifier, this);

		// Convert TOC items to navigation items
		var items = new List<INavigationItem>();
		var index = 0;
		foreach (var tocItem in documentationSet.Toc)
		{
			var navItem = ConvertToNavigationItem(
				tocItem,
				index++,
				context,
				parent: null,
				root: NavigationRoot,
				prefixProvider: PathPrefixProvider,
				depth: Depth,
				parentPath: ""
			);

			if (navItem != null)
				items.Add(navItem);
		}

		NavigationItems = items;
	}

	private readonly string _pathPrefix;

	/// <summary>
	/// Gets the path prefix. When PathPrefixProvider is set to a different instance, returns that provider's prefix.
	/// Otherwise returns the prefix set during construction.
	/// </summary>
	public string PathPrefix => PathPrefixProvider == this ? _pathPrefix : PathPrefixProvider.PathPrefix;

	public IPathPrefixProvider PathPrefixProvider { get; set; }

	public GitCheckoutInformation Git { get; }

	private readonly Dictionary<Uri, INodeNavigationItem<INavigationModel, INavigationItem>> _tableOfContentNodes = [];
	public IReadOnlyDictionary<Uri, INodeNavigationItem<INavigationModel, INavigationItem>> TableOfContentNodes => _tableOfContentNodes;

	public Uri Identifier { get; }

	/// <inheritdoc />
	public string Url
	{
		get
		{
			var rootUrl = PathPrefixProvider.PathPrefix.TrimEnd('/');
			return string.IsNullOrEmpty(rootUrl) ? "/" : rootUrl;
		}
	}

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden { get; }

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink { get; }

	/// <inheritdoc />
	public int Depth { get; }

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public IDocumentationFile Index { get; }

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown { get; }

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	private INavigationItem? ConvertToNavigationItem(
		ITableOfContentsItem tocItem,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IPathPrefixProvider prefixProvider,
		int depth,
		string parentPath
	) =>
		tocItem switch
		{
			FileRef fileRef => CreateFileNavigation(fileRef, index, context, parent, root, prefixProvider, parentPath),
			CrossLinkRef crossLinkRef => CreateCrossLinkNavigation(crossLinkRef, index, parent, root),
			FolderRef folderRef => CreateFolderNavigation(folderRef, index, context, parent, root, prefixProvider, depth, parentPath),
			IsolatedTableOfContentsRef tocRef => CreateTocNavigation(tocRef, index, context, parent, root, prefixProvider, depth, parentPath),
			_ => null
		};

	private INavigationItem? CreateFileNavigation(
		FileRef fileRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IPathPrefixProvider prefixProvider,
		string parentPath
	)
	{
		// Combine parent path with file path
		var fullPath = string.IsNullOrEmpty(parentPath)
			? fileRef.RelativePath
			: $"{parentPath}/{fileRef.RelativePath}";

		// Create documentation file from factory
		var fs = context.ReadFileSystem;
		var fileInfo = fs.FileInfo.NewCombine(context.DocumentationSourceDirectory.FullName, parentPath, fileRef.RelativePath);
		var documentationFile = _factory.TryCreateDocumentationFile(fileInfo);
		if (documentationFile == null)
		{
			context.EmitError(context.ConfigurationPath, $"File navigation '{fileRef.RelativePath}' could not be created");
			return null;
		}

		var leafNavigationArgs = new FileNavigationArgs(fullPath, fileRef.Hidden, index, parent, root, prefixProvider);
		// Check if file has children
		if (fileRef.Children.Count <= 0)
			return DocumentationNavigationFactory.CreateFileNavigationLeaf(documentationFile, leafNavigationArgs);

		// No children - return a leaf
		// Validate: index files may not have children
		if (fileRef is IndexFileRef)
		{
			context.EmitError(context.ConfigurationPath, $"File navigation '{fileRef.RelativePath}' is an index file and may not have children");
			return null;
		}

		// Create temporary file navigation for children to reference
		var virtualFileNavigationArgs = new VirtualFileNavigationArgs(fullPath, fileRef.Hidden, index, 0, parent, root, prefixProvider, []);
		var tempFileNavigation = DocumentationNavigationFactory.CreateVirtualFileNavigation(documentationFile, virtualFileNavigationArgs);

		// Process children recursively
		var children = new List<INavigationItem>();
		var childIndex = 0;
		foreach (var child in fileRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child, childIndex++, context,
				tempFileNavigation, root,
				prefixProvider, // Files don't change the URL root
				0, // Depth will be set by child
				fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
					? fullPath[..^3] // Remove .md extension for children's parent path
					: fullPath
			);
			if (childNav != null)
				children.Add(childNav);
		}

		// Create final file navigation with actual children
		virtualFileNavigationArgs = virtualFileNavigationArgs with
		{
			Depth = parent?.Depth + 1 ?? 0,
			NavigationItems = children
		};

		var finalFileNavigation = DocumentationNavigationFactory.CreateVirtualFileNavigation(documentationFile, virtualFileNavigationArgs);

		// Update children's Parent to point to the final file navigation
		foreach (var child in children)
			child.Parent = finalFileNavigation;

		return finalFileNavigation;
	}

	private INavigationItem CreateCrossLinkNavigation(
		CrossLinkRef crossLinkRef,
		int index,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root)
	{
		var title = crossLinkRef.Title ?? crossLinkRef.CrossLinkUri.OriginalString;
		var model = new CrossLinkModel(crossLinkRef.CrossLinkUri, title);

		return new CrossLinkNavigationLeaf(
			model,
			crossLinkRef.CrossLinkUri.OriginalString,
			crossLinkRef.Hidden,
			parent,
			root
		)
		{
			NavigationIndex = index
		};
	}

	private INavigationItem CreateFolderNavigation(
		FolderRef folderRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IPathPrefixProvider prefixProvider,
		int depth,
		string parentPath
	)
	{
		var folderPath = string.IsNullOrEmpty(parentPath)
			? folderRef.RelativePath
			: $"{parentPath}/{folderRef.RelativePath}";

		// Create temporary folder navigation for parent reference
		var children = new List<INavigationItem>();
		var childIndex = 0;

		var folderNavigation = new FolderNavigation(
			depth + 1,
			folderPath,
			parent,
			root,
			prefixProvider,
			[]
		);

		foreach (var child in folderRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child,
				childIndex++,
				context,
				folderNavigation,
				root,
				prefixProvider, // Folders don't change the URL root
				depth + 1,
				folderPath
			);

			if (childNav != null)
				children.Add(childNav);
		}

		// Create folder navigation with actual children
		var finalFolderNavigation = new FolderNavigation(depth + 1, folderPath, parent, root, prefixProvider, children)
		{
			NavigationIndex = index
		};

		// Update children's Parent to point to the final folder navigation
		foreach (var child in children)
			child.Parent = finalFolderNavigation;

		return finalFolderNavigation;
	}

	private static DocumentationSetNavigation GetDocumentationSetRoot(IRootNavigationItem<INavigationModel, INavigationItem> root)
	{
		// Walk up the tree to find the DocumentationSetNavigation root
		var current = root;
		while (current is TableOfContentsNavigation toc && toc.Parent is IRootNavigationItem<INavigationModel, INavigationItem> parentRoot)
			current = parentRoot;

		if (current is DocumentationSetNavigation docSetNav)
			return docSetNav;

		throw new InvalidOperationException("Could not find DocumentationSetNavigation root in navigation tree");
	}

	private INavigationItem CreateTocNavigation(
		IsolatedTableOfContentsRef tocRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IPathPrefixProvider prefixProvider,
		int depth,
		string parentPath
	)
	{
		// Determine the full TOC path for file system operations
		string tocPath;
		if (parent is TableOfContentsNavigation parentToc)
		{
			// Nested TOC: use parent TOC's path as base
			tocPath = $"{parentToc.ParentPath}/{tocRef.Source}";
		}
		else
		{
			// Root-level TOC: use parentPath (which comes from folder structure)
			tocPath = string.IsNullOrEmpty(parentPath)
				? tocRef.Source
				: $"{parentPath}/{tocRef.Source}";
		}

		// Resolve the TOC directory
		var tocDirectory = context.ReadFileSystem.DirectoryInfo.New(
			context.ReadFileSystem.Path.Combine(context.DocumentationSourceDirectory.FullName, tocPath)
		);

		// Read and deserialize the toc.yml file
		var tocFilePath = context.ReadFileSystem.Path.Combine(tocDirectory.FullName, "toc.yml");
		TableOfContentsFile? tocFile = null;

		if (context.ReadFileSystem.File.Exists(tocFilePath))
			tocFile = TableOfContentsFile.Deserialize(context.ReadFileSystem.File.ReadAllText(tocFilePath));
		else
			context.EmitError(context.ConfigurationPath, $"Table of contents file not found: {tocFilePath}");

		// Create the TOC navigation that will be the parent for children
		// For nested TOCs, use just the source name as parentPath since prefixProvider handles the full path
		// For root-level TOCs, use the full tocPath
		var navigationParentPath = parent is TableOfContentsNavigation ? tocRef.Source : tocPath;

		var tocNavigation = new TableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			navigationParentPath,
			parent,
			prefixProvider,
			[],
			Git,
			_tableOfContentNodes
		);

		// Convert children
		var children = new List<INavigationItem>();
		var childIndex = 0;

		// First, process items from the toc.yml file if it exists
		if (tocFile != null)
		{
			foreach (var child in tocFile.Toc)
			{
				var childNav = ConvertToNavigationItem(
					child,
					childIndex++,
					context,
					tocNavigation,
					root,
					tocNavigation, // TOC navigation becomes the new URL root for its children
					depth + 1,
					"" // Reset parentPath since TOC is new prefixProvider - children paths are relative to this TOC
				);

				if (childNav != null)
					children.Add(childNav);
			}
		}

		// Then, process items from tocRef.Children
		// In DocumentationSetFile, TOC references can only have other TOC references as children
		foreach (var child in tocRef.Children)
		{
			// Validate that TOC children are only other TOC references
			if (child is not IsolatedTableOfContentsRef)
			{
				context.EmitError(
					context.ConfigurationPath,
					$"TableOfContents navigation does not allow nested children, found: {child.GetType().Name}"
				);
				continue;
			}

			var childNav = ConvertToNavigationItem(
				child,
				childIndex++,
				context,
				tocNavigation,
				root,
				tocNavigation, // TOC navigation becomes the new URL root for its children
				depth + 1,
				"" // Reset parentPath since TOC is new prefixProvider - children paths are relative to this TOC
			);

			if (childNav != null)
				children.Add(childNav);
		}

		var finalTocNavigation = new TableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			navigationParentPath,
			parent,
			prefixProvider,
			children,
			Git,
			_tableOfContentNodes
		)
		{
			NavigationIndex = index
		};

		// Update children's Parent to point to the final TOC navigation
		foreach (var child in children)
			child.Parent = finalTocNavigation;

		return finalTocNavigation;
	}

}
