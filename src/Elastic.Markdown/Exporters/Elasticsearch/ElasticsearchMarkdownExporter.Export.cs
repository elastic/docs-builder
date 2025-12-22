// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Elasticsearch;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Search;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Markdown.Exporters.Elasticsearch.Enrichment;
using Elastic.Markdown.Helpers;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;
using static System.StringSplitOptions;
using MarkdownParser = Markdig.Parsers.MarkdownParser;

namespace Elastic.Markdown.Exporters.Elasticsearch;

public partial class ElasticsearchMarkdownExporter
{
	/// <summary>
	/// Assigns hash, last updated, and batch index date to a documentation document.
	/// </summary>
	private void AssignDocumentMetadata(DocumentationDocument doc)
	{
		var semanticHash = _semanticChannel.Channel.ChannelHash;
		var lexicalHash = _lexicalChannel.Channel.ChannelHash;
		var hash = HashedBulkUpdate.CreateHash(semanticHash, lexicalHash,
			doc.Url, doc.Type, doc.StrippedBody ?? string.Empty, string.Join(",", doc.Headings.OrderBy(h => h)),
			doc.SearchTitle ?? string.Empty,
			doc.NavigationSection ?? string.Empty, doc.NavigationDepth.ToString("N0"),
			doc.NavigationTableOfContents.ToString("N0"),
			_fixedSynonymsHash
		);
		doc.Hash = hash;
		doc.LastUpdated = _batchIndexDate;
		doc.BatchIndexDate = _batchIndexDate;
	}

	private static void CommonEnrichments(DocumentationDocument doc, INavigationItem? navigationItem)
	{
		doc.SearchTitle = CreateSearchTitle();
		// if we have no navigation, initialize to 20 since rank_feature would score 0 too high
		doc.NavigationDepth = navigationItem?.NavigationDepth ?? 20;
		doc.NavigationTableOfContents = navigationItem switch
		{
			// release-notes get effectively flattened by product, so we to dampen its effect slightly
			IRootNavigationItem<INavigationModel, INavigationItem> when navigationItem.NavigationSection == "release notes" =>
				Math.Min(4 * doc.NavigationDepth, 48),
			IRootNavigationItem<INavigationModel, INavigationItem> => Math.Min(2 * doc.NavigationDepth, 48),
			INodeNavigationItem<INavigationModel, INavigationItem> => 50,
			_ => 100
		};
		doc.NavigationSection = navigationItem?.NavigationSection;
		if (doc.Type == "api")
			doc.NavigationSection = "api";

		// this section gets promoted in the navigation we don't want it to be promoted in the search results
		// e.g. `Use high-contrast mode in Kibana - ( docs cloud-account high contrast`
		if (doc.NavigationSection == "manage your cloud account and preferences")
			doc.NavigationDepth *= 2;

		string CreateSearchTitle()
		{
			// skip doc and the section
			var split = new[] { '/', ' ', '-', '.', '_' };
			var urlComponents = new HashSet<string>(
				doc.Url.Split('/', RemoveEmptyEntries).Skip(2)
					.SelectMany(c => c.Split(split, RemoveEmptyEntries)).ToArray()
			);
			var title = doc.Title;
			//skip tokens already part of the title we don't want to influence TF/IDF
			var tokensInTitle = new HashSet<string>(title.Split(split, RemoveEmptyEntries).Select(t => t.ToLowerInvariant()));
			return $"{doc.Title} - {string.Join(" ", urlComponents.Where(c => !tokensInTitle.Contains(c.ToLowerInvariant())))}";
		}
	}
	public async ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		var file = fileContext.SourceFile;
		var navigation = fileContext.PositionaNavigation;
		var currentNavigation = navigation.GetNavigationFor(file);
		var url = currentNavigation.Url;

		if (url is "/docs" or "/docs/404")
		{
			// Skip the root and 404 pages
			_logger.LogInformation("Skipping export for {Url}", url);
			return true;
		}

		// Remove the first h1 because we already have the title
		// and we don't want it to appear in the body
		var h1 = fileContext.Document.Descendants<HeadingBlock>().FirstOrDefault(h => h.Level == 1);
		if (h1 is not null)
			_ = fileContext.Document.Remove(h1);

		var body = LlmMarkdownExporter.ConvertToLlmMarkdown(fileContext.Document, fileContext.BuildContext);

		var headings = fileContext.Document.Descendants<HeadingBlock>()
			.Select(h => h.GetData("header") as string ?? string.Empty) // TODO: Confirm that 'header' data is correctly set for all HeadingBlock instances and that this extraction is reliable.
			.Where(text => !string.IsNullOrEmpty(text))
			.ToArray();

		var strippedBody = body.StripMarkdown();
		var @abstract = !string.IsNullOrEmpty(strippedBody)
			? strippedBody[..Math.Min(strippedBody.Length, 400)] + " " + string.Join(" \n- ", headings)
			: string.Empty;

		// this is temporary until https://github.com/elastic/docs-builder/pull/2070 lands
		// this PR will add a service for us to resolve to a versioning scheme.
		var appliesTo = fileContext.SourceFile.YamlFrontMatter?.AppliesTo ?? ApplicableTo.Default;

