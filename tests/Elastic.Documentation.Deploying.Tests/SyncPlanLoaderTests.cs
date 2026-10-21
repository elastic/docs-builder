// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using Actions.Core.Services;
using Amazon.S3;
using Amazon.S3.Transfer;
using AwesomeAssertions;
using Elastic.Codex;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Deploying.Synchronization;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Integrations.S3;
using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Deploying.Tests;

/// <summary>
/// <see cref="SyncPlanLoader"/> is the one way a sync plan file becomes a record: parsed and checked once, every violation in one result, and
/// <see cref="IncrementalDeployService.Apply"/> reads the plan through it.
/// </summary>
public class SyncPlanLoaderTests
{
	private static SyncPlan Plan(bool remoteListingCompleted = true, int totalSourceFiles = 2000, int totalSyncRequests = 1) =>
		new()
		{
			DeleteThresholdDefault = null,
			ExcludePatterns = [],
			RemoteListingCompleted = remoteListingCompleted,
			TotalSourceFiles = totalSourceFiles,
			TotalRemoteFiles = 2000,
			TotalSyncRequests = totalSyncRequests,
			DeleteRequests = [],
			AddRequests = [new AddRequest { LocalPath = "/out/a.html", DestinationPath = "a.html" }],
			UpdateRequests = [],
			SkipRequests = []
		};

	[Test]
	public void Load_AValidPlanBreaksNothing()
	{
		var result = SyncPlanLoader.Load(SyncPlan.Serialize(Plan()), "plan.json");

		result.Value.Should().NotBeNull();
		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public void Load_ReportsEveryViolationOnce_AndStillReturnsThePlan()
	{
		// a partial listing and an empty build output: two errors, plus the total that does not add up (a warning)
		var json = SyncPlan.Serialize(Plan(remoteListingCompleted: false, totalSourceFiles: 0, totalSyncRequests: 5));

		var result = SyncPlanLoader.Load(json, "plan.json");

		result.Value.Should().NotBeNull();
		var ruleIds = result.Diagnostics.Select(d => d.RuleId).ToList();
		ruleIds.Should().Contain(SyncPlanConstraints.RemoteListingComplete.Name);
		ruleIds.Should().Contain(SyncPlanConstraints.SourceFilesPresent.Name);
		ruleIds.Should().Contain(SyncPlanConstraints.TotalSyncRequestsIsAddUpdateDelete.Name);
		result.Diagnostics.Select(d => (d.RuleId, d.Message)).Should().OnlyHaveUniqueItems();
		result.Diagnostics.Should().OnlyContain(d => d.File == "plan.json");
	}

	[Test]
	[Arguments("{ not json")]
	[Arguments("null")]
	[Arguments("")]
	public void Load_TextThatDoesNotParseIsAnErrorDiagnostic_NeverAnException(string text)
	{
		var result = SyncPlanLoader.Load(text, "plan.json");

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error && d.File == "plan.json");
	}

	[Test]
	public void TheRawReaderIsNotPublic_AndEveryPublicEntryReturnsAResult()
	{
		typeof(SyncPlan)
			.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static)
			.Should()
			.BeNull("a public SyncPlan.Deserialize returns an unchecked record");

