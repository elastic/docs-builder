// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Isolated;

public interface IDocumentationFile : INavigationModel
{
	string NavigationTitle { get; }
}
public record DocumentationDirectory(string NavigationTitle) : IDocumentationFile;

public class DocumentationSetNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>
{
	public DocumentationSetNavigation(DocumentationSetFile documentationSet, IDocumentationSetContext context)
	{
		// Initialize root properties
		NavigationRoot = this;
		Parent = null;
		Depth = 0;
		Hidden = false;
		IsCrossLink = false;
		Id = ShortId.Create(documentationSet.Project ?? "root");
		Index = new DocumentationDirectory(documentationSet.Project ?? "Documentation");
		IsUsingNavigationDropdown = documentationSet.Features.PrimaryNav ?? false;

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
				urlRoot: NavigationRoot,
				depth: Depth,
				parentPath: "",
				allowNestedToc: true
			);

			if (navItem != null)
				items.Add(navItem);
		}

		NavigationItems = items;
	}

	/// <inheritdoc />
	public string Url { get; set; } = "/";

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
		IRootNavigationItem<INavigationModel, INavigationItem> urlRoot,
		int depth,
		string parentPath,
		bool allowNestedToc = true)
	{
		// Validate TableOfContentsNavigation children
		if (parent is TableOfContentsNavigation tocParent)
		{
			// Check if this is a root-level TOC (parent is not a TOC)
			var isRootLevelToc = tocParent.Parent is not TableOfContentsNavigation;

			if (isRootLevelToc)
			{
				// Root-level TOC validation
				if (!allowNestedToc)
				{
					// When nested TOC is not allowed, any child is an error
					context.EmitError(
						context.ConfigurationPath,
						$"TableOfContents navigation does not allow nested children, found: {tocItem.GetType().Name}"
					);
				}
				else if (tocItem is not IsolatedTableOfContentsRef)
				{
					// When nested TOC is allowed, only TableOfContentsRef children are permitted
					context.EmitError(
						context.ConfigurationPath,
						$"TableOfContents navigation may only contain other TOC references as children, found: {tocItem.GetType().Name}"
					);
				}
			}
			else
			{
				// Nested TOC validation - nested TOCs should not have children when allowNestedToc is false
				if (!allowNestedToc)
				{
					context.EmitError(
						context.ConfigurationPath,
						$"TableOfContents navigation does not allow nested children, found: {tocItem.GetType().Name}"
					);
				}
			}
		}

		return tocItem switch
		{
			FileRef fileRef => CreateFileNavigation(fileRef, index, context, parent, root, urlRoot, parentPath),
			CrossLinkRef crossLinkRef => CreateCrossLinkNavigation(crossLinkRef, index, parent, root),
			FolderRef folderRef => CreateFolderNavigation(folderRef, index, context, parent, root, urlRoot, depth, parentPath, allowNestedToc),
			IsolatedTableOfContentsRef tocRef => CreateTocNavigation(tocRef, index, context, parent, root, urlRoot, depth, parentPath, allowNestedToc),
			_ => null
		};
	}

	private INavigationItem CreateFileNavigation(
		FileRef fileRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IRootNavigationItem<INavigationModel, INavigationItem> urlRoot,
		string parentPath
	)
	{
		// Extract title from file path
		var title = context.ReadFileSystem.Path.GetFileNameWithoutExtension(fileRef.RelativePath);

		// Combine parent path with file path
		var fullPath = string.IsNullOrEmpty(parentPath)
			? fileRef.RelativePath
			: $"{parentPath}/{fileRef.RelativePath}";

		// Create model
		var model = new CrossLinkModel(new Uri(fileRef.RelativePath, UriKind.Relative), title);

		// Check if file has children
		if (fileRef.Children.Count > 0)
		{
			// Validate: index files may not have children
			if (fileRef is IndexFileRef)
			{
				context.EmitError(context.ConfigurationPath,
					$"File navigation '{fileRef.RelativePath}' is an index file and may not have children");
				// Return a leaf to prevent further errors
				return new FileNavigationLeaf(model, fullPath, fileRef.Hidden, parent, root, urlRoot)
				{
					NavigationIndex = index
				};
			}

			// Create temporary file navigation for children to reference
			var tempFileNavigation = new FileNavigation(model, fullPath, fileRef.Hidden, 0, parent, root, urlRoot, []);

			// Process children recursively
			var children = new List<INavigationItem>();
			var childIndex = 0;
			foreach (var child in fileRef.Children)
			{
				var childNav = ConvertToNavigationItem(
					child, childIndex++, context,
					tempFileNavigation, root,
					urlRoot, // Files don't change the URL root
					0, // Depth will be set by child
					fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
						? fullPath[..^3] // Remove .md extension for children's parent path
						: fullPath,
					false // Files don't allow nested TOCs
				);
				if (childNav != null)
					children.Add(childNav);
			}

			// Create final file navigation with actual children
			var finalFileNavigation = new FileNavigation(
				model, fullPath, fileRef.Hidden,
				parent?.Depth + 1 ?? 0,
				parent, root, urlRoot, children)
			{
				NavigationIndex = index
			};

			// Update children's Parent to point to the final file navigation
			foreach (var child in children)
				child.Parent = finalFileNavigation;

			return finalFileNavigation;
		}

		// No children - return a leaf
		return new FileNavigationLeaf(model, fullPath, fileRef.Hidden, parent, root, urlRoot)
		{
			NavigationIndex = index
		};
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
		IRootNavigationItem<INavigationModel, INavigationItem> urlRoot,
		int depth,
		string parentPath,
		bool allowNestedToc)
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
			urlRoot,
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
				urlRoot,
				depth + 1,
				folderPath,
				allowNestedToc
			);

			if (childNav != null)
				children.Add(childNav);
		}

		// Create folder navigation with actual children
		var finalFolderNavigation = new FolderNavigation(depth + 1, folderPath, parent, root, urlRoot, children)
		{
			NavigationIndex = index
		};

		// Update children's Parent to point to the final folder navigation
		foreach (var child in children)
			child.Parent = finalFolderNavigation;

		return finalFolderNavigation;
	}

	private INavigationItem CreateTocNavigation(
		IsolatedTableOfContentsRef tocRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IRootNavigationItem<INavigationModel, INavigationItem> urlRoot,
		int depth,
		string parentPath,
		bool allowNestedToc
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
		// For nested TOCs, use just the source name as parentPath since urlRoot handles the full path
		// For root-level TOCs, use the full tocPath
		var navigationParentPath = parent is TableOfContentsNavigation ? tocRef.Source : tocPath;

		var tocNavigation = new TableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			navigationParentPath,
			parent,
			urlRoot,
			[]
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
					tocNavigation, // TOC navigation becomes the new URL root
					depth + 1,
					"", // Reset parentPath since TOC is new urlRoot - children paths are relative to this TOC
					parent is not TableOfContentsNavigation && allowNestedToc // Only allow nested TOCs if this TOC's parent is NOT a TOC
				);

				if (childNav != null)
					children.Add(childNav);
			}
		}

		// Then, process items from tocRef.Children
		foreach (var child in tocRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child,
				childIndex++,
				context,
				tocNavigation,
				root,
				tocNavigation, // TOC navigation becomes the new URL root
				depth + 1,
				"", // Reset parentPath since TOC is new urlRoot - children paths are relative to this TOC
				parent is not TableOfContentsNavigation && allowNestedToc // Only allow nested TOCs if this TOC's parent is NOT a TOC
			);

			if (childNav != null)
				children.Add(childNav);
		}

		// If no children, add a placeholder
		if (children.Count == 0)
		{
			var placeholderModel = new CrossLinkModel(new Uri(tocRef.Source, UriKind.Relative), tocRef.Source);
			children.Add(new FileNavigationLeaf(placeholderModel, tocRef.Source, false, tocNavigation, root, tocNavigation));
		}

		var finalTocNavigation = new TableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			navigationParentPath,
			parent,
			urlRoot,
			children
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

