// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Clients.Elasticsearch;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Mcp.Remote.Gateways;

/// <summary>
/// Gateway implementation for document-specific operations.
/// Uses Elasticsearch to fetch documents by URL.
/// </summary>
public class DocumentGateway(
	ElasticsearchClientAccessor clientAccessor,
	ILogger<DocumentGateway> logger)
	: IDocumentGateway
{
	/// <inheritdoc />
	public async Task<DocumentResult?> GetByUrlAsync(string url, CancellationToken ct = default)
	{
		try
		{
			var normalizedUrl = NormalizeUrl(url);
			var response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(s => s
				.Indices(clientAccessor.Options.IndexName)
				.Query(q => q.Term(t => t.Field(f => f.Url).Value(normalizedUrl)))
				.Size(1)
				.Source(sf => sf.Filter(f => f.Includes(
					e => e.Url,
					e => e.Title,
					e => e.Type,
					e => e.Description,
					e => e.NavigationSection,
					e => e.Body,
					e => e.Parents,
					e => e.Headings,
					e => e.Links,
					e => e.AiShortSummary,
					e => e.AiRagOptimizedSummary,
					e => e.AiQuestions,
					e => e.AiUseCases,
					e => e.LastUpdated,
					e => e.Product,
					e => e.RelatedProducts
				))),
				ct);

			if (!response.IsValidResponse || response.Documents.Count == 0)
			{
				logger.LogDebug("Document not found for URL: {Url} (normalized: {Normalized})", url, normalizedUrl);
				return null;
			}

			var doc = response.Documents.First();
			return new DocumentResult
			{
				Url = doc.Url,
				Title = doc.Title,
				Type = doc.Type,
				Description = doc.Description,
				NavigationSection = doc.NavigationSection,
				Body = doc.Body,
				Parents = doc.Parents.Select(p => new DocumentParent
				{
					Title = p.Title,
					Url = p.Url
				}).ToArray(),
				Headings = doc.Headings,
				Links = doc.Links,
				AiShortSummary = doc.AiShortSummary,
				AiRagOptimizedSummary = doc.AiRagOptimizedSummary,
				AiQuestions = doc.AiQuestions,
				AiUseCases = doc.AiUseCases,
				LastUpdated = doc.LastUpdated,
				Product = doc.Product?.Id != null ? new DocumentProduct
				{
					Id = doc.Product.Id,
					Repository = doc.Product.Repository
				} : null,
				RelatedProducts = doc.RelatedProducts?
						.Where(p => p.Id != null)
						.Select(p => new DocumentProduct
						{
							Id = p.Id!,
							Repository = p.Repository
						}).ToArray()
			};
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error fetching document by URL: {Url}", url);
			throw;
		}
	}

	/// <inheritdoc />
	public async Task<DocumentStructure?> GetStructureAsync(string url, CancellationToken ct = default)
	{
		try
		{
			var normalizedUrl = NormalizeUrl(url);
			var response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(s => s
				.Indices(clientAccessor.Options.IndexName)
				.Query(q => q.Term(t => t.Field(f => f.Url).Value(normalizedUrl)))
				.Size(1)
				.Source(sf => sf.Filter(f => f.Includes(
					e => e.Url,
					e => e.Title,
					e => e.Parents,
					e => e.Headings,
					e => e.Links,
					e => e.Body,
					e => e.AiShortSummary,
					e => e.AiQuestions,
					e => e.AiUseCases
				))),
				ct);

			if (!response.IsValidResponse || response.Documents.Count == 0)
			{
				logger.LogDebug("Document not found for URL: {Url} (normalized: {Normalized})", url, normalizedUrl);
				return null;
			}

			var doc = response.Documents.First();
			return new DocumentStructure
			{
				Url = doc.Url,
				Title = doc.Title,
				HeadingCount = doc.Headings.Length,
				LinkCount = doc.Links.Length,
				ParentCount = doc.Parents.Length,
				BodyLength = doc.Body?.Length ?? 0,
				Headings = doc.Headings,
				Parents = doc.Parents.Select(p => new DocumentParent
				{
					Title = p.Title,
					Url = p.Url
				}).ToArray(),
				HasAiSummary = !string.IsNullOrEmpty(doc.AiShortSummary),
				HasAiQuestions = doc.AiQuestions is { Length: > 0 },
				HasAiUseCases = doc.AiUseCases is { Length: > 0 }
			};
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error fetching document structure for URL: {Url}", url);
			throw;
		}
	}

	/// <summary>
	/// Normalizes a document URL to the path-only format stored in the index (e.g. <c>/docs/section/page</c>).
	/// Accepts full URLs (<c>https://www.elastic.co/docs/…</c>), path-only URLs with or without leading slash,
	/// and strips query strings, fragments, and trailing slashes.
	/// </summary>
	internal static string NormalizeUrl(string url)
	{
		url = url.Trim();

		// Parse absolute URLs and extract the path component
		if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
			url = uri.AbsolutePath;

		// Ensure leading slash
		if (!url.StartsWith('/'))
			url = "/" + url;

		// Strip trailing slash (index stores paths without trailing slash)
		url = url.TrimEnd('/');

		return url;
	}
}
