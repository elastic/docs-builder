// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Core.Changes;

/// <summary>Use case for the documentation changes feed.</summary>
public partial class ChangesUsecase(IChangesGateway changesGateway, ILogger<ChangesUsecase> logger)
{
	public async Task<ChangesApiResponse> GetChangesAsync(ChangesApiRequest request, Cancel ctx = default)
	{
		var cursor = DecodeCursor(request.Cursor);
		var pageSize = Math.Clamp(request.PageSize, 1, ChangesDefaults.MaxPageSize);

		var result = await changesGateway.GetChangesAsync(
			new ChangesRequest
			{
				Since = request.Since,
				PageSize = pageSize,
				Cursor = cursor
			},
			ctx
		);

		var nextCursor = result.NextCursor is not null
			? EncodeCursor(result.NextCursor)
			: null;

		var hasMore = nextCursor is not null;

		LogChanges(logger, request.Since, result.Pages.Count, hasMore);

		return new ChangesApiResponse
		{
			Pages = result.Pages,
			HasMore = hasMore,
			NextCursor = nextCursor
		};
	}

	private static ChangesPageCursor? DecodeCursor(string? cursor)
	{
		if (string.IsNullOrWhiteSpace(cursor))
			return null;

		try
		{
			var remainder = cursor.Length % 4;
			var paddingLength = (4 - remainder) % 4;
			var base64 = cursor
				.Replace('-', '+')
				.Replace('_', '/')
				+ new string('=', paddingLength);

			var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
			var parts = JsonSerializer.Deserialize<JsonElement[]>(json);
			if (parts is not [{ ValueKind: JsonValueKind.Number } epochEl, { ValueKind: JsonValueKind.String } urlEl])
				return null;

			return new ChangesPageCursor(epochEl.GetInt64(), urlEl.GetString()!);
		}
		catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
		{
			return null;
		}
	}

	private static string EncodeCursor(ChangesPageCursor cursor)
	{
		var buffer = new ArrayBufferWriter<byte>();
		using var writer = new Utf8JsonWriter(buffer);
		writer.WriteStartArray();
		writer.WriteNumberValue(cursor.LastUpdatedEpochMs);
		writer.WriteStringValue(cursor.Url);
		writer.WriteEndArray();
		writer.Flush();

		return Convert.ToBase64String(buffer.WrittenSpan)
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
	}

	[LoggerMessage(Level = LogLevel.Information,
		Message = "Changes feed returned {Count} pages since {Since} (hasMore: {HasMore})")]
	private static partial void LogChanges(ILogger logger, DateTimeOffset since, int count, bool hasMore);
}

/// <summary>API request for the changes feed endpoint.</summary>
public record ChangesApiRequest
{
	public required DateTimeOffset Since { get; init; }
	public int PageSize { get; init; } = ChangesDefaults.PageSize;
	public string? Cursor { get; init; }
}

/// <summary>API response for the changes feed endpoint.</summary>
public record ChangesApiResponse
{
	public required IReadOnlyList<ChangedPageDto> Pages { get; init; }
	public required bool HasMore { get; init; }
	public string? NextCursor { get; init; }
}

/// <summary>A single changed page in the API response.</summary>
public record ChangedPageDto
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public required DateTimeOffset LastUpdated { get; init; }
}
