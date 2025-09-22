// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Documentation.Assembler;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;

namespace Documentation.Assembler.Navigation;

/// <summary>
/// Generates enhanced navigation sections for the llms.txt file
/// </summary>
public class LlmsNavigationEnhancer
{
	public async Task<string> GenerateNavigationSectionsAsync(GlobalNavigation navigation)
	{
		var content = new StringBuilder();

		// Get top-level navigation items (excluding hidden ones)
		var topLevelItems = navigation.TopLevelItems.Where(item => !item.Hidden).ToList();

		foreach (var topLevelItem in topLevelItems)
		{
			if (topLevelItem is not DocumentationGroup group)
				continue;

			// Create H2 section for the category
			var categoryTitle = GetCategoryDisplayName(group.NavigationTitle);
			_ = content.AppendLine($"## {categoryTitle}");
			_ = content.AppendLine();

			// Get first-level children
			var firstLevelChildren = GetFirstLevelChildren(group);

			if (firstLevelChildren.Count > 0)
			{
				foreach (var child in firstLevelChildren)
				{
					var title = child.NavigationTitle;
					var url = ConvertToAbsoluteMarkdownUrl(child.Url);
					var description = await GetDescriptionAsync(child);

					_ = !string.IsNullOrEmpty(description)
						? content.AppendLine($"* [{title}]({url}): {description}")
						: content.AppendLine($"* [{title}]({url})");
				}
				_ = content.AppendLine();
			}
		}

		return content.ToString();
	}

	private static string GetCategoryDisplayName(string navigationTitle) =>
		// Convert navigation titles to display names
		navigationTitle switch
		{
			"Get started" => "Get started",
			"Solutions" => "Solutions",
			"Manage data" => "Manage data",
			"Explore and analyze" => "Explore and analyze",
			"Deploy and manage" => "Deploy and manage",
			"Manage your Cloud account and preferences" => "Manage your Cloud account",
			"Troubleshoot" => "Troubleshoot",
			"Extend and contribute" => "Extend and contribute",
			"Release notes" => "Release notes",
			"Reference" => "Reference",
			_ => navigationTitle
		};

	private static List<INavigationItem> GetFirstLevelChildren(DocumentationGroup group)
	{
		var children = new List<INavigationItem>();

		foreach (var item in group.NavigationItems)
		{
			// Only include non-hidden items
			if (item.Hidden)
				continue;

			// Add the item to our list
			children.Add(item);
		}

		return children;
	}

	private static string ConvertToAbsoluteMarkdownUrl(string url)
	{
		// Convert HTML URLs to .md URLs for LLM consumption
		// e.g., "/docs/solutions/search/" -> "https://www.elastic.co/docs/solutions/search.md"
		var cleanUrl = url.TrimStart('/');

		// Remove "docs/" prefix if present for the markdown filename
		var markdownPath = cleanUrl;
		if (markdownPath.StartsWith("docs/"))
			markdownPath = markdownPath.Substring(5);

		// Convert directory URLs to .md files
		if (markdownPath.EndsWith('/'))
			markdownPath = markdownPath.TrimEnd('/') + ".md";
		else if (!markdownPath.EndsWith(".md"))
			markdownPath += ".md";

		// Make absolute URL using the canonical base URL (always https://www.elastic.co for production)
		var baseUrl = "https://www.elastic.co";
		return $"{baseUrl}/docs/{markdownPath}";
	}

	private static async Task<string?> GetDescriptionAsync(INavigationItem navigationItem)
	{
		var descriptionGenerator = new DescriptionGenerator();

		return navigationItem switch
		{
			// For file navigation items, extract from frontmatter or generate
			FileNavigationItem fileItem when fileItem.Model is MarkdownFile markdownFile =>
				await GetDescriptionFromMarkdownFileAsync(markdownFile, descriptionGenerator),

			// For documentation groups, try to get from index file
			DocumentationGroup group when group.Index is MarkdownFile indexFile =>
				await GetDescriptionFromMarkdownFileAsync(indexFile, descriptionGenerator),

			_ => null
		};
	}

	private static async Task<string?> GetDescriptionFromMarkdownFileAsync(MarkdownFile markdownFile, DescriptionGenerator descriptionGenerator)
	{
		// First try frontmatter description
		if (!string.IsNullOrEmpty(markdownFile.YamlFrontMatter?.Description))
			return markdownFile.YamlFrontMatter.Description;

		// Fallback to generating description from content
		try
		{
			var document = await markdownFile.MinimalParseAsync(default);
			return descriptionGenerator.GenerateDescription(document);
		}
		catch
		{
			// If parsing fails, return null (no description)
			return null;
		}
	}
}