public class FolderNavigation : INodeNavigationItem<IDocumentationFile, INavigationItem>
{
	private readonly string _folderPath;
	private readonly IRootNavigationItem<INavigationModel, INavigationItem> _urlRoot;

	public FolderNavigation(
		int depth,
		string parentPath,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot,
		IRootNavigationItem<INavigationModel, INavigationItem> urlRoot,
		IReadOnlyCollection<INavigationItem> navigationItems
	)
	{
		_folderPath = parentPath;
		_urlRoot = urlRoot;
		NavigationItems = navigationItems;
		NavigationRoot = navigationRoot;
		Parent = parent;
		var title = navigationItems.FirstOrDefault()?.NavigationTitle ?? parentPath;
		Index = new DocumentationDirectory(title);
		Depth = depth;
		Hidden = false;
		IsCrossLink = false;
		Id = ShortId.Create(parentPath);
	}

	/// <inheritdoc />
	public string Url
	{
		get
		{
			// Check if there's an index file among the children
			var hasIndexChild = NavigationItems.Any(item =>
				item is FileNavigationLeaf &&
				item.NavigationTitle.Equals("index", StringComparison.OrdinalIgnoreCase));

			// If no index child exists, use the first child's URL
			if (!hasIndexChild && NavigationItems.Count > 0)
				return NavigationItems.First().Url;

			// Otherwise, use the folder path
			var rootUrl = _urlRoot.Url.TrimEnd('/');
			return string.IsNullOrEmpty(rootUrl) ? $"/{_folderPath}" : $"{rootUrl}/{_folderPath}";
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

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }
}

public class FileNavigation(
	CrossLinkModel model,
	string relativePath,
	bool hidden,
	int depth,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent,
	IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot,
	IRootNavigationItem<INavigationModel, INavigationItem> urlRoot,
	IReadOnlyCollection<INavigationItem> navigationItems
) : INodeNavigationItem<IDocumentationFile, INavigationItem>
{
	/// <inheritdoc />
	public string Url
	{
		get
		{
			var rootUrl = urlRoot.Url.TrimEnd('/');
			// Remove extension while preserving directory path
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
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; init; } = navigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden { get; init; } = hidden;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink { get; }

	/// <inheritdoc />
	public int Depth { get; init; } = depth;

	/// <inheritdoc />
	public string Id { get; } = ShortId.Create(relativePath);

	/// <inheritdoc />
	public IDocumentationFile Index { get; init; } = model;

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; init; } = navigationItems;
}

public class TableOfContentsNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>
{
	public TableOfContentsNavigation(
		IDirectoryInfo tableOfContentsDirectory,
		int depth,
		string parentPath,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> urlRoot,
		IReadOnlyCollection<INavigationItem> navigationItems
	)
	{
		TableOfContentsDirectory = tableOfContentsDirectory;
		NavigationItems = navigationItems;
		Parent = parent;
		UrlRoot = urlRoot;
		var title = navigationItems.FirstOrDefault()?.NavigationTitle ?? parentPath;
		Index = new DocumentationDirectory(title);
		NavigationRoot = this;
		Hidden = false;
		IsUsingNavigationDropdown = false;
		IsCrossLink = false;
		Id = ShortId.Create(parentPath);
		Depth = depth;
		ParentPath = parentPath;
	}

