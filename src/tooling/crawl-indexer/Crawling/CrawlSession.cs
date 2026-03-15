// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CrawlIndexer.Html;
using CrawlIndexer.Indexing;
using Elastic.Documentation.Diagnostics;

namespace CrawlIndexer.Crawling;

/// <summary>
/// Encapsulates the crawl loop shared across site and guide commands.
/// Progress is reported via action callbacks so presentation stays in the command layer.
/// </summary>
public class CrawlSession<TDocument>(
	IAdaptiveCrawler crawler,
	IDocumentExtractor<TDocument> extractor,
	IDocumentExporter<TDocument> exporter,
	IDiagnosticsCollector diagnostics
)
{
	/// <summary>Invoked when a URL is successfully crawled. Args: url, byteCount.</summary>
	public Action<string, int>? OnUrlCrawled { get; set; }

	/// <summary>Invoked when a URL is skipped. Args: url, reason.</summary>
	public Action<string, string>? OnUrlSkipped { get; set; }

	/// <summary>Invoked when a URL fails to crawl. Args: url, error.</summary>
	public Action<string, string>? OnUrlFailed { get; set; }

	/// <summary>Invoked on a fatal crawl error that stops the loop. Args: url, error.</summary>
	public Action<string, string>? OnFatalError { get; set; }

	/// <summary>Invoked when a document is successfully indexed.</summary>
	public Action? OnUrlIndexed { get; set; }

	/// <summary>Invoked when a URL returns 404. Args: url.</summary>
	public Action<string>? OnUrlUnavailable { get; set; }

	/// <summary>Invoked on an indexing-level error. Args: url, error.</summary>
	public Action<string, string>? OnIndexingError { get; set; }

	/// <summary>
	/// Runs the crawl loop over the provided decisions, invoking callbacks for each outcome.
	/// </summary>
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
					diagnostics.EmitError(result.Url, $"Fatal error: {result.Error}");
					break;
				}

				if (result.StatusCode == 404)
				{
					OnUrlUnavailable?.Invoke(result.Url);
					continue;
				}

				OnUrlFailed?.Invoke(result.Url, result.Error ?? "Unknown error");
				diagnostics.EmitError(result.Url, $"Failed to crawl: {result.Error}");
				continue;
			}

			OnUrlCrawled?.Invoke(result.Url, result.Content?.Length ?? 0);

			try
			{
				var document = await extractor.ExtractAsync(result, ct);

				if (document is null)
				{
					OnUrlSkipped?.Invoke(result.Url, "Failed to extract");
					diagnostics.EmitWarning(result.Url, "Failed to extract document from HTML");
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
				diagnostics.EmitError(result.Url, $"Failed to index: {ex.Message}", ex);
			}
		}
	}
}
