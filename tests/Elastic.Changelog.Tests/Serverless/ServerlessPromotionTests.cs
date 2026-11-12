// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Serverless;

namespace Elastic.Changelog.Tests.Serverless;

public class ServerlessPromotionTests
{
	private const string VersionsYaml =
		"""
		services:
		  kibana:
		    versions:
		      production-canary-ds-1: "883abb05af0b"
		      production-noncanary-ds-1: "883abb05af0b"
		      production-noncanary-ds-4: "7dd981dcd7d3"
		      production-noncanary-ds-5: "7dd981dcd7d3"
		      production-noncanary-ds-50: "aaaaaaaaaaaa"
		""";

	private const string Ds5 = "production-noncanary-ds-5";

	[Test]
	public void ParseFinalSliceVersion_ReadsDs5NotDs1() =>
		ServerlessPromotion.ParseFinalSliceVersion(VersionsYaml, Ds5).Should().Be("7dd981dcd7d3");

	[Test]
	public void ParseFinalSliceVersion_ReturnsNullWhenMissing() =>
		ServerlessPromotion.ParseFinalSliceVersion("services: {}", Ds5).Should().BeNull();

	[Test]
	[Arguments("gitops: production-noncanary-ds-4,production-noncanary-ds-5 Artifact promotion for kibana to 7dd981dcd7d3 (#199421)", true)]
	[Arguments("gitops: production-noncanary-ds-1 Artifact promotion for kibana to 883abb05af0b (#202658)", false)]
	[Arguments("gitops: dev Artifact promotion for kibana to 914daa00d40e (#202639)", false)]
	[Arguments("gitops: production-noncanary-ds-50 Artifact promotion", false)]
	public void IsFinalSlicePromotion(string message, bool expected) =>
		ServerlessPromotion.IsFinalSlicePromotion(message, Ds5).Should().Be(expected);

	[Test]
	[Arguments("7dd981dcd7d3", "7dd981dcd7d3", true)]
	[Arguments("7dd981dcd7d3", "7DD981DCD7D3AAAAAAAAAAAAAAAAAAAAAAAAAAAA", true)]
	[Arguments("7dd981dcd7d3", "03c0df903631", false)]
	public void SameRef(string a, string b, bool expected) => ServerlessPromotion.SameRef(a, b).Should().Be(expected);

	[Test]
	public void FindPreviousVersion_SkipsCurrentAndReturnsFirstDifferent() =>
		ServerlessPromotion
			.FindPreviousVersion(["7dd981dcd7d3", null, "7dd981dcd7d3", "03c0df903631", "2fd663f3c363"], "7DD981DCD7D3ffff")
			.Should()
			.Be("03c0df903631");

	[Test]
	public void FindPreviousVersion_ReturnsNullWhenAllMatch() =>
		ServerlessPromotion.FindPreviousVersion(["7dd981dcd7d3"], "7dd981dcd7d3").Should().BeNull();

	private const string TreeJson =
		"""
		{"sha":"a","tree":[
		  {"path":"elasticsearch","mode":"160000","type":"commit","sha":"b8965d0ee9108765f983c1bb7af08e598b5bdb30"},
		  {"path":"docs","mode":"040000","type":"tree","sha":"1111111111111111111111111111111111111111"},
		  {"path":"elasticsearch-extra","mode":"040000","type":"tree","sha":"2222222222222222222222222222222222222222"}
		]}
		""";

	[Test]
	public void FindSubmoduleSha_ReturnsCommitEntry() =>
		ServerlessPromotion.FindSubmoduleSha(TreeJson, "elasticsearch").Should().Be("b8965d0ee9108765f983c1bb7af08e598b5bdb30");

	[Test]
	public void FindSubmoduleSha_IgnoresTreesAndMissingPaths()
	{
		ServerlessPromotion.FindSubmoduleSha(TreeJson, "docs").Should().BeNull();
		ServerlessPromotion.FindSubmoduleSha(TreeJson, "missing").Should().BeNull();
		ServerlessPromotion.FindSubmoduleSha("{}", "elasticsearch").Should().BeNull();
	}

	[Test]
	public void Elasticsearch_BundlesServerlessRepoAndElasticsearchSubmodule()
	{
		var es = ServerlessPromotion.Services["elasticsearch"];
		es.Repository.Should().Be("elasticsearch-serverless");
		es.Submodules.Should().ContainSingle().Which.Repository.Should().Be("elasticsearch");
		ServerlessPromotion.Services["kibana"].Submodules.Should().BeEmpty();
	}

	// ── Elastic Cloud Hosted (frontend-adminconsole) ─────────────────────

	private const string EchFinalSlice = "production-sds-1";

