// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Renderers.LlmMarkdown;

namespace Elastic.Documentation.Assembler.Navigation;

/// <summary>
/// Generates enhanced navigation sections for the llms.txt file
/// </summary>
public class LlmsNavigationEnhancer
{
	public string GenerateNavigationSections(SiteNavigation navigation, Uri canonicalBaseUrl)
	{
		var content = new StringBuilder();

		// Get top-level navigation items (excluding hidden ones)
		var topLevelItems = navigation.TopLevelItems.Where(item => !item.Hidden).ToArray();

		foreach (var topLevelItem in topLevelItems)
		{
			if (topLevelItem is not { } group)
				continue;

			// Create H2 section for the category - use H1 title if available, fallback to navigation title
			var categoryTitle = GetBestTitle(group);
			_ = content.AppendLine(CultureInfo.InvariantCulture, $"## {categoryTitle}");
			_ = content.AppendLine();

			// Get first-level children
			var firstLevelChildren = GetFirstLevelChildren(group);

			if (firstLevelChildren.Count != 0)
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


	private static IReadOnlyCollection<INavigationItem> GetFirstLevelChildren(INodeNavigationItem<INavigationModel, INavigationItem> group) =>
		group.NavigationItems.Where(i => !i.Hidden).ToArray();

	/// <summary>
	/// Gets the best title for a navigation item, preferring H1 content over navigation title
	/// </summary>
	private static string GetBestTitle(INavigationItem navigationItem) => navigationItem switch
	{
		// For file navigation items, prefer the H1 title from the Markdown content
		ILeafNavigationItem<MarkdownFile> markdownNavigation =>
			markdownNavigation.Model.Title ?? markdownNavigation.NavigationTitle,

		// For documentation groups, try to get the full title of the index
		INodeNavigationItem<MarkdownFile, INavigationItem> markdownNodeNavigation =>
			markdownNodeNavigation.Index.Title ?? markdownNodeNavigation.NavigationTitle,

		// For other navigation item types, use the navigation title
		_ => navigationItem.NavigationTitle
	};

	private static string? GetDescription(INavigationItem navigationItem) => navigationItem switch
	{
		// Cross-repository links don't have descriptions in frontmatter
		ILeafNavigationItem<INavigationModel> { IsCrossLink: true } => null,

		// For file navigation items, extract from frontmatter
		ILeafNavigationItem<MarkdownFile> markdownNavigation =>
			markdownNavigation.Model.YamlFrontMatter?.Description,

		// For documentation groups, try to get from index file
		INodeNavigationItem<MarkdownFile, INavigationItem> markdownNodeNavigation =>
			markdownNodeNavigation.Index.YamlFrontMatter?.Description,

		// API-related navigation items (these don't have markdown frontmatter)
		// Check by namespace to avoid direct assembly references
		{ } item when item.GetType().FullName?.StartsWith("Elastic.ApiExplorer.", StringComparison.Ordinal) == true => null,

		// Throw exception for any unhandled navigation item types
		_ => throw new InvalidOperationException($"Unhandled navigation item type: {navigationItem.GetType().FullName}")
	};
}
