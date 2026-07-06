// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Export;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using Elastic.Ingest.Elasticsearch.Indices;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;
using static System.StringSplitOptions;
using MarkdownParser = Markdig.Parsers.MarkdownParser;

namespace Elastic.Markdown.Exporters.Elasticsearch;

public partial class ElasticsearchMarkdownExporter
{
	private IDocumentInferrerService? _inferService;

	/// <summary>
	/// Assigns hash, last updated, and batch index date to a documentation document.
	/// </summary>
	private void AssignDocumentMetadata(DocumentationDocument doc)
	{
		var semanticHash = _semanticTypeContext?.Hash ?? string.Empty;
		var lexicalHash = _lexicalTypeContext.Hash;
		var hash = HashedBulkUpdate.CreateHash(semanticHash, lexicalHash,
			doc.Url, doc.Type, doc.StrippedBody ?? string.Empty, string.Join(",", doc.Headings.OrderBy(h => h)),
			doc.SearchTitle ?? string.Empty,
			doc.NavigationSection ?? string.Empty, doc.NavigationDepth.ToString("N0"),
			doc.NavigationTableOfContents.ToString("N0"),
			doc.ContentTier,
			_fixedSynonymsHash,
			string.Join(",", doc.Parents?.Select(p => $"{p.Url}:{p.Title}") ?? [])
		);
		doc.Hash = hash;
	}

	/// <summary>Computes and assigns the whitespace-normalized content hash for change detection.</summary>
	private static void AssignContentHash(DocumentationDocument doc) =>
		doc.ContentBodyHash = ContentHash.CreateNormalized(doc.StrippedBody ?? string.Empty);

