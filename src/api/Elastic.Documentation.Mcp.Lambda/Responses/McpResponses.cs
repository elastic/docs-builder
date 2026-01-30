// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Mcp.Lambda.Responses;

// Common error response
public sealed record ErrorResponse(string Error, List<string>? Details = null);

// SemanticSearch response
public sealed record SemanticSearchResponse
{
	public required string Query { get; init; }
	public required int TotalHits { get; init; }
	public required bool IsSemanticQuery { get; init; }
	public required List<SearchResultDto> Results { get; init; }
}

public sealed record SearchResultDto
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required float Score { get; init; }
	public string? AiShortSummary { get; init; }
	public string? NavigationSection { get; init; }
	public string? Product { get; init; }
	public DateTimeOffset? LastUpdated { get; init; }
}

// FindRelatedDocs response
public sealed record RelatedDocsResponse
{
	public required string Topic { get; init; }
	public required int Count { get; init; }
	public required List<RelatedDocDto> RelatedDocs { get; init; }
}

public sealed record RelatedDocDto
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required float Score { get; init; }
	public string? AiShortSummary { get; init; }
	public string? Product { get; init; }
}

// CheckCoherence response
public sealed record CoherenceCheckResponse
{
	public required string Topic { get; init; }
	public required int TotalDocuments { get; init; }
	public required int AnalyzedDocuments { get; init; }
	public required Dictionary<string, int> SectionCoverage { get; init; }
	public required Dictionary<string, int> ProductCoverage { get; init; }
	public required int DocsWithAiSummary { get; init; }
	public required int DocsWithRagSummary { get; init; }
	public required double CoverageScore { get; init; }
	public required List<CoherenceDocDto> TopDocuments { get; init; }
}

public sealed record CoherenceDocDto
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public string? AiShortSummary { get; init; }
	public string? NavigationSection { get; init; }
	public string? Product { get; init; }
}

// FindInconsistencies response
public sealed record InconsistenciesResponse
{
	public required string Topic { get; init; }
	public string? FocusArea { get; init; }
	public required int TotalDocuments { get; init; }
	public required List<InconsistencyDto> PotentialInconsistencies { get; init; }
	public required Dictionary<string, int> ProductBreakdown { get; init; }
}

public sealed record InconsistencyDto
{
	public required string Type { get; init; }
	public required InconsistencyDocDto Document1 { get; init; }
	public required InconsistencyDocDto Document2 { get; init; }
	public required string Product { get; init; }
	public required string Reason { get; init; }
}

public sealed record InconsistencyDocDto
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public string? AiShortSummary { get; init; }
}

// GetDocumentByUrl response
public sealed record DocumentResponse
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required string Type { get; init; }
	public string? Description { get; init; }
	public string? NavigationSection { get; init; }
	public string? AiShortSummary { get; init; }
	public string? AiRagOptimizedSummary { get; init; }
	public string[]? AiQuestions { get; init; }
	public string[]? AiUseCases { get; init; }
	public DateTimeOffset? LastUpdated { get; init; }
	public required List<ParentDto> Parents { get; init; }
	public required List<string> Headings { get; init; }
	public ProductDto? Product { get; init; }
	public List<ProductDto>? RelatedProducts { get; init; }
	public string? Body { get; init; }
	public int BodyLength { get; init; }
}

public sealed record ParentDto
{
	public required string Title { get; init; }
	public required string Url { get; init; }
}

public sealed record ProductDto
{
	public required string Id { get; init; }
	public string? Repository { get; init; }
}

// AnalyzeDocumentStructure response
public sealed record DocumentStructureResponse
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required int HeadingCount { get; init; }
	public required int LinkCount { get; init; }
	public required int ParentCount { get; init; }
	public required int BodyLength { get; init; }
	public required List<string> Headings { get; init; }
	public required List<ParentDto> Parents { get; init; }
	public required AiEnrichmentStatusDto AiEnrichment { get; init; }
}

public sealed record AiEnrichmentStatusDto
{
	public required bool HasSummary { get; init; }
	public required bool HasQuestions { get; init; }
	public required bool HasUseCases { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(SemanticSearchResponse))]
[JsonSerializable(typeof(SearchResultDto))]
[JsonSerializable(typeof(RelatedDocsResponse))]
[JsonSerializable(typeof(RelatedDocDto))]
[JsonSerializable(typeof(CoherenceCheckResponse))]
[JsonSerializable(typeof(CoherenceDocDto))]
[JsonSerializable(typeof(InconsistenciesResponse))]
[JsonSerializable(typeof(InconsistencyDto))]
[JsonSerializable(typeof(InconsistencyDocDto))]
[JsonSerializable(typeof(DocumentResponse))]
[JsonSerializable(typeof(ParentDto))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(DocumentStructureResponse))]
[JsonSerializable(typeof(AiEnrichmentStatusDto))]
public sealed partial class McpJsonContext : JsonSerializerContext;
