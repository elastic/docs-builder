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

public class DocumentationSetNavigation : ISiteNavigation<IDocumentationFile>
{
	public DocumentationSetNavigation(DocumentationSetFile documentationSet, IDocumentationSetContext context)
	{
	}

	/// <inheritdoc />
	public IReadOnlyCollection<IDocumentationFile> NavigationItems { get; }
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

