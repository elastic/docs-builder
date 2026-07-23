// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using AwesomeAssertions;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Configuration;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Tests.Uploading;

/// <summary>
/// Backfill-mode behavior of <see cref="ChangelogUploadService"/>: explicit file selection,
/// create-only writes, and strict registry failure semantics — plus proof that live (non-backfill)
/// behavior is unchanged.
/// </summary>
[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
public class ChangelogUploadBackfillTests
{
	private readonly MockFileSystem _mockFileSystem;
	private readonly ScopedFileSystem _fileSystem;
	private readonly IAmazonS3 _s3Client = A.Fake<IAmazonS3>();
	private readonly ChangelogUploadService _service;
	private readonly TestDiagnosticsCollector _collector;
	private readonly string _bundleDir;

	// language=yaml
	private const string ElasticsearchBundleYaml = """
		products:
		  - product: elasticsearch
		    target: 9.0.0
		    lifecycle: ga
		    repo: elasticsearch
		    owner: elastic
		entries:
		  - file:
		      name: 1234-feature.yaml
		      checksum: abc123def456
		    type: enhancement
		    title: Historical feature
		    prs:
		      - https://github.com/elastic/elasticsearch/pull/1234
		""";

	public ChangelogUploadBackfillTests(ITestOutputHelper output)
	{
		_mockFileSystem = new MockFileSystem(new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		_fileSystem = FileSystemFactory.ScopeCurrentWorkingDirectory(_mockFileSystem);
		_service = new ChangelogUploadService(NullLoggerFactory.Instance, fileSystem: _fileSystem, s3Client: _s3Client);
		_collector = new TestDiagnosticsCollector(output);
		_bundleDir = _mockFileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "releases");
		_mockFileSystem.Directory.CreateDirectory(_bundleDir);
	}

	private string AddBundle(string fileName, string yaml)
	{
		var path = _mockFileSystem.Path.Join(_bundleDir, fileName);
		_mockFileSystem.AddFile(path, new MockFileData(Encoding.UTF8.GetBytes(yaml)));
		return path;
	}

	private static string S3Etag(string yaml) =>
		Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(yaml)));

	private ChangelogUploadArguments BackfillArgs(params string[] files) => new()
	{
		ArtifactType = ArtifactType.Bundle,
		Target = UploadTargetKind.S3,
		S3BucketName = "test-bucket",
		Directory = _bundleDir,
		Backfill = true,
		Files = files
	};

	private void RemoteObjectsAbsent() =>
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

	private void RegistriesAbsent() =>
		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

	private void PutsSucceed() =>
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse());

	[Fact]
	public async Task Upload_Backfill_UploadsExactlySelectedFiles()
	{
		var selected = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);
		_ = AddBundle("elasticsearch-9.1.0.yaml", ElasticsearchBundleYaml.Replace("9.0.0", "9.1.0"));

		RemoteObjectsAbsent();
		RegistriesAbsent();
		PutsSucceed();

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs(selected), ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// The selected file is written create-only; the unselected sibling in the same directory is untouched.
		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/elasticsearch/elasticsearch-9.0.0.yaml" && r.IfNoneMatch == "*"),
			A<CancellationToken>._
		)).MustHaveHappenedOnceExactly();

		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/elasticsearch/elasticsearch-9.1.0.yaml"),
			A<CancellationToken>._
		)).MustNotHaveHappened();

		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/elasticsearch/registry.json"),
			A<CancellationToken>._
		)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Upload_Backfill_RelativeSelection_ResolvesAgainstBundleDirectory()
	{
		_ = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		RemoteObjectsAbsent();
		RegistriesAbsent();
		PutsSucceed();

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs("elasticsearch-9.0.0.yaml"), ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/elasticsearch/elasticsearch-9.0.0.yaml"),
			A<CancellationToken>._
		)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Upload_Backfill_MultiProductBundle_CreatesTargetPerProduct()
	{
		// language=yaml
		var selected = AddBundle("stack-9.0.0.yaml", """
			products:
			  - product: elasticsearch
			    target: 9.0.0
			    repo: elasticsearch
			  - product: kibana
			    target: 9.0.0
			    repo: kibana
			entries:
			  - file:
			      name: 9999-cross-product.yaml
			      checksum: aaa111bbb222
			    type: enhancement
			    title: Cross-product improvement
			""");

		RemoteObjectsAbsent();
		RegistriesAbsent();
		PutsSucceed();

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs(selected), ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/elasticsearch/stack-9.0.0.yaml" && r.IfNoneMatch == "*"),
			A<CancellationToken>._
		)).MustHaveHappenedOnceExactly();

		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/kibana/stack-9.0.0.yaml" && r.IfNoneMatch == "*"),
			A<CancellationToken>._
		)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Upload_Backfill_IdenticalRemoteContent_SkipsAndSucceeds()
	{
		var selected = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Returns(new GetObjectMetadataResponse { ETag = $"\"{S3Etag(ElasticsearchBundleYaml)}\"" });
		RegistriesAbsent();
		PutsSucceed();

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs(selected), ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// The bundle object is skipped (identical content), but the registry is still reconciled.
		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/elasticsearch/elasticsearch-9.0.0.yaml"),
			A<CancellationToken>._
		)).MustNotHaveHappened();

		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/elasticsearch/registry.json"),
			A<CancellationToken>._
		)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Upload_Backfill_ExistingKeyDifferentContent_FailsWithoutOverwrite()
	{
		var selected = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Returns(new GetObjectMetadataResponse { ETag = "\"different-remote-content\"" });

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs(selected), ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);

		// Nothing is written: neither the conflicting object nor a registry entry misrepresenting it.
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_Backfill_ConcurrentCreateLosesRace_FailsAsConflict()
	{
		var selected = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		// The key is absent at inspection time, but the conditional PUT loses the race (412).
		RemoteObjectsAbsent();
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Precondition Failed") { StatusCode = HttpStatusCode.PreconditionFailed });

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs(selected), ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);

		// The conflicted target is excluded from registry reconciliation.
		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/elasticsearch/registry.json"),
			A<CancellationToken>._
		)).MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_Backfill_RegistryRefreshException_FailsOperation()
	{
		var selected = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		RemoteObjectsAbsent();
		PutsSucceed();
		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Internal Server Error") { StatusCode = HttpStatusCode.InternalServerError });

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs(selected), ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task Upload_Backfill_RegistryConcurrencyExhaustion_FailsOperation()
	{
		var selected = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		RemoteObjectsAbsent();
		RegistriesAbsent();
		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/elasticsearch/elasticsearch-9.0.0.yaml"),
			A<CancellationToken>._
		)).Returns(new PutObjectResponse());
		// Every conditional registry write loses the optimistic-concurrency race.
		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "bundle/elasticsearch/registry.json"),
			A<CancellationToken>._
		)).Throws(new AmazonS3Exception("Precondition Failed") { StatusCode = HttpStatusCode.PreconditionFailed });

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs(selected), ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task Upload_Live_RegistryRefreshException_DoesNotFailUpload()
	{
		// Live (non-backfill) semantics are unchanged: a registry refresh failure is a warning,
		// not an error, because the bundle objects themselves are already in S3.
		_ = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		RemoteObjectsAbsent();
		PutsSucceed();
		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Internal Server Error") { StatusCode = HttpStatusCode.InternalServerError });

		var args = new ChangelogUploadArguments
		{
			ArtifactType = ArtifactType.Bundle,
			Target = UploadTargetKind.S3,
			S3BucketName = "test-bucket",
			Directory = _bundleDir
		};
		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, args, ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task Upload_Backfill_WithoutFiles_FailsWithoutS3Calls()
	{
		_ = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs(), ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_Backfill_ChangelogArtifactType_Rejected()
	{
		var args = new ChangelogUploadArguments
		{
			ArtifactType = ArtifactType.Changelog,
			Target = UploadTargetKind.S3,
			S3BucketName = "test-bucket",
			Directory = _bundleDir,
			Backfill = true,
			Files = ["entry.yaml"]
		};
		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, args, ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_Backfill_WithSkipEtagCheck_Rejected()
	{
		var selected = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		var args = BackfillArgs(selected) with { SkipEtagCheck = true };
		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, args, ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_FilesWithoutBackfill_Rejected()
	{
		var selected = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		var args = BackfillArgs(selected) with { Backfill = false };
		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, args, ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_Backfill_MissingSelectedFile_FailsBeforeAnyWrite()
	{
		var missing = _mockFileSystem.Path.Join(_bundleDir, "does-not-exist.yaml");

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs(missing), ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_Backfill_SelectedBundleWithoutProducts_FailsBeforeAnyWrite()
	{
		// A silently skipped selection would violate "uploads exactly the selected files":
		// a file the operator named but that cannot be mapped to a destination is an error.
		// language=yaml
		var selected = AddBundle("no-products.yaml", """
			entries:
			  - file:
			      name: 1-old.yaml
			      checksum: deadbeef
			    type: bug-fix
			    title: No destination derivable
			""");
		_ = AddBundle("elasticsearch-9.0.0.yaml", ElasticsearchBundleYaml);

		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, BackfillArgs(selected, _mockFileSystem.Path.Join(_bundleDir, "elasticsearch-9.0.0.yaml")), ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);

		// Fails before any write — including the valid file in the same selection.
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}
}