	private const string AdminconsoleVersionsYaml =
		"""
		services:
		  frontend-adminconsole:
		    versions:
		      govcloud-moderate-sds-1: "d3124c7dda83"
		      govcloud-staging-sds-1: "d3124c7dda83"
		      production-sds-1: "d3124c7dda83"
		      staging-sds-1: "2d4c3217bf7c"
		""";

	[Test]
	public void FrontendAdminconsole_BundlesTheCloudRepositoryWithTheEchProfileOnMaster()
	{
		var ech = ServerlessPromotion.Services["frontend-adminconsole"];
		ech.Repository.Should().Be("cloud");
		ech.Profile.Should().Be("ech-release");
		ech.VersionsPath.Should().Be("services/frontend-adminconsole/versions.yaml");
		ech.FinalSlice.Should().Be(EchFinalSlice);
		ech.BaseBranch.Should().Be("master");
		ech.Submodules.Should().BeEmpty();
	}

	[Test]
	public void Services_KeepTheirOwnFinalSliceAndBaseBranch()
	{
		ServerlessPromotion.Services["kibana"].FinalSlice.Should().Be(Ds5);
		ServerlessPromotion.Services["kibana"].BaseBranch.Should().Be("main");
		ServerlessPromotion.Services["elasticsearch"].FinalSlice.Should().Be(Ds5);
		ServerlessPromotion.Services["elasticsearch"].BaseBranch.Should().Be("main");
	}

	[Test]
	public void ParseFinalSliceVersion_ReadsProductionSds1ForFrontendAdminconsole() =>
		ServerlessPromotion.ParseFinalSliceVersion(AdminconsoleVersionsYaml, EchFinalSlice).Should().Be("d3124c7dda83");

	[Test]
	public void ParseFinalSliceVersion_DoesNotReadAnotherServicesSliceNames() =>
		ServerlessPromotion.ParseFinalSliceVersion(AdminconsoleVersionsYaml, Ds5).Should().BeNull();

	[Test]
	[Arguments("gitops: production-sds-1 Artifact promotion for frontend-adminconsole to d3124c7dda836280f00d8c4b1259f7a2b96ec765 (#224336)", true)]
	[Arguments("gitops: staging-sds-1 Artifact promotion for frontend-adminconsole to 2d4c3217bf7cc0a885781ab82bf65c945ec44203 (#229116)", false)]
	[Arguments("gitops: govcloud-moderate-sds-1 Artifact promotion for frontend-adminconsole to d3124c7dda83 (#224464)", false)]
	[Arguments("gitops: govcloud-staging-sds-1 Artifact promotion for frontend-adminconsole to d3124c7dda83 (#224422)", false)]
	public void IsFinalSlicePromotion_ForFrontendAdminconsole(string message, bool expected) =>
		ServerlessPromotion.IsFinalSlicePromotion(message, EchFinalSlice).Should().Be(expected);

	// ── History walk ─────────────────────────────────────────────────────

	private static string Yaml(string ds5) =>
		$"""
		services:
		  kibana:
		    versions:
		      production-noncanary-ds-1: "{ds5}"
		      production-noncanary-ds-5: "{ds5}"
		""";

	private static GitopsCommit Dev(string sha) => new(sha, "gitops: dev Artifact promotion for kibana to 914daa00d40e (#1)");
	private static GitopsCommit Prod(string sha) =>
		new(sha, "gitops: production-noncanary-ds-4,production-noncanary-ds-5 Artifact promotion for kibana to x (#2)");

	[Test]
	public async Task FindPreviousVersionAsync_ReadsOnlyFinalSliceCommitsAndReturnsTheFirstDifferentVersion()
	{
		var reads = new List<string>();
		var pages = new[] { (IReadOnlyList<GitopsCommit>)[Dev("d1"), Prod("p-current"), Dev("d2")], [Dev("d3"), Prod("p-previous")] };
		var yaml = new Dictionary<string, string> { ["p-current"] = Yaml("7dd981dcd7d3"), ["p-previous"] = Yaml("03c0df903631") };

		var result = await ServerlessPromotion.FindPreviousVersionAsync(
			page => Task.FromResult<IReadOnlyList<GitopsCommit>?>(page <= pages.Length ? pages[page - 1] : []),
			sha =>
			{
				reads.Add(sha);
				return Task.FromResult<string?>(yaml[sha]);
			},
			new PreviousVersionSearch(Ds5, "7DD981DCD7D3")
		);

		result.Status.Should().Be(HistoryWalkStatus.Found);
		result.PreviousVersion.Should().Be("03c0df903631");
		result.ScannedCommits.Should().Be(5);
		reads.Should().Equal("p-current", "p-previous");
	}

