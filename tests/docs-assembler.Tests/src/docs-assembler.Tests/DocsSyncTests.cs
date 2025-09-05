// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Documentation.Assembler.Deploying;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling.Diagnostics;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var config = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		var context = new AssembleContext(config, configurationContext, "dev", collector, fileSystem, fileSystem, null, Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly"));
		A.CallTo(() => mockS3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<Cancel>._))
			.Returns(new ListObjectsV2Response
			{
				S3Objects =
				[
					new S3Object { Key = "docs/delete.md" },
					new S3Object
					{
						Key = "docs/skip.md",
						ETag = "\"69048c0964c9577a399b138b706a467a\""
					}, // This is the result of CalculateS3ETag
					new S3Object
					{
						Key = "docs/update.md",
						ETag = "\"existing-etag\""
					}
				]
			});
		var planStrategy = new AwsS3SyncPlanStrategy(new LoggerFactory(), mockS3Client, "fake", context);

		// Act
		var plan = await planStrategy.Plan(null, Cancel.None);

		// Assert

		plan.TotalRemoteFiles.Should().Be(3);

		plan.TotalSourceFiles.Should().Be(5);
		plan.TotalSyncRequests.Should().Be(6); //including skip on server

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

	[Theory]
	[InlineData(0, 10_000, 10_000, 0, 10_000, 0.2, false)]
	[InlineData(8_000, 10_000, 10_000, 0, 2000, 0.2, true)]
	[InlineData(7900, 10_000, 10_000, 0, 2100, 0.2, false)]
	[InlineData(10_000, 0, 10_000, 10_000, 0, 0.2, true)]
	[InlineData(2000, 0, 2000, 2000, 0, 0.2, true)]
	// When total files to sync is lower than 100 we enforce a minimum ratio of 0.8
	[InlineData(20, 40, 40, 0, 20, 0.2, true)]
	[InlineData(19, 100, 100, 0, 81, 0.2, false)]
	// When total files to sync is lower than 1000 we enforce a minimum ratio of 0.5
	[InlineData(200, 400, 400, 0, 200, 0.2, true)]
	[InlineData(199, 1000, 1000, 0, 801, 0.2, false)]
	public async Task ValidateAdditionsPlan(
		int localFiles,
		int remoteFiles,
		int totalFilesToSync,
		int totalFilesToAdd,
		int totalFilesToRemove,
		float deleteThreshold,
		bool valid
	)
	{
		var (validator, _, plan) = await SetupS3SyncContextSetup(localFiles, remoteFiles, deleteThreshold);

		// Assert

		plan.TotalSourceFiles.Should().Be(localFiles);
		plan.TotalSyncRequests.Should().Be(totalFilesToSync);

		plan.AddRequests.Count.Should().Be(totalFilesToAdd);
		plan.DeleteRequests.Count.Should().Be(totalFilesToRemove);

		var validationResult = validator.Validate(plan);
		if (plan.TotalSyncRequests <= 100)
			validationResult.DeleteThreshold.Should().Be(Math.Max(deleteThreshold, 0.8f));
		else if (plan.TotalSyncRequests <= 1000)
			validationResult.DeleteThreshold.Should().Be(Math.Max(deleteThreshold, 0.5f));

		validationResult.Valid.Should().Be(valid, $"Delete ratio is {validationResult.DeleteRatio} when maximum is {validationResult.DeleteThreshold}");
	}

	[Theory]
	[InlineData(10_000, 0, 10_000, 0, 0, 0.2, true)]
	[InlineData(2000, 0, 2000, 0, 0, 0.2, true)]
	[InlineData(0, 10_000, 10_000, 0, 10_000, 0.2, false)]
	[InlineData(0, 10_000, 10_000, 0, 10_000, 1.0, false)]
	[InlineData(20, 10_000, 10_000, 20, 9980, 0.2, false)]
	[InlineData(20, 10_000, 10_000, 20, 9980, 1.0, true)]
	[InlineData(8_000, 10_000, 10_000, 8000, 2000, 0.2, true)]
	[InlineData(7900, 10_000, 10_000, 7900, 2100, 0.2, false)]
	public async Task ValidateUpdatesPlan(
		int localFiles,
		int remoteFiles,
		int totalFilesToSync,
		int totalFilesToUpdate,
		int totalFilesToRemove,
		float deleteThreshold,
		bool valid
	)
	{
		var (validator, _, plan) = await SetupS3SyncContextSetup(localFiles, remoteFiles, deleteThreshold, "different-etag");

		// Assert

		plan.TotalSourceFiles.Should().Be(localFiles);
		plan.TotalSyncRequests.Should().Be(totalFilesToSync);

		plan.UpdateRequests.Count.Should().Be(totalFilesToUpdate);
		plan.DeleteRequests.Count.Should().Be(totalFilesToRemove);

		var validationResult = validator.Validate(plan);
		if (plan.TotalSyncRequests <= 100)
			validationResult.DeleteThreshold.Should().Be(Math.Max(deleteThreshold, 0.8f));
		else if (plan.TotalSyncRequests <= 1000)
			validationResult.DeleteThreshold.Should().Be(Math.Max(deleteThreshold, 0.5f));

		validationResult.Valid.Should().Be(valid, $"Delete ratio is {validationResult.DeleteRatio} when maximum is {validationResult.DeleteThreshold}");
	}

	private static async Task<(DocsSyncPlanValidator validator, AwsS3SyncPlanStrategy planStrategy, SyncPlan plan)> SetupS3SyncContextSetup(
		int localFiles, int remoteFiles, float? deleteThreshold = null, string etag = "etag")
	{
		// Arrange
		IReadOnlyCollection<IDiagnosticsOutput> diagnosticsOutputs = [];
		var collector = new DiagnosticsCollector(diagnosticsOutputs);
		var mockS3Client = A.Fake<IAmazonS3>();
		var fileSystem = new MockFileSystem(new MockFileSystemOptions
		{
			CurrentDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly")
		});
		foreach (var i in Enumerable.Range(0, localFiles))
			fileSystem.AddFile($"docs/file-{i}.md", new MockFileData($"# Local Document {i}"));

		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var config = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		var context = new AssembleContext(config, configurationContext, "dev", collector, fileSystem, fileSystem, null, Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly"));

		var s3Objects = new List<S3Object>();
		foreach (var i in Enumerable.Range(0, remoteFiles))
		{
			s3Objects.Add(new S3Object
			{
				Key = $"docs/file-{i}.md",
				ETag = etag
			});
		}

		A.CallTo(() => mockS3Client.ListObjectsV2Async(A<ListObjectsV2Request>._, A<Cancel>._))
			.Returns(new ListObjectsV2Response
			{
				S3Objects = s3Objects
			});

		var mockEtagCalculator = A.Fake<IS3EtagCalculator>();
		A.CallTo(() => mockEtagCalculator.CalculateS3ETag(A<string>._, A<Cancel>._)).Returns("etag");
		var planStrategy = new AwsS3SyncPlanStrategy(new LoggerFactory(), mockS3Client, "fake", context, mockEtagCalculator);

		// Act
		var plan = await planStrategy.Plan(deleteThreshold, Cancel.None);
		var validator = new DocsSyncPlanValidator(new LoggerFactory());
		return (validator, planStrategy, plan);
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
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var config = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		var checkoutDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly");
		var context = new AssembleContext(config, configurationContext, "dev", collector, fileSystem, fileSystem, null, checkoutDirectory);
		var plan = new SyncPlan
		{
			RemoteListingCompleted = true,
			DeleteThresholdDefault = null,
			TotalRemoteFiles = 0,
			TotalSourceFiles = 5,
			TotalSyncRequests = 6,
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
		A.CallTo(() => moxS3Client.DeleteObjectsAsync(A<DeleteObjectsRequest>._, A<Cancel>._))
			.Returns(new DeleteObjectsResponse
			{
				HttpStatusCode = System.Net.HttpStatusCode.OK
			});
		var transferredFiles = Array.Empty<string>();
		A.CallTo(() => moxTransferUtility.UploadDirectoryAsync(A<TransferUtilityUploadDirectoryRequest>._, A<Cancel>._))
			.Invokes((TransferUtilityUploadDirectoryRequest request, Cancel _) =>
			{
				transferredFiles = fileSystem.Directory.GetFiles(request.Directory, request.SearchPattern, request.SearchOption);
			});
		var applier = new AwsS3SyncApplyStrategy(new LoggerFactory(), moxS3Client, moxTransferUtility, "fake", context, collector);

		// Act
		await applier.Apply(plan, Cancel.None);

		// Assert
		transferredFiles.Length.Should().Be(4); // 3 add requests + 1 update request
		transferredFiles.Should().NotContain("docs/skip.md");

		A.CallTo(() => moxS3Client.DeleteObjectsAsync(A<DeleteObjectsRequest>._, A<Cancel>._))
			.MustHaveHappenedOnceExactly();

		A.CallTo(() => moxTransferUtility.UploadDirectoryAsync(A<TransferUtilityUploadDirectoryRequest>._, A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}
}
