// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search;

/// <summary>
/// Common contract for all indexed document types (documentation pages, site pages, etc.)
/// </summary>
public interface IDocument
{
	string Title { get; set; }
	string SearchTitle { get; set; }
	string Type { get; set; }
	string Url { get; set; }
	string Hash { get; set; }

	DateTimeOffset BatchIndexDate { get; set; }
	DateTimeOffset LastUpdated { get; set; }

	string? Description { get; set; }
	string[] Headings { get; set; }
	string? Body { get; set; }
	string? StrippedBody { get; set; }
	string? Abstract { get; set; }
	bool Hidden { get; set; }

	// AI enrichment
	string? AiRagOptimizedSummary { get; set; }
	string? AiShortSummary { get; set; }
	string? AiSearchQuery { get; set; }
	string[]? AiQuestions { get; set; }
	string[]? AiUseCases { get; set; }
	string? EnrichmentPromptHash { get; set; }

	// HTTP caching for incremental sync
	string? HttpEtag { get; set; }
	DateTimeOffset? HttpLastModified { get; set; }
}
