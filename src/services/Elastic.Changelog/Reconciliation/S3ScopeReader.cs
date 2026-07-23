// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace Elastic.Changelog.Reconciliation;

/// <summary>A raw object listing row: key, normalized ETag, size, and last-modified.</summary>
public sealed record ListedObject(string Key, string ETag, long Size, DateTimeOffset? LastModified);

/// <summary>
/// The <em>read-only</em> S3 surface the reconciliation code operates on. Inspection and all
/// public-bucket checks are built exclusively against this interface, so they structurally
/// cannot write: the write boundary around the scrubber-owned public bucket is enforced by
/// the type system, not by convention.
/// </summary>
public interface IS3ScopeReader
{
	/// <summary>The bucket this reader is bound to.</summary>
	string BucketName { get; }

	/// <summary>Lists every object under <paramref name="prefix"/>, paginating to completion.</summary>
	Task<IReadOnlyList<ListedObject>> ListObjectsAsync(string prefix, Cancel ctx);

	/// <summary>Reads an object's content and normalized ETag; null when the key does not exist.</summary>
	Task<(string Content, string ETag)?> TryGetObjectAsync(string key, Cancel ctx);
}

/// <summary>Read-only adapter over <see cref="IAmazonS3"/> bound to a single bucket.</summary>
public sealed class S3ScopeReader(IAmazonS3 s3Client, string bucketName) : IS3ScopeReader
{
	/// <inheritdoc />
	public string BucketName => bucketName;

	/// <summary>Strips the surrounding quotes S3 returns around ETag values.</summary>
	public static string NormalizeETag(string? etag) => etag?.Trim('"') ?? string.Empty;

	/// <inheritdoc />
	public async Task<IReadOnlyList<ListedObject>> ListObjectsAsync(string prefix, Cancel ctx)
	{
		var request = new ListObjectsV2Request
		{
			BucketName = bucketName,
			Prefix = prefix,
			MaxKeys = 1000
		};

		var objects = new List<ListedObject>();
		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(request, ctx);
			foreach (var obj in response.S3Objects ?? [])
				objects.Add(new ListedObject(obj.Key, NormalizeETag(obj.ETag), obj.Size ?? 0, obj.LastModified is { } modified ? modified : null));
			request.ContinuationToken = response.NextContinuationToken;
		} while (response.IsTruncated == true);

		return objects;
	}

	/// <inheritdoc />
	public async Task<(string Content, string ETag)?> TryGetObjectAsync(string key, Cancel ctx)
	{
		try
		{
			using var response = await s3Client.GetObjectAsync(new GetObjectRequest
			{
				BucketName = bucketName,
				Key = key
			}, ctx);

			await using var stream = response.ResponseStream;
			using var reader = new StreamReader(stream);
			var content = await reader.ReadToEndAsync(ctx);
			return (content, NormalizeETag(response.ETag));
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}
}