	/// <summary>
	/// Classifies a docs page into the shared <see cref="ContentTiers"/> taxonomy. docs-builder owns
	/// this classification for its own docs/api content — it only agrees with other producers
	/// (e.g. essc) on the <see cref="ContentTiers"/> string constants themselves.
	/// </summary>
	internal static string ClassifyContentTier(INavigationItem? navigationItem, string url)
	{
		var section = navigationItem?.NavigationSection;
		if (IsReleaseNotes(section, url))
			return ContentTiers.Peripheral;
		if (IsSupplementary(section, url))
			return ContentTiers.Supplementary;
		if (IsPrimary(navigationItem, section))
			return ContentTiers.Primary;
		return ContentTiers.Reference;

		static bool IsReleaseNotes(string? section, string url) =>
			section == "release notes" || url.Contains("/release-notes/", StringComparison.OrdinalIgnoreCase);

		// Aligns with the existing `diminish_terms` in search.yml (plugin, glossary, curator, hadoop, integration, client).
		static bool IsSupplementary(string? section, string url) =>
			ContainsAny(section, "deprecat", "plugin", "glossary")
			|| url.Contains("/docs/extend/", StringComparison.OrdinalIgnoreCase);

		// A section's root/index page is effectively that section's overview — treat it as primary
		// alongside pages explicitly living under a "get started"/"getting started"/"overview" section.
		static bool IsPrimary(INavigationItem? navigationItem, string? section) =>
			navigationItem is IRootNavigationItem<INavigationModel, INavigationItem>
			|| ContainsAny(section, "get started", "getting started", "overview");

		static bool ContainsAny(string? value, params string[] fragments) =>
			value is not null && fragments.Any(f => value.Contains(f, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Internal (rather than private) so tests can assert navigation_* rank features never fall back
	/// to the contract's penalty default, and that search_title construction is correct.
	/// </summary>
	internal static void CommonEnrichments(DocumentationDocument doc, INavigationItem? navigationItem)
	{
		// OpenApiDocumentExporter builds its own search title, including the raw operation id
		// (e.g. "_bulk") — CreateSearchTitle below is tuned for markdown pages and derives extra
		// tokens from the URL by splitting on '_' among other chars, which would strip that
		// leading underscore. Leave API docs' search_title alone.
		if (doc.ContentType != "api")
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
		// doc.Type is the fixed CLR/$type discriminator ("docs" for every DocumentationDocument) —
		// ContentType is the field OpenApiDocumentExporter actually sets to "api" per-document.
		if (doc.ContentType == "api")
			doc.NavigationSection = "api";

		// this section gets promoted in the navigation we don't want it to be promoted in the search results
		// e.g. `Use high-contrast mode in Kibana - ( docs cloud-account high contrast`
		if (doc.NavigationSection == "manage your cloud account and preferences")
			doc.NavigationDepth *= 2;

		// API reference pages have no navigation item to classify from — treat them as plain reference content.
		// (doc.Type is a fixed CLR/$type discriminator, always "docs" — ContentType is the field
		// OpenApiDocumentExporter actually sets to "api".)
		doc.ContentTier = doc.ContentType == "api" ? ContentTiers.Reference : ClassifyContentTier(navigationItem, doc.Url);

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

		_inferService ??= fileContext.InferenceService;

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
		var strippedBody = PlainTextExporter.ConvertToPlainText(fileContext.Document, fileContext.BuildContext);

		var headings = fileContext.Document.Descendants<HeadingBlock>()
			.Select(h => h.GetData("header") as string ?? string.Empty) // TODO: Confirm that 'header' data is correctly set for all HeadingBlock instances and that this extraction is reliable.
			.Where(text => !string.IsNullOrEmpty(text))
			.ToArray();
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
			SearchTitle = file.Title, //updated in CommonEnrichments
			Body = body,
			StrippedBody = strippedBody,
			Description = fileContext.SourceFile.YamlFrontMatter?.Description,
			Abstract = @abstract,
			Applies = appliesTo.ToAppliesTo(),
			Parents = navigation.GetParentsOfMarkdownFile(file).Select(i => new ParentDocument
			{
				Title = i.NavigationTitle,
				Url = i.Url
			}).Reverse().ToArray(),
			Headings = headings,
			Hidden = fileContext.NavigationItem.Hidden
		};

		// Infer product and repository metadata
		var mappedPages = fileContext.SourceFile.YamlFrontMatter?.MappedPages;
		var inference = fileContext.InferenceService.InferForMarkdown(
			fileContext.BuildContext.Git.RepositoryName,
			mappedPages,
			fileContext.DocumentationSet.Configuration.Products,
			fileContext.SourceFile.YamlFrontMatter?.Products,
			appliesTo
		);
		doc.Product = inference.Product is not null
			? new IndexedProduct { Id = inference.Product.Id, Repository = inference.Repository }
			: null;
		doc.RelatedProducts = inference.RelatedProducts.Count > 0
			? inference.RelatedProducts.Select(p => new IndexedProduct
			{
				Id = p.Id,
				Repository = p.Repository ?? inference.Repository
			}).ToArray()
			: null;

		CommonEnrichments(doc, currentNavigation);
		AssignContentHash(doc);
		AssignDocumentMetadata(doc);

		return await WriteDocumentAsync(doc, ctx);
	}

	/// <inheritdoc />
	public async ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx)
	{
		if (_context.BuildType != BuildType.Assembler)
		{
			_logger.LogInformation("Skipping OpenAPI export for non-assembler build");
			return true;
		}

		// this is temporary; once we implement Elastic.ApiExplorer, this should flow through
		// we'll rename IMarkdownExporter to IDocumentationFileExporter at that point
		_logger.LogInformation("Exporting OpenAPI documentation to Elasticsearch");

		var exporter = new OpenApiDocumentExporter(_versionsConfiguration, _inferService);

		await foreach (var doc in exporter.ExportDocuments(limitPerSource: null, ctx))
		{
			var document = MarkdownParser.Parse(doc.Body ?? string.Empty);

			doc.Body = LlmMarkdownExporter.ConvertToLlmMarkdown(document, _context);
			doc.StrippedBody = PlainTextExporter.ConvertToPlainText(document, _context);

			var headings = document.Descendants<HeadingBlock>()
				.Select(h => h.GetData("header") as string ?? string.Empty) // TODO: Confirm that 'header' data is correctly set for all HeadingBlock instances and that this extraction is reliable.
				.Where(text => !string.IsNullOrEmpty(text))
				.ToArray();
			var @abstract = !string.IsNullOrEmpty(doc.StrippedBody)
				? doc.Body[..Math.Min(doc.StrippedBody.Length, 400)] + " " + string.Join(" \n- ", doc.Headings)
				: string.Empty;
			doc.Abstract = @abstract;
			doc.Headings = headings;
			CommonEnrichments(doc, null);
			AssignContentHash(doc);
			AssignDocumentMetadata(doc);

			if (!await WriteDocumentAsync(doc, ctx))
			{
				_logger.LogError("Failed to write OpenAPI document {Url}", doc.Url);
				return false;
			}
		}

		_logger.LogInformation("Finished exporting OpenAPI documentation");
		return true;
	}

}
