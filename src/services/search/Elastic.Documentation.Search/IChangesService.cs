// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search;

/// <summary>Gateway interface for querying documentation page changes.</summary>
public interface IChangesService
{
	Task<ChangesResponse> GetChangesAsync(ChangesRequest request, Cancel ctx = default);
}

/// <summary>API request for the changes feed endpoint.</summary>
public record ChangesRequest
{
	public required DateTimeOffset Since { get; init; }
	public int PageSize { get; init; } = ChangesDefaults.PageSize;
	public string? Cursor { get; init; }
}

/// <summary>API response for the changes feed endpoint.</summary>
public record ChangesResponse
{
	/// <summary>The changed pages in this batch.</summary>
	public required IReadOnlyList<ChangedPageDto> Pages { get; init; }
	/// <summary>True when more changed pages follow. Pass <c>nextCursor</c> as <c>cursor</c> to read them.</summary>
	/// <example>true</example>
	public required bool HasMore { get; init; }
	/// <summary>The cursor of the next batch; null on the last one.</summary>
	/// <example>"WzE3OTQ4OTYyMTE3NzIsIi9kb2NzL3JlZmVyZW5jZS9vcGVudGVsZW1ldHJ5L2Vkb3Qtc2Rrcy9qYXZhL21pZ3JhdGlvbiJd"</example>
	public string? NextCursor { get; init; }
}

/// <summary>Internal request for the changes gateway.</summary>
public record ChangesInternalRequest
{
	public required DateTimeOffset Since { get; init; }
	public int PageSize { get; init; } = ChangesDefaults.PageSize;
	public ChangesPageCursor? Cursor { get; init; }
}

/// <summary>Internal result from the changes gateway.</summary>
public record ChangesResult
{
	public required IReadOnlyList<ChangedPageDto> Pages { get; init; }
	public ChangesPageCursor? NextCursor { get; init; }
}

/// <summary>Cursor for search_after pagination over changed pages.</summary>
public record ChangesPageCursor(long ContentLastUpdatedEpochMs, string Url);

/// <summary>Shared defaults for the changes feed.</summary>
public static class ChangesDefaults
{
	public const int PageSize = 100;
	public const int MaxPageSize = 1000;
}

/// <summary>A single changed page in the API response.</summary>
public record ChangedPageDto
{
	/// <summary>The page's path on www.elastic.co.</summary>
	/// <example>"/docs/reference/opentelemetry/edot-sdks/java/configuration"</example>
	public required string Url { get; init; }
	/// <summary>The page title.</summary>
	/// <example>"Configure the EDOT Java agent"</example>
	public required string Title { get; init; }
	/// <summary>When the page's content last changed.</summary>
	/// <example>"2026-11-17T06:16:51.7648902+00:00"</example>
	public required DateTimeOffset LastUpdated { get; init; }
}
