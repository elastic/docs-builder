// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search;

public interface IRelatedPagesService
{
	Task<RelatedPagesResponse> GetRelatedPagesAsync(string path, Cancel ctx = default);
}

public record RelatedPagesResponse
{
	public required string Query { get; init; }
	public required IReadOnlyList<RelatedPage> Results { get; init; }
}

public record RelatedPage
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required RelatedPageParent[] Parents { get; init; }
}

public record RelatedPageParent
{
	public required string Title { get; init; }
	public required string Url { get; init; }
}
