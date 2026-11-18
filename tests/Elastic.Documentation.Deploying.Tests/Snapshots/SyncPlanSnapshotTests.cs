// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using AwesomeAssertions;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Deploying.Synchronization;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Integrations.S3;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Testing;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Nullean.Snapshots;
using Nullean.Snapshots.Json;

namespace Elastic.Documentation.Deploying.Tests.Snapshots;

/// <summary>
/// Plans a sync of a local output folder against a fake bucket and snapshots the resulting plan as the JSON that is written
/// to disk, with the output folder replaced by <c>{output}</c> and each request list sorted (the planner collects them in
/// parallel, so their order is not stable).
/// </summary>
public class SyncPlanSnapshotTests
{
	private sealed class RecordingCollector() : DiagnosticsCollector([])
	{
		private readonly List<Diagnostic> _diagnostics = [];

		public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

		public override void Write(Diagnostic diagnostic)
		{
			IncrementSeverityCount(diagnostic);
			_diagnostics.Add(diagnostic);
		}

		public override DiagnosticsCollector StartAsync(Cancel ctx) => this;

		public override Task StopAsync(Cancel cancellationToken) => Task.CompletedTask;
	}

	private static readonly string OutputDirectory = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly");

	private sealed record Remote(string Key, string? Content = null, DateTime? LastModified = null);

