// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
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

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
public class ChangelogUploadServiceTests
{
	private readonly MockFileSystem _mockFileSystem;
	private readonly ScopedFileSystem _fileSystem;
	private readonly IAmazonS3 _s3Client = A.Fake<IAmazonS3>();
	private readonly ChangelogUploadService _service;
	private readonly TestDiagnosticsCollector _collector;
	private readonly string _changelogDir;

	public ChangelogUploadServiceTests(ITestOutputHelper output)
	{
		_mockFileSystem = new MockFileSystem(new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		_fileSystem = FileSystemFactory.ScopeCurrentWorkingDirectory(_mockFileSystem);
		_service = new ChangelogUploadService(NullLoggerFactory.Instance, fileSystem: _fileSystem, s3Client: _s3Client);
		_collector = new TestDiagnosticsCollector(output);
		_changelogDir = _mockFileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog");
		_mockFileSystem.Directory.CreateDirectory(_changelogDir);
	}

	private string AddChangelog(string fileName, string yaml)
	{
		var path = _mockFileSystem.Path.Join(_changelogDir, fileName);
		_mockFileSystem.AddFile(path, new MockFileData(yaml));
		return path;
	}

	[Fact]
	public void DiscoverUploadTargets_SingleProduct_MapsToCorrectS3Key()
	{
		// language=yaml
		var path = AddChangelog("entry.yaml", """
			title: New feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - "100"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(1);
		targets[0].LocalPath.Should().Be(path);
		targets[0].S3Key.Should().Be("elasticsearch/changelogs/entry.yaml");
		_collector.Errors.Should().Be(0);
	}

	[Fact]
	public void DiscoverUploadTargets_MultipleProducts_CreatesTargetPerProduct()
	{
		// language=yaml
		AddChangelog("fix.yaml", """
			title: Cross-product fix
			type: bug-fix
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			prs:
			  - "200"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(2);
		targets.Should().Contain(t => t.S3Key == "elasticsearch/changelogs/fix.yaml");
		targets.Should().Contain(t => t.S3Key == "kibana/changelogs/fix.yaml");
	}

	[Fact]
	public void DiscoverUploadTargets_InvalidProductName_SkipsWithWarning()
	{
		// language=yaml
		AddChangelog("bad.yaml", """
			title: Bad product
			type: feature
			products:
			  - product: "../traversal"
			prs:
			  - "300"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().BeEmpty();
		_collector.Warnings.Should().BeGreaterThan(0);
	}

	[Fact]
	public void DiscoverUploadTargets_NoProducts_ReturnsEmpty()
	{
		// language=yaml
		AddChangelog("noproducts.yaml", """
			title: No products
			type: feature
			prs:
			  - "400"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().BeEmpty();
		_collector.Errors.Should().Be(0);
	}

	[Fact]
	public void DiscoverUploadTargets_EmptyDirectory_ReturnsEmpty()
	{
		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().BeEmpty();
	}

	[Fact]
	public void DiscoverUploadTargets_MixedValidAndInvalidProducts_FiltersCorrectly()
	{
		// language=yaml
		AddChangelog("mixed.yaml", """
			title: Mixed products
			type: feature
			products:
			  - product: elasticsearch
			  - product: "bad product with spaces"
			  - product: kibana
			prs:
			  - "500"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(2);
		targets.Should().Contain(t => t.S3Key == "elasticsearch/changelogs/mixed.yaml");
		targets.Should().Contain(t => t.S3Key == "kibana/changelogs/mixed.yaml");
		_collector.Warnings.Should().BeGreaterThan(0);
	}

	[Fact]
	public void DiscoverUploadTargets_MultipleFiles_DiscoversBoth()
	{
		// language=yaml
		AddChangelog("first.yaml", """
			title: First
			type: feature
			products:
			  - product: elasticsearch
			prs:
			  - "1"
			""");
		// language=yaml
		AddChangelog("second.yaml", """
			title: Second
			type: bug-fix
			products:
			  - product: kibana
			prs:
			  - "2"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(2);
		targets.Should().Contain(t => t.S3Key == "elasticsearch/changelogs/first.yaml");
		targets.Should().Contain(t => t.S3Key == "kibana/changelogs/second.yaml");
	}

	[Fact]
	public void DiscoverUploadTargets_ProductWithHyphensAndUnderscores_Accepted()
	{
		// language=yaml
		AddChangelog("hyphen.yaml", """
			title: Hyphenated
			type: feature
			products:
			  - product: elastic-agent
			  - product: cloud_hosted
			prs:
			  - "600"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(2);
		targets.Should().Contain(t => t.S3Key == "elastic-agent/changelogs/hyphen.yaml");
		targets.Should().Contain(t => t.S3Key == "cloud_hosted/changelogs/hyphen.yaml");
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().Be(0);
	}

	[Fact]
	public async Task Upload_WithValidChangelogs_UploadsToS3()
	{
		// language=yaml
		AddChangelog("entry.yaml", """
			title: New feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - "100"
			""");

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse());

		var args = new ChangelogUploadArguments
		{
			ArtifactType = ArtifactType.Changelog,
			Target = UploadTargetKind.S3,
			S3BucketName = "test-bucket",
			Directory = _changelogDir
		};
		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, args, ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "elasticsearch/changelogs/entry.yaml" && r.BucketName == "test-bucket"),
			A<CancellationToken>._
		)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Upload_EmptyDirectory_ReturnsTrue()
	{
		var args = new ChangelogUploadArguments
		{
			ArtifactType = ArtifactType.Changelog,
			Target = UploadTargetKind.S3,
			S3BucketName = "test-bucket",
			Directory = _changelogDir
		};
		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, args, ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_WithFailedUpload_ReturnsFalseAndEmitsError()
	{
		// language=yaml
		AddChangelog("fail.yaml", """
			title: Will fail
			type: feature
			products:
			  - product: elasticsearch
			prs:
			  - "700"
			""");

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Access Denied") { StatusCode = HttpStatusCode.Forbidden });

		var args = new ChangelogUploadArguments
		{
			ArtifactType = ArtifactType.Changelog,
			Target = UploadTargetKind.S3,
			S3BucketName = "test-bucket",
			Directory = _changelogDir
		};
		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, args, ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task Upload_ElasticsearchTarget_SkipsWithoutS3Calls()
	{
		AddChangelog("skip.yaml", """
			title: Ignored
			type: feature
			products:
			  - product: elasticsearch
			prs:
			  - "800"
			""");

		var args = new ChangelogUploadArguments
		{
			ArtifactType = ArtifactType.Changelog,
			Target = UploadTargetKind.Elasticsearch,
			S3BucketName = "test-bucket",
			Directory = _changelogDir
		};
		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, args, ct);

		result.Should().BeTrue();

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_BundleArtifactType_UploadsToS3()
	{
		AddChangelog("v9.2.0.yaml", """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1234-feature.yaml
			      checksum: abc123
			    type: enhancement
			    title: New feature
			""");

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Returns(new PutObjectResponse());

		var args = new ChangelogUploadArguments
		{
			ArtifactType = ArtifactType.Bundle,
			Target = UploadTargetKind.S3,
			S3BucketName = "test-bucket",
			Directory = _changelogDir
		};
		var ct = TestContext.Current.CancellationToken;
		var result = await _service.Upload(_collector, args, ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "elasticsearch/bundles/v9.2.0.yaml" && r.BucketName == "test-bucket"),
			A<CancellationToken>._
		)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public void DiscoverBundleUploadTargets_MultipleProducts_CreatesTargetPerProduct()
	{
		AddChangelog("v9.2.0.yaml", """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			entries:
			  - file:
			      name: 1234-feature.yaml
			""");

		var targets = _service.DiscoverBundleUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(2);
		targets.Should().Contain(t => t.S3Key == "elasticsearch/bundles/v9.2.0.yaml");
		targets.Should().Contain(t => t.S3Key == "kibana/bundles/v9.2.0.yaml");
	}

	[Fact]
	public void DiscoverBundleUploadTargets_InvalidProduct_SkipsWithWarning()
	{
		AddChangelog("bad-bundle.yaml", """
			products:
			  - product: "../traversal"
			entries: []
			""");

		var targets = _service.DiscoverBundleUploadTargets(_collector, _changelogDir);

		targets.Should().BeEmpty();
		_collector.Warnings.Should().BeGreaterThan(0);
	}
}
