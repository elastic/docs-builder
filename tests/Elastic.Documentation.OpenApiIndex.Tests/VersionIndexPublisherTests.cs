// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using AwesomeAssertions;
using FakeItEasy;

namespace Elastic.Documentation.OpenApiIndex.Tests;

public class VersionIndexPublisherTests
{
	private const string BucketName = "test-bucket";

	/// <summary>The exact serialized index for a bucket holding only <c>elastic/elasticsearch/8.16/openapi.json</c>.</summary>
	private const string Index816Json = /*lang=json,strict*/  """{"elastic/elasticsearch":{"openapi.json":{"8":{"version":"8.16"}}}}""";

	private readonly IAmazonS3 _s3Client = A.Fake<IAmazonS3>();
	private readonly List<PutObjectRequest> _puts = [];

	public VersionIndexPublisherTests() =>
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._))
			.Invokes((PutObjectRequest request, Cancel _) => _puts.Add(request))
			.Returns(new PutObjectResponse());

	private VersionIndexPublisher CreatePublisher() => new(_s3Client, BucketName);

	private void GivenBucketContains(params string[] keys) =>
		A.CallTo(() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<Cancel>._)).Returns(new ListObjectsV2Response
		{
			S3Objects = [.. keys.Select(k => new S3Object { Key = k })],
			IsTruncated = false
		});

	private void GivenNoPublishedIndex() =>
		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<Cancel>._)).Throws(new AmazonS3Exception("Not Found")
		{
			StatusCode = HttpStatusCode.NotFound
		});

	private void GivenPublishedIndex(string body, string etag = "\"etag-1\"") =>
		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<Cancel>._)).Returns(new GetObjectResponse
		{
			ETag = etag,
			ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(body))
		});

	[Fact]
	public async Task RefreshAsync_NoPublishedIndex_CreatesWithIfNoneMatch()
	{
		GivenBucketContains("elastic/elasticsearch/8.16/openapi.json");
		GivenNoPublishedIndex();

		await CreatePublisher().RefreshAsync(TestContext.Current.CancellationToken);

		var put = _puts.Should().ContainSingle().Subject;
		put.BucketName.Should().Be(BucketName);
		put.Key.Should().Be(VersionIndexPublisher.IndexKey);
		put.IfNoneMatch.Should().Be("*");
		put.IfMatch.Should().BeNull();
		put.ContentBody.Should().Be(Index816Json);
	}

	[Fact]
	public async Task RefreshAsync_PublishedIndexIsStale_UpdatesWithIfMatch()
	{
		GivenBucketContains("elastic/elasticsearch/8.17/openapi.json");
		GivenPublishedIndex(Index816Json, "\"stale-etag\"");

		await CreatePublisher().RefreshAsync(TestContext.Current.CancellationToken);

		var put = _puts.Should().ContainSingle().Subject;
		put.IfMatch.Should().Be("\"stale-etag\"");
		put.IfNoneMatch.Should().BeNull();
		put.ContentBody.Should().Contain("8.17");
	}

	[Fact]
	public async Task RefreshAsync_RebuildMatchesPublishedIndexByteForByte_SkipsWrite()
	{
		GivenBucketContains("elastic/elasticsearch/8.16/openapi.json");
		GivenPublishedIndex(Index816Json);

		await CreatePublisher().RefreshAsync(TestContext.Current.CancellationToken);

		_puts.Should().BeEmpty();
	}

	[Fact]
	public async Task RefreshAsync_ConditionalWriteConflict_ThrowsWithoutRetrying()
	{
		// A conflict means another invocation updated index.json concurrently. RefreshAsync does not retry
		// in process — it throws, so the handler returns the message to the queue and SQS redelivers it.
		GivenBucketContains("elastic/elasticsearch/8.16/openapi.json");
		GivenNoPublishedIndex();
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).Throws(new AmazonS3Exception("Precondition Failed")
		{
			StatusCode = HttpStatusCode.PreconditionFailed
		});

		var act = () => CreatePublisher().RefreshAsync(TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<AmazonS3Exception>();
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task RefreshAsync_KeyOfUnexpectedShape_IsReturnedAndTheRestIndexed()
	{
		GivenBucketContains("elastic/elasticsearch/8.16/openapi.json", "not-a-valid-key");
		GivenNoPublishedIndex();

		var ignoredKeys = await CreatePublisher().RefreshAsync(TestContext.Current.CancellationToken);

		ignoredKeys.Should().ContainSingle().Which.Should().Be("not-a-valid-key");
		_puts.Should().ContainSingle().Which.ContentBody.Should().Contain("8.16");
	}

	[Fact]
	public async Task RefreshAsync_IndexKeyItselfInListing_IsExcludedFromRebuild()
	{
		GivenBucketContains("elastic/elasticsearch/8.16/openapi.json", VersionIndexPublisher.IndexKey);
		GivenNoPublishedIndex();

		var ignoredKeys = await CreatePublisher().RefreshAsync(TestContext.Current.CancellationToken);

		ignoredKeys.Should().BeEmpty();
	}

	[Fact]
	public async Task RefreshAsync_PaginatedListing_CombinesAllPages()
	{
		A.CallTo(
			() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>.That.Matches(r => r.ContinuationToken == null), A<Cancel>._)
		).Returns(new ListObjectsV2Response
		{
			S3Objects = [new S3Object { Key = "elastic/elasticsearch/8.16/openapi.json" }],
			IsTruncated = true,
			NextContinuationToken = "page-2"
		});
		A.CallTo(
			() => _s3Client.ListObjectsV2Async(A<ListObjectsV2Request>.That.Matches(r => r.ContinuationToken == "page-2"), A<Cancel>._)
		).Returns(new ListObjectsV2Response
		{
			S3Objects = [new S3Object { Key = "elastic/kibana/8.16/kibana.json" }],
			IsTruncated = false
		});
		GivenNoPublishedIndex();

		await CreatePublisher().RefreshAsync(TestContext.Current.CancellationToken);

		var put = _puts.Should().ContainSingle().Subject;
		put.ContentBody.Should().Contain("elasticsearch").And.Contain("kibana");
	}
}