	private static async Task<string> PlanAsync(
		Dictionary<string, string> local,
		IReadOnlyList<Remote> remote,
		string[]? excludes = null,
		float? threshold = null,
		bool bucketExists = true,
		string[]? symlinks = null
	)
	{
		var collector = new RecordingCollector();
		var fileSystem = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = OutputDirectory });
		foreach (var (path, content) in local)
			fileSystem.AddFile(path, new MockFileData(content));
		foreach (var link in symlinks ?? [])
		{
			fileSystem.AddFile(link + ".target", new MockFileData("target"));
			fileSystem.File.CreateSymbolicLink(link, link + ".target");
		}

		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var config = AssemblyLoader.Create(configurationContext.ConfigurationFileProvider);
		var context = new AssembleContext(
			config,
			configurationContext,
			"dev",
			collector,
			CheckoutsFileSystem.FromWorkingDirectory(fileSystem),
			null,
			OutputDirectory
		);

		var s3 = A.Fake<IAmazonS3>();
		if (!bucketExists)
			_ = A.CallTo(() => s3.GetBucketAclAsync(A<GetBucketAclRequest>._, A<Cancel>._)).Throws(new AmazonS3Exception("no such bucket"));
		_ = A.CallTo(() => s3.ListObjectsV2Async(A<ListObjectsV2Request>._, A<Cancel>._)).Returns(new ListObjectsV2Response
		{
			S3Objects = [.. remote.Select(r => new S3Object { Key = r.Key, ETag = $"\"etag-{r.Content}\"", LastModified = r.LastModified })]
		});

		var etags = A.Fake<IS3EtagCalculator>();
		_ = A.CallTo(() => etags.CalculateS3ETag(A<string>._, A<Cancel>._)).ReturnsLazily(
			call => Task.FromResult($"etag-{fileSystem.File.ReadAllText((string)call.Arguments[0]!)}")
		);

		var plan = await new AwsS3SyncPlanStrategy(new LoggerFactory(), s3, "example-bucket", context, etags).Plan(
			threshold,
			excludes ?? [],
			Cancel.None
		);
		var validation = new DocsSyncPlanValidator(new LoggerFactory()).Validate(plan);

		var normalised = plan with
		{
			AddRequests = [.. plan.AddRequests.OrderBy(r => r.DestinationPath, StringComparer.Ordinal)],
			UpdateRequests = [.. plan.UpdateRequests.OrderBy(r => r.DestinationPath, StringComparer.Ordinal)],
			SkipRequests = [.. plan.SkipRequests.OrderBy(r => r.DestinationPath, StringComparer.Ordinal)],
			DeleteRequests = [.. plan.DeleteRequests.OrderBy(r => r.DestinationPath, StringComparer.Ordinal)]
		};
		var json = SyncPlan
			.Serialize(normalised)
			.Replace("\r\n", "\n", StringComparison.Ordinal)
			.Replace(OutputDirectory.Replace('\\', '/'), "{output}", StringComparison.Ordinal)
			.Replace(OutputDirectory.Replace("\\", "\\\\"), "{output}", StringComparison.Ordinal)
			// JSON escapes the platform separator of a local path
			.Replace("\\\\", "/", StringComparison.Ordinal);
		var sb = new StringBuilder(json);
		_ = sb.Append(
			$"\n--- validation: valid={validation.Valid} ratio={validation.DeleteRatio:0.####} threshold={validation.DeleteThreshold:0.####}\n"
		);
		_ = sb.Append("--- diagnostics\n").Append(collector.Diagnostics.ToSnapshotText());
		return sb.ToString();
	}

	private static Dictionary<string, string> Files(params string[] paths) => paths.ToDictionary(p => p, p => $"content of {p}");

	private static Remote Same(string key) => new(key, $"content of {key}");

	[Test]
	public async Task EmptyBucket_EveryLocalFileIsAnAdd() =>
		(await PlanAsync(Files("docs/index.html", "docs/guide/a.html", "docs/guide/b.html"), [])).Should().MatchSnapshot();

	[Test]
	public async Task IdenticalBucket_EveryFileIsSkipped() =>
		(await PlanAsync(Files("docs/index.html", "docs/a.html"), [Same("docs/index.html"), Same("docs/a.html")])).Should().MatchSnapshot();

	[Test]
	public async Task MixOfAddUpdateSkipAndDelete() =>
		(await PlanAsync(
			Files("docs/new.html", "docs/changed.html", "docs/same.html"),
			[Same("docs/same.html"), new("docs/changed.html", "old content"), Same("docs/gone.html"), Same("docs/gone/deep/page.html")]
		)).Should().MatchSnapshot();

	[Test]
	public async Task EverythingDeleted_ValidatorRejectsAnEmptyOutput() =>
		(await PlanAsync([], [Same("docs/a.html"), Same("docs/b.html")])).Should().MatchSnapshot();

	[Test]
	public async Task ExcludePatterns_ApplyToUploadsAndDeletes() =>
		(await PlanAsync(
			Files("docs/index.html", "docs/_preview/pr-1/index.html", "docs/app.js.map", "docs/keep.js"),
			[Same("docs/_preview/pr-2/index.html"), Same("docs/old.js.map"), Same("docs/stale.html")],
			["docs/_preview/*", "**/*.map"]
		)).Should().MatchSnapshot();

	[Test]
	public async Task ExcludePatternWithTrailingStarExpandsToRecursive() =>
		(await PlanAsync(Files("docs/a/b/c/d.html", "docs/z.html"), [Same("docs/a/x.html")], ["docs/a/*"])).Should().MatchSnapshot();

	[Test]
	public async Task SymlinksInTheOutputAreNotUploaded() =>
		(await PlanAsync(Files("docs/real.html"), [], symlinks: ["docs/link.html"])).Should().MatchSnapshot();

	[Test]
	public async Task StaleHashedAssets_NewestTenPerFamilyAreRetained()
	{
		var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var remote = Enumerable
			.Range(0, 12)
			.Select(i => new Remote($"docs/_static/chunk-Search-{i:D8}.js", "old", start.AddDays(i)))
			.ToList();
		remote.Add(new Remote("docs/_static/main-aaaaaaaa.js", "old", start));
		remote.Add(new Remote("docs/_static/styles.css", "old", start));
		(await PlanAsync(Files("docs/index.html", "docs/_static/chunk-Search-current0.js"), remote)).Should().MatchSnapshot();
	}

	[Test]
	public async Task BucketThatDoesNotExist_PlanIsIncompleteAndAnErrorIsReported() =>
		(await PlanAsync(Files("docs/index.html"), [], bucketExists: false)).Should().MatchSnapshot();

	[Test]
	public async Task UserSuppliedDeleteThreshold() =>
		(await PlanAsync(
			Files("docs/a.html"),
			[Same("docs/a.html"), Same("docs/b.html"), Same("docs/c.html")],
			threshold: 0.1f
		)).Should().MatchSnapshot();

	[Test]
	public void SerializeAndDeserialize_RoundTripsEveryField()
	{
		var plan = new SyncPlan
		{
			DeleteThresholdDefault = 0.25f,
			ExcludePatterns = ["docs/_preview/*"],
			RemoteListingCompleted = true,
			TotalSourceFiles = 4,
			TotalRemoteFiles = 3,
			TotalSyncRequests = 5,
			DeleteRequests = [new DeleteRequest { DestinationPath = "docs/gone.html" }],
			AddRequests = [new AddRequest { LocalPath = "{output}/docs/new.html", DestinationPath = "docs/new.html" }],
			UpdateRequests = [new UpdateRequest { LocalPath = "{output}/docs/changed.html", DestinationPath = "docs/changed.html" }],
			SkipRequests = [new SkipRequest { LocalPath = "{output}/docs/same.html", DestinationPath = "docs/same.html" }],
			RetainedPaths = ["docs/_static/chunk-Search-00000001.js"]
		};
		var json = SyncPlan.Serialize(plan);
		json.Should().MatchJsonSnapshot();
		SyncPlan.Serialize(SyncPlan.Deserialize(json)).Should().Be(json);
	}

	[Test]
	public void Deserialize_MissingOptionalRetainField_DefaultsToEmpty()
	{
		const string json =
			"""
			{"deletion_threshold_default":null,"exclude_patterns":[],"remote_listing_completed":false,"total_source_files":0,"total_remote_files":0,"total_sync_requests":0,"delete":[],"add":[],"update":[],"skip":[]}
			""";
		SyncPlan.Serialize(SyncPlan.Deserialize(json)).Should().MatchJsonSnapshot();
	}

	[Test]
	public void ValidatorTable()
	{
		var sb = new StringBuilder("source remote deletes completed threshold => valid ratio threshold-used\n");
		var validator = new DocsSyncPlanValidator(new LoggerFactory());
		(int Source, int Remote, int Deletes, bool Completed, float? Threshold)[] rows =
		[
			(10, 0, 0, true, null),
			(10, 0, 0, true, 0.9f),
			(0, 10, 10, true, null),
			(10, 10, 3, false, null),
			(10, 10, 2, true, null),
			(10, 10, 3, true, null),
			(10, 10, 8, true, null),
			(10, 10, 9, true, null),
			(10, 10, 9, true, 0.95f),
			(500, 500, 250, true, null),
			(500, 500, 251, true, null),
			(500, 500, 251, true, 0.9f),
			(5000, 5000, 1000, true, null),
			(5000, 5000, 1001, true, null),
			(5000, 5000, 1001, true, 0.5f),
			(100, 100, 80, true, null),
			(100, 100, 81, true, null),
			(101, 101, 50, true, null),
			(101, 101, 51, true, null),
			(1000, 1000, 500, true, null),
			(1000, 1000, 501, true, null),
			(1001, 1001, 201, true, null),
			(1001, 1001, 200, true, null)
		];
		foreach (var r in rows)
		{
			var plan = new SyncPlan
			{
				DeleteThresholdDefault = r.Threshold,
				ExcludePatterns = [],
				RemoteListingCompleted = r.Completed,
				TotalSourceFiles = r.Source,
				TotalRemoteFiles = r.Remote,
				TotalSyncRequests = r.Deletes,
				DeleteRequests = [.. Enumerable.Range(0, r.Deletes).Select(i => new DeleteRequest { DestinationPath = $"docs/{i}.html" })],
				AddRequests = [],
				UpdateRequests = [],
				SkipRequests = []
			};
			var result = validator.Validate(plan);
			_ = sb.Append(
				$"{r.Source} {r.Remote} {r.Deletes} {r.Completed} {r.Threshold?.ToString() ?? "default"} => {result.Valid} {result.DeleteRatio:0.####} {result.DeleteThreshold:0.####}\n"
			);
		}
		sb.ToString().Should().MatchSnapshot();
	}
}
