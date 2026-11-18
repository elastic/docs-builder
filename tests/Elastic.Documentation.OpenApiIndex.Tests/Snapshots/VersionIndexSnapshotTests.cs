// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using AwesomeAssertions;
using FakeItEasy;
using Nullean.Snapshots;
using Nullean.Snapshots.Json;

namespace Elastic.Documentation.OpenApiIndex.Tests.Snapshots;

/// <summary>
/// The version index built for several sets of bucket keys, the publish call it leads to, and the invalidation paths.
/// The JSON is what <c>index.json</c> holds, so key order (ordinal, so "10" sorts before "8") is part of the record.
/// </summary>
public class VersionIndexSnapshotTests
{
	private static readonly JsonSerializerOptions EmptyOptions = new();

	private static string Render(IEnumerable<string> keys)
	{
		var (index, invalid) = VersionIndexBuilder.Build(keys);
		var json = JsonSerializer.Serialize(
			index,
			VersionIndexJsonContext.Default.SortedDictionaryStringSortedDictionaryStringSortedDictionaryStringVersionIndexEntry
		);
		var invalidJson = JsonSerializer.Serialize(invalid, EmptyOptions);
		return $$"""{"index":{{json}},"invalidKeys":{{invalidJson}}}""";
	}

	[Test]
	public void SeveralReposFilesAndMajors() =>
		Render([
			"elastic/elasticsearch/8.2/openapi.json",
			"elastic/elasticsearch/8.16/openapi.json",
			"elastic/elasticsearch/8.9/openapi.json",
			"elastic/elasticsearch/9.0/openapi.json",
			"elastic/elasticsearch/9.10/openapi.json",
			"elastic/elasticsearch/9.9/openapi.json",
			"elastic/elasticsearch/10.1/openapi.json",
			"elastic/elasticsearch/main/openapi.json",
			"elastic/elasticsearch/8.16/openapi-serverless.json",
			"elastic/kibana/9.1/openapi.json",
			"elastic/kibana/8.19/openapi.json",
			"elastic/kibana/main/openapi.json",
			"example-org/example-repo/1.0/spec.yaml"
		]).Should().MatchJsonSnapshot();

	[Test]
	public void KeysInReverseOrderGiveTheSameIndex() =>
		Render([
			"elastic/kibana/main/openapi.json",
			"elastic/kibana/8.19/openapi.json",
			"elastic/kibana/9.1/openapi.json",
			"elastic/elasticsearch/9.9/openapi.json",
			"elastic/elasticsearch/9.10/openapi.json",
			"elastic/elasticsearch/9.0/openapi.json"
		]).Should().MatchJsonSnapshot();

	[Test]
	public void MainAndMasterShareTheMainMoniker() =>
		Render([
			"elastic/has-master/master/openapi.json",
			"elastic/has-both/master/openapi.json",
			"elastic/has-both/main/openapi.json",
			"elastic/has-both-reversed/main/openapi.json",
			"elastic/has-both-reversed/master/openapi.json"
		]).Should().MatchJsonSnapshot();

	[Test]
	public void SameMinorPublishedTwiceKeepsTheFirstVersionText() =>
		Render([
			"elastic/a/9.05/openapi.json",
			"elastic/a/9.5/openapi.json",
			"elastic/b/9.5/openapi.json",
			"elastic/b/9.05/openapi.json",
			"elastic/c/09.1/openapi.json",
			"elastic/c/9.1/openapi.json"
		]).Should().MatchJsonSnapshot();

	[Test]
	public void KeysThatDoNotMatchTheExpectedShapeAreReportedAndSkipped() =>
		Render([
			"openapi.json",
			"elastic/openapi.json",
			"elastic/repo/openapi.json",
			"elastic/repo/9.1/sub/openapi.json",
			"/elastic/repo/9.1/openapi.json",
			"elastic//9.1/openapi.json",
			"elastic/repo/9.1/",
			"elastic/repo/9/openapi.json",
			"elastic/repo/.5/openapi.json",
			"elastic/repo/9./openapi.json",
			"elastic/repo/+9.1/openapi.json",
			"elastic/repo/9.-1/openapi.json",
			"elastic/repo/9. 1/openapi.json",
			"elastic/repo/9.1.2/openapi.json",
			"elastic/repo/v9.1/openapi.json",
			"elastic/repo/9.x/openapi.json",
			"elastic/repo/MAIN/openapi.json",
			"elastic/repo/9.99999999999/openapi.json",
			"elastic/repo/feature-branch/openapi.json",
			"index.json",
			"elastic/repo/9.1/openapi.json"
		]).Should().MatchJsonSnapshot();

	[Test]
	public void NoKeysGiveAnEmptyIndex() => Render([]).Should().MatchJsonSnapshot();

