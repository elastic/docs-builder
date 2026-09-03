// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Reconciliation;

/// <summary>
/// A stateful in-memory S3 behind a FakeItEasy <see cref="IAmazonS3"/>, holding one or more
/// buckets so a single client can serve the scrubber's private-read/public-write flow. ETags are
/// the MD5 of the content (matching real single-part uploads), conditional PUTs and DELETEs
/// (<c>If-Match</c> / <c>If-None-Match</c>) enforce real 412 semantics, listings honor the
/// <c>/</c> delimiter and paginate with <see cref="PageSize"/>. Every read and write call is
/// recorded so tests can assert exactly what happened — or that nothing did. The hooks simulate
/// concurrent writers at precise interleaving points.
/// </summary>
internal sealed class FakeS3
{
	private readonly Lock _lock = new();
	private readonly Dictionary<string, Dictionary<string, (string Content, string ETag)>> _buckets = [with(StringComparer.Ordinal)];

	public IAmazonS3 Client { get; } = A.Fake<IAmazonS3>();

	/// <summary>Every <c>PutObject</c> call received, in order.</summary>
	public List<PutObjectRequest> Puts { get; } = [];

	/// <summary>Every <c>DeleteObject</c> call received, in order.</summary>
	public List<DeleteObjectRequest> Deletes { get; } = [];

	/// <summary>Every <c>GetObject</c> call received, in order.</summary>
	public List<GetObjectRequest> Gets { get; } = [];

	/// <summary>Number of <c>ListObjectsV2</c> calls received.</summary>
	public int ListCalls { get; private set; }

	/// <summary>Objects per listing page, to exercise pagination.</summary>
	public int PageSize { get; set; } = 1000;

	/// <summary>Runs before each <c>PutObject</c> is evaluated, with the 1-based call number — simulates a concurrent writer.</summary>
	public Action<int>? BeforePut { get; set; }

	/// <summary>Runs before each <c>DeleteObject</c> is evaluated, with the 1-based call number.</summary>
	public Action<int>? BeforeDelete { get; set; }

	/// <summary>Runs after a <c>GetObject</c> resolved its content (which is returned unchanged), with the key and 1-based call number — simulates the source changing right after a read.</summary>
	public Action<string, int>? AfterGet { get; set; }

	/// <summary>Runs before every <c>ListObjectsV2</c> evaluation with the 1-based call number.</summary>
	public Action<int>? OnList { get; set; }

	private int _puts;
	private int _deletes;
	private int _gets;

	public FakeS3(params string[] bucketNames)
	{
		foreach (var bucket in bucketNames)
			_buckets[bucket] = [with(StringComparer.Ordinal)];

		_ = A.CallTo(() => Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._)).ReturnsLazily(
			(ListObjectsV2Request r, CancellationToken _) => List(r)
		);

