// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;
using Microsoft.Extensions.Logging;

namespace Elastic.SiteSearch.Cli.LabsCrawl;

/// <summary>Vendored crawl→extract→index loop (logging instead of diagnostics channel).</summary>
public class LabsCrawlLoop(
	IAdaptiveCrawler crawler,
	IDocumentExtractor<LabsDocument> extractor,
	IDocumentExporter<LabsDocument> exporter,
	ILogger<LabsCrawlLoop> logger
)
{
	public Action<string, int>? OnUrlCrawled { get; set; }
	public Action<string, string>? OnUrlSkipped { get; set; }
	public Action<string, string>? OnUrlFailed { get; set; }
	public Action<string, string>? OnFatalError { get; set; }
	public Action? OnUrlIndexed { get; set; }
	public Action<string>? OnUrlUnavailable { get; set; }
	public Action<string, string>? OnIndexingError { get; set; }

	public async Task RunAsync(IReadOnlyList<CrawlDecision> decisions, CancellationToken ct)
	{
		await foreach (var result in crawler.CrawlAsync(decisions, ct))
		{
			if (ct.IsCancellationRequested)
				break;

			if (result.NotModified)
			{
				OnUrlSkipped?.Invoke(result.Url, "Not modified (304)");
				continue;
			}

			if (!result.Success)
			{
				if (result.FatalError)
				{
					OnFatalError?.Invoke(result.Url, result.Error ?? "Fatal error");
					logger.LogError("Fatal crawl error at {Url}: {Error}", result.Url, result.Error);
					break;
				}

				if (result.StatusCode == 404)
				{
					OnUrlUnavailable?.Invoke(result.Url);
					continue;
				}

				OnUrlFailed?.Invoke(result.Url, result.Error ?? "Unknown error");
				logger.LogWarning("Failed to crawl {Url}: {Error}", result.Url, result.Error);
				continue;
			}

			OnUrlCrawled?.Invoke(result.Url, result.Content?.Length ?? 0);

			try
			{
				var document = await extractor.ExtractAsync(result, ct);

				if (document is null)
				{
					OnUrlSkipped?.Invoke(result.Url, "Failed to extract");
					logger.LogWarning("Failed to extract document from HTML: {Url}", result.Url);
					continue;
				}

				await exporter.ExportAsync(document, ct);
				OnUrlIndexed?.Invoke();
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				OnIndexingError?.Invoke(result.Url, ex.Message);
				logger.LogError(ex, "Failed to index {Url}", result.Url);
			}
		}
	}
}
