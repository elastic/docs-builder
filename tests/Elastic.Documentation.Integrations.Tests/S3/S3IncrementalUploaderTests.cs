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

	private string UniquePath(string name) =>
		_fileSystem.Path.Join(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), name);

	[Fact]
	public async Task Upload_NewFile_UploadsSuccessfully()
	{
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData("new changelog"u8.ToArray()));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._))
			.Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._))
			.Returns(new PutObjectResponse());

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([new UploadTarget(path, "elasticsearch/changelogs/entry.yaml")], ct);

		result.Uploaded.Should().Be(1);
		result.Skipped.Should().Be(0);
		result.Failed.Should().Be(0);

		A.CallTo(() => _s3Client.PutObjectAsync(
			A<PutObjectRequest>.That.Matches(r => r.Key == "elasticsearch/changelogs/entry.yaml" && r.BucketName == BucketName),
			A<Cancel>._
		)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Upload_UnchangedFile_SkipsUpload()
	{
		var content = "unchanged changelog"u8.ToArray();
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData(content));
		var localEtag = Convert.ToHexStringLower(MD5.HashData(content));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._))
			.Returns(new GetObjectMetadataResponse { ETag = $"\"{localEtag}\"" });

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([new UploadTarget(path, "kibana/changelogs/entry.yaml")], ct);

		result.Uploaded.Should().Be(0);
		result.Skipped.Should().Be(1);
		result.Failed.Should().Be(0);

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task Upload_ChangedFile_UploadsNewVersion()
	{
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData("updated changelog"u8.ToArray()));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._))
			.Returns(new GetObjectMetadataResponse { ETag = "\"stale-etag\"" });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._))
			.Returns(new PutObjectResponse());

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([new UploadTarget(path, "elasticsearch/changelogs/entry.yaml")], ct);

		result.Uploaded.Should().Be(1);
		result.Skipped.Should().Be(0);
		result.Failed.Should().Be(0);
	}

	[Fact]
	public async Task Upload_S3PutFails_CountsAsFailure()
	{
		var path = UniquePath("entry.yaml");
		_fileSystem.AddFile(path, new MockFileData("content"u8.ToArray()));

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(A<GetObjectMetadataRequest>._, A<Cancel>._))
			.Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._))
			.Throws(new AmazonS3Exception("Access Denied") { StatusCode = HttpStatusCode.Forbidden });

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([new UploadTarget(path, "elasticsearch/changelogs/entry.yaml")], ct);

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

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(
			A<GetObjectMetadataRequest>.That.Matches(r => r.Key == "es/changelogs/new.yaml"),
			A<Cancel>._
		)).Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

		A.CallTo(() => _s3Client.GetObjectMetadataAsync(
			A<GetObjectMetadataRequest>.That.Matches(r => r.Key == "es/changelogs/unchanged.yaml"),
			A<Cancel>._
		)).Returns(new GetObjectMetadataResponse { ETag = $"\"{unchangedEtag}\"" });

		A.CallTo(() => _s3Client.PutObjectAsync(A<PutObjectRequest>._, A<Cancel>._))
			.Returns(new PutObjectResponse());

		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([
			new UploadTarget(newPath, "es/changelogs/new.yaml"),
			new UploadTarget(unchangedPath, "es/changelogs/unchanged.yaml")
		], ct);

		result.Uploaded.Should().Be(1);
		result.Skipped.Should().Be(1);
		result.Failed.Should().Be(0);
	}

	[Fact]
	public async Task Upload_EmptyList_ReturnsZeroCounts()
	{
		var uploader = CreateUploader();
		var ct = TestContext.Current.CancellationToken;
		var result = await uploader.Upload([], ct);

		result.Uploaded.Should().Be(0);
		result.Skipped.Should().Be(0);
		result.Failed.Should().Be(0);
	}
}
