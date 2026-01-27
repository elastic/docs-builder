// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Mcp.Lambda.Gateways;

/// <summary>
/// Gateway interface for document-specific operations in the MCP Lambda.
/// </summary>
public interface IDocumentGateway
{
	/// <summary>
	/// Gets a document by its URL.
	/// </summary>
	Task<DocumentResult?> GetByUrlAsync(string url, CancellationToken ct = default);

	/// <summary>
	/// Gets the structure of a document by its URL.
	/// </summary>
	Task<DocumentStructure?> GetStructureAsync(string url, CancellationToken ct = default);
}

/// <summary>
/// Result model for a document lookup.
/// </summary>
public record DocumentResult
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required string Type { get; init; }
	public string? Description { get; init; }
	public string? NavigationSection { get; init; }
	public string? Body { get; init; }
	public string? AiShortSummary { get; init; }
	public string? AiRagOptimizedSummary { get; init; }
	public string[]? AiQuestions { get; init; }
	public string[]? AiUseCases { get; init; }
	public DateTimeOffset? LastUpdated { get; init; }
	public DocumentParent[] Parents { get; init; } = [];
	public string[] Headings { get; init; } = [];
	public string[] Links { get; init; } = [];
	public DocumentProduct? Product { get; init; }
	public DocumentProduct[]? RelatedProducts { get; init; }
}

/// <summary>
/// Document structure analysis result.
/// </summary>
public record DocumentStructure
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required int HeadingCount { get; init; }
	public required int LinkCount { get; init; }
	public required int ParentCount { get; init; }
	public required int BodyLength { get; init; }
	public required string[] Headings { get; init; }
	public required DocumentParent[] Parents { get; init; }
	public bool HasAiSummary { get; init; }
	public bool HasAiQuestions { get; init; }
	public bool HasAiUseCases { get; init; }
}

/// <summary>
/// Parent document reference in breadcrumb trail.
/// </summary>
public record DocumentParent
{
	public required string Title { get; init; }
	public required string Url { get; init; }
}

/// <summary>
/// Product reference for a document.
/// </summary>
public record DocumentProduct
{
	public required string Id { get; init; }
	public string? Repository { get; init; }
}
