// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions.TestingHelpers;
using Documentation.Assembler.Deploying;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;
using FluentAssertions;
using Moq;
using Amazon.Runtime;

namespace Documentation.Assembler.Tests;

public class DocsSyncTests
{
	[Fact]
	public async Task TestPlan()
	{
		// Arrange
		IReadOnlyCollection<IDiagnosticsOutput> diagnosticsOutputs = [];
		var collector = new DiagnosticsCollector(diagnosticsOutputs);
		var mockS3Client = new Mock<IAmazonS3>();
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/add1.md", new MockFileData("# New Document 1") },
			{ "docs/add2.md", new MockFileData("# New Document 2") },
			{ "docs/add3.md", new MockFileData("# New Document 3") },
			{ "docs/skip.md", new MockFileData("# Skipped Document") },
			{ "docs/update.md", new MockFileData("# Existing Document") },
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly")
		});

		var context = new AssembleContext("dev", collector, fileSystem, fileSystem, null, Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly"));

		mockS3Client.Setup(client => client.ListObjectsV2Async(
			It.IsAny<Amazon.S3.Model.ListObjectsV2Request>(),
			It.IsAny<Cancel>()
		)).ReturnsAsync(new Amazon.S3.Model.ListObjectsV2Response
		{
			S3Objects =
			[
				new Amazon.S3.Model.S3Object
				{
					Key = "docs/delete.md",
				},
				new Amazon.S3.Model.S3Object
				{
					Key = "docs/skip.md",
					ETag = "\"69048c0964c9577a399b138b706a467a\"" // This is the result of CalculateS3ETag
				},
				new Amazon.S3.Model.S3Object
				{
					Key = "docs/update.md",
					ETag = "\"existing-etag\""
				}
			]
		});
		var planStrategy = new AwsS3SyncPlanStrategy(mockS3Client.Object, "fake", context, new LoggerFactory());

		// Act
		var plan = await planStrategy.Plan(Cancel.None);

		// Assert
		plan.AddRequests.Count.Should().Be(3);
		plan.AddRequests.Should().Contain(i => i.DestinationPath == "docs/add1.md");
		plan.AddRequests.Should().Contain(i => i.DestinationPath == "docs/add2.md");
		plan.AddRequests.Should().Contain(i => i.DestinationPath == "docs/add3.md");

		plan.UpdateRequests.Count.Should().Be(1);
		plan.UpdateRequests.Should().Contain(i => i.DestinationPath == "docs/update.md");

		plan.SkipRequests.Count.Should().Be(1);
		plan.SkipRequests.Should().Contain(i => i.DestinationPath == "docs/skip.md");

		plan.DeleteRequests.Count.Should().Be(1);
		plan.DeleteRequests.Should().Contain(i => i.DestinationPath == "docs/delete.md");
	}

	[Fact]
	public async Task TestApply()
	{
		// Arrange
		IReadOnlyCollection<IDiagnosticsOutput> diagnosticsOutputs = [];
		var collector = new DiagnosticsCollector(diagnosticsOutputs);

		// Create a real S3 client with minimal configuration
		var s3Config = new AmazonS3Config
		{
			UseHttp = true,
			ForcePathStyle = true
		};
		var s3Client = new AmazonS3Client(new AnonymousAWSCredentials(), s3Config);
		var mockS3Client = new Mock<IAmazonS3>();
		var mockTransferUtility = new Mock<ITransferUtility>();

		// Setup the mock to use the same config as the real client
		mockS3Client.SetupGet(x => x.Config).Returns(s3Config);

		// Create a mock filesystem with test files and temp directory
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/add1.md", new MockFileData("# New Document 1") },
			{ "docs/add2.md", new MockFileData("# New Document 2") },
			{ "docs/add3.md", new MockFileData("# New Document 3") },
			{ "docs/skip.md", new MockFileData("# Skipped Document") },
			{ "docs/update.md", new MockFileData("# Existing Document") },
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly")
		});

		var context = new AssembleContext("dev", collector, fileSystem, fileSystem, null, Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly"));

		var plan = new SyncPlan
		{
			Count = 6,
			AddRequests = [
				new AddRequest { LocalPath = "docs/add1.md", DestinationPath = "docs/add1.md" },
				new AddRequest { LocalPath = "docs/add2.md", DestinationPath = "docs/add2.md" },
				new AddRequest { LocalPath = "docs/add3.md", DestinationPath = "docs/add3.md" }
			],
			UpdateRequests = [
				new UpdateRequest
					{ LocalPath = "docs/update.md", DestinationPath = "docs/update.md" }
			],
			SkipRequests = [
				new SkipRequest
					{ LocalPath = "docs/skip.md", DestinationPath = "docs/skip.md" }
			],
			DeleteRequests = [
				new DeleteRequest
					{ DestinationPath = "docs/delete.md" }
			]
		};

		// Setup S3 client to handle DeleteObjects operation
		mockS3Client.Setup(client => client.DeleteObjectsAsync(
			It.IsAny<Amazon.S3.Model.DeleteObjectsRequest>(),
			It.IsAny<Cancel>()
		)).ReturnsAsync(new Amazon.S3.Model.DeleteObjectsResponse
		{
			HttpStatusCode = System.Net.HttpStatusCode.OK
		});

		// Setup TransferUtility to verify upload request
		mockTransferUtility.Setup(utility => utility.UploadDirectoryAsync(
			It.IsAny<TransferUtilityUploadDirectoryRequest>(),
			It.IsAny<Cancel>()
		)).Callback<TransferUtilityUploadDirectoryRequest, Cancel>((request, _) =>
		{
			var files = context.ReadFileSystem.Directory.GetFiles(request.Directory, request.SearchPattern, request.SearchOption);
			files.Length.Should().Be(4); // 3 add requests + 1 update request
		});

		var applier = new AwsS3SyncApplyStrategy(mockS3Client.Object, mockTransferUtility.Object, "fake", context, new LoggerFactory(), collector);

		// Act
		await applier.Apply(plan, Cancel.None);

		// Assert
		mockS3Client.Verify(client => client.DeleteObjectsAsync(
			It.Is<Amazon.S3.Model.DeleteObjectsRequest>(req => req.Objects.Any(o => o.Key == "docs/delete.md")),
			It.IsAny<Cancel>()), Times.Once);

		mockTransferUtility.Verify(utility => utility.UploadDirectoryAsync(
			It.Is<TransferUtilityUploadDirectoryRequest>(req =>
				req.BucketName == "fake" &&
				req.SearchPattern == "*" &&
				req.SearchOption == SearchOption.AllDirectories &&
				req.UploadFilesConcurrently),
			It.IsAny<Cancel>()), Times.Once);
	}
}
