// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Isolated;

// A model for nodes in the navigation representing directories e.g., sets, toc's and folders.
public interface IDocumentationFileFactory<out TModel> where TModel : IDocumentationFile
{
	TModel? TryCreateDocumentationFile(IFileInfo path, IFileSystem readFileSystem);
}

public static class DocumentationNavigationFactory
{
	public static ILeafNavigationItem<TModel> CreateFileNavigationLeaf<TModel>(TModel model, IFileInfo fileInfo, FileNavigationArgs args)
		where TModel : IDocumentationFile =>
		new FileNavigationLeaf<TModel>(model, fileInfo, args) { NavigationIndex = args.NavigationIndex };

	public static INodeNavigationItem<TModel, INavigationItem> CreateVirtualFileNavigation<TModel>(TModel model, IFileInfo fileInfo,
		VirtualFileNavigationArgs args)
		where TModel : IDocumentationFile =>
		new VirtualFileNavigation<TModel>(model, fileInfo, args) { NavigationIndex = args.NavigationIndex };
}

public interface IDocumentationSetNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>
{
	IReadOnlyDictionary<Uri, INodeNavigationItem<IDocumentationFile, INavigationItem>> TableOfContentNodes { get; }
}

public class DocumentationSetNavigation<TModel>
	: IDocumentationSetNavigation, INavigationPathPrefixProvider, IPathPrefixProvider

	where TModel : IDocumentationFile
{
	private readonly IDocumentationFileFactory<TModel> _factory;

	public DocumentationSetNavigation(
		DocumentationSetFile documentationSet,
		IDocumentationSetContext context,
		IDocumentationFileFactory<TModel> factory,
		IRootNavigationItem<INavigationModel, INavigationItem>? parent = null,
		IRootNavigationItem<INavigationModel, INavigationItem>? root = null,
		string? pathPrefix = null
	)
	{
		_factory = factory;
		_pathPrefix = pathPrefix ?? string.Empty;
		// Initialize root properties
		NavigationRoot = root ?? this;
		Parent = parent;
		Depth = 0;
		Hidden = false;
		IsCrossLink = false;
		PathPrefixProvider = this;
		Id = ShortId.Create(documentationSet.Project ?? "root");
		IsUsingNavigationDropdown = documentationSet.Features.PrimaryNav ?? false;
		Git = context.Git;
		Identifier = new Uri($"{Git.RepositoryName}://");
		_ = _tableOfContentNodes.TryAdd(Identifier, this);

		// Convert TOC items to navigation items
		var items = new List<INavigationItem>();
		var index = -1;
		foreach (var tocItem in documentationSet.TableOfContents)
		{
			var navItem = ConvertToNavigationItem(
				tocItem,
				index++,
				context,
				parent: null,
				root: NavigationRoot,
				prefixProvider: PathPrefixProvider,
				depth: Depth
			);

			if (navItem != null)
				items.Add(navItem);
		}

		NavigationItems = items;
		_ = this.UpdateNavigationIndex(context);
		Index = this.FindIndex<IDocumentationFile>(new NotFoundModel($"{PathPrefix}/index.md"));

	}

	private readonly string _pathPrefix;

	/// <summary>
	/// Gets the path prefix. When PathPrefixProvider is set to a different instance, it returns that provider's prefix.
	/// Otherwise, returns the prefix set during construction.
	/// </summary>
	public string PathPrefix => PathPrefixProvider == this ? _pathPrefix : PathPrefixProvider.PathPrefix;

	public IPathPrefixProvider PathPrefixProvider { get; set; }

	public GitCheckoutInformation Git { get; }

	private readonly Dictionary<Uri, INodeNavigationItem<IDocumentationFile, INavigationItem>> _tableOfContentNodes = [];
	public IReadOnlyDictionary<Uri, INodeNavigationItem<IDocumentationFile, INavigationItem>> TableOfContentNodes => _tableOfContentNodes;

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
	public ILeafNavigationItem<IDocumentationFile> Index { get; }

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
		int depth
	) =>
		tocItem switch
		{
			FileRef fileRef => CreateFileNavigation(fileRef, index, context, parent, root, prefixProvider),
			CrossLinkRef crossLinkRef => CreateCrossLinkNavigation(crossLinkRef, index, parent, root),
			FolderRef folderRef => CreateFolderNavigation(folderRef, index, context, parent, root, prefixProvider, depth),
			IsolatedTableOfContentsRef tocRef => CreateTocNavigation(tocRef, index, context, parent, root, prefixProvider, depth),
			_ => null
		};

	#region CreateFileNavigation Helper Methods

	/// <summary>
	/// Creates a temporary file navigation placeholder used during construction before children are processed.
	/// This is distinct from the factory method to make it clear this is a temporary instance.
	/// </summary>
	private INodeNavigationItem<TModel, INavigationItem> CreateTemporaryFileNavigation(
		TModel documentationFile,
		IFileInfo fileInfo,
		string fullPath,
		bool hidden,
		int index,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IPathPrefixProvider prefixProvider)
	{
		var virtualFileNavigationArgs = new VirtualFileNavigationArgs(fullPath, hidden, index, 0, parent, root, prefixProvider, []);
		return new VirtualFileNavigation<TModel>(documentationFile, fileInfo, virtualFileNavigationArgs);
	}

	/// <summary>
	/// Resolves the file info based on the file path. Since LoadAndResolve has already processed paths,
	/// we simply combine the documentation source directory with the file path.
	/// </summary>
	private static IFileInfo ResolveFileInfo(IDocumentationSetContext context, string filePath)
	{
		var fs = context.ReadFileSystem;
		// FileRef.Path already contains the correct path from LoadAndResolve
		return fs.FileInfo.New(fs.Path.Combine(context.DocumentationSourceDirectory.FullName, filePath));
	}

	/// <summary>
	/// Creates the documentation file from the factory, emitting an error if creation fails.
	/// </summary>
	private TModel? CreateDocumentationFile(
		IFileInfo fileInfo,
		IFileSystem fileSystem,
		IDocumentationSetContext context,
		string fullPath)
	{
		var relativePath = Path.GetRelativePath(context.DocumentationSourceDirectory.FullName, fileInfo.FullName);
		var documentationFile = _factory.TryCreateDocumentationFile(fileInfo, fileSystem);
		if (documentationFile == null)
			context.EmitError(context.ConfigurationPath, $"File navigation '{relativePath}' could not be created. {fullPath}");

		return documentationFile;
	}

	/// <summary>
	/// Processes children recursively and returns the list of navigation items.
	/// Since LoadAndResolve has already prepended parent paths to all children,
	/// we don't need to calculate paths here.
	/// </summary>
	private List<INavigationItem> ProcessFileChildren(
		FileRef fileRef,
		IDocumentationSetContext context,
		INodeNavigationItem<TModel, INavigationItem> tempFileNavigation,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IPathPrefixProvider prefixProvider)
	{
		var children = new List<INavigationItem>();
		var childIndex = 0;

		foreach (var child in fileRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child, childIndex++, context,
				(INodeNavigationItem<INavigationModel, INavigationItem>)tempFileNavigation, root,
				prefixProvider, // Files don't change the URL root
				0 // Depth will be set by child
			);
			if (childNav != null)
				children.Add(childNav);
		}

		return children;
	}

	/// <summary>
	/// Ensures the first item in the navigation items is the index file (index.md or the first file in the list).
	/// </summary>
	private static void EnsureIndexIsFirst(List<INavigationItem> children)
	{
		if (children.Count == 0)
			return;

		// Find an item named "index" or "index.md"
		var indexItem = children.FirstOrDefault(c =>
			c is ILeafNavigationItem<IDocumentationFile> leaf &&
			(leaf.Model.NavigationTitle.Equals("index", StringComparison.OrdinalIgnoreCase) ||
			 (leaf is FileNavigationLeaf<IDocumentationFile> fileLeaf &&
			  fileLeaf.FileInfo.Name.Equals("index.md", StringComparison.OrdinalIgnoreCase))));

		// If found and it's not already first, move it to the front
		if (indexItem != null && children[0] != indexItem)
		{
			_ = children.Remove(indexItem);
			children.Insert(0, indexItem);
		}
	}

	/// <summary>
	/// Validates that navigation items has at least one item, emitting an error if not.
	/// </summary>
	private static void ValidateNavigationItems(
		List<INavigationItem> children,
		IDocumentationSetContext context,
		string fullPath)
	{
		if (children.Count < 1)
		{
			context.EmitError(context.ConfigurationPath,
				$"File navigation '{fullPath}' has children defined but none could be created");
		}
	}

	#endregion

	private INavigationItem? CreateFileNavigation(
		FileRef fileRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IPathPrefixProvider prefixProvider
	)
	{
		// FileRef.Path already contains the correct path from LoadAndResolve
		var fullPath = fileRef.Path;

		// Create file info and documentation file
		var fileInfo = ResolveFileInfo(context, fullPath);
		var documentationFile = CreateDocumentationFile(fileInfo, context.ReadFileSystem, context, fullPath);
		if (documentationFile == null)
			return null;

		// Handle leaf case (no children)
		if (fileRef.Children.Count <= 0)
		{
			var leafNavigationArgs = new FileNavigationArgs(fullPath, fileRef.Hidden, index, parent, root, prefixProvider);
			return DocumentationNavigationFactory.CreateFileNavigationLeaf(documentationFile, fileInfo, leafNavigationArgs);
		}

		// Create temporary file navigation for children to reference
		var tempFileNavigation = CreateTemporaryFileNavigation(documentationFile, fileInfo, fullPath, fileRef.Hidden, index, parent, root, prefixProvider);

		// Process children recursively
		// Note: LoadAndResolve has already prepended parent paths to all children, so we don't need to calculate parentPathForChildren
		var children = ProcessFileChildren(fileRef, context, tempFileNavigation, root, prefixProvider);

		// Validate and order children
		ValidateNavigationItems(children, context, fullPath);
		EnsureIndexIsFirst(children);

		// Create final file navigation with actual children
		var virtualFileNavigationArgs = new VirtualFileNavigationArgs(
			fullPath,
			fileRef.Hidden,
			index,
			parent?.Depth + 1 ?? 0,
			parent,
			root,
			prefixProvider,
			children
		);

		var finalFileNavigation = DocumentationNavigationFactory.CreateVirtualFileNavigation(documentationFile, fileInfo, virtualFileNavigationArgs);

		// Update children's Parent to point to the final file navigation
		foreach (var child in children)
			child.Parent = (INodeNavigationItem<INavigationModel, INavigationItem>)finalFileNavigation;

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

	private INavigationItem? CreateFolderNavigation(
		FolderRef folderRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IPathPrefixProvider prefixProvider,
		int depth
	)
	{
		// FolderRef.Path already contains the correct path from LoadAndResolve
		var folderPath = folderRef.Path;

		// Create temporary placeholder for parent reference
		var children = new List<INavigationItem>();
		var childIndex = 0;

		var placeholderNavigation = new TemporaryNavigationPlaceholder(
			depth + 1,
			ShortId.Create(folderPath),
			parent,
			root,
			prefixProvider,
			folderPath
		);

		// LoadAndResolve has already populated children (either from YAML or auto-discovered)
		foreach (var child in folderRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child,
				childIndex++,
				context,
				placeholderNavigation,
				root,
				prefixProvider, // Keep parent's prefix provider
				depth + 1
			);

			if (childNav != null)
				children.Add(childNav);
		}

		// Validate that we have children (LoadAndResolve should have ensured this)
		if (children.Count == 0)
		{
			context.Collector.EmitError(folderRef.Context, $"Folder navigation '{folderPath}' has children defined but none could be created ({folderRef.Context}:)");
			return null;
		}

		// Create folder navigation with actual children
		var finalFolderNavigation = new FolderNavigation(depth + 1, folderPath, parent, root, children)
		{
			NavigationIndex = index
		};

		// Update children's Parent to point to the final folder navigation
		foreach (var child in children)
			child.Parent = finalFolderNavigation;

		return finalFolderNavigation;
	}

	private INavigationItem? CreateTocNavigation(
		IsolatedTableOfContentsRef tocRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IPathPrefixProvider prefixProvider,
		int depth
	)
	{
		// tocRef.Path is now the FULL path (e.g., "guides/api" or "setup/advanced") after LoadAndResolve
		// Extract just the last segment (relative part) for the URL path prefix
		var fullTocPath = tocRef.Path;
		var lastSlashIndex = fullTocPath.LastIndexOf('/');
		var relativeTocPath = lastSlashIndex >= 0 ? fullTocPath[(lastSlashIndex + 1)..] : fullTocPath;

		var tocDirectory = context.ReadFileSystem.DirectoryInfo.New(
			context.ReadFileSystem.Path.Combine(context.DocumentationSourceDirectory.FullName, fullTocPath)
		);

		// TODO: Add validation for TOCs with children in parent YAML
		// This is a known limitation - TOCs should not have children defined in parent YAML

		var placeholderNavigation = new TemporaryNavigationPlaceholder(
			depth + 1,
			ShortId.Create(fullTocPath),
			parent,
			root,
			prefixProvider,
			relativeTocPath, // Use relative path (last segment) for URL construction
			tocDirectory
		);

		// Convert children
		var children = new List<INavigationItem>();
		var childIndex = 0;

		// LoadAndResolve has already resolved children from the toc.yml file and prepended full paths.
		// Children have full paths (e.g., "guides/api/reference.md"), so they should use the same prefix provider
		// as the TOC (not the TOC itself as prefix provider) to avoid double-prepending the TOC path.
		foreach (var child in tocRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child,
				childIndex++,
				context,
				placeholderNavigation,
				root,
				prefixProvider, // Use same prefix provider as TOC, since children already have full paths from LoadAndResolve
				depth + 1
			);

			if (childNav != null)
				children.Add(childNav);
		}

		// Validate TOCs have children
		if (children.Count == 0)
		{
			if (tocRef.Children.Count == 0)
				context.Collector.EmitError(tocRef.Context, $"Table of contents navigation '{fullTocPath}' has no children defined ({tocRef.Context}:)");
			else
				context.Collector.EmitError(tocRef.Context, $"Table of contents navigation '{fullTocPath}' has children defined but none could be created ({tocRef.Context}:)");
			return null;
		}

		var finalTocNavigation = new TableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			fullTocPath, // Use full path for identifier registration
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
