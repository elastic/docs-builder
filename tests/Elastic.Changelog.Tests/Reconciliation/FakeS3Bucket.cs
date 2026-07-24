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
/// A stateful in-memory S3 bucket behind a FakeItEasy <see cref="IAmazonS3"/>: objects live in a
/// dictionary, ETags are the MD5 of the content (matching real single-part uploads and
/// <c>S3EtagCalculator</c>), and conditional PUTs (<c>If-Match</c> / <c>If-None-Match</c>) enforce
/// real 412 semantics. Every write call is recorded so tests can assert exactly what mutated —
/// or that nothing did.
/// </summary>
internal sealed class FakeS3Bucket
{
	private readonly Dictionary<string, (string Content, string ETag)> _objects = [with(StringComparer.Ordinal)];

	public IAmazonS3 Client { get; } = A.Fake<IAmazonS3>();

	/// <summary>Every <c>PutObject</c> call received, in order.</summary>
	public List<PutObjectRequest> Puts { get; } = [];

	/// <summary>Every <c>CopyObject</c> call received, in order.</summary>
	public List<CopyObjectRequest> Copies { get; } = [];

	/// <summary>Runs once immediately before the first <c>PutObject</c> is evaluated — simulates a concurrent writer.</summary>
	public Action? BeforeFirstPut { get; set; }

	/// <summary>Runs before every <c>ListObjects</c> evaluation with the 1-based call number — simulates propagation mid-poll.</summary>
	public Action<int>? OnList { get; set; }

	/// <summary>Number of <c>ListObjects</c> calls received.</summary>
	public int ListCalls { get; private set; }

	private bool _firstPutSeen;

	public FakeS3Bucket()
	{
		_ = A.CallTo(() => Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<CancellationToken>._))
			.ReturnsLazily((ListObjectsV2Request r, CancellationToken _) => List(r));

		_ = A.CallTo(() => Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.ReturnsLazily((GetObjectRequest r, CancellationToken _) => Get(r));

		_ = A.CallTo(() => Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.ReturnsLazily((GetObjectMetadataRequest r, CancellationToken _) => Head(r));

		_ = A.CallTo(() => Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.ReturnsLazily((PutObjectRequest r, CancellationToken _) => Put(r));

		_ = A.CallTo(() => Client.CopyObjectAsync(A<CopyObjectRequest>._, A<CancellationToken>._))
			.ReturnsLazily((CopyObjectRequest r, CancellationToken _) => Copy(r));
	}

	// MD5 is what real S3 uses for single-part ETags; this mirrors S3EtagCalculator.
	[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
	public static string ETagOf(string content) =>
		Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(content)));

	/// <summary>Seeds or replaces an object; returns its (unquoted) ETag.</summary>
	public string Seed(string key, string content)
	{
		var etag = ETagOf(content);
		_objects[key] = (content, etag);
		return etag;
	}

	public void Remove(string key) => _objects.Remove(key);

	public bool Exists(string key) => _objects.ContainsKey(key);

	public string ContentOf(string key) => _objects[key].Content;

	private ListObjectsV2Response List(ListObjectsV2Request request)
	{
		ListCalls++;
		OnList?.Invoke(ListCalls);
		return new ListObjectsV2Response
		{
			S3Objects = _objects
				.Where(kv => kv.Key.StartsWith(request.Prefix ?? string.Empty, StringComparison.Ordinal))
				.OrderBy(kv => kv.Key, StringComparer.Ordinal)
				.Select(kv => new S3Object
				{
					Key = kv.Key,
					ETag = $"\"{kv.Value.ETag}\"",
					Size = kv.Value.Content.Length,
					LastModified = new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc)
				})
				.ToList(),
			IsTruncated = false
		};
	}

	private GetObjectResponse Get(GetObjectRequest request)
	{
		if (!_objects.TryGetValue(request.Key, out var obj))
			throw NotFound();

		return new GetObjectResponse
		{
			ETag = $"\"{obj.ETag}\"",
			ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(obj.Content))
		};
	}

	private GetObjectMetadataResponse Head(GetObjectMetadataRequest request)
	{
		if (!_objects.ContainsKey(request.Key))
			throw NotFound();

		var response = new GetObjectMetadataResponse();
		response.Headers.ContentType = "application/yaml";
		response.Metadata.Add("x-amz-meta-origin", "test");
		return response;
	}

	private PutObjectResponse Put(PutObjectRequest request)
	{
		if (!_firstPutSeen)
		{
			_firstPutSeen = true;
			BeforeFirstPut?.Invoke();
		}

		Puts.Add(request);

		var exists = _objects.TryGetValue(request.Key, out var current);
		if (request.IfNoneMatch == "*" && exists)
			throw PreconditionFailed();
		if (request.IfMatch is { } ifMatch && (!exists || ifMatch.Trim('"') != current.ETag))
			throw PreconditionFailed();

		_ = Seed(request.Key, request.ContentBody);
		return new PutObjectResponse();
	}

	private CopyObjectResponse Copy(CopyObjectRequest request)
	{
		Copies.Add(request);
		if (!_objects.TryGetValue(request.SourceKey, out var value))
			throw NotFound();

		_objects[request.DestinationKey] = value;
		return new CopyObjectResponse();
	}

	private static AmazonS3Exception NotFound() =>
		new("Not Found") { StatusCode = HttpStatusCode.NotFound };

	private static AmazonS3Exception PreconditionFailed() =>
		new("Precondition Failed") { StatusCode = HttpStatusCode.PreconditionFailed };
}
