// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using AwesomeAssertions;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Integrations.S3;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Tests.Uploading;

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
public class RegistryBuilderTests
{
	private readonly MockFileSystem _mockFileSystem;
	private readonly ScopedFileSystem _fileSystem;
	private readonly IAmazonS3 _s3Client = A.Fake<IAmazonS3>();
	private readonly TestDiagnosticsCollector _collector;
	private readonly string _bundleDir;
	private readonly RegistryBuilder _builder;
	private readonly List<PutObjectRequest> _puts = [];

	public RegistryBuilderTests(ITestOutputHelper output)
	{
		_mockFileSystem = new MockFileSystem(new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		_fileSystem = FileSystemFactory.ScopeCurrentWorkingDirectory(_mockFileSystem);
		_collector = new TestDiagnosticsCollector(output);
		_bundleDir = _mockFileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "releases");
		_mockFileSystem.Directory.CreateDirectory(_bundleDir);
		var etagCalculator = new S3EtagCalculator(NullLoggerFactory.Instance, _fileSystem);
		// Pin time so generated_at is deterministic in tests.
		var fixedTime = new FakeTimeProvider(new DateTimeOffset(2026, 5, 6, 12, 0, 0, TimeSpan.Zero));
		_builder = new RegistryBuilder(
			NullLoggerFactory.Instance,
			_fileSystem,
			_s3Client,
			etagCalculator,
			"test-bucket",
			fixedTime);

		// Capture every manifest PUT so tests can inspect the body and conditional headers.
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Invokes((PutObjectRequest r, CancellationToken _) => _puts.Add(r))
			.Returns(new PutObjectResponse());
	}

	private string AddBundle(string fileName, string product, string target)
	{
		var path = _mockFileSystem.Path.Join(_bundleDir, fileName);
		// language=yaml
		_mockFileSystem.AddFile(path, new MockFileData($$"""
			products:
			  - product: {{product}}
			    target: {{target}}
			    repo: {{product}}
			    owner: elastic
			entries:
			  - file:
			      name: 1-feature.yaml
			      checksum: deadbeef
			    type: enhancement
			    title: Sample
			"""));
		return path;
	}

	private void StubExistingManifestNotFound() =>
		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

	private void StubExistingManifest(string product, Registry manifest, string etag = "\"existing-etag\"") =>
		A.CallTo(() => _s3Client.GetObjectAsync(
				A<GetObjectRequest>.That.Matches(r => r.Key == $"{product}/registry.json"),
				A<CancellationToken>._))
			.ReturnsLazily(() => MakeManifestResponse(manifest, etag));

