// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;

namespace Elastic.Documentation.OpenApiIndex;

/// <summary>
/// Rebuilds <c>index.json</c> at the root of the <c>elastic-docs-openapi-specs</c> bucket from a full LIST
/// of that bucket, and writes it back under an ETag precondition.
/// </summary>
/// <remarks>
/// Rebuilding from the listing rather than merging the triggering event is what keeps this correct: it
/// self-heals after a missed event, and a deleted object simply cannot produce an entry. A failed
/// precondition is deliberately not retried in process — it throws, the caller hands the message back to
/// SQS, and redelivery re-runs this same LIST-rebuild-PUT.
/// </remarks>
public sealed class VersionIndexPublisher(IAmazonS3 s3Client, string bucketName)
{
	public const string IndexKey = "index.json";

	/// <summary>
	/// Rebuilds the index and writes it back unless it is byte-identical to what is already published.
	/// Returns the object keys that were ignored because they did not match the expected key shape.
	/// </summary>
	public async Task<IReadOnlyList<string>> RefreshAsync(Cancel ctx)
	{
		var keys = await ListSpecKeysAsync(ctx).ConfigureAwait(false);
		var (index, invalidKeys) = VersionIndexBuilder.Build(keys);
		var json = JsonSerializer.Serialize(
			index,
			VersionIndexJsonContext.Default.SortedDictionaryStringSortedDictionaryStringSortedDictionaryStringVersionIndexEntry
		);

		var existing = await TryGetExistingIndexAsync(ctx).ConfigureAwait(false);
		if (!string.Equals(existing?.Body, json, StringComparison.Ordinal))
			await PutIndexAsync(json, existing?.ETag, ctx).ConfigureAwait(false);

		return invalidKeys;
	}

	private async Task<List<string>> ListSpecKeysAsync(Cancel ctx)
	{
		var keys = new List<string>();
		var request = new ListObjectsV2Request { BucketName = bucketName };

		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(request, ctx).ConfigureAwait(false);
			foreach (var s3Object in response.S3Objects ?? [])
			{
				if (!string.Equals(s3Object.Key, IndexKey, StringComparison.Ordinal))
					keys.Add(s3Object.Key);
			}
			request.ContinuationToken = response.NextContinuationToken;
		}
		while (response.IsTruncated == true);

		return keys;
	}

	/// <summary>The published index's ETag and raw body, or null when it does not exist yet. The body is compared, never parsed.</summary>
	private async Task<(string? ETag, string Body)?> TryGetExistingIndexAsync(Cancel ctx)
	{
		try
		{
			using var response = await s3Client.GetObjectAsync(
				new GetObjectRequest { BucketName = bucketName, Key = IndexKey },
				ctx
			).ConfigureAwait(false);

			using var reader = new StreamReader(response.ResponseStream);
			return (response.ETag, await reader.ReadToEndAsync(ctx).ConfigureAwait(false));
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	private async Task PutIndexAsync(string json, string? etag, Cancel ctx)
	{
		var request = new PutObjectRequest
		{
			BucketName = bucketName,
			Key = IndexKey,
			ContentBody = json,
			ContentType = "application/json"
		};

		// Optimistic concurrency: update only if unchanged, create only if still absent.
		if (etag is null)
			request.IfNoneMatch = "*";
		else
			request.IfMatch = etag;

		_ = await s3Client.PutObjectAsync(request, ctx).ConfigureAwait(false);
	}
}
