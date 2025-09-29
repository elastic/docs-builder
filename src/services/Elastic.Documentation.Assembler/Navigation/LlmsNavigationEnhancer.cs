// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Myst.Renderers.LlmMarkdown;

namespace Elastic.Documentation.Assembler.Navigation;

/// <summary>
/// Generates enhanced navigation sections for the llms.txt file
/// </summary>
public class LlmsNavigationEnhancer
{
	public string GenerateNavigationSections(GlobalNavigation navigation, Uri canonicalBaseUrl)
	{
		var content = new StringBuilder();

		// Get top-level navigation items (excluding hidden ones)
		var topLevelItems = navigation.TopLevelItems.Where(item => !item.Hidden).ToArray();

		foreach (var topLevelItem in topLevelItems)
		{
			if (topLevelItem is not DocumentationGroup group)
				continue;

			// Create H2 section for the category - use H1 title if available, fallback to navigation title
			var categoryTitle = GetBestTitle(group);
			_ = content.AppendLine(CultureInfo.InvariantCulture, $"## {categoryTitle}");
			_ = content.AppendLine();

			// Get first-level children
			var firstLevelChildren = GetFirstLevelChildren(group);

			if (firstLevelChildren.Any())
			{
				foreach (var child in firstLevelChildren)
				{
					var title = GetBestTitle(child);
					var url = LlmRenderingHelpers.MakeAbsoluteUrl(canonicalBaseUrl, child.Url);
					var description = GetDescription(child);

					_ = !string.IsNullOrEmpty(description)
						? content.AppendLine(CultureInfo.InvariantCulture, $"* [{title}]({url}): {description}")
						: content.AppendLine(CultureInfo.InvariantCulture, $"* [{title}]({url})");
				}
				_ = content.AppendLine();
			}
		}

		return content.ToString();
	}


	private static IEnumerable<INavigationItem> GetFirstLevelChildren(DocumentationGroup group) =>
		group.NavigationItems.Where(i => !i.Hidden);

	/// <summary>
	/// Gets the best title for a navigation item, preferring H1 content over navigation title
	/// </summary>
	private static string GetBestTitle(INavigationItem navigationItem) => navigationItem switch
	{
		// For file navigation items, prefer the H1 title from the markdown content
		FileNavigationItem fileItem when !string.IsNullOrEmpty(fileItem.Model.Title)
			=> fileItem.Model.Title,
		FileNavigationItem fileItem
			=> fileItem.NavigationTitle,

		// For documentation groups, prefer the H1 title from the index file
		DocumentationGroup group when !string.IsNullOrEmpty(group.Index?.Title)
			=> group.Index.Title,
		DocumentationGroup group
			=> group.NavigationTitle,

		// For other navigation item types, use the navigation title
		_ => navigationItem.NavigationTitle
	};

	private static string? GetDescription(INavigationItem navigationItem) => navigationItem switch
	{
		// For file navigation items, extract from frontmatter
		FileNavigationItem fileItem when fileItem.Model is MarkdownFile markdownFile
			=> markdownFile.YamlFrontMatter?.Description,

		// For documentation groups, try to get from index file
		DocumentationGroup group when group.Index is MarkdownFile indexFile
			=> indexFile.YamlFrontMatter?.Description,

		// For table of contents trees (inherits from DocumentationGroup, but handled explicitly)
		TableOfContentsTree tocTree when tocTree.Index is MarkdownFile indexFile
			=> indexFile.YamlFrontMatter?.Description,

		// Cross-repository links don't have descriptions in frontmatter
		CrossLinkNavigationItem => null,

		// API-related navigation items (these don't have markdown frontmatter)
		// Check by namespace to avoid direct assembly references
		INavigationItem item when item.GetType().FullName?.StartsWith("Elastic.ApiExplorer.", StringComparison.Ordinal) == true => null,

		// Throw exception for any unhandled navigation item types
		_ => throw new InvalidOperationException($"Unhandled navigation item type: {navigationItem.GetType().FullName}")
	};
}
