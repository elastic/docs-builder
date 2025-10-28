// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
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

[DebuggerDisplay("{Url}")]
public class DocumentationSetNavigation<TModel>
	: IDocumentationSetNavigation, INavigationHomeAccessor, INavigationHomeProvider

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
		_navigationRoot = root ?? this;
		Parent = parent;
		Depth = 0;
		Hidden = false;
		IsCrossLink = false;
		HomeProvider = this;
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
				parent: this,
				homeProvider: HomeProvider,
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
	private readonly IRootNavigationItem<INavigationModel, INavigationItem> _navigationRoot;

	/// <summary>
	/// Gets the path prefix. When HomeProvider is set to a different instance, it returns that provider's prefix.
	/// Otherwise, returns the prefix set during construction.
	/// </summary>
	public string PathPrefix => HomeProvider == this ? _pathPrefix : HomeProvider.PathPrefix;

	public INavigationHomeProvider HomeProvider { get; set; }

	public GitCheckoutInformation Git { get; }

	private readonly Dictionary<Uri, INodeNavigationItem<IDocumentationFile, INavigationItem>> _tableOfContentNodes = [];
	public IReadOnlyDictionary<Uri, INodeNavigationItem<IDocumentationFile, INavigationItem>> TableOfContentNodes => _tableOfContentNodes;

	public Uri Identifier { get; }

	/// <inheritdoc />
	public string Url
	{
		get
		{
			var rootUrl = HomeProvider.PathPrefix.TrimEnd('/');
			return string.IsNullOrEmpty(rootUrl) ? "/" : rootUrl;
		}
	}

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot =>
		HomeProvider == this ? _navigationRoot : HomeProvider.NavigationRoot;

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
		INavigationHomeProvider homeProvider,
		int depth
	) =>
		tocItem switch
		{
			FileRef fileRef => CreateFileNavigation(fileRef, index, context, parent, homeProvider),
			CrossLinkRef crossLinkRef => CreateCrossLinkNavigation(crossLinkRef, index, parent, homeProvider),
			FolderRef folderRef => CreateFolderNavigation(folderRef, index, context, parent, homeProvider, depth),
			IsolatedTableOfContentsRef tocRef => CreateTocNavigation(tocRef, index, context, parent, homeProvider, depth),
			_ => null
		};

	#region CreateFileNavigation Helper Methods

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

	#endregion

	private INavigationItem? CreateFileNavigation(
		FileRef fileRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		INavigationHomeProvider homeProvider
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
			var leafNavigationArgs = new FileNavigationArgs(fullPath, fileRef.Hidden, index, parent, homeProvider);
			return DocumentationNavigationFactory.CreateFileNavigationLeaf(documentationFile, fileInfo, leafNavigationArgs);
		}

		// Create file navigation with empty children initially
		var virtualFileNavigationArgs = new VirtualFileNavigationArgs(
			fullPath,
			fileRef.Hidden,
			index,
			parent?.Depth + 1 ?? 0,
			null, // Parent will be set after processing children
			homeProvider,
			[]
		);
		var fileNavigation = DocumentationNavigationFactory.CreateVirtualFileNavigation(documentationFile, fileInfo, virtualFileNavigationArgs);

		// Process children recursively
		var children = new List<INavigationItem>();
		var childIndex = 0;

		foreach (var child in fileRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child, childIndex++, context,
				(INodeNavigationItem<INavigationModel, INavigationItem>)fileNavigation,
				homeProvider, // Files don't change the URL root
				0 // Depth will be set by child
			);
			if (childNav != null)
				children.Add(childNav);
		}

		// Validate and order children
		if (children.Count < 1)
		{
			context.EmitError(context.ConfigurationPath,
				$"File navigation '{fullPath}' has children defined but none could be created");
			return null;
		}

		EnsureIndexIsFirst(children);

		// Create final file navigation with actual children and correct parent
		var finalVirtualFileNavigationArgs = new VirtualFileNavigationArgs(
			fullPath,
			fileRef.Hidden,
			index,
			parent?.Depth + 1 ?? 0,
			parent,
			homeProvider,
			children
		);

		var finalFileNavigation = DocumentationNavigationFactory.CreateVirtualFileNavigation(documentationFile, fileInfo, finalVirtualFileNavigationArgs);

		// Update children's Parent to point to the final file navigation
		foreach (var child in children)
			child.Parent = (INodeNavigationItem<INavigationModel, INavigationItem>)finalFileNavigation;

		return finalFileNavigation;
	}

	private INavigationItem CreateCrossLinkNavigation(
		CrossLinkRef crossLinkRef,
		int index,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		INavigationHomeProvider homeProvider)
	{
		var title = crossLinkRef.Title ?? crossLinkRef.CrossLinkUri.OriginalString;
		var model = new CrossLinkModel(crossLinkRef.CrossLinkUri, title);

		return new CrossLinkNavigationLeaf(
			model,
			crossLinkRef.CrossLinkUri.OriginalString,
			crossLinkRef.Hidden,
			parent,
			homeProvider
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
		INavigationHomeProvider homeProvider,
		int depth
	)
	{
		// FolderRef.Path already contains the correct path from LoadAndResolve
		var folderPath = folderRef.Path;

		// Create folder navigation with null parent initially - we'll pass it to children but set it properly after
		var folderNavigation = new FolderNavigation(depth + 1, folderPath, null, homeProvider, [])
		{
			NavigationIndex = index
		};

		// Process children - they can reference folderNavigation as their parent
		var children = new List<INavigationItem>();
		var childIndex = 0;

		// LoadAndResolve has already populated children (either from YAML or auto-discovered)
		foreach (var child in folderRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child,
				childIndex++,
				context,
				folderNavigation,
				homeProvider, // Keep parent's home provider
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

		// Now create the final folder navigation with the correct parent and children
		var finalFolderNavigation = new FolderNavigation(depth + 1, folderPath, parent, homeProvider, children)
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
		INavigationHomeProvider homeProvider,
		int depth
	)
	{
		// tocRef.Path is now the FULL path (e.g., "guides/api" or "setup/advanced") after LoadAndResolve
		var fullTocPath = tocRef.Path;

		var tocDirectory = context.ReadFileSystem.DirectoryInfo.New(
			context.ReadFileSystem.Path.Combine(context.DocumentationSourceDirectory.FullName, fullTocPath)
		);

		// TODO: Add validation for TOCs with children in parent YAML
		// This is a known limitation - TOCs should not have children defined in parent YAML

		// According to url-building.md line 19-21: "We are not actually changing the PathPrefix,
		// we create the scope to be able to rehome during Assembler builds."
		// So TOC uses the SAME PathPrefix as its parent - it only creates a scope for rehoming
		var scopedPathPrefix = homeProvider.PathPrefix;

		// Create the TOC navigation with empty children initially
		// We use null parent temporarily - we'll set it properly at the end using the public setter
		var tocNavigation = new TableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			fullTocPath,
			null, // Temporary null parent
			scopedPathPrefix,
			[],
			Git,
			_tableOfContentNodes
		)
		{
			NavigationIndex = index
		};

		// Create a scoped HomeProvider for TOC children
		// According to url-building.md: "In isolated builds the NavigationRoot is always the DocumentationSetNavigation"
		// So we use the parent's NavigationRoot, not the TOC itself
		var tocHomeProvider = new NavigationHomeProvider(scopedPathPrefix, homeProvider.NavigationRoot);

		// Convert children - pass tocNavigation as parent and tocHomeProvider as HomeProvider (TOC creates new scope)
		var children = new List<INavigationItem>();
		var childIndex = 0;

		// LoadAndResolve has already resolved children from the toc.yml file and prepended full paths.
		// Children have full paths (e.g., "guides/api/reference.md"), so they should use the TOC's scoped HomeProvider
		foreach (var child in tocRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child,
				childIndex++,
				context,
				tocNavigation,
				tocHomeProvider, // Use the scoped HomeProvider with correct NavigationRoot
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

		// Now recreate the TOC navigation with the actual children
		// Unfortunately we need to create it twice because NavigationItems is readonly
		// But this time we need to remove the old one from _tableOfContentNodes first
		var identifier = new Uri($"{Git.RepositoryName}://{fullTocPath}");
		_ = _tableOfContentNodes.Remove(identifier);

		var finalTocNavigation = new TableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			fullTocPath,
			parent, // Now set the correct parent
			scopedPathPrefix,
			children,
			Git,
			_tableOfContentNodes
		)
		{
			NavigationIndex = index
		};

		// Update children's Parent to point to the final TOC navigation
		// Note: We don't update HomeProvider here because children already have the correct tocHomeProvider
		// which provides the scoped PathPrefix and correct NavigationRoot
		foreach (var child in children)
			child.Parent = finalTocNavigation;

		return finalTocNavigation;
	}

}
