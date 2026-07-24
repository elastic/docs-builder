// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions.TestingHelpers;
using Actions.Core.Services;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AwesomeAssertions;
using Elastic.Codex;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Deploying;
using Elastic.Documentation.Deploying.Synchronization;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Integrations.S3;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.IntegrationTests;

/// <summary>
/// End-to-end round-trip tests that drive <see cref="IncrementalDeployService"/> — plan writes
/// a JSON plan file to a mock filesystem, apply reads it back and syncs against a mocked S3 —
/// for both the assembler and codex contexts.
/// </summary>
/// <remarks>
/// File mix for both tests: 3 adds, 1 update (stale ETag), 1 skip (ETag matches the mocked
/// calculator), 1 remote-only delete. A mocked <see cref="IS3EtagCalculator"/> returns a fixed
/// ETag per file so the skip decision is deterministic regardless of mock-FS internals.
/// </remarks>
public class IncrementalDeployRoundTripTests
{
	// Fixed ETags returned by the mocked calculator
	private const string SkipETag = "aaaa0000skip0000etag0000aaaa0000";
	private const string AnyOtherETag = "bbbb1111other1111etag1111bbbb1111";

	[Fact]
	public async Task AssemblerRoundTrip()
	{
		var outputDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly");
		var (fs, s3, xfer, gh, svc) = Arrange(outputDir);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var config = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		var collector = new DiagnosticsCollector([]);
		var scopedFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fs);
		var scopedWriteFs = FileSystemFactory.ScopeCurrentWorkingDirectoryForWrite(fs);
		var context = new AssembleContext(config, configurationContext, "dev", collector, scopedFs, scopedWriteFs, null, outputDir);