		_ = A.CallTo(() => Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._)).ReturnsLazily(
			(GetObjectRequest r, CancellationToken _) => Get(r)
		);

		_ = A.CallTo(() => Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._)).ReturnsLazily(
			(GetObjectMetadataRequest r, CancellationToken _) => Head(r)
		);

		_ = A.CallTo(() => Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).ReturnsLazily(
			(PutObjectRequest r, CancellationToken _) => Put(r)
		);

		_ = A.CallTo(() => Client.DeleteObjectAsync(A<DeleteObjectRequest>._, A<CancellationToken>._)).ReturnsLazily(
			(DeleteObjectRequest r, CancellationToken _) => Delete(r)
		);
	}

	// MD5 is what real S3 uses for single-part ETags.
	[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
	public static string ETagOf(string content) => Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(content)));

	/// <summary>Seeds or replaces an object; returns its (unquoted) ETag.</summary>
	public string Seed(string bucket, string key, string content)
	{
		var etag = ETagOf(content);
		Store(bucket)[key] = (content, etag);
		return etag;
	}

	public void Remove(string bucket, string key) => Store(bucket).Remove(key);

	public bool Exists(string bucket, string key) => Store(bucket).ContainsKey(key);

	public string ContentOf(string bucket, string key) => Store(bucket)[key].Content;

	/// <summary>The keys of every <c>GetObject</c> call for <paramref name="bucket"/>.</summary>
	public IReadOnlyList<string> GetsFor(string bucket) =>
		[.. Gets.Where(g => string.Equals(g.BucketName, bucket, StringComparison.Ordinal)).Select(g => g.Key)];

	private Dictionary<string, (string Content, string ETag)> Store(string bucket) =>
		_buckets.TryGetValue(bucket, out var store)
			? store
			: throw new InvalidOperationException($"Bucket {bucket} was not declared on this FakeS3");

	private ListObjectsV2Response List(ListObjectsV2Request request)
	{
		int n;
		lock (_lock)
			n = ++ListCalls;
		OnList?.Invoke(n);

		Dictionary<string, (string Content, string ETag)> store;
		lock (_lock)
			store = Store(request.BucketName).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
		var prefix = request.Prefix ?? string.Empty;
		var objects = new List<S3Object>();
		var commonPrefixes = new SortedSet<string>(StringComparer.Ordinal);

		foreach (var (key, value) in store.Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(
			kv => kv.Key,
			StringComparer.Ordinal
		))
		{
			var rest = key[prefix.Length..];
			var slash = rest.IndexOf('/', StringComparison.Ordinal);
			if (request.Delimiter == "/" && slash >= 0)
			{
				_ = commonPrefixes.Add(prefix + rest[..(slash + 1)]);
				continue;
			}

			objects.Add(new S3Object
			{
				Key = key,
				ETag = $"\"{value.ETag}\"",
				Size = value.Content.Length,
				LastModified = new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc)
			});
		}

		var start = request.ContinuationToken is { } token ? int.Parse(token, System.Globalization.CultureInfo.InvariantCulture) : 0;
		var page = objects.Skip(start).Take(PageSize).ToList();
		var truncated = start + page.Count < objects.Count;

		return new ListObjectsV2Response
		{
			S3Objects = page,
			CommonPrefixes = [.. commonPrefixes],
			IsTruncated = truncated,
			NextContinuationToken = truncated ? (start + page.Count).ToString(System.Globalization.CultureInfo.InvariantCulture) : null
		};
	}

	private GetObjectResponse Get(GetObjectRequest request)
	{
		(string Content, string ETag) obj;
		int n;
		lock (_lock)
		{
			Gets.Add(request);
			n = ++_gets;
			if (!Store(request.BucketName).TryGetValue(request.Key, out obj))
				throw NotFound();
		}

		// Capture the response before the hook runs, so a hook that reseeds the key simulates a
		// write landing right after this read.
		var response = new GetObjectResponse
		{
			ETag = $"\"{obj.ETag}\"",
			ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(obj.Content))
		};
		AfterGet?.Invoke(request.Key, n);
		return response;
	}

	private GetObjectMetadataResponse Head(GetObjectMetadataRequest request)
	{
		(string Content, string ETag) obj;
		lock (_lock)
		{
			if (!Store(request.BucketName).TryGetValue(request.Key, out obj))
				throw NotFound();
		}

		var response = new GetObjectMetadataResponse { ETag = $"\"{obj.ETag}\"" };
		response.Headers.ContentType = "application/yaml";
		return response;
	}

	private PutObjectResponse Put(PutObjectRequest request)
	{
		int n;
		lock (_lock)
			n = ++_puts;
		BeforePut?.Invoke(n);

		lock (_lock)
		{
			Puts.Add(request);

			var store = Store(request.BucketName);
			var exists = store.TryGetValue(request.Key, out var current);
			if (request.IfNoneMatch == "*" && exists)
				throw PreconditionFailed();
			if (request.IfMatch is { } ifMatch && (!exists || ifMatch.Trim('"') != current.ETag))
				throw PreconditionFailed();

			_ = Seed(request.BucketName, request.Key, request.ContentBody);
		}
		return new PutObjectResponse();
	}

	private DeleteObjectResponse Delete(DeleteObjectRequest request)
	{
		int n;
		lock (_lock)
			n = ++_deletes;
		BeforeDelete?.Invoke(n);

		lock (_lock)
		{
			Deletes.Add(request);

			var store = Store(request.BucketName);
			var exists = store.TryGetValue(request.Key, out var current);
			if (request.IfMatch is { } ifMatch)
			{
				if (!exists)
					throw NotFound();
				if (ifMatch.Trim('"') != current.ETag)
					throw PreconditionFailed();
			}

			_ = store.Remove(request.Key);
		}
		return new DeleteObjectResponse();
	}

	private static AmazonS3Exception NotFound() => new("Not Found") { StatusCode = HttpStatusCode.NotFound };

	private static AmazonS3Exception PreconditionFailed() => new("Precondition Failed") { StatusCode = HttpStatusCode.PreconditionFailed };
}