	[Test]
	public async Task FindPreviousVersionAsync_UsesTheRequestedFinalSlice_NotDs5()
	{
		const string ds1 = "production-noncanary-ds-1";

		static GitopsCommit Slice(string sha, string slice) => new(sha, $"gitops: {slice} Artifact promotion for kibana to x (#3)");

		static string TwoSlices(string ds1Version, string ds5Version) =>
			$"""
			services:
			  kibana:
			    versions:
			      production-noncanary-ds-1: "{ds1Version}"
			      production-noncanary-ds-5: "{ds5Version}"
			""";

		var reads = new List<string>();
		var commits =
			(IReadOnlyList<GitopsCommit>)[Slice("s5-new", Ds5), Slice("s1-current", ds1), Slice("s5-old", Ds5), Slice("s1-previous", ds1)];
		var yaml = new Dictionary<string, string>
		{
			["s5-new"] = TwoSlices("111111111111", "555555555555"),
			["s1-current"] = TwoSlices("aaaaaaaaaaaa", "555555555555"),
			["s5-old"] = TwoSlices("aaaaaaaaaaaa", "444444444444"),
			["s1-previous"] = TwoSlices("bbbbbbbbbbbb", "444444444444")
		};

		var result = await ServerlessPromotion.FindPreviousVersionAsync(
			page => Task.FromResult<IReadOnlyList<GitopsCommit>?>(page == 1 ? commits : []),
			sha =>
			{
				reads.Add(sha);
				return Task.FromResult<string?>(yaml[sha]);
			},
			new PreviousVersionSearch(ds1, "aaaaaaaaaaaa")
		);

		result.Status.Should().Be(HistoryWalkStatus.Found);
		result.PreviousVersion.Should().Be("bbbbbbbbbbbb");
		result.ScannedCommits.Should().Be(4);
		reads.Should().Equal("s1-current", "s1-previous");
	}

	[Test]
	public async Task FindPreviousVersionAsync_AFailedFileReadFailsTheWalk_NotSkipsTheCommit()
	{
		var result = await ServerlessPromotion.FindPreviousVersionAsync(
			page => Task.FromResult<IReadOnlyList<GitopsCommit>?>(page == 1 ? [Prod("p-newer"), Prod("p-older")] : []),
			sha => Task.FromResult<string?>(sha == "p-newer" ? null : Yaml("2fd663f3c363")),
			new PreviousVersionSearch(Ds5, "7dd981dcd7d3")
		);

		// Skipping p-newer would return the older version 2fd663f3c363 and bundle the wrong range.
		result.Status.Should().Be(HistoryWalkStatus.Failed);
		result.PreviousVersion.Should().BeNull();
	}

	[Test]
	public async Task FindPreviousVersionAsync_AFailedPageRequestFailsTheWalk()
	{
		var result = await ServerlessPromotion.FindPreviousVersionAsync(
			page => Task.FromResult<IReadOnlyList<GitopsCommit>?>(page == 1 ? [Dev("d1")] : null),
			_ => Task.FromResult<string?>(Yaml("x")),
			new PreviousVersionSearch(Ds5, "7dd981dcd7d3")
		);

		result.Status.Should().Be(HistoryWalkStatus.Failed);
		result.ScannedCommits.Should().Be(1);
	}

	[Test]
	public async Task FindPreviousVersionAsync_FindsAVersionBeyondTheOldTenPageLimit()
	{
		var result = await ServerlessPromotion.FindPreviousVersionAsync(
			page => Task.FromResult<IReadOnlyList<GitopsCommit>?>(page <= 15 ? [page == 15 ? Prod("p-old") : Dev($"d{page}")] : []),
			_ => Task.FromResult<string?>(Yaml("03c0df903631")),
			new PreviousVersionSearch(Ds5, "7dd981dcd7d3")
		);

		result.Status.Should().Be(HistoryWalkStatus.Found);
		result.ScannedCommits.Should().Be(15);
	}

	[Test]
	public async Task FindPreviousVersionAsync_ReportsTheCapAndTheEndOfHistoryApart()
	{
		var capped = await ServerlessPromotion.FindPreviousVersionAsync(
			_ => Task.FromResult<IReadOnlyList<GitopsCommit>?>([Dev("d")]),
			_ => Task.FromResult<string?>(Yaml("x")),
			new PreviousVersionSearch(Ds5, "7dd981dcd7d3", MaxPages: 3)
		);
		var exhausted = await ServerlessPromotion.FindPreviousVersionAsync(
			page => Task.FromResult<IReadOnlyList<GitopsCommit>?>(page == 1 ? [Prod("p")] : []),
			_ => Task.FromResult<string?>(Yaml("7dd981dcd7d3")),
			new PreviousVersionSearch(Ds5, "7dd981dcd7d3")
		);

		capped.Status.Should().Be(HistoryWalkStatus.CapReached);
		capped.ScannedCommits.Should().Be(3);
		exhausted.Status.Should().Be(HistoryWalkStatus.NotFound);
	}
}