		await RunRoundTrip(fs, s3, xfer, gh, svc, context, outputDir);
	}

	[Fact]
	public async Task CodexRoundTrip()
	{
		var outputDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "codex", "docs");
		var (fs, s3, xfer, gh, svc) = Arrange(outputDir);
		var collector = new DiagnosticsCollector([]);
		var scopedFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fs);
		var scopedWriteFs = FileSystemFactory.ScopeCurrentWorkingDirectoryForWrite(fs);
		// CodexContext only stores configurationPath — it never reads from it —
		// so we can point to any path without adding it to the mock FS.
		var codexConfig = new CodexConfiguration { Environment = "dev" };
		var configFile = fs.FileInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "codex.yml"));
		var context = new CodexContext(codexConfig, configFile, collector, scopedFs, scopedWriteFs, null, outputDir);

		await RunRoundTrip(fs, s3, xfer, gh, svc, context, outputDir);
	}

	/// <summary>
	/// Shared arrange: a mock filesystem with local docs, a mocked ETag calculator, stubbed S3,
	/// and the deploy service.
	/// </summary>
	/// <remarks>
	/// The mocked <see cref="IS3EtagCalculator"/> returns <see cref="SkipETag"/> for
	/// <c>skip.md</c> and <see cref="AnyOtherETag"/> for everything else. The S3 listing mirrors
	/// this so that <c>skip.md</c> matches (→ skip), <c>update.md</c> does not (→ update), and
	/// <c>delete.md</c> has no local counterpart (→ delete). The three <c>add*.md</c> files
	/// have no remote entry (→ add). This exercises every sync category in a single round-trip.
	///
	/// The delete ratio (1/6 ≈ 17 %) is below the enforced 0.8 floor for small sync sets, so
	/// <c>deleteThreshold: 1.0f</c> is passed to allow any deletion ratio.
	/// </remarks>
	private static (MockFileSystem fs, IAmazonS3 s3, ITransferUtility xfer, ICoreService gh, IncrementalDeployService svc)
		Arrange(string outputDir)
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ Path.Join(outputDir, "docs/add1.md"), new MockFileData("# New Document 1") },
			{ Path.Join(outputDir, "docs/add2.md"), new MockFileData("# New Document 2") },
			{ Path.Join(outputDir, "docs/add3.md"), new MockFileData("# New Document 3") },
			{ Path.Join(outputDir, "docs/skip.md"), new MockFileData("# Skipped Document") },
			{ Path.Join(outputDir, "docs/update.md"), new MockFileData("# Existing Document") },
		}, new MockFileSystemOptions { CurrentDirectory = outputDir });

		var s3 = A.Fake<IAmazonS3>();
		var xfer = A.Fake<ITransferUtility>();
		var gh = A.Fake<ICoreService>();

		// Mocked ETag calculator: skip.md returns SkipETag (matches remote → skip);
		// all other files return AnyOtherETag (remote has "stale-etag" → update).
		var etagCalculator = A.Fake<IS3EtagCalculator>();
		A.CallTo(() => etagCalculator.CalculateS3ETag(A<string>.That.EndsWith("skip.md"), A<Cancel>._))
			.Returns(SkipETag);
		A.CallTo(() => etagCalculator.CalculateS3ETag(A<string>.That.Not.EndsWith("skip.md"), A<Cancel>._))
			.Returns(AnyOtherETag);

		A.CallTo(() => s3.ListObjectsV2Async(A<ListObjectsV2Request>._, A<Cancel>._))
			.Returns(new ListObjectsV2Response
			{
				S3Objects =
				[
					new S3Object { Key = "docs/delete.md" },
					new S3Object { Key = "docs/skip.md", ETag = $"\"{SkipETag}\"" },
					new S3Object { Key = "docs/update.md", ETag = "\"stale-etag\"" },
				]
			});

		A.CallTo(() => s3.DeleteObjectsAsync(A<DeleteObjectsRequest>._, A<Cancel>._))
			.Returns(new DeleteObjectsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });
		A.CallTo(() => xfer.UploadAsync(A<TransferUtilityUploadRequest>._, A<Cancel>._))
			.Returns(Task.CompletedTask);

		var svc = new IncrementalDeployService(new LoggerFactory(), gh, s3, xfer, etagCalculator);
		return (fs, s3, xfer, gh, svc);
	}

	private static async Task RunRoundTrip(
		MockFileSystem fs,
		IAmazonS3 s3,
		ITransferUtility xfer,
		ICoreService gh,
		IncrementalDeployService svc,
		IDocsSyncContext context,
		string outputDir)
	{
		var uploadRequests = new ConcurrentBag<TransferUtilityUploadRequest>();
		A.CallTo(() => xfer.UploadAsync(A<TransferUtilityUploadRequest>._, A<Cancel>._))
			.Invokes((TransferUtilityUploadRequest request, Cancel _) =>
			{
				uploadRequests.Add(request);
			})
			.Returns(Task.CompletedTask);

		var planPath = Path.Join(outputDir, "sync-plan.json");

		// Act — Plan
		// deleteThreshold: 1.0 permits any delete ratio (needed because the validator
		// enforces a 0.8 floor for small sync sets where TotalSyncRequests < 100)
		var planOk = await svc.Plan(context.Collector, context, "fake-bucket", planPath, deleteThreshold: 1.0f, excludePatterns: [], Cancel.None);
		planOk.Should().BeTrue("plan should succeed with valid file mix");
		fs.File.Exists(planPath).Should().BeTrue("plan JSON must be written to the mock filesystem");

		// Act — Apply (reads plan.json from the same mock filesystem)
		var applyOk = await svc.Apply(context.Collector, context, "fake-bucket", planPath, Cancel.None);
		applyOk.Should().BeTrue("apply should succeed");

		// Assert — GitHub Actions output
		A.CallTo(() => gh.SetOutputAsync("plan-valid", "true")).MustHaveHappenedOnceExactly();

		// Assert — uploads: 3 adds + 1 update; skip.md and remote-only delete.md not uploaded
		uploadRequests.Count.Should().Be(4);
		uploadRequests.Should().OnlyContain(r => r.BucketName == "fake-bucket");
		uploadRequests.Should().OnlyContain(r => r.PartSize == S3EtagCalculator.PartSize);
		uploadRequests.Select(r => Path.GetFileName(r.FilePath)).Should()
			.BeEquivalentTo(["add1.md", "add2.md", "add3.md", "update.md"],
				"skip.md is unchanged (ETag matches) so it is not re-uploaded");
		uploadRequests.Select(r => r.Key).Should()
			.BeEquivalentTo(["docs/add1.md", "docs/add2.md", "docs/add3.md", "docs/update.md"]);

		// Assert — deletes: exactly one S3 delete call for docs/delete.md
		A.CallTo(() => s3.DeleteObjectsAsync(
				A<DeleteObjectsRequest>.That.Matches(r => r.Objects.Any(o => o.Key == "docs/delete.md")),
				A<Cancel>._))
			.MustHaveHappenedOnceExactly();

		// Assert — per-file uploads only; no staging directory upload
		A.CallTo(() => xfer.UploadAsync(A<TransferUtilityUploadRequest>._, A<Cancel>._))
			.MustHaveHappened();
		A.CallTo(() => xfer.UploadDirectoryAsync(A<TransferUtilityUploadDirectoryRequest>._, A<Cancel>._))
			.MustNotHaveHappened();
	}
}

