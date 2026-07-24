// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using System.Text.Json;
using Elastic.Documentation;
using Elastic.Documentation.Navigation;
using Elastic.Markdown.IO;
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
			_index = new PagefindIndex(new PagefindIndexOptions { Language = "en" }, _fileSystem);
		}

		var file = fileContext.SourceFile;
		var navigation = fileContext.PositionaNavigation;
		var currentNavigation = navigation.GetNavigationFor(file);
		var url = currentNavigation.Url;

		if (url is "/docs" or "/docs/404")
			return ValueTask.FromResult(true);

		var h1 = fileContext.Document.Descendants<HeadingBlock>().FirstOrDefault(h => h.Level == 1);
		if (h1 is not null)
			_ = fileContext.Document.Remove(h1);

		var body = PlainTextExporter.ConvertToPlainText(fileContext.Document, fileContext.BuildContext);

		var headings = fileContext.Document.Descendants<HeadingBlock>()
			.Select(h => h.GetData("header") as string ?? string.Empty)
			.Where(text => !string.IsNullOrEmpty(text))
			.ToArray();

		var parents = navigation.GetParentsOfMarkdownFile(file).Reverse().ToArray();
		var breadcrumbsMeta = BuildBreadcrumbsMeta(parents, fileContext.BuildContext.CanonicalBaseUrl);

		var segments = new List<WeightedSegment>();
		if (!string.IsNullOrEmpty(file.Title))
			segments.Add(new WeightedSegment(file.Title, Weight: 7));
		foreach (var heading in headings)
			segments.Add(new WeightedSegment(heading, Weight: 4));
		if (!string.IsNullOrEmpty(body))
			segments.Add(new WeightedSegment(body, Weight: 1));

		var meta = new Dictionary<string, string> { ["title"] = file.Title ?? url };
		if (!string.IsNullOrEmpty(breadcrumbsMeta))
			meta["breadcrumbs"] = breadcrumbsMeta;

		try
		{
			_index.AddRecord(new PagefindRecord
			{
				Url = url,
				Title = file.Title ?? url,
				Content = body,
				WeightedSegments = segments,
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
