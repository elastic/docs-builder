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
using Elastic.Documentation.Integrations.S3;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Integrations.Tests.S3;

[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
public class S3IncrementalUploaderTests
{
	private readonly MockFileSystem _fileSystem = new();
	private readonly IAmazonS3 _s3Client = A.Fake<IAmazonS3>();
	private const string BucketName = "test-bucket";

	private S3IncrementalUploader CreateUploader() =>
		new(NullLoggerFactory.Instance, _s3Client, _fileSystem, new S3EtagCalculator(NullLoggerFactory.Instance, _fileSystem), BucketName);

	private string UniquePath(string name) => _fileSystem.Path.Join(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), name);

	[Fact]
	public async Task Upload_NewFile_UploadsSuccessfully()
	{
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData("new changelog"u8.ToArray()));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).Throws(new AmazonS3Exception(
			"Not Found"
		)
		{ StatusCode = HttpStatusCode.NotFound });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).Returns(new PutObjectResponse());

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([new UploadTarget(path, "elasticsearch/changelog/entry.yaml")], ctx: ct);

		result.Uploaded.Should().Be(1);
		result.New.Should().Be(1);
		result.Replaced.Should().Be(0);
		result.Skipped.Should().Be(0);
		result.NotOverwritten.Should().Be(0);
		result.Failed.Should().Be(0);

		A.CallTo(
			() => _s3Client.PutObjectAsync(
				A<PutObjectRequest>.That.Matches(r => r.Key == "elasticsearch/changelog/entry.yaml" && r.BucketName == BucketName),
				A<Cancel>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Upload_UnchangedFile_SkipsUpload()
	{
		var content = "unchanged changelog"u8.ToArray();
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData(content));
		var localEtag = Convert.ToHexStringLower(MD5.HashData(content));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).Returns(new GetObjectMetadataResponse
		{
			ETag = $"\"{localEtag}\""
		});

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([new UploadTarget(path, "kibana/changelog/entry.yaml")], ctx: ct);

		result.Uploaded.Should().Be(0);
		result.New.Should().Be(0);
		result.Replaced.Should().Be(0);
		result.Skipped.Should().Be(1);
		result.NotOverwritten.Should().Be(0);
		result.Failed.Should().Be(0);

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_UnchangedFile_WithSkipEtagCheck_Uploads()
	{
		var content = "unchanged changelog"u8.ToArray();
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData(content));
		var localEtag = Convert.ToHexStringLower(MD5.HashData(content));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).Returns(new GetObjectMetadataResponse
		{
			ETag = $"\"{localEtag}\""
		});

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).Returns(new PutObjectResponse());

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([new UploadTarget(path, "kibana/changelog/entry.yaml")], skipEtagCheck: true, ctx: ct);

		result.Uploaded.Should().Be(1);
		result.New.Should().Be(0);
		result.Replaced.Should().Be(1);
		result.Skipped.Should().Be(0);
		result.Failed.Should().Be(0);

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).MustHaveHappenedOnceExactly();

		A.CallTo(
			() => _s3Client.PutObjectAsync(
				A<PutObjectRequest>.That.Matches(r => r.Key == "kibana/changelog/entry.yaml" && r.BucketName == BucketName),
				A<Cancel>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Upload_ChangedFile_UploadsNewVersion()
	{
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData("updated changelog"u8.ToArray()));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).Returns(new GetObjectMetadataResponse
		{
			ETag = "\"stale-etag\""
		});

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).Returns(new PutObjectResponse());

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([new UploadTarget(path, "elasticsearch/changelog/entry.yaml")], ctx: ct);

		result.Uploaded.Should().Be(1);
		result.New.Should().Be(0);
		result.Replaced.Should().Be(1);
		result.Skipped.Should().Be(0);
		result.Failed.Should().Be(0);
	}

	[Fact]
	public async Task Upload_S3PutFails_CountsAsFailure()
	{
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData("content"u8.ToArray()));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).Throws(new AmazonS3Exception(
			"Not Found"
		)
		{ StatusCode = HttpStatusCode.NotFound });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).Throws(new AmazonS3Exception("Access Denied")
		{
			StatusCode = HttpStatusCode.Forbidden
		});

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([new UploadTarget(path, "elasticsearch/changelog/entry.yaml")], ctx: ct);

		result.Uploaded.Should().Be(0);
		result.Skipped.Should().Be(0);
		result.Failed.Should().Be(1);
	}

	[Fact]
	public async Task Upload_MixedTargets_ReportsCorrectCounts()
	{
		var newPath = UniquePath("new.yaml");
		var unchangedPath = UniquePath("unchanged.yaml");
		_fileSystem.AddFile(newPath, new MockFileData("new"u8.ToArray()));
		_fileSystem.AddFile(unchangedPath, new MockFileData("unchanged"u8.ToArray()));
		var unchangedEtag = Convert.ToHexStringLower(MD5.HashData("unchanged"u8.ToArray()));

		A.CallTo(
			() => _s3Client.GetObjectMetadataAsync(
				A<GetObjectMetadataRequest>.That.Matches(r => r.Key == "es/changelog/new.yaml"),
				A<Cancel>._
			)
		).Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

		A.CallTo(
			() => _s3Client.GetObjectMetadataAsync(
				A<GetObjectMetadataRequest>.That.Matches(r => r.Key == "es/changelog/unchanged.yaml"),
				A<Cancel>._
			)
		).Returns(new GetObjectMetadataResponse { ETag = $"\"{unchangedEtag}\"" });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).Returns(new PutObjectResponse());

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload(
			[new UploadTarget(newPath, "es/changelog/new.yaml"), new UploadTarget(unchangedPath, "es/changelog/unchanged.yaml")],
			ctx: ct
		);

		result.Uploaded.Should().Be(1);
		result.New.Should().Be(1);
		result.Replaced.Should().Be(0);
		result.Skipped.Should().Be(1);
		result.Failed.Should().Be(0);
	}

	[Fact]
	public async Task Upload_EmptyList_ReturnsZeroCounts()
	{
		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([], ctx: ct);

		result.Uploaded.Should().Be(0);
		result.New.Should().Be(0);
		result.Replaced.Should().Be(0);
		result.Skipped.Should().Be(0);
		result.NotOverwritten.Should().Be(0);
		result.Failed.Should().Be(0);
		result.Conflicts.Should().BeEmpty();
	}

	[Fact]
	public async Task Upload_ChangedFile_WithNoOverwrite_SkipsPutAndRecordsRemoteContent()
	{
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData("local cloud-serverless"u8.ToArray()));
		const string remoteYaml = "title: existing\nproducts:\n  - product: elasticsearch\n";

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).Returns(new GetObjectMetadataResponse
		{
			ETag = "\"stale-etag\""
		});

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<Cancel>._)).Returns(new GetObjectResponse
		{
			ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(remoteYaml))
		});

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload(
			[new UploadTarget(path, "changelog/elastic/elasticsearch/main/entry.yaml")],
			new S3UploadOptions { NoOverwrite = true },
			ctx: ct
		);

		result.Uploaded.Should().Be(0);
		result.NotOverwritten.Should().Be(1);
		result.Failed.Should().Be(0);
		result.Conflicts.Should().ContainSingle();
		result.Conflicts[0].S3Key.Should().Be("changelog/elastic/elasticsearch/main/entry.yaml");
		result.Conflicts[0].LocalPath.Should().Be(path);
		result.Conflicts[0].RemoteContent.Should().Be(remoteYaml);

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_NewFile_WithNoOverwrite_Uploads()
	{
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData("new changelog"u8.ToArray()));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).Throws(new AmazonS3Exception(
			"Not Found"
		)
		{ StatusCode = HttpStatusCode.NotFound });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).Returns(new PutObjectResponse());

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload(
			[new UploadTarget(path, "elasticsearch/changelog/entry.yaml")],
			new S3UploadOptions { NoOverwrite = true },
			ctx: ct
		);

		result.New.Should().Be(1);
		result.NotOverwritten.Should().Be(0);
		result.Failed.Should().Be(0);

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Upload_NoOverwrite_WhenGetObjectFails_StillRefusesPut()
	{
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData("local"u8.ToArray()));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).Returns(new GetObjectMetadataResponse
		{
			ETag = "\"stale-etag\""
		});

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<Cancel>._)).Throws(new AmazonS3Exception("Access Denied")
		{
			StatusCode = HttpStatusCode.Forbidden
		});

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload(
			[new UploadTarget(path, "elasticsearch/changelog/entry.yaml")],
			new S3UploadOptions { NoOverwrite = true },
			ctx: ct
		);

		result.NotOverwritten.Should().Be(1);
		result.Conflicts[0].RemoteContent.Should().BeNull();
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_InlineMarker_WithNoOverwriteWhenExists_SkipsPut()
	{
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).Returns(new GetObjectMetadataResponse
		{
			ETag = "\"marker-etag\""
		});

		A.CallTo(() => _s3Client.GetObjectAsync(A<GetObjectRequest>._, A<Cancel>._)).Returns(new GetObjectResponse
		{
			ResponseStream = new MemoryStream("link: 100"u8.ToArray())
		});

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload(
			[new UploadTarget(string.Empty, "changelog/elastic/elasticsearch/main/200.yaml", "link: 100")],
			new S3UploadOptions { NoOverwrite = true },
			ctx: ct
		);

		result.NotOverwritten.Should().Be(1);
		result.Conflicts[0].RemoteContent.Should().Be("link: 100");
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_InlineMarker_MissingKey_UploadsAsNew()
	{
		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._)).Throws(new AmazonS3Exception(
			"Not Found"
		)
		{ StatusCode = HttpStatusCode.NotFound });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).Returns(new PutObjectResponse());

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload(
			[new UploadTarget(string.Empty, "changelog/elastic/elasticsearch/main/200.yaml", "link: 100")],
			ctx: ct
		);

		result.New.Should().Be(1);
		result.Replaced.Should().Be(0);
		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._)).MustHaveHappenedOnceExactly();
	}
}
