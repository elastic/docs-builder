// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using AwesomeAssertions;
using Elastic.Changelog.Migration;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Tests.Migration;

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 mirrors the S3 single-part ETag the service compares against")]
public class WebMigrationServiceTests
{
	private const string Bucket = "test-bucket";
	private static readonly string[] InScopeVersions = ["1.10.0", "1.9.0", "1.7.0", "1.4.1"];

	private readonly MockFileSystem _mockFileSystem;
	private readonly ScopedFileSystem _fileSystem;
	private readonly IAmazonS3 _s3Client = A.Fake<IAmazonS3>();
	private readonly TestDiagnosticsCollector _collector;
	private readonly StubHandler _httpHandler;

	public WebMigrationServiceTests(ITestOutputHelper output)
	{
		_mockFileSystem = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });
		_fileSystem = CheckoutsFileSystem.FromWorkingDirectory(_mockFileSystem).Write;
		_collector = new TestDiagnosticsCollector(output);
		_httpHandler =
			new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ReleaseNotesFixture.Markdown) });
	}

	private WebMigrationService CreateService() => new(NullLoggerFactory.Instance, _fileSystem, _s3Client, _httpHandler);

	// Default arguments cover the whole checked-in scope table (today: edot-java only).
	private static MigrateFromWebArguments Args(bool dryRun = false, string bucket = Bucket, string[]? versions = null) =>
		new() { S3BucketName = bucket, DryRun = dryRun, Versions = versions ?? [] };

	private static string Key(string version) => $"bundle/edot-java/{version}.yaml";

	/// <summary>Fakes an empty bucket: every HEAD/GET misses, every PUT succeeds and records the body's MD5.</summary>
	private Dictionary<string, string> FakeEmptyBucket()
	{
		var putEtags = new Dictionary<string, string>(StringComparer.Ordinal);

		A.CallTo(
			() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._)
		).Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._)).Throws(new AmazonS3Exception("Not Found")
		{
			StatusCode = HttpStatusCode.NotFound
		});
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).ReturnsLazily((
			PutObjectRequest request,
			CancellationToken _
		) =>
		{
			using var buffer = new MemoryStream();
			request.InputStream.CopyTo(buffer);
			var etag = Convert.ToHexStringLower(MD5.HashData(buffer.ToArray()));
			putEtags[request.Key] = etag;
			return new PutObjectResponse { ETag = $"\"{etag}\"" };
		});

		return putEtags;
	}

	/// <summary>Fakes a bucket where the given keys already exist with the given ETags.</summary>
	private void FakeExistingKeys(IReadOnlyDictionary<string, string> etags) =>
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._)).ReturnsLazily(
			(GetObjectMetadataRequest request, CancellationToken _) =>
				etags.TryGetValue(request.Key, out var etag)
					? new GetObjectMetadataResponse { ETag = $"\"{etag}\"" }
					: throw new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound }
		);

	[Fact]
	public async Task FirstRun_CreatesEveryInScopeKeyWithCreateOnlySemantics()
	{
		_ = FakeEmptyBucket();
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args(), ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		foreach (var version in InScopeVersions)
		{
			A.CallTo(
				() =>
					_s3Client.PutObjectAsync(
						A<PutObjectRequest>.That.Matches(r => r.Key == Key(version) && r.BucketName == Bucket && r.IfNoneMatch == "*"),
						A<CancellationToken>._
					)
			).MustHaveHappenedOnceExactly();
		}

		service.LastResults.Where(r => r.Outcome == "created").Should().HaveCount(InScopeVersions.Length);
		service.LastResults.Should().AllSatisfy(r => r.Outcome.Should().NotBe("failed"));
	}

	[Fact]
	public async Task Run_NeverWritesARegistryManifest()
	{
		// The scrubber Lambda owns the public manifests and shallow maps (elastic/docs-builder#3738);
		// the client-side refresh is retired (elastic/docs-builder#3760). Migration writes YAML
		// bundle objects only — the S3 events those creates emit trigger the reconciliation.
		_ = FakeEmptyBucket();
		var ct = TestContext.Current.CancellationToken;

		var result = await CreateService().MigrateFromWeb(_collector, Args(), ct);

		result.Should().BeTrue();
		A.CallTo(
			() =>
				_s3Client.PutObjectAsync(
					A<PutObjectRequest>.That.Matches(r => r.Key.EndsWith("registry.json", StringComparison.Ordinal)),
					A<CancellationToken>._
				)
		).MustNotHaveHappened();
	}

	[Fact]
	public async Task SecondRun_OverSameScope_IsANoOpWithAllSkips()
	{
		var putEtags = FakeEmptyBucket();
		var ct = TestContext.Current.CancellationToken;
		_ = await CreateService().MigrateFromWeb(_collector, Args(), ct);
		putEtags.Should().NotBeEmpty();

		// Second run against a bucket that now contains exactly what the first run created.
		Fake.ClearRecordedCalls(_s3Client);
		FakeExistingKeys(putEtags);

		var service = CreateService();
		var result = await service.MigrateFromWeb(_collector, Args(), ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).MustNotHaveHappened();
		service.LastResults.Where(r => r.Detail.Contains("identical content")).Should().HaveCount(InScopeVersions.Length);
	}

	[Fact]
	public async Task ExistingKeyWithDifferentContent_IsSkippedAndNeverOverwritten()
	{
		_ = FakeEmptyBucket();
		FakeExistingKeys(InScopeVersions.ToDictionary(Key, _ => "0000aaaa0000aaaa0000aaaa0000aaaa"));
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args(), ct);

		result.Should().BeTrue("skipping existing keys is the expected safe outcome, not a failure");
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).MustNotHaveHappened();
		service.LastResults.Where(r => r.Detail.Contains("different content")).Should().HaveCount(InScopeVersions.Length);
	}

	[Fact]
	public async Task ConcurrentCreate_PreconditionFailed_IsReportedAsSkipNotFailure()
	{
		_ = FakeEmptyBucket();
		// The key appears between the HEAD check and the conditional PUT: S3 answers 412.
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).Throws(new AmazonS3Exception(
			"Precondition Failed"
		)
		{ StatusCode = HttpStatusCode.PreconditionFailed });
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args(), ct);

		result.Should().BeTrue();
		service.LastResults.Where(r => r.Detail.Contains("concurrently")).Should().HaveCount(InScopeVersions.Length);
	}

	[Fact]
	public async Task PutFailure_IsReportedPerKeyAndFailsTheRun()
	{
		_ = FakeEmptyBucket();
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).Throws(new AmazonS3Exception(
			"Access Denied"
		)
		{ StatusCode = HttpStatusCode.Forbidden });
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args(), ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		service.LastResults
			.Where(r => r.Outcome == "failed" && r.Detail.Contains("Access Denied"))
			.Should()
			.HaveCount(InScopeVersions.Length);
	}

	[Fact]
	public async Task VersionsBeyondTheCutoff_AreSkippedAndNeverUploaded()
	{
		_ = FakeEmptyBucket();
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args(), ct);

		result.Should().BeTrue();
		// 2.0.0 > cutoff 1.10.0: owned by the live pipeline.
		A.CallTo(
			() => _s3Client.PutObjectAsync(A<PutObjectRequest>.That.Matches(r => r.Key == Key("2.0.0")), A<CancellationToken>._)
		).MustNotHaveHappened();
		var cutoffResult = service.LastResults.Should().ContainSingle(r => r.Key == Key("2.0.0")).Subject;
		cutoffResult.Outcome.Should().Be("skipped");
		cutoffResult.Detail.Should().Contain("beyond cutoff 1.10.0");
	}

	[Fact]
	public async Task VersionsFilter_RestrictsTheRunToTheSelection()
	{
		_ = FakeEmptyBucket();
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args(versions: ["1.9.0"]), ct);

		result.Should().BeTrue();
		A.CallTo(
			() => _s3Client.PutObjectAsync(A<PutObjectRequest>.That.Matches(r => r.Key == Key("1.9.0")), A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
		service.LastResults.Where(r => r.Outcome == "created").Should().ContainSingle();
		service.LastResults.Where(r => r.Detail.Contains("--versions")).Should().HaveCount(InScopeVersions.Length - 1);
	}

	[Fact]
	public async Task DryRunWithoutBucket_TouchesNoS3AtAll()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args(dryRun: true, bucket: ""), ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		A.CallTo(_s3Client).MustNotHaveHappened();
		service.LastResults.Where(r => r.Outcome == "would-create").Should().HaveCount(InScopeVersions.Length);
		service.LastResults.Where(r => r.Outcome == "would-create").Should().AllSatisfy(r => r.ETag.Should().NotBeNullOrEmpty());
	}

	[Fact]
	public async Task DryRunWithBucket_InspectsExistenceButNeverWrites()
	{
		_ = FakeEmptyBucket();
		FakeExistingKeys(new Dictionary<string, string> { [Key("1.9.0")] = "0000aaaa0000aaaa0000aaaa0000aaaa" });
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args(dryRun: true), ct);

		result.Should().BeTrue();
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).MustNotHaveHappened();
		service.LastResults.Where(r => r.Outcome == "would-create").Should().HaveCount(InScopeVersions.Length - 1);
		service.LastResults.Should().ContainSingle(r => r.Key == Key("1.9.0") && r.Outcome == "skipped");
	}

	[Fact]
	public async Task ProductNotInScopeTable_FailsWithoutAnyNetworkAccess()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args() with { Products = ["not-configured"] }, ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_httpHandler.RequestedPaths.Should().BeEmpty();
		A.CallTo(_s3Client).MustNotHaveHappened();
	}

	[Fact]
	public async Task ProductsFilter_SelectsOnlyTheRequestedTableEntries()
	{
		_ = FakeEmptyBucket();
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args() with { Products = ["edot-java"] }, ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		service.LastResults.Where(r => r.Outcome == "created").Should().HaveCount(InScopeVersions.Length);
		service.LastResults.Should().AllSatisfy(r => r.Key.Should().StartWith("bundle/edot-java/"));
	}

	[Fact]
	public async Task FetchFailure_FailsWithoutAnyS3Calls()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var service = new WebMigrationService(NullLoggerFactory.Instance, _fileSystem, _s3Client, handler);
		var ct = TestContext.Current.CancellationToken;

		var result = await service.MigrateFromWeb(_collector, Args(), ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		A.CallTo(_s3Client).MustNotHaveHappened();
	}

	[Fact]
	public async Task FetchesTheMarkdownFromThePinnedRefOnRawGithubusercontent()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.MigrateFromWeb(_collector, Args(dryRun: true, bucket: ""), ct);

		_httpHandler.RequestedPaths
			.Should()
			.Equal("/elastic/elastic-otel-java/9a61ce4faaf08e272c433a083bcc6f0e96d80e0a/docs/release-notes/index.md");
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public List<string> RequestedPaths { get; } = [];

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return responder(request);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
