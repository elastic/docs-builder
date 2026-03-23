// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.Changes;

/// <summary>Gateway interface for querying documentation page changes.</summary>
public interface IChangesGateway
{
	Task<ChangesResult> GetChangesAsync(ChangesRequest request, Cancel ctx = default);
}

/// <summary>Internal request for the changes gateway.</summary>
public record ChangesRequest
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
public record ChangesPageCursor(long LastUpdatedEpochMs, string Url);

/// <summary>Shared defaults for the changes feed.</summary>
public static class ChangesDefaults
{
	public const int PageSize = 100;
	public const int MaxPageSize = 1000;
}