	private static GetObjectResponse MakeManifestResponse(Registry manifest, string etag)
	{
		var json = JsonSerializer.Serialize(manifest, RegistryJsonContext.Default.Registry);
		return new GetObjectResponse
		{
			ETag = etag,
			ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(json))
		};
	}

	private static Registry Deserialize(string? json) =>
		JsonSerializer.Deserialize(json!, RegistryJsonContext.Default.Registry)!;

	[Fact]
	public async Task Refresh_NoExistingManifest_CreatesManifestWithIfNoneMatch()
	{
		var path = AddBundle("9.3.0.yaml", "elasticsearch", "9.3.0");
		var targets = new List<UploadTarget> { new(path, "elasticsearch/bundle/9.3.0.yaml") };
		StubExistingManifestNotFound();

		var result = await _builder.RefreshAsync(_collector, targets, TestContext.Current.CancellationToken);

		result.Updated.Should().Be(1);
		_puts.Should().ContainSingle();
		var put = _puts[0];
		put.Key.Should().Be("elasticsearch/registry.json");
		put.IfNoneMatch.Should().Be("*");
		put.IfMatch.Should().BeNull();

		var manifest = Deserialize(put.ContentBody);
		manifest.Product.Should().Be("elasticsearch");
		manifest.Bundles.Should().ContainSingle();
		manifest.Bundles[0].File.Should().Be("9.3.0.yaml");
		manifest.Bundles[0].Target.Should().Be("9.3.0");
		manifest.Bundles[0].ETag.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task Refresh_ExistingManifest_MergesByFileNameAndUsesIfMatch()
	{
		var existing = new Registry
		{
			Product = "elasticsearch",
			GeneratedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
			Bundles =
			[
				new RegistryBundle { File = "9.2.0.yaml", Target = "9.2.0", ETag = "old-etag-1" },
				new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = "old-etag-2" }
			]
		};
		StubExistingManifest("elasticsearch", existing, "\"manifest-v1\"");

		// Re-upload of 9.3.0 (new content → new ETag) plus a brand-new 9.4.0.
		var newer = AddBundle("9.3.0.yaml", "elasticsearch", "9.3.0");
		var ten = AddBundle("9.4.0.yaml", "elasticsearch", "9.4.0");
		var targets = new List<UploadTarget>
		{
			new(newer, "elasticsearch/bundle/9.3.0.yaml"),
			new(ten,   "elasticsearch/bundle/9.4.0.yaml")
		};

		var result = await _builder.RefreshAsync(_collector, targets, TestContext.Current.CancellationToken);

		result.Updated.Should().Be(1);
		_puts.Should().ContainSingle();
		_puts[0].IfMatch.Should().Be("\"manifest-v1\"");
		_puts[0].IfNoneMatch.Should().BeNull();

		var manifest = Deserialize(_puts[0].ContentBody);
		manifest.Bundles.Should().HaveCount(3);
		manifest.Bundles.Should().Contain(b => b.File == "9.2.0.yaml" && b.ETag == "old-etag-1");

		var nineThree = manifest.Bundles.Single(b => b.File == "9.3.0.yaml");
		nineThree.ETag.Should().NotBe("old-etag-2"); // replaced by the freshly-uploaded ETag
		manifest.Bundles.Should().Contain(b => b.File == "9.4.0.yaml");

		manifest.Bundles[0].File.Should().Be("9.4.0.yaml"); // sorted target-desc
	}

	[Fact]
	public async Task Refresh_SortsManifestByVersionNotLexicographically()
	{
		StubExistingManifestNotFound();

		// Version order (not byte order): 9.10.0 must come before 9.9.0 in the written manifest.
		var v910 = AddBundle("9.10.0.yaml", "elasticsearch", "9.10.0");
		var v99 = AddBundle("9.9.0.yaml", "elasticsearch", "9.9.0");
		var targets = new List<UploadTarget>
		{
			new(v99, "elasticsearch/bundle/9.9.0.yaml"),
			new(v910, "elasticsearch/bundle/9.10.0.yaml")
		};

		_ = await _builder.RefreshAsync(_collector, targets, TestContext.Current.CancellationToken);

		var manifest = Deserialize(_puts[0].ContentBody);
		manifest.Bundles.Select(b => b.Target).Should().Equal("9.10.0", "9.9.0");
	}

	[Fact]
	public async Task Refresh_MultipleProducts_WritesOneManifestPerProduct()
	{
		var es = AddBundle("9.3.0.yaml", "elasticsearch", "9.3.0");
		var kb = AddBundle("kb-9.3.0.yaml", "kibana", "9.3.0");
		var targets = new List<UploadTarget>
		{
			new(es, "elasticsearch/bundle/9.3.0.yaml"),
			new(kb, "kibana/bundle/kb-9.3.0.yaml")
		};
		StubExistingManifestNotFound();

		var result = await _builder.RefreshAsync(_collector, targets, TestContext.Current.CancellationToken);

		result.Updated.Should().Be(2);
		_puts.Should().HaveCount(2);
		_puts.Should().Contain(p => p.Key == "elasticsearch/registry.json");
		_puts.Should().Contain(p => p.Key == "kibana/registry.json");
	}

	[Fact]
	public async Task Refresh_MultiProductBundle_RecordsTargetPerProduct()
	{
		// One bundle file declaring two products with *different* targets.
		var path = _mockFileSystem.Path.Join(_bundleDir, "multi.yaml");
		// language=yaml
		_mockFileSystem.AddFile(path, new MockFileData("""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    repo: elasticsearch
			    owner: elastic
			  - product: kibana
			    target: 9.4.0
			    repo: kibana
			    owner: elastic
			entries:
			  - file:
			      name: 1-feature.yaml
			      checksum: deadbeef
			    type: enhancement
			    title: Sample
			"""));
		var targets = new List<UploadTarget>
		{
			new(path, "elasticsearch/bundle/multi.yaml"),
			new(path, "kibana/bundle/multi.yaml")
		};
		StubExistingManifestNotFound();

		_ = await _builder.RefreshAsync(_collector, targets, TestContext.Current.CancellationToken);

		var es = Deserialize(_puts.Single(p => p.Key == "elasticsearch/registry.json").ContentBody);
		es.Bundles[0].Target.Should().Be("9.3.0");

		var kb = Deserialize(_puts.Single(p => p.Key == "kibana/registry.json").ContentBody);
		kb.Bundles[0].Target.Should().Be("9.4.0");
	}

	[Fact]
	public async Task Refresh_ExistingManifestUnreadable_OverwritesUsingLiveETag()
	{
		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.ReturnsLazily(() => new GetObjectResponse
			{
				ETag = "\"corrupt-etag\"",
				ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes("not json {{{"))
			});

		var path = AddBundle("9.3.0.yaml", "elasticsearch", "9.3.0");
		var targets = new List<UploadTarget> { new(path, "elasticsearch/bundle/9.3.0.yaml") };

		var result = await _builder.RefreshAsync(_collector, targets, TestContext.Current.CancellationToken);

		result.Updated.Should().Be(1);
		_puts.Should().ContainSingle();
		_puts[0].IfMatch.Should().Be("\"corrupt-etag\""); // conditional overwrite of the corrupt object
		var manifest = Deserialize(_puts[0].ContentBody);
		manifest.Bundles.Should().ContainSingle();
		manifest.Bundles[0].File.Should().Be("9.3.0.yaml");
	}

	[Fact]
	public async Task Refresh_BundleWithoutTarget_RecordsEntryWithoutTarget()
	{
		var path = _mockFileSystem.Path.Join(_bundleDir, "no-target.yaml");
		// language=yaml
		_mockFileSystem.AddFile(path, new MockFileData("""
			entries: []
			"""));
		var targets = new List<UploadTarget> { new(path, "elasticsearch/bundle/no-target.yaml") };
		StubExistingManifestNotFound();

		var result = await _builder.RefreshAsync(_collector, targets, TestContext.Current.CancellationToken);

		result.Updated.Should().Be(1);
		var manifest = Deserialize(_puts[0].ContentBody);
		manifest.Bundles.Should().ContainSingle();
		manifest.Bundles[0].Target.Should().BeNull();
		manifest.Bundles[0].ETag.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task Refresh_UnchangedManifest_SkipsWrite()
	{
		var path = AddBundle("9.3.0.yaml", "elasticsearch", "9.3.0");
		var etagCalculator = new S3EtagCalculator(NullLoggerFactory.Instance, _fileSystem);
		var bundleEtag = await etagCalculator.CalculateS3ETag(path, TestContext.Current.CancellationToken);

		// Existing manifest already contains exactly what this run would produce.
		var existing = new Registry
		{
			Product = "elasticsearch",
			GeneratedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
			Bundles = [new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = bundleEtag }]
		};
		StubExistingManifest("elasticsearch", existing, "\"manifest-v1\"");

		var targets = new List<UploadTarget> { new(path, "elasticsearch/bundle/9.3.0.yaml") };
		var result = await _builder.RefreshAsync(_collector, targets, TestContext.Current.CancellationToken);

		result.Unchanged.Should().Be(1);
		result.Updated.Should().Be(0);
		_puts.Should().BeEmpty();
	}

	[Fact]
	public async Task Refresh_ConcurrentWrite_RetriesAfterPreconditionFailed()
	{
		var v1 = new Registry
		{
			Product = "elasticsearch",
			GeneratedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
			Bundles = [new RegistryBundle { File = "9.2.0.yaml", Target = "9.2.0", ETag = "etag-92" }]
		};
		// Second read reflects a concurrent writer that added 9.3.0 and bumped the object ETag.
		var v2 = new Registry
		{
			Product = "elasticsearch",
			GeneratedAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
			Bundles =
			[
				new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = "etag-93" },
				new RegistryBundle { File = "9.2.0.yaml", Target = "9.2.0", ETag = "etag-92" }
			]
		};
		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.ReturnsNextFromSequence(
				MakeManifestResponse(v1, "\"manifest-v1\""),
				MakeManifestResponse(v2, "\"manifest-v2\""));

		// First PUT loses the optimistic-concurrency race; the second succeeds.
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Precondition Failed") { StatusCode = HttpStatusCode.PreconditionFailed })
			.Once();

		var path = AddBundle("9.4.0.yaml", "elasticsearch", "9.4.0");
		var targets = new List<UploadTarget> { new(path, "elasticsearch/bundle/9.4.0.yaml") };

		var result = await _builder.RefreshAsync(_collector, targets, TestContext.Current.CancellationToken);

		result.Updated.Should().Be(1);
		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.MustHaveHappenedTwiceExactly();
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.MustHaveHappenedTwiceExactly();

		// Two PUTs were attempted (asserted above); the first threw on the precondition failure before
		// _puts.Add ran, so only the successful retry is captured here. It used the re-read ETag and
		// merged both the concurrent and local entries.
		_puts.Should().ContainSingle();
		_puts[0].IfMatch.Should().Be("\"manifest-v2\"");
		var manifest = Deserialize(_puts[0].ContentBody);
		manifest.Bundles.Select(b => b.File).Should().BeEquivalentTo(["9.4.0.yaml", "9.3.0.yaml", "9.2.0.yaml"]);
	}

	[Fact]
	public async Task Refresh_PersistentConcurrentWrite_EmitsWarningAndReportsFailure()
	{
		var existing = new Registry
		{
			Product = "elasticsearch",
			GeneratedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
			Bundles = [new RegistryBundle { File = "9.2.0.yaml", Target = "9.2.0", ETag = "etag-92" }]
		};
		StubExistingManifest("elasticsearch", existing, "\"manifest-v1\"");

		// Every PUT loses the race.
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Precondition Failed") { StatusCode = HttpStatusCode.PreconditionFailed });

		var path = AddBundle("9.4.0.yaml", "elasticsearch", "9.4.0");
		var targets = new List<UploadTarget> { new(path, "elasticsearch/bundle/9.4.0.yaml") };

		var result = await _builder.RefreshAsync(_collector, targets, TestContext.Current.CancellationToken);

		result.Failed.Should().Be(1);
		result.Updated.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task Refresh_NoTargets_WritesNothing()
	{
		var result = await _builder.RefreshAsync(_collector, [], TestContext.Current.CancellationToken);
		result.Should().Be(new RegistryBuilder.RefreshResult(0, 0, 0));
		_puts.Should().BeEmpty();
	}

	private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
