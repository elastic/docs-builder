// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using System.Text.Json;
using Elastic.Documentation;
using Elastic.Documentation.Navigation;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.InlineParsers;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;
using Pagefind.Net;
using Pagefind.Net.Frontend;

namespace Elastic.Markdown.Exporters.Pagefind;

public sealed class PagefindMarkdownExporter(ILoggerFactory logFactory) : IMarkdownExporter
{
	private readonly ILogger _logger = logFactory.CreateLogger<PagefindMarkdownExporter>();

	private PagefindIndex? _index;
	private IFileSystem? _fileSystem;
	private int _indexed;

	public ValueTask StartAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask StopAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		// Pagefind static search is only relevant for isolated builds and serve mode;
		// assembler and codex builds use the Elasticsearch-backed search API.
		if (fileContext.BuildContext.BuildType != BuildType.Isolated)
			return ValueTask.FromResult(true);

		if (_index is null)
		{
			_fileSystem = fileContext.BuildContext.WriteFileSystem;
			_index = new PagefindIndex(new PagefindIndexOptions { Language = "en", IndexedMetaFields = ["title"] }, _fileSystem);
		}

		var file = fileContext.SourceFile;
		var navigation = fileContext.PositionaNavigation;
		var currentNavigation = navigation.GetNavigationFor(file);
		var url = currentNavigation.Url;

		if (url is "/docs" or "/docs/404")
			return ValueTask.FromResult(true);

		var sections = BuildHtmlSections(fileContext);

		var parents = navigation.GetParentsOfMarkdownFile(file).Reverse().ToArray();
		var breadcrumbsMeta = BuildBreadcrumbsMeta(parents, fileContext.BuildContext.CanonicalBaseUrl);

		var meta = new Dictionary<string, string> { ["title"] = file.Title ?? url };
		if (!string.IsNullOrEmpty(breadcrumbsMeta))
			meta["breadcrumbs"] = breadcrumbsMeta;

		try
		{
			_index.AddHtmlRecord(new HtmlPageData
			{
				Url = url,
				Sections = sections,
				Meta = meta
			});
			_indexed++;
		}
		catch (PagefindIndexingException ex)
		{
			_logger.LogWarning(ex, "Failed to index {Url} for static search, skipping", ex.Url);
		}

		return ValueTask.FromResult(true);
	}

	private const int MaxHeadings = 10;
	private const int MaxBodySections = 5;

	private static List<HtmlSection> BuildHtmlSections(MarkdownExportFileContext fileContext)
	{
		var sections = new List<HtmlSection>();
		var file = fileContext.SourceFile;

		if (!string.IsNullOrEmpty(file.Title))
			sections.Add(new HtmlSection("h1", file.Title));

		var headingCount = 0;
		var bodyCount = 0;
		var currentBodyLines = new List<string>();

		foreach (var block in fileContext.Document)
		{
			if (block is HeadingBlock heading)
			{
				FlushBody(sections, currentBodyLines, ref bodyCount);
				if (headingCount >= MaxHeadings)
					continue;

				var text = heading.GetData("header") as string ?? string.Empty;
				if (!string.IsNullOrEmpty(text))
				{
					var anchor = heading.GetData("anchor") as string;
					var slugTarget = (anchor ?? text) ?? string.Empty;
					if (slugTarget.Contains('$'))
						slugTarget = HeadingAnchorParser.InlineAnchors().Replace(slugTarget, "");
					var id = slugTarget.Slugify();
					sections.Add(new HtmlSection($"h{heading.Level}", text, id));
					headingCount++;
				}
			}
			else
			{
				if (bodyCount >= MaxBodySections)
					continue;

				var text = PlainTextExporter.ConvertBlockToPlainText(block, fileContext.BuildContext);
				if (!string.IsNullOrEmpty(text))
					currentBodyLines.Add(text);
			}
		}

		FlushBody(sections, currentBodyLines, ref bodyCount);
		return sections;
	}

	private static void FlushBody(List<HtmlSection> sections, List<string> lines, ref int bodyCount)
	{
		if (lines.Count == 0)
			return;

		if (bodyCount < MaxBodySections)
		{
			sections.Add(new HtmlSection("p", string.Join(" ", lines)));
			bodyCount++;
		}
		lines.Clear();
	}

	public async ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx)
	{
		if (_index is null || _indexed == 0)
		{
			_logger.LogInformation("No pages to index for static search");
			return true;
		}

		_logger.LogInformation("Writing static search index for {Count} pages", _indexed);

		var staticDir = Path.Combine(outputFolder.FullName, "_static");
		await _index.WriteAsync(staticDir, ctx);
		var pagefindDir = Path.Combine(staticDir, "pagefind");
		_ = await PagefindFrontend.ExtractToAsync(
			_fileSystem!, pagefindDir, force: false, ctx);

		_logger.LogInformation("Generated static search index with {Count} pages", _indexed);
		return true;
	}

	private static string BuildBreadcrumbsMeta(IReadOnlyList<INavigationItem> parents, Uri? canonicalBaseUrl)
	{
		if (parents.Count == 0)
			return string.Empty;

		var baseUrl = canonicalBaseUrl?.ToString().TrimEnd('/') ?? string.Empty;
		var sb = new StringBuilder();
		_ = sb.Append("{\"itemListElement\":[");
		for (var i = 0; i < parents.Count; i++)
		{
			if (i > 0)
				_ = sb.Append(',');
			var title = JsonEncodedText.Encode(parents[i].NavigationTitle);
			var itemUrl = JsonEncodedText.Encode($"{baseUrl}{parents[i].Url}");
			_ = sb.Append($"{{\"name\":\"{title}\",\"item\":\"{itemUrl}\"}}");
		}
		_ = sb.Append("]}");
		return sb.ToString();
	}
}