/// <summary>
/// Verifies that <c>--exclude</c> patterns prevent matching objects from being uploaded or deleted.
/// </summary>
/// <remarks>
/// Scenario: the S3 bucket contains a <c>_preview/pr-42/index.html</c> object written by a
/// PR-preview workflow. The local build output does NOT contain that file. Without excludes the
/// planner would queue it for deletion; with <c>_preview/*</c> excluded it must be untouched.
/// Similarly a local file under an excluded prefix must not be added to the plan.
/// </remarks>
public class IncrementalDeployExcludeTests
{
	private const string SkipETag = "aaaa0000skip0000etag0000aaaa0000";
	private const string AnyOtherETag = "bbbb1111other1111etag1111bbbb1111";

	[Fact]
	public async Task ExcludedRemoteObjectsAreNotDeleted()
	{
		var outputDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "codex", "docs");

		// Local build output: one regular page, nothing under _preview/
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ Path.Join(outputDir, "docs/page.md"), new MockFileData("# Page") },
		}, new MockFileSystemOptions { CurrentDirectory = outputDir });

		var s3 = A.Fake<IAmazonS3>();
		var xfer = A.Fake<ITransferUtility>();
		var gh = A.Fake<ICoreService>();

		var etagCalculator = A.Fake<IS3EtagCalculator>();
		A.CallTo(() => etagCalculator.CalculateS3ETag(A<string>._, A<Cancel>._)).Returns(AnyOtherETag);

		// Remote has both a regular page (stale) and a preview object that lives alongside codex output
		A.CallTo(() => s3.ListObjectsV2Async(A<ListObjectsV2Request>._, A<Cancel>._))
			.Returns(new ListObjectsV2Response
			{
				S3Objects =
				[
					new S3Object { Key = "docs/page.md", ETag = "\"stale-etag\"" },
					new S3Object { Key = "_preview/pr-42/index.html" },
					new S3Object { Key = "403/index.html" },
					new S3Object { Key = "404/index.html" },
				]
			});

		A.CallTo(() => s3.DeleteObjectsAsync(A<DeleteObjectsRequest>._, A<Cancel>._))
			.Returns(new DeleteObjectsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

		var svc = new IncrementalDeployService(new LoggerFactory(), gh, s3, xfer, etagCalculator);
		var collector = new DiagnosticsCollector([]);
		var scopedFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fs);
		var scopedWriteFs = FileSystemFactory.ScopeCurrentWorkingDirectoryForWrite(fs);
		var codexConfig = new CodexConfiguration { Environment = "dev" };
		var configFile = fs.FileInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "codex.yml"));
		var context = new CodexContext(codexConfig, configFile, collector, scopedFs, scopedWriteFs, null, outputDir);

		var planPath = Path.Join(outputDir, "sync-plan.json");
		var planOk = await svc.Plan(
			context.Collector,
			context,
			"fake-bucket",
			planPath,
			deleteThreshold: 1.0f,
			excludePatterns: ["_preview/*", "403/*", "404/*"],
			Cancel.None);
		planOk.Should().BeTrue("plan should succeed");

		var applyOk = await svc.Apply(context.Collector, context, "fake-bucket", planPath, Cancel.None);
		applyOk.Should().BeTrue("apply should succeed");

		// The plan must not contain deletions for any excluded key
		var planJson = fs.File.ReadAllText(planPath);
		var plan = SyncPlan.Deserialize(planJson);
		plan.DeleteRequests.Should().NotContain(r => r.DestinationPath.StartsWith("_preview/", StringComparison.Ordinal),
			"excluded _preview/* objects must not be queued for deletion");
		plan.DeleteRequests.Should().NotContain(r => r.DestinationPath.StartsWith("403/", StringComparison.Ordinal),
			"excluded 403/* objects must not be queued for deletion");
		plan.DeleteRequests.Should().NotContain(r => r.DestinationPath.StartsWith("404/", StringComparison.Ordinal),
			"excluded 404/* objects must not be queued for deletion");

		// docs/page.md is not excluded so it should be an update (stale ETag)
		plan.UpdateRequests.Should().Contain(r => r.DestinationPath == "docs/page.md");

		// No S3 delete calls should include excluded prefixes
		A.CallTo(() => s3.DeleteObjectsAsync(
				A<DeleteObjectsRequest>.That.Matches(r => r.Objects.Any(o =>
					o.Key.StartsWith("_preview/", StringComparison.Ordinal) ||
					o.Key.StartsWith("403/", StringComparison.Ordinal) ||
					o.Key.StartsWith("404/", StringComparison.Ordinal))),
				A<Cancel>._))
			.MustNotHaveHappened();

		// Excluded patterns are recorded in the plan file
		plan.ExcludePatterns.Should().BeEquivalentTo(["_preview/*", "403/*", "404/*"],
			"plan must record which patterns were excluded");
	}
}
