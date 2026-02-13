// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Text.Json;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Mcp.Remote.Responses;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Mcp.Remote.Tools;

/// <summary>
/// MCP tools for checking documentation coherence and finding inconsistencies.
/// </summary>
[McpServerToolType]
public class CoherenceTools(IFullSearchGateway fullSearchGateway)
{
	/// <summary>
	/// Checks documentation coherence for a given topic.
	/// </summary>
	[McpServerTool, Description("Checks documentation coherence for a given topic by finding all related documents and analyzing their coverage.")]
	public async Task<string> CheckCoherence(
		[Description("Topic or concept to check coherence for")] string topic,
		[Description("Maximum number of documents to analyze (default: 20)")] int limit = 20,
		CancellationToken cancellationToken = default)
	{
		try
		{
			limit = Math.Clamp(limit, 5, 50);

			var request = new FullSearchRequest
			{
				Query = topic,
				PageNumber = 1,
				PageSize = limit
			};

			var result = await fullSearchGateway.SearchAsync(request, cancellationToken);

			// Analyze coherence based on:
			// - Document coverage (how many docs cover the topic)
			// - AI summaries (similar documents should have complementary, not conflicting summaries)
			// - Navigation sections (spread across sections)
			// - Products (coverage across products)

			var navigationSections = result.Results
				.Where(r => !string.IsNullOrEmpty(r.NavigationSection))
				.GroupBy(r => r.NavigationSection)
				.ToDictionary(g => g.Key!, g => g.Count());

			var products = result.Results
				.Where(r => r.Product != null)
				.GroupBy(r => r.Product!.DisplayName)
				.ToDictionary(g => g.Key, g => g.Count());

			var docsWithSummaries = result.Results.Count(r => !string.IsNullOrEmpty(r.AiShortSummary));
			var docsWithRagSummaries = result.Results.Count(r => !string.IsNullOrEmpty(r.AiRagOptimizedSummary));

			var response = new CoherenceCheckResponse
			{
				Topic = topic,
				TotalDocuments = result.TotalHits,
				AnalyzedDocuments = result.Results.Count,
				SectionCoverage = navigationSections,
				ProductCoverage = products,
				DocsWithAiSummary = docsWithSummaries,
				DocsWithRagSummary = docsWithRagSummaries,
				CoverageScore = CalculateCoverageScore(result.TotalHits, navigationSections.Count, products.Count),
				TopDocuments = result.Results.Take(5).Select(r => new CoherenceDocDto
				{
					Url = r.Url,
					Title = r.Title,
					AiShortSummary = r.AiShortSummary,
					NavigationSection = r.NavigationSection,
					Product = r.Product?.DisplayName
				}).ToList()
			};

			return JsonSerializer.Serialize(response, McpJsonContext.Default.CoherenceCheckResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	/// <summary>
	/// Finds potential inconsistencies in documentation for a given topic.
	/// </summary>
	[McpServerTool, Description("Finds potential inconsistencies in documentation by comparing documents about the same topic.")]
	public async Task<string> FindInconsistencies(
		[Description("Topic or concept to check for inconsistencies")] string topic,
		[Description("Specific area to focus on (e.g., 'installation', 'configuration')")] string? focusArea = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var query = focusArea != null ? $"{topic} {focusArea}" : topic;

			var request = new FullSearchRequest
			{
				Query = query,
				PageNumber = 1,
				PageSize = 30
			};

			var result = await fullSearchGateway.SearchAsync(request, cancellationToken);

			// Group by product to find potential inconsistencies
			var byProduct = result.Results
				.Where(r => r.Product != null)
				.GroupBy(r => r.Product!.Id)
				.ToDictionary(g => g.Key, g => g.ToList());

			// Find documents that might have overlapping or conflicting content
			var potentialOverlaps = new List<InconsistencyDto>();

			foreach (var productGroup in byProduct)
			{
				var docs = productGroup.Value;
				if (docs.Count > 1)
				{
					// Documents in the same product covering similar topics might have inconsistencies
					for (var i = 0; i < docs.Count - 1; i++)
					{
						for (var j = i + 1; j < docs.Count; j++)
						{
							var doc1 = docs[i];
							var doc2 = docs[j];

							// If both have AI summaries, they could be compared
							if (!string.IsNullOrEmpty(doc1.AiShortSummary) && !string.IsNullOrEmpty(doc2.AiShortSummary))
							{
								potentialOverlaps.Add(new InconsistencyDto
								{
									Type = "potential_overlap",
									Document1 = new InconsistencyDocDto
									{
										Url = doc1.Url,
										Title = doc1.Title,
										AiShortSummary = doc1.AiShortSummary
									},
									Document2 = new InconsistencyDocDto
									{
										Url = doc2.Url,
										Title = doc2.Title,
										AiShortSummary = doc2.AiShortSummary
									},
									Product = doc1.Product?.DisplayName ?? productGroup.Key,
									Reason = "Multiple documents in same product covering similar topic"
								});
							}
						}
					}
				}
			}

			var response = new InconsistenciesResponse
			{
				Topic = topic,
				FocusArea = focusArea,
				TotalDocuments = result.TotalHits,
				PotentialInconsistencies = potentialOverlaps.Take(10).ToList(),
				ProductBreakdown = byProduct.ToDictionary(g => g.Key, g => g.Value.Count)
			};

			return JsonSerializer.Serialize(response, McpJsonContext.Default.InconsistenciesResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	private static double CalculateCoverageScore(int totalDocs, int sectionCount, int productCount)
	{
		// Simple scoring: more docs, sections, and products = better coverage
		var docScore = Math.Min(totalDocs / 10.0, 1.0) * 0.4;
		var sectionScore = Math.Min(sectionCount / 3.0, 1.0) * 0.3;
		var productScore = Math.Min(productCount / 2.0, 1.0) * 0.3;
		return Math.Round((docScore + sectionScore + productScore) * 100, 1);
	}
}
