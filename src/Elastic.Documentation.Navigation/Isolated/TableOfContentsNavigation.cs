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
				root: this,
				depth: 0,
				parentPath: "",
				allowNestedToc: true
			);

			if (navItem != null)
				items.Add(navItem);
		}

		NavigationItems = items;
	}

	/// <inheritdoc />
	public string Url => NavigationItems.FirstOrDefault()?.Url ?? "/";

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
		int depth,
		string parentPath,
		bool allowNestedToc = true)
	{
		// Validate TableOfContentsNavigation children
		if (parent is TableOfContentsNavigation)
		{
			if (!allowNestedToc)
			{
				// When nested TOC is not allowed, any child is an error
				context.EmitError(
					context.ConfigurationPath,
					$"TableOfContents navigation does not allow nested children, found: {tocItem.GetType().Name}"
				);
			}
			else if (tocItem is not TableOfContentsRef)
			{
				// When nested TOC is allowed, only TableOfContentsRef children are permitted
				context.EmitError(
					context.ConfigurationPath,
					$"TableOfContents navigation may only contain other TOC references as children, found: {tocItem.GetType().Name}"
				);
			}
		}

		return tocItem switch
		{
			FileRef fileRef => CreateFileNavigation(fileRef, index, context, parent, root),
			CrossLinkRef crossLinkRef => CreateCrossLinkNavigation(crossLinkRef, index, parent, root),
			FolderRef folderRef => CreateFolderNavigation(folderRef, index, context, parent, root, depth, parentPath, allowNestedToc),
			TableOfContentsRef tocRef => CreateTocNavigation(tocRef, index, context, parent, root, depth, parentPath, allowNestedToc),
			_ => null
		};
	}

	private INavigationItem CreateFileNavigation(
		FileRef fileRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root)
	{
		// Extract title from file path
		var title = context.ReadFileSystem.Path.GetFileNameWithoutExtension(fileRef.RelativePath);

		// Create model
		var model = new CrossLinkModel(new Uri(fileRef.RelativePath, UriKind.Relative), title);

		// Construct URL (convert .md to .html)
		var url = $"/{fileRef.RelativePath.Replace(".md", ".html", StringComparison.OrdinalIgnoreCase)}";

		return new FileNavigationLeaf(model, url, fileRef.Hidden, parent, root)
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
		var title = crossLinkRef.Title ?? crossLinkRef.CrossLinkUri.ToString();
		var model = new CrossLinkModel(crossLinkRef.CrossLinkUri, title);

		return new CrossLinkNavigationLeaf(
			model,
			crossLinkRef.CrossLinkUri.ToString(),
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
		int depth,
		string parentPath,
		bool allowNestedToc)
	{
		var folderPath = string.IsNullOrEmpty(parentPath)
			? folderRef.RelativePath
			: $"{parentPath}/{folderRef.RelativePath}";

		// Convert children first
		var children = new List<INavigationItem>();
		var childIndex = 0;

		var folderNavigation = new FolderNavigation(
			depth + 1,
			folderPath,
			parent,
			root,
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
				depth + 1,
				folderPath,
				allowNestedToc
			);

			if (childNav != null)
				children.Add(childNav);
		}

		// Create folder navigation with actual children
		return new FolderNavigation(depth + 1, folderPath, parent, root, children)
		{
			NavigationIndex = index
		};
	}

	private INavigationItem CreateTocNavigation(
		TableOfContentsRef tocRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		int depth,
		string parentPath,
		bool allowNestedToc)
	{
		var tocPath = string.IsNullOrEmpty(parentPath)
			? tocRef.Source
			: $"{parentPath}/{tocRef.Source}";

		// Resolve the TOC directory
		var tocDirectory = context.ReadFileSystem.DirectoryInfo.New(
			context.ReadFileSystem.Path.Combine(context.DocumentationSourceDirectory.FullName, tocPath)
		);

		// Create the TOC navigation that will be the parent for children
		var tocNavigation = new TableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			tocPath,
			parent,
			[]
		);

		// Convert children
		var children = new List<INavigationItem>();
		var childIndex = 0;

		foreach (var child in tocRef.Children)
		{
			var childNav = ConvertToNavigationItem(
				child,
				childIndex++,
				context,
				tocNavigation,
				root,
				depth + 1,
				tocPath,
				allowNestedToc
			);

			if (childNav != null)
				children.Add(childNav);
		}

		// If no children, add a placeholder
		if (children.Count == 0)
		{
			var placeholderModel = new CrossLinkModel(new Uri(tocRef.Source, UriKind.Relative), tocRef.Source);
			children.Add(new FileNavigationLeaf(placeholderModel, $"/{tocRef.Source}/", false, tocNavigation, root));
		}

		return new TableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			tocPath,
			parent,
			children
		)
		{
			NavigationIndex = index
		};
	}
}

public class FolderNavigation : INodeNavigationItem<IDocumentationFile, INavigationItem>
{
	public FolderNavigation(
		int depth,
		string parentPath,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot,
		IReadOnlyCollection<INavigationItem> navigationItems
	)
	{
		if (navigationItems.Count == 0)
			throw new ArgumentException("NavigationItems must contain at least one item", nameof(navigationItems));
		NavigationItems = navigationItems;
		NavigationRoot = navigationRoot;
		Parent = parent;
		Index = new DocumentationDirectory(navigationItems.First().NavigationTitle);
		Depth = depth;
		Hidden = false;
		IsCrossLink = false;
		Id = ShortId.Create(parentPath);
	}

	/// <inheritdoc />
	public string Url => NavigationItems.First().Url;

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

public class TableOfContentsNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>
{
	public TableOfContentsNavigation(
		IDirectoryInfo tableOfContentsDirectory,
		int depth,
		string parentPath,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IReadOnlyCollection<INavigationItem> navigationItems
	)
	{
		if (navigationItems.Count == 0)
			throw new ArgumentException("NavigationItems must contain at least one item", nameof(navigationItems));
		TableOfContentsDirectory = tableOfContentsDirectory;
		NavigationItems = navigationItems;
		Parent = parent;
		Index = new DocumentationDirectory(navigationItems.First().NavigationTitle);
		NavigationRoot = this;
		Hidden = false;
		IsUsingNavigationDropdown = false;
		IsCrossLink = false;
		Id = ShortId.Create(parentPath);
		Depth = depth;
	}

	/// <inheritdoc />
	public string Url => NavigationItems.First().Url;

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
	string url,
	bool hidden,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent,
	IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot
)
	: ILeafNavigationItem<IDocumentationFile>
{
	/// <inheritdoc />
	public IDocumentationFile Model { get; init; } = model;

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
	public bool IsCrossLink { get; }
}

