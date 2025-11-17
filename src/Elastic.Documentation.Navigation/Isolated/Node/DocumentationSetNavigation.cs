// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Toc.DetectionRules;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation.Isolated.Leaf;

namespace Elastic.Documentation.Navigation.Isolated.Node;

public interface IDocumentationSetNavigation
{
	IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> TableOfContentNodes { get; }
}

[DebuggerDisplay("{Url}")]
public class DocumentationSetNavigation<TModel>
	: IDocumentationSetNavigation, IRootNavigationItem<TModel, INavigationItem>, INavigationHomeAccessor, INavigationHomeProvider

	where TModel : class, IDocumentationFile
{
	private readonly IDocumentationFileFactory<TModel> _factory;
	private readonly ICrossLinkResolver _crossLinkResolver;

	public DocumentationSetNavigation(
		DocumentationSetFile documentationSet,
		IDocumentationSetContext context,
		IDocumentationFileFactory<TModel> factory,
		IRootNavigationItem<INavigationModel, INavigationItem>? parent = null,
		IRootNavigationItem<INavigationModel, INavigationItem>? root = null,
		string? pathPrefix = null,
		ICrossLinkResolver? crossLinkResolver = null
	)
	{
		_context = context;
		_factory = factory;
		_crossLinkResolver = crossLinkResolver ?? NoopCrossLinkResolver.Instance;
		PathPrefix = pathPrefix ?? string.Empty;
		// Initialize root properties
		NavigationRoot = root ?? this;
		Parent = parent;
		Hidden = false;
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
				homeAccessor: this
			);

			if (navItem != null)
				items.Add(navItem);
		}

		// Handle empty TOC - emit errors and create a minimal structure
		if (items.Count == 0)
		{
			var setName = documentationSet.Project ?? "unnamed";
			var setPath = context.ConfigurationPath.FullName;

			// Emit error if TOC was defined but no items could be created
			if (documentationSet.TableOfContents.Count > 0)
				context.EmitError(context.ConfigurationPath, $"Documentation set '{setName}' ({setPath}) table of contents has items defined but none could be created");
			// Emit error if TOC was never defined
			else
				context.EmitError(context.ConfigurationPath, $"Documentation set '{setName}' ({setPath}) has no table of contents defined");

			Index = null!;
			NavigationItems = [];
		}
		else
		{
			var indexNavigation = items.QueryIndex<TModel>(this, $"{PathPrefix}/index.md", out var navigationItems);
			Index = indexNavigation;
			NavigationItems = navigationItems;
			_ = this.UpdateNavigationIndex(context);
		}

	}

	/// <summary>
	/// Gets the path prefix. When HomeProvider is set to a different instance, it returns that provider's prefix.
	/// Otherwise, returns the prefix set during construction.
	/// </summary>
	public string PathPrefix => HomeProvider == this ? field : HomeProvider.PathPrefix;

	public INavigationHomeProvider HomeProvider { get; set; }

	public GitCheckoutInformation Git { get; }

	private readonly Dictionary<Uri, IRootNavigationItem<TModel, INavigationItem>> _tableOfContentNodes = [];
	private readonly IDocumentationSetContext _context;
	public IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> TableOfContentNodes =>
		_tableOfContentNodes.ToDictionary(kvp => kvp.Key, IRootNavigationItem<IDocumentationFile, INavigationItem> (kvp) => kvp.Value);

	public Uri Identifier { get; }

	/// <inheritdoc />
	public string Url => Index.Url;

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public string? NavigationTooltip => Index.NavigationTooltip;

	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot =>
		HomeProvider == this ? field : HomeProvider.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden { get; }

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	public string Id { get; }

	/// <inheritdoc />
	public ILeafNavigationItem<TModel> Index { get; private set; }

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown { get; }

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; }

	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) => SetNavigationItems(navigationItems);
	private void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems)
	{
		var indexNavigation = navigationItems.QueryIndex<TModel>(this, $"{PathPrefix}/index.md", out navigationItems);
		Index = indexNavigation;
		NavigationItems = navigationItems;
		_ = this.UpdateNavigationIndex(_context);
	}


	private INavigationItem? ConvertToNavigationItem(
		ITableOfContentsItem tocItem,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		INavigationHomeAccessor homeAccessor
	) =>
		tocItem switch
		{
			FileRef fileRef => CreateFileNavigation(fileRef, index, context, parent, homeAccessor),
			CrossLinkRef crossLinkRef => CreateCrossLinkNavigation(crossLinkRef, index, parent, homeAccessor),
			FolderRef folderRef => CreateFolderNavigation(folderRef, index, context, parent, homeAccessor),
			IsolatedTableOfContentsRef tocRef => CreateTocNavigation(tocRef, index, context, parent, homeAccessor),
			_ => null
		};

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
		string fullPath
	)
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

	private INavigationItem? CreateFileNavigation(
		FileRef fileRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		INavigationHomeAccessor homeAccessor
	)
	{
		// FileRef.Path already contains the correct path from LoadAndResolve
		var fullPath = fileRef.PathRelativeToDocumentationSet;

		// Create file info and documentation file
		var fileInfo = fileRef switch
		{
			DetectionRuleRef ruleRef => ruleRef.FileInfo,
			_ => ResolveFileInfo(context, fullPath)
		};
		var documentationFile = CreateDocumentationFile(fileInfo, context.ReadFileSystem, context, fullPath);
		if (documentationFile == null)
			return null;

		// Handle leaf case (no children)
		if (fileRef.Children.Count <= 0)
		{
			var leafNavigationArgs = new FileNavigationArgs(fullPath, fileRef.PathRelativeToContainer, fileRef.Hidden, index, parent, homeAccessor);
			return DocumentationNavigationFactory.CreateFileNavigationLeaf(documentationFile, fileInfo, leafNavigationArgs);
		}

		// Create file navigation with empty children initially
		var virtualFileNavigationArgs = new VirtualFileNavigationArgs(
			fullPath,
			fileRef.PathRelativeToContainer,
			fileRef.Hidden,
			index,
			parent,
			homeAccessor
		);
		var fileNavigation = DocumentationNavigationFactory.CreateVirtualFileNavigation(documentationFile, fileInfo, virtualFileNavigationArgs);

		// Process children recursively
		var children = new List<INavigationItem>();
		var childIndex = 0;

		foreach (var child in fileRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child, childIndex++, context,
				fileNavigation,
				homeAccessor // Depth will be set by child
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
		fileNavigation.SetNavigationItems(children);

		return fileNavigation;
	}

	private INavigationItem? CreateCrossLinkNavigation(
		CrossLinkRef crossLinkRef,
		int index,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		INavigationHomeAccessor homeAccessor
	)
	{
		var title = crossLinkRef.Title ?? crossLinkRef.CrossLinkUri.OriginalString;
		if (!_crossLinkResolver.TryResolve(s => _context.EmitError(_context.ConfigurationPath, s), crossLinkRef.CrossLinkUri, out var resolvedUri))
			return null;
		var model = new CrossLinkModel(resolvedUri, title);

		return new CrossLinkNavigationLeaf(
			model,
			resolvedUri.ToString(),
			crossLinkRef.Hidden,
			parent,
			homeAccessor
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
		INavigationHomeAccessor homeAccessor
	)
	{
		// FolderRef.Path already contains the correct path from LoadAndResolve
		var folderPath = folderRef.PathRelativeToDocumentationSet;

		// Create folder navigation with null parent initially - we'll pass it to children but set it properly after
		var folderNavigation = new FolderNavigation<TModel>(folderPath, parent, homeAccessor)
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
				homeAccessor
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
		folderNavigation.SetNavigationItems(children);
		return folderNavigation;
	}

	private INavigationItem? CreateTocNavigation(
		IsolatedTableOfContentsRef tocRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		INavigationHomeAccessor homeAccessor
	)
	{
		// tocRef.Path is now the FULL path (e.g., "guides/api" or "setup/advanced") after LoadAndResolve
		var fullTocPath = tocRef.PathRelativeToDocumentationSet;

		var tocDirectory = context.ReadFileSystem.DirectoryInfo.New(
			context.ReadFileSystem.Path.Combine(context.DocumentationSourceDirectory.FullName, fullTocPath)
		);

		var assemblerBuild = context.AssemblerBuild;
		// for assembler builds we ensure toc's create their own home provider sot that they can be re-homed easily
		var isolatedHomeProvider = assemblerBuild
			? new NavigationHomeProvider(homeAccessor.HomeProvider.PathPrefix, homeAccessor.HomeProvider.NavigationRoot)
			: homeAccessor.HomeProvider;

		// Create the TOC navigation with empty children initially
		// We use null parent temporarily - we'll set it properly at the end using the public setter
		// Pass tocHomeProvider so the TOC uses parent's NavigationRoot (enables dynamic URL updates)
		var tocNavigation = new TableOfContentsNavigation<TModel>(
			tocDirectory,
			fullTocPath,
			parent, // Temporary null parent
			isolatedHomeProvider.PathPrefix,
			Git,
			_tableOfContentNodes,
			isolatedHomeProvider
		)
		{
			NavigationIndex = index
		};

		// Convert children - pass tocNavigation as parent and tocHomeProvider as HomeProvider (TOC creates new scope)
		var children = new List<INavigationItem>();
		var childIndex = 0;

		//children scoped to documentation-set in isolated builds, to docset in assembler builds
		var childHomeAccessor = assemblerBuild ? tocNavigation : homeAccessor;

		foreach (var child in tocRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child,
				childIndex++,
				context,
				tocNavigation,
				childHomeAccessor
			);

			if (childNav != null)
				children.Add(childNav);
		}

		// Validate TOCs have children
		if (children.Count == 0)
		{
			context.Collector.EmitError(tocRef.Context,
				tocRef.Children.Count == 0
					? $"Table of contents navigation '{fullTocPath}' has no children defined ({tocRef.Context}:)"
					: $"Table of contents navigation '{fullTocPath}' has children defined but none could be created ({tocRef.Context}:)");
			return null;
		}
		tocNavigation.SetNavigationItems(children);

		return tocNavigation;
	}

}
