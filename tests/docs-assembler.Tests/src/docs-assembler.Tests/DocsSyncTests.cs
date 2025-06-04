// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Amazon.S3;
using Amazon.S3.Transfer;
using Documentation.Assembler.Deploying;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Site;
using Elastic.Markdown.IO;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Tests;

public class DocsSyncTests
{
	[Fact]
	public async Task TestPlan()
	{
		// Arrange
		IReadOnlyCollection<IDiagnosticsOutput> diagnosticsOutputs = [];
		var collector = new DiagnosticsCollector(diagnosticsOutputs);
		var mockS3Client = A.Fake<IAmazonS3>();
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
		A.CallTo(() => mockS3Client.ListObjectsV2Async(A<Amazon.S3.Model.ListObjectsV2Request>._, A<Cancel>._))
			.Returns(new Amazon.S3.Model.ListObjectsV2Response
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
						ETag = "\"69048c0964c9577a399b138b706a467a\""
					}, // This is the result of CalculateS3ETag
					new Amazon.S3.Model.S3Object
					{
						Key = "docs/update.md",
						ETag = "\"existing-etag\""
					}
				]
			});
		var planStrategy = new AwsS3SyncPlanStrategy(mockS3Client, "fake", context, new LoggerFactory());

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
		var moxS3Client = A.Fake<IAmazonS3>();
		var moxTransferUtility = A.Fake<ITransferUtility>();
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
		A.CallTo(() => moxS3Client.DeleteObjectsAsync(A<Amazon.S3.Model.DeleteObjectsRequest>._, A<Cancel>._))
			.Returns(new Amazon.S3.Model.DeleteObjectsResponse
			{
				HttpStatusCode = System.Net.HttpStatusCode.OK
			});
		var transferredFiles = Array.Empty<string>();
		A.CallTo(() => moxTransferUtility.UploadDirectoryAsync(A<TransferUtilityUploadDirectoryRequest>._, A<Cancel>._))
			.Invokes((TransferUtilityUploadDirectoryRequest request, Cancel _) =>
			{
				transferredFiles = fileSystem.Directory.GetFiles(request.Directory, request.SearchPattern, request.SearchOption);
			});
		var applier = new AwsS3SyncApplyStrategy(moxS3Client, moxTransferUtility, "fake", context, new LoggerFactory(), collector);

		// Act
		await applier.Apply(plan, Cancel.None);

		// Assert
		transferredFiles.Length.Should().Be(4); // 3 add requests + 1 update request
		transferredFiles.Should().NotContain("docs/skip.md");

		A.CallTo(() => moxS3Client.DeleteObjectsAsync(A<Amazon.S3.Model.DeleteObjectsRequest>._, A<Cancel>._))
			.MustHaveHappenedOnceExactly();

		A.CallTo(() => moxTransferUtility.UploadDirectoryAsync(A<TransferUtilityUploadDirectoryRequest>._, A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}
}
