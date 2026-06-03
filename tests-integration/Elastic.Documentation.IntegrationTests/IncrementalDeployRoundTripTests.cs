// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
		IReadOnlyCollection<IDiagnosticsOutput> diagnosticsOutputs = [];
		var collector = new DiagnosticsCollector(diagnosticsOutputs);
		var scopedFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fs);
		var scopedWriteFs = FileSystemFactory.ScopeCurrentWorkingDirectoryForWrite(fs);
		var context = new AssembleContext(config, configurationContext, "dev", collector, scopedFs, scopedWriteFs, null, outputDir);

		await RunRoundTrip(fs, s3, xfer, gh, svc, context, collector, outputDir);
	}

	[Fact]
	public async Task CodexRoundTrip()
	{
		var outputDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "codex", "docs");
		var (fs, s3, xfer, gh, svc) = Arrange(outputDir);
		IReadOnlyCollection<IDiagnosticsOutput> diagnosticsOutputs = [];
		var collector = new DiagnosticsCollector(diagnosticsOutputs);
		var scopedFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fs);
		var scopedWriteFs = FileSystemFactory.ScopeCurrentWorkingDirectoryForWrite(fs);
		// CodexContext only stores configurationPath — it never reads from it —
		// so we can point to any path without adding it to the mock FS.
		var codexConfig = new CodexConfiguration { Environment = "dev" };
		var configFile = fs.FileInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "codex.yml"));
		var context = new CodexContext(codexConfig, configFile, collector, scopedFs, scopedWriteFs, null, outputDir);

		await RunRoundTrip(fs, s3, xfer, gh, svc, context, collector, outputDir);
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
		IDiagnosticsCollector collector,
		string outputDir)
	{
		// Capture the files passed to the upload call
		var transferredFiles = Array.Empty<string>();
		A.CallTo(() => xfer.UploadDirectoryAsync(A<TransferUtilityUploadDirectoryRequest>._, A<Cancel>._))
			.Invokes((TransferUtilityUploadDirectoryRequest request, Cancel _) =>
			{
				transferredFiles = fs.Directory.GetFiles(request.Directory, request.SearchPattern, request.SearchOption);
			});

		var planPath = Path.Join(outputDir, "sync-plan.json");

		// Act — Plan
		// deleteThreshold: 1.0 permits any delete ratio (needed because the validator
		// enforces a 0.8 floor for small sync sets where TotalSyncRequests < 100)
		var planOk = await svc.Plan(collector, context, "fake-bucket", planPath, deleteThreshold: 1.0f, Cancel.None);
		planOk.Should().BeTrue("plan should succeed with valid file mix");
		fs.File.Exists(planPath).Should().BeTrue("plan JSON must be written to the mock filesystem");

		// Act — Apply (reads plan.json from the same mock filesystem)
		var applyOk = await svc.Apply(collector, context, "fake-bucket", planPath, Cancel.None);
		applyOk.Should().BeTrue("apply should succeed");

		// Assert — GitHub Actions output
		A.CallTo(() => gh.SetOutputAsync("plan-valid", "true")).MustHaveHappenedOnceExactly();

		// Assert — uploads: 3 adds + 1 update; skip.md and remote-only delete.md not uploaded
		transferredFiles.Select(Path.GetFileName).Should()
			.BeEquivalentTo(["add1.md", "add2.md", "add3.md", "update.md"],
				"skip.md is unchanged (ETag matches) so it is not re-uploaded");

		// Assert — deletes: exactly one S3 delete call for docs/delete.md
		A.CallTo(() => s3.DeleteObjectsAsync(
				A<DeleteObjectsRequest>.That.Matches(r => r.Objects.Any(o => o.Key == "docs/delete.md")),
				A<Cancel>._))
			.MustHaveHappenedOnceExactly();

		// Assert — uploads called once
		A.CallTo(() => xfer.UploadDirectoryAsync(A<TransferUtilityUploadDirectoryRequest>._, A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}
}
