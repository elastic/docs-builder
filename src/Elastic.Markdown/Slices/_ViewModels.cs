// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Myst.FrontMatter;

namespace Elastic.Markdown.Slices;

public interface IBreadCrumbModel
{
	MarkdownFile[] Parents { get; }
	string? UrlPathPrefix { get; }
}

public interface ITableOfContentsModel
{
	IReadOnlyCollection<PageTocItem> PageTocItems { get; }
	string? GithubEditUrl { get; }
}

public class MainViewModel : IBreadCrumbModel, ITableOfContentsModel
{
	public required string Title { get; init; }
	public required string TitleRaw { get; init; }
	public required string MarkdownHtml { get; init; }
	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
	public required MarkdownFile? PreviousDocument { get; init; }
	public required MarkdownFile? NextDocument { get; init; }
	public required string? UrlPathPrefix { get; init; }
	public required string? GithubEditUrl { get; init; }
	public required ApplicableTo? Applies { get; init; }
	public required bool AllowIndexing { get; init; }

	private MarkdownFile[]? _parents;
	public MarkdownFile[] Parents
	{
		get
		{
			if (_parents is not null)
				return _parents;

			_parents = [.. CurrentDocument.YieldParents()];
			return _parents;
		}
	}

}

public class IndexViewModel
{
	public required string Title { get; init; }
	public required string TitleRaw { get; init; }
	public required string MarkdownHtml { get; init; }
	public required DocumentationGroup Tree { get; init; }
	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
	public required MarkdownFile? PreviousDocument { get; init; }
	public required MarkdownFile? NextDocument { get; init; }
	public required string NavigationHtml { get; init; }
	public required string? UrlPathPrefix { get; init; }
	public required string? GithubEditUrl { get; init; }
	public required ApplicableTo? Applies { get; init; }
	public required bool AllowIndexing { get; init; }
}

public class LayoutViewModel : IBreadCrumbModel, ITableOfContentsModel
{
	/// Used to identify the navigation for the current compilation
	/// We want to reset users sessionStorage every time this changes to invalidate
	/// the guids that no longer exist
	public static string CurrentNavigationId { get; } = Guid.NewGuid().ToString("N")[..8];
	public string Title { get; set; } = "Elastic Documentation";
	public string RawTitle { get; set; } = "Elastic Documentation";
	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }
	public required DocumentationGroup Tree { get; init; }
	public string[] ParentIds => [.. CurrentDocument.YieldParentGroups()];
	public required MarkdownFile CurrentDocument { get; init; }
	public required MarkdownFile? Previous { get; init; }
	public required MarkdownFile? Next { get; init; }
	public required string NavigationHtml { get; set; }
	public required string? UrlPathPrefix { get; set; }
	public required string? GithubEditUrl { get; set; }
	public required bool AllowIndexing { get; init; }

	private MarkdownFile[]? _parents;
	public MarkdownFile[] Parents
	{
		get
		{
			if (_parents is not null)
				return _parents;

			_parents = [.. CurrentDocument.YieldParents()];
			return _parents;
		}
	}

	public string Static(string path)
	{
		path = $"_static/{path.TrimStart('/')}";
		return $"{UrlPathPrefix}/{path}";
	}

	public string Link(string path)
	{
		path = path.TrimStart('/');
		return $"{UrlPathPrefix}/{path}";
	}
}

public class PageTocItem
{
	public required string Heading { get; init; }
	public required string Slug { get; init; }
	public required int Level { get; init; }
}


public class NavigationViewModel
{
	public required DocumentationGroup Tree { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
}

public class NavigationTreeItem
{
	public required int Level { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
	public required DocumentationGroup SubTree { get; init; }
}