		var doc = new DocumentationDocument
		{
			Url = url,
			Title = file.Title,
			Type = "doc",
			SearchTitle = file.Title, //updated in CommonEnrichments
			Body = body,
			StrippedBody = strippedBody,
			Description = fileContext.SourceFile.YamlFrontMatter?.Description,
			Abstract = @abstract,
			Applies = appliesTo,
			Parents = navigation.GetParentsOfMarkdownFile(file).Select(i => new ParentDocument
			{
				Title = i.NavigationTitle,
				Url = i.Url
			}).Reverse().ToArray(),
			Headings = headings,
			Hidden = fileContext.NavigationItem.Hidden
		};

		CommonEnrichments(doc, currentNavigation);

		// AI Enrichment - hybrid approach:
		// - Cache hits: enrich processor applies fields at index time
		// - Cache misses: apply fields inline before indexing
		doc.ContentHash = ContentHashGenerator.Generate(doc.Title, doc.StrippedBody ?? string.Empty);
		await TryEnrichDocumentAsync(doc, ctx);

		AssignDocumentMetadata(doc);

		if (_indexStrategy == IngestStrategy.Multiplex)
			return await _lexicalChannel.TryWrite(doc, ctx) && await _semanticChannel.TryWrite(doc, ctx);
		return await _lexicalChannel.TryWrite(doc, ctx);
	}

	/// <inheritdoc />
	public async ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx)
	{

		// this is temporary; once we implement Elastic.ApiExplorer, this should flow through
		// we'll rename IMarkdownExporter to IDocumentationFileExporter at that point
		_logger.LogInformation("Exporting OpenAPI documentation to Elasticsearch");

		var exporter = new OpenApiDocumentExporter(_versionsConfiguration);

		await foreach (var doc in exporter.ExportDocuments(limitPerSource: null, ctx))
		{
			var document = MarkdownParser.Parse(doc.Body ?? string.Empty);

			doc.Body = LlmMarkdownExporter.ConvertToLlmMarkdown(document, _context);

			var headings = document.Descendants<HeadingBlock>()
				.Select(h => h.GetData("header") as string ?? string.Empty) // TODO: Confirm that 'header' data is correctly set for all HeadingBlock instances and that this extraction is reliable.
				.Where(text => !string.IsNullOrEmpty(text))
				.ToArray();

			doc.StrippedBody = doc.Body.StripMarkdown();
			var @abstract = !string.IsNullOrEmpty(doc.StrippedBody)
				? doc.Body[..Math.Min(doc.StrippedBody.Length, 400)] + " " + string.Join(" \n- ", doc.Headings)
				: string.Empty;
			doc.Abstract = @abstract;
			doc.Headings = headings;
			CommonEnrichments(doc, null);

			// AI Enrichment - hybrid approach
			doc.ContentHash = ContentHashGenerator.Generate(doc.Title, doc.StrippedBody ?? string.Empty);
			await TryEnrichDocumentAsync(doc, ctx);

			AssignDocumentMetadata(doc);

			// Write to channels following the multiplex or reindex strategy
			if (_indexStrategy == IngestStrategy.Multiplex)
			{
				if (!await _lexicalChannel.TryWrite(doc, ctx) || !await _semanticChannel.TryWrite(doc, ctx))
				{
					_logger.LogError("Failed to write OpenAPI document {Url}", doc.Url);
					return false;
				}
			}
			else
			{
				if (!await _lexicalChannel.TryWrite(doc, ctx))
				{
					_logger.LogError("Failed to write OpenAPI document {Url}", doc.Url);
					return false;
				}
			}
		}

		_logger.LogInformation("Finished exporting OpenAPI documentation");
		return true;
	}

	/// <summary>
	/// Hybrid AI enrichment: cache hits rely on enrich processor, cache misses apply fields inline.
	/// </summary>
	private async ValueTask TryEnrichDocumentAsync(DocumentationDocument doc, Cancel ctx)
	{
		if (_enrichmentCache is null || _llmClient is null || string.IsNullOrWhiteSpace(doc.ContentHash))
			return;

		// Check if enrichment exists in cache
		if (_enrichmentCache.Exists(doc.ContentHash))
		{
			// Cache hit - enrich processor will apply fields at index time
			_ = Interlocked.Increment(ref _cacheHitCount);
			return;
		}

		// Check if we've hit the limit for new enrichments
		var current = Interlocked.Increment(ref _newEnrichmentCount);
		if (current > _enrichmentOptions.MaxNewEnrichmentsPerRun)
		{
			_ = Interlocked.Decrement(ref _newEnrichmentCount);
			return;
		}

		// Cache miss - generate enrichment inline and apply directly
		try
		{
			var enrichment = await _llmClient.EnrichAsync(doc.Title, doc.StrippedBody ?? string.Empty, ctx);
			if (enrichment is not { HasData: true })
				return;

			// Store in cache for future runs
			await _enrichmentCache.StoreAsync(doc.ContentHash, enrichment, _enrichmentOptions.PromptVersion, ctx);

			// Apply fields directly (enrich processor won't have this entry yet)
			doc.AiRagOptimizedSummary = enrichment.RagOptimizedSummary;
			doc.AiShortSummary = enrichment.ShortSummary;
			doc.AiSearchQuery = enrichment.SearchQuery;
			doc.AiQuestions = enrichment.Questions;
			doc.AiUseCases = enrichment.UseCases;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Failed to enrich document {Url}", doc.Url);
			_ = Interlocked.Decrement(ref _newEnrichmentCount);
		}
	}
}