		typeof(SyncPlanLoader)
			.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Where(m => m.ReturnType != typeof(Result<SyncPlan>))
			.Should()
			.BeEmpty();
	}

	private static (IncrementalDeployService Service, CodexContext Context, DiagnosticsCollector Collector, MockFileSystem Fs) Arrange(
		string? planText
	)
	{
		var outputDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "codex", "docs");
		var planPath = Path.Join(outputDir, "sync-plan.json");
		var files = new Dictionary<string, MockFileData>();
		if (planText is not null)
			files[planPath] = new MockFileData(planText);
		var fs = new MockFileSystem(files, new MockFileSystemOptions { CurrentDirectory = outputDir });
		var collector = new DiagnosticsCollector([]);
		var configFile = fs.FileInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "codex.yml"));
		var context = new CodexContext(
			new CodexConfiguration { Environment = "dev" },
			configFile,
			collector,
			CheckoutsFileSystem.FromWorkingDirectory(fs),
			null,
			outputDir
		);
		var service = new IncrementalDeployService(
			new LoggerFactory(),
			A.Fake<ICoreService>(),
			A.Fake<IAmazonS3>(),
			A.Fake<ITransferUtility>(),
			A.Fake<IS3EtagCalculator>()
		);
		return (service, context, collector, fs);
	}

	private static string PlanPath => Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "codex", "docs", "sync-plan.json");

	[Test]
	public async Task Apply_ANonParsingPlanIsOneErrorOnThePlanFile_NotAnException()
	{
		var (service, context, collector, _) = Arrange("{ not json");

		var ok = await service.Apply(collector, context, "fake-bucket", PlanPath, Cancel.None);

		ok.Should().BeFalse();
		collector.Errors.Should().Be(1);
	}

	[Test]
	public async Task Apply_ABadPlanIsRejectedAndReportedOnce()
	{
		var (service, context, collector, _) = Arrange(SyncPlan.Serialize(Plan(remoteListingCompleted: false)));

		var ok = await service.Apply(collector, context, "fake-bucket", PlanPath, Cancel.None);

		ok.Should().BeFalse();
		collector
			.Errors
			.Should()
			.Be(1, "the verdict is one error on the plan file; the violations behind it are logged once by the validator");
	}

	[Test]
	public async Task Apply_AnEmptyPlanReturnsEarlyBeforeItsViolationsAreJudged()
	{
		// nothing to sync: the early return is kept, even for a plan whose rules would fail
		var (service, context, collector, _) = Arrange(SyncPlan.Serialize(Plan(remoteListingCompleted: false, totalSyncRequests: 0) with
		{
			AddRequests = []
		}));

		var ok = await service.Apply(collector, context, "fake-bucket", PlanPath, Cancel.None);

		ok.Should().BeTrue();
		collector.Errors.Should().Be(0);
	}

	[Test]
	public void Planned_TotalSyncRequestsIsTheSumOfTheFourListsSoThePlannerCannotDisagreeWithThem()
	{
		var result = SyncPlanLoader.Planned(
			true,
			0.5f,
			["a/**"],
			10,
			2000,
			[new DeleteRequest { DestinationPath = "d.html" }],
			[new AddRequest { LocalPath = "/out/a.html", DestinationPath = "a.html" }],
			[new UpdateRequest { LocalPath = "/out/u.html", DestinationPath = "u.html" }],
			[new SkipRequest { LocalPath = "/out/s.html", DestinationPath = "s.html" }]
		);

		// FINDING (pre-existing, unchanged): the planner counts skipped files in the total and the rule says add + update + delete; that is a warning on every plan with a skip
		result.HasErrors.Should().BeFalse();
		result.Diagnostics.Should().ContainSingle().Which.RuleId.Should().Be("total-sync-requests-is-add-update-delete");
		var plan = result.Value!;
		plan.TotalSyncRequests.Should().Be(4);
		plan.TotalRemoteFiles.Should().Be(10);
		plan.TotalSourceFiles.Should().Be(2000);
		plan.DeleteThresholdDefault.Should().Be(0.5f);
		plan.ExcludePatterns.Should().Equal("a/**");
	}

	[Test]
	public void Planned_IncompleteRemoteListing_IsAnErrorAndThePlanRidesAlong()
	{
		var result = SyncPlanLoader.Planned(false, null, [], 0, 2000, [], [], [], [], "plan.json");

		result.HasErrors.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Diagnostics.Should().OnlyContain(d => d.File == "plan.json");
	}

	[Test]
	public void Planned_NoSourceFiles_IsAnError() =>
		SyncPlanLoader.Planned(true, null, [], 0, 0, [], [], [], []).HasErrors.Should().BeTrue();
}
