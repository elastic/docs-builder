// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.V2;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Markdown;
using Elastic.Markdown.Page;
using Microsoft.Extensions.Logging;
using RazorSlices;

namespace Elastic.Documentation.Assembler.Building;

/// <summary>
/// Generates stub HTML pages for all <see cref="PlaceholderNavigationLeaf"/> and
/// <see cref="PlaceholderNavigationNode"/> items in the V2 navigation tree.
/// Each stub renders the full site chrome (header, nav sidebar, footer) with an H1
/// title and "coming soon" body, using the computed placeholder URL.
/// </summary>
public class PlaceholderPageWriter(
	ILoggerFactory logFactory,
	SiteNavigationV2 navigation,
	GlobalNavigationHtmlWriter htmlWriter,
	AssembleContext context,
	FeatureFlags featureFlags,
	StaticFileContentHashProvider staticFileHashProvider
)
{
	private readonly ILogger _logger = logFactory.CreateLogger<PlaceholderPageWriter>();

	public async Task WriteAllAsync(Cancel ctx)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var placeholders = CollectPlaceholders(navigation.V2NavigationItems).ToList();

		// Group placeholders by section so we render each section's nav once
		var groups = placeholders.GroupBy(p => navigation.GetSectionForUrl(p.Item.Url));
		foreach (var group in groups)
		{
			var section = group.Key;
			// Use the first placeholder in the group to resolve the section's nav HTML
			var representative = group.First().Item;
			var renderResult = await htmlWriter.RenderNavigation(navigation, representative, ctx);

			foreach (var (title, navItem) in group)
			{
				if (!seen.Add(navItem.Url))
					continue;
				await WritePlaceholderPageAsync(title, navItem, renderResult, ctx);
			}
		}

		_logger.LogInformation("Wrote {Count} placeholder pages", seen.Count);
	}

	private static IEnumerable<(string Title, ILeafNavigationItem<INavigationModel> Item)> CollectPlaceholders(
		IReadOnlyList<INavigationItem> items
	)
	{
		foreach (var item in items)
		{
			switch (item)
			{
				case PlaceholderNavigationLeaf leaf:
					yield return (leaf.NavigationTitle, leaf);
					break;
				case PlaceholderNavigationNode group:
					yield return (group.NavigationTitle, group.Index);
					foreach (var child in CollectPlaceholders(group.NavigationItems.ToList()))
						yield return child;
					break;
				case LabelNavigationNode label:
					foreach (var child in CollectPlaceholders(label.NavigationItems.ToList()))
						yield return child;
					break;
				case INodeNavigationItem<INavigationModel, INavigationItem> node:
					foreach (var child in CollectPlaceholders(node.NavigationItems.ToList()))
						yield return child;
					break;
			}
		}
	}

	private async Task WritePlaceholderPageAsync(
		string title,
		ILeafNavigationItem<INavigationModel> navItem,
		NavigationRenderResult renderResult,
		Cancel ctx
	)
	{
		var model = CreateViewModel(title, navItem, renderResult);
		var slice = PlaceholderPage.Create(model);
		var html = await slice.RenderAsync(cancellationToken: ctx);

		// item.Url is like "/docs/_placeholder/AABBCCDD" — write to outputDir + url + /index.html
		var relativePath = navItem.Url.TrimStart('/');
		var outputPath = Path.Combine(context.OutputDirectory.FullName, relativePath, "index.html");
		var outputDir = Path.GetDirectoryName(outputPath)!;
		if (!context.WriteFileSystem.Directory.Exists(outputDir))
			_ = context.WriteFileSystem.Directory.CreateDirectory(outputDir);

		await context.WriteFileSystem.File.WriteAllTextAsync(outputPath, html, ctx);
	}

	private MarkdownLayoutViewModel CreateViewModel(
		string title,
		ILeafNavigationItem<INavigationModel> navItem,
		NavigationRenderResult renderResult
	)
	{
		var env = context.Environment;
		var rawPrefix = env.PathPrefix;
		var urlPathPrefix = string.IsNullOrEmpty(rawPrefix) ? string.Empty : $"/{rawPrefix.TrimStart('/')}";
		var rootPath = string.IsNullOrEmpty(urlPathPrefix) ? "/" : $"{urlPathPrefix.TrimEnd('/')}/";

		return new MarkdownLayoutViewModel
		{
			DocsBuilderVersion = ShortId.Create(BuildContext.Version),
			DocSetName = "Elastic Docs",
			Title = title,
			Description = string.Empty,
			CurrentNavigationItem = navItem,
			Previous = null,
			Next = null,
			NavigationHtml = renderResult.Html,
			NavV2Sections = renderResult.Sections,
			ActiveSectionId = renderResult.ActiveSectionId,
			UrlPathPrefix = urlPathPrefix,
			Htmx = new DefaultHtmxAttributeProvider(rootPath),
			CanonicalBaseUrl = context.CanonicalBaseUrl,
			Features = featureFlags,
			GoogleTagManager = new GoogleTagManagerConfiguration
			{
				Enabled = env.GoogleTagManager.Enabled,
				Id = env.GoogleTagManager.Id,
				Auth = env.GoogleTagManager.Auth,
				Preview = env.GoogleTagManager.Preview,
				CookiesWin = env.GoogleTagManager.CookiesWin
			},
			Optimizely = new OptimizelyConfiguration
			{
				Enabled = env.Optimizely.Enabled,
				Id = env.Optimizely.Id
			},
			AllowIndexing = false,
			StaticFileContentHashProvider = staticFileHashProvider,
			BuildType = BuildType.Assembler,
			GithubEditUrl = null,
			MarkdownUrl = string.Empty,
			HideEditThisPage = true,
			ReportIssueUrl = null,
			Breadcrumbs = [],
			PageTocItems = [],
			Layout = null,
			VersioningSystem = context.VersionsConfiguration.GetVersioningSystem(VersioningSystemId.All),
			VersionDropdownSerializedModel = null,
			CurrentVersion = null,
			AllVersionsUrl = null
		};
	}
}