	/// <inheritdoc />
	public virtual string Url
	{
		get
		{
			var rootUrl = UrlRoot.Url.TrimEnd('/');
			return string.IsNullOrEmpty(rootUrl) ? $"/{ParentPath}" : $"{rootUrl}/{ParentPath}";
		}
	}

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	public IRootNavigationItem<INavigationModel, INavigationItem> UrlRoot { get; }

	/// <inheritdoc />
	public bool Hidden { get; }

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink { get; }

	/// <inheritdoc />
	public int Depth { get; }

	public string ParentPath { get; }

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public IDocumentationFile Index { get; }

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown { get; }

	public IDirectoryInfo TableOfContentsDirectory { get; }

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }
}

public record CrossLinkModel(Uri CrossLinkUri, string NavigationTitle) : IDocumentationFile;

public class CrossLinkNavigationLeaf(
	CrossLinkModel model,
	string url,
	bool hidden,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent,
	IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot
) : ILeafNavigationItem<CrossLinkModel>
{
	/// <inheritdoc />
	public CrossLinkModel Model { get; init; } = model;

	/// <inheritdoc />
	public string Url { get; init; } = url;

	/// <inheritdoc />
	public bool Hidden { get; init; } = hidden;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; init; } = navigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public string NavigationTitle => Model.NavigationTitle;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink => true;

}

public class FileNavigationLeaf(
	CrossLinkModel model,
	string relativePath,
	bool hidden,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent,
	IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot,
	IRootNavigationItem<INavigationModel, INavigationItem> urlRoot
)
	: ILeafNavigationItem<IDocumentationFile>
{
	/// <inheritdoc />
	public IDocumentationFile Model { get; init; } = model;

	/// <inheritdoc />
	public string Url
	{
		get
		{
			var rootUrl = urlRoot.Url.TrimEnd('/');
			// Remove extension while preserving directory path
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
	public bool Hidden { get; init; } = hidden;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; init; } = navigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public string NavigationTitle => Model.NavigationTitle;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink { get; }
}