	[Test]
	public void ThreeSegmentRepoKeysAreRejected() =>
		Render(["elastic/9.1/openapi.json", "a/b/c/d/openapi.json"]).Should().MatchJsonSnapshot();

	[Test]
	public void InvalidationPathsTable()
	{
		var paths = OpenApiInvalidationPaths.Build([
			"elastic/kibana/9.1/openapi.json",
			"/elastic/kibana/9.1/openapi.json",
			"elastic/kibana/9.1/openapi.json",
			"",
			"  ",
			"//double/leading.json",
			"index.json",
			"a/b/main/x.json"
		]);
		string.Join('\n', paths).Should().MatchSnapshot();
	}

	private static async Task<string> PublishAsync(string[][] pages, string? existingBody, string existingEtag = "\"etag-1\"")
	{
		var s3 = A.Fake<IAmazonS3>();
		var puts = new List<PutObjectRequest>();
		_ = A
			.CallTo(() => s3.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._))
			.Invokes((PutObjectRequest r, Cancel _) => puts.Add(r))
			.Returns(new PutObjectResponse());

		var call = 0;
		var tokens = new List<string?>();
		_ = A.CallTo(() => s3.ListObjectsV2Async(A<ListObjectsV2Request>._, A<Cancel>._)).ReturnsLazily((
			ListObjectsV2Request request,
			Cancel _
		) =>
		{
			tokens.Add(request.ContinuationToken);
			var page = pages[call++];
			return new ListObjectsV2Response
			{
				S3Objects = [.. page.Select(k => new S3Object { Key = k })],
				IsTruncated = call < pages.Length,
				NextContinuationToken = $"token-{call}"
			};
		});

		if (existingBody is null)
			_ = A.CallTo(() => s3.GetObjectAsync(A<GetObjectRequest>._, A<Cancel>._)).Throws(new AmazonS3Exception("Not Found")
			{
				StatusCode = HttpStatusCode.NotFound
			});
		else
			_ = A.CallTo(() => s3.GetObjectAsync(A<GetObjectRequest>._, A<Cancel>._)).ReturnsLazily(
				() => new GetObjectResponse { ETag = existingEtag, ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(existingBody)) }
			);

		var invalid = await new VersionIndexPublisher(s3, "example-specs").RefreshAsync(TestContext.Current!.Execution.CancellationToken);

		var sb = new StringBuilder();
		_ = sb.Append(CultureInfo.InvariantCulture, $"list continuation tokens: [{string.Join(", ", tokens.Select(t => t ?? "null"))}]\n");
		_ = sb.Append(CultureInfo.InvariantCulture, $"invalid keys: [{string.Join(", ", invalid)}]\n");
		_ = sb.Append(CultureInfo.InvariantCulture, $"puts: {puts.Count}\n");
		foreach (var put in puts)
			_ = sb.Append(
				CultureInfo.InvariantCulture,
				$"  bucket={put.BucketName} key={put.Key} contentType={put.ContentType} ifMatch={put.IfMatch ?? "null"} ifNoneMatch={put.IfNoneMatch ?? "null"}\n  body={put.ContentBody}\n"
			);
		return sb.ToString();
	}

	private const string Bucket816 = "elastic/elasticsearch/8.16/openapi.json";

	[Test]
	public async Task Publish_NoIndexYet_CreatesWithIfNoneMatch() =>
		(await PublishAsync([[Bucket816, "index.json", "bad-key"]], null)).Should().MatchSnapshot();

	[Test]
	public async Task Publish_UnchangedIndex_WritesNothing() =>
		(await PublishAsync(
			[[Bucket816]],
			"""{"elastic/elasticsearch":{"openapi.json":{"8":{"version":"8.16"}}}}"""
		)).Should().MatchSnapshot();

	[Test]
	public async Task Publish_ChangedIndex_ReplacesWithIfMatch() =>
		(await PublishAsync(
			[[Bucket816, "elastic/elasticsearch/8.17/openapi.json"]],
			"""{"elastic/elasticsearch":{"openapi.json":{"8":{"version":"8.16"}}}}""",
			"\"etag-77\""
		)).Should().MatchSnapshot();

	[Test]
	public async Task Publish_ExistingBodyThatDiffersOnlyInWhitespace_IsRewritten() =>
		(await PublishAsync(
			[[Bucket816]],
			"""{ "elastic/elasticsearch": { "openapi.json": { "8": { "version": "8.16" } } } }"""
		)).Should().MatchSnapshot();

	[Test]
	public async Task Publish_ListingAcrossSeveralPages() =>
		(await PublishAsync(
			[[Bucket816], ["elastic/kibana/9.1/openapi.json", "index.json"], ["elastic/kibana/9.2/openapi.json"]],
			null
		)).Should().MatchSnapshot();

	[Test]
	public async Task Publish_EmptyBucket_WritesAnEmptyObject() => (await PublishAsync([[]], null)).Should().MatchSnapshot();
}
