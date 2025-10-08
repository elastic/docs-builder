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
		Index = NavigationItems.FindIndex<IDocumentationFile>()
			?? throw new InvalidOperationException($"Could not find index file in {nameof(DocumentationSetNavigation<TModel>)}");

		var navigationIndex = 0;
		UpdateNavigationIndex(NavigationItems, context, ref navigationIndex);
	}

	private readonly string _pathPrefix;

	/// <summary>
	/// Gets the path prefix. When PathPrefixProvider is set to a different instance, returns that provider's prefix.
	/// Otherwise returns the prefix set during construction.
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

	private void UpdateNavigationIndex(IReadOnlyCollection<INavigationItem> navigationItems, IDocumentationSetContext context, ref int navigationIndex)
	{
		foreach (var item in navigationItems)
		{
			switch (item)
			{
				case ILeafNavigationItem<INavigationModel> leaf:
					var fileIndex = Interlocked.Increment(ref navigationIndex);
					leaf.NavigationIndex = fileIndex;
					break;
				case INodeNavigationItem<INavigationModel, INavigationItem> node:
					var groupIndex = Interlocked.Increment(ref navigationIndex);
					node.NavigationIndex = groupIndex;
					UpdateNavigationIndex(node.NavigationItems, context, ref navigationIndex);
					break;
				default:
					context.EmitError(context.ConfigurationPath, $"{nameof(DocumentationSetNavigation<TModel>)}.{nameof(UpdateNavigationIndex)}: Unhandled navigation item type: {item.GetType()}");
					break;
			}
		}
	}


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
	/// Resolves the relative path for URL generation, handling parent path and deeplinked paths.
	/// </summary>
	private static string ResolveFileRelativePath(string fileRefPath, string parentPath)
	{
		if (string.IsNullOrEmpty(parentPath))
			return fileRefPath;

		// Extract parent's directory (everything before the last /)
		var parentDir = parentPath.Contains('/')
			? parentPath[..parentPath.LastIndexOf('/')]
			: "";

		// Extract child's directory from fileRef.Path
		var childDir = fileRefPath.Contains('/')
			? fileRefPath[..fileRefPath.LastIndexOf('/')]
			: "";

		// Check for deeplinked paths where child's directory is already in parent path
		// Case 1: parentDir ends with childDir (e.g., parentPath="guides/clients/getting-started", childDir="clients")
		// Case 2: parentPath ends with childDir (e.g., parentPath="guides/clients", childDir="clients")
		return !string.IsNullOrEmpty(childDir) &&
			(parentDir == childDir || parentDir.EndsWith($"/{childDir}", StringComparison.Ordinal) ||
				parentPath.EndsWith(childDir, StringComparison.Ordinal))
			? fileRefPath[(childDir.Length + 1)..] // Strip child's directory from path
			: fileRefPath.StartsWith($"{parentPath}/", StringComparison.Ordinal)
				? fileRefPath[(parentPath.Length + 1)..] // If file path starts with parent path, extract just the relative part
				: fileRefPath;
	}

	/// <summary>
	/// Combines parent path with relative path to create the full file path.
	/// </summary>
	private static string CreateFullFilePath(string relativePathForUrl, string parentPath) =>
		string.IsNullOrEmpty(parentPath)
			? relativePathForUrl
			: $"{parentPath}/{relativePathForUrl}";

	/// <summary>
	/// Resolves the file info based on the context and prefix provider.
	/// </summary>
	private static IFileInfo ResolveFileInfo(
		IDocumentationSetContext context,
		IPathPrefixProvider prefixProvider,
		string relativePathForUrl,
		string fullPath)
	{
		var fs = context.ReadFileSystem;

		// When inside a TOC, files are relative to the TOC directory, not the parent path
		// Check both actual TableOfContentsNavigation and temporary placeholders
		var tocDirectory = prefixProvider switch
		{
			TableOfContentsNavigation toc => toc.TableOfContentsDirectory,
			TemporaryNavigationPlaceholder placeholder => placeholder.TableOfContentsDirectory,
			_ => null
		};

		if (tocDirectory != null)
		{
			// For TOC children, use the TOC directory as the base
			return fs.FileInfo.New(fs.Path.Combine(tocDirectory.FullName, relativePathForUrl));
		}

		// For other files, use the documentation source directory + full path
		return fs.FileInfo.New(fs.Path.Combine(context.DocumentationSourceDirectory.FullName, fullPath));
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
		var documentationFile = _factory.TryCreateDocumentationFile(fileInfo, fileSystem);
		if (documentationFile == null)
			context.EmitError(context.ConfigurationPath, $"File navigation '{fullPath}' could not be created");

		return documentationFile;
	}

	/// <summary>
	/// Computes the parent path for children by removing .md extension and /index suffix if present.
	/// </summary>
	private static string DetermineParentPathForChildren(string fullPath)
	{
		// Remove .md extension
		var parentPathForChildren = fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			? fullPath[..^3]
			: fullPath;

		// If this is an index file, also remove the /index suffix for children's parent path
		if (parentPathForChildren.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
			parentPathForChildren = parentPathForChildren[..^6]; // Remove "/index"

		return parentPathForChildren;
	}

	/// <summary>
	/// Processes children recursively and returns the list of navigation items.
	/// </summary>
	private List<INavigationItem> ProcessFileChildren(
		FileRef fileRef,
		IDocumentationSetContext context,
		INodeNavigationItem<TModel, INavigationItem> tempFileNavigation,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IPathPrefixProvider prefixProvider,
		string parentPathForChildren)
	{
		var children = new List<INavigationItem>();
		var childIndex = 0;

		foreach (var child in fileRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child, childIndex++, context,
				(INodeNavigationItem<INavigationModel, INavigationItem>)tempFileNavigation, root,
				prefixProvider, // Files don't change the URL root
				0, // Depth will be set by child
				parentPathForChildren
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
		IPathPrefixProvider prefixProvider,
		string parentPath
	)
	{
		// Resolve paths
		var relativePathForUrl = ResolveFileRelativePath(fileRef.Path, parentPath);
		var fullPath = CreateFullFilePath(relativePathForUrl, parentPath);

		// Create file info and documentation file
		var fileInfo = ResolveFileInfo(context, prefixProvider, relativePathForUrl, fullPath);
		var documentationFile = CreateDocumentationFile(fileInfo, context.ReadFileSystem, context, fullPath);
		if (documentationFile == null)
			return null;

		// Handle leaf case (no children)
		if (fileRef.Children.Count <= 0)
		{
			var leafNavigationArgs = new FileNavigationArgs(fullPath, fileRef.Hidden, index, parent, root, prefixProvider);
			return DocumentationNavigationFactory.CreateFileNavigationLeaf(documentationFile, fileInfo, leafNavigationArgs);
		}

		// Validate: index files may not have children
		if (fileRef is IndexFileRef)
		{
			context.EmitError(context.ConfigurationPath, $"File navigation '{fileRef.Path}' is an index file and may not have children");
			return null;
		}

		// Create temporary file navigation for children to reference
		var tempFileNavigation = CreateTemporaryFileNavigation(documentationFile, fileInfo, fullPath, fileRef.Hidden, index, parent, root, prefixProvider);

		// Process children recursively
		var parentPathForChildren = DetermineParentPathForChildren(fullPath);
		var children = ProcessFileChildren(fileRef, context, tempFileNavigation, root, prefixProvider, parentPathForChildren);

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
		int depth,
		string parentPath
	)
	{
		var folderPath = string.IsNullOrEmpty(parentPath)
			? folderRef.Path
			: $"{parentPath}/{folderRef.Path}";

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

		foreach (var child in folderRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child,
				childIndex++,
				context,
				placeholderNavigation,
				root,
				prefixProvider, // Folders don't change the URL root
				depth + 1,
				folderPath
			);

			if (childNav != null)
				children.Add(childNav);
		}

		// Validate folders have children
		if (children.Count == 0)
		{
			if (folderRef.Children.Count == 0)
				context.EmitError(context.ConfigurationPath, $"Folder navigation '{folderPath}' has no children defined");
			else
				context.EmitError(context.ConfigurationPath, $"Folder navigation '{folderPath}' has children defined but none could be created");
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
		int depth,
		string parentPath
	)
	{
		// Determine the full TOC path for file system operations
		string tocPath;

		// Check if parent is a TOC (or placeholder for a TOC being constructed)
		var parentTocPath = parent switch
		{
			TableOfContentsNavigation toc => toc.ParentPath,
			TemporaryNavigationPlaceholder placeholder when placeholder.TableOfContentsDirectory != null => placeholder.ParentPath,
			_ => null
		};

		if (parentTocPath != null)
		{
			// Nested TOC: use parent TOC's path as base
			tocPath = $"{parentTocPath}/{tocRef.Source}";
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

		var placeholderNavigation = new TemporaryNavigationPlaceholder(
			depth + 1,
			ShortId.Create(navigationParentPath),
			parent,
			root,
			prefixProvider,
			navigationParentPath,
			tocDirectory
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
					placeholderNavigation,
					root,
					placeholderNavigation, // Placeholder acts as the new prefix provider for children
					depth + 1,
					"" // Reset parentPath since TOC is new prefixProvider - children paths are relative to this TOC
				);

				if (childNav != null)
					children.Add(childNav);
			}
		}

		// Validate that TOC references should not have children defined in navigation
		if (tocRef.Children.Count > 0)
		{
			context.EmitError(
				context.ConfigurationPath,
				$"TableOfContents '{tocRef.Source}' may not contain children, define children in '{tocRef.Source}/toc.yml' instead."
			);
		}

		// Validate TOCs have children
		if (children.Count == 0)
		{
			var hasTocFileChildren = tocFile?.Toc.Count > 0;
			var hasTocRefChildren = tocRef.Children.Count > 0;

			if (!hasTocFileChildren && !hasTocRefChildren)
				context.EmitError(context.ConfigurationPath, $"Table of contents navigation '{navigationParentPath}' has no children defined");
			else
				context.EmitError(context.ConfigurationPath, $"Table of contents navigation '{navigationParentPath}' has children defined but none could be created");
			return null;
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
