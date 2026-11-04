// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.ReleaseSchedule;

namespace Elastic.Documentation.Configuration.Tests.ReleaseSchedule;

/// <summary>
/// The decisions of <c>release unified bundle</c>: which products take part, and the range of commits each one
/// bundles. The command only fetches the manifests and calls the bundler with what the plan says.
/// </summary>
public class UnifiedBundlePlannerTests
{
	private const string ElasticsearchStart = "1111111111111111111111111111111111111111";
	private const string ElasticsearchEnd = "2222222222222222222222222222222222222222";
	private const string KibanaCommit = "3333333333333333333333333333333333333333";

	private static Product CreateProduct(
		string id,
		ReleaseNotesPath path = ReleaseNotesPath.DailyReleasableArtifacts,
		string? repository = null,
		string? draArtifact = null
	) =>
		new()
		{
			Id = id,
			DisplayName = id,
			Repository = repository,
			DraArtifact = draArtifact,
			Features = new ProductFeatures { PublicReference = true, ReleaseNotes = path }
		};

	private static ElasticBuildManifest Manifest(string version, params (string Key, string? Commit)[] projects) =>
		new()
		{
			Version = version,
			BuildId = $"{version}-build",
			Projects = projects.ToDictionary(p => p.Key, p => new ManifestProject { CommitHash = p.Commit })
		};

	// ── product selection ───────────────────────────────────────────────

	[Test]
	public void SelectDraProducts_MixedReleaseNotesPaths_KeepsOnlyDraProducts()
	{
		Product[] all =
		[
			CreateProduct("elasticsearch"),
			CreateProduct("auditbeat", ReleaseNotesPath.OnRelease),
			CreateProduct("curator", ReleaseNotesPath.None),
			CreateProduct("kibana")
		];

		UnifiedBundlePlanner.SelectDraProducts(all).Select(p => p.Id).Should().Equal("elasticsearch", "kibana");
	}

	[Test]
	public void FilterProducts_NoRequest_KeepsEveryProduct()
	{
		IReadOnlyList<Product> dra = [CreateProduct("elasticsearch"), CreateProduct("kibana")];

		var (selected, unknown) = UnifiedBundlePlanner.FilterProducts(dra, null);

		selected.Should().HaveCount(2);
		unknown.Should().BeEmpty();
	}

	[Test]
	public void FilterProducts_RequestedSubset_KeepsOnlyThoseIgnoringCase()
	{
		IReadOnlyList<Product> dra = [CreateProduct("elasticsearch"), CreateProduct("kibana"), CreateProduct("logstash")];

		var (selected, unknown) = UnifiedBundlePlanner.FilterProducts(dra, ["Kibana", "logstash"]);

		selected.Select(p => p.Id).Should().Equal("kibana", "logstash");
		unknown.Should().BeEmpty();
	}

	[Test]
	public void FilterProducts_IdThatIsNotADraProduct_IsNamedAsUnknown()
	{
		IReadOnlyList<Product> dra = [CreateProduct("elasticsearch")];

		var (_, unknown) = UnifiedBundlePlanner.FilterProducts(dra, ["elasticsearch", "auditbeat"]);

		unknown.Should().Equal("auditbeat");
	}

	// ── the range of a product ──────────────────────────────────────────

	[Test]
	public void Plan_ChangedProduct_BundlesTheRangeFromThePreviousToTheTargetCommit()
	{
		var plan = UnifiedBundlePlanner.Plan(
			[CreateProduct("elasticsearch")],
			"9.5.5",
			Manifest("9.5.5", ("elasticsearch", ElasticsearchEnd)),
			Manifest("9.5.4", ("elasticsearch", ElasticsearchStart))
		);

		plan.IsComplete.Should().BeTrue();
		plan.Ranges.Should().Equal(new ProductBundleRange("elasticsearch", "elasticsearch", "9.5.5", ElasticsearchStart, ElasticsearchEnd));
		plan.Unchanged.Should().BeEmpty();
	}

	[Test]
	public void Plan_ProductWhoseArtifactDiffersFromItsRepository_LooksUpTheArtifactAndBundlesTheRepository()
	{
		var product = CreateProduct("elastic-agent", repository: "elastic-agent", draArtifact: "elastic-agent-core");

		var plan = UnifiedBundlePlanner.Plan(
			[product],
			"9.5.5",
			Manifest("9.5.5", ("elastic-agent-core", ElasticsearchEnd)),
			Manifest("9.5.4", ("elastic-agent-core", ElasticsearchStart))
		);

		plan.Ranges.Should().ContainSingle().Which.RepoKey.Should().Be("elastic-agent");
		plan.IsComplete.Should().BeTrue();
	}

	[Test]
	public void Plan_SameCommitInBothManifests_IsUnchangedAndGetsNoRange()
	{
		var plan = UnifiedBundlePlanner.Plan(
			[CreateProduct("kibana")],
			"9.5.5",
			Manifest("9.5.5", ("kibana", KibanaCommit.ToUpperInvariant())),
			Manifest("9.5.4", ("kibana", KibanaCommit))
		);

		plan.Ranges.Should().BeEmpty();
		plan.Unchanged.Should().Equal("kibana");
		plan.IsComplete.Should().BeTrue();
	}

	[Test]
	public void Plan_ProductMissingFromTheTargetManifest_BlocksThePlan()
	{
		var plan = UnifiedBundlePlanner.Plan(
			[CreateProduct("kibana")],
			"9.5.5",
			Manifest("9.5.5"),
			Manifest("9.5.4", ("kibana", KibanaCommit))
		);

		plan.IsComplete.Should().BeFalse();
		plan.Problems.Should().Equal(new PlanProblem("kibana", "kibana", PlanProblemKind.MissingFromManifest));
		plan.Ranges.Should().BeEmpty();
	}

	[Test]
	public void Plan_ProductMissingFromThePreviousManifest_BlocksThePlanInsteadOfGuessingAStart()
	{
		// A product that is new in this release has no earlier commit. Bundling its whole history would be wrong.
		var plan = UnifiedBundlePlanner.Plan(
			[CreateProduct("cloudbeat")],
			"9.5.5",
			Manifest("9.5.5", ("cloudbeat", ElasticsearchEnd)),
			Manifest("9.5.4")
		);

		plan.IsComplete.Should().BeFalse();
		plan.Problems.Should().Equal(new PlanProblem("cloudbeat", "cloudbeat", PlanProblemKind.MissingFromPreviousManifest));
		plan.Ranges.Should().BeEmpty();
	}

	[Test]
	public void Plan_EmptyCommitHash_CountsAsMissing()
	{
		var plan = UnifiedBundlePlanner.Plan(
			[CreateProduct("kibana")],
			"9.5.5",
			Manifest("9.5.5", ("kibana", "")),
			Manifest("9.5.4", ("kibana", KibanaCommit))
		);

		plan.Problems.Should().ContainSingle().Which.Kind.Should().Be(PlanProblemKind.MissingFromManifest);
	}

	[Test]
	public void Plan_MixedProducts_ReportsEveryProblemAndStillPlansTheOthers()
	{
		var plan = UnifiedBundlePlanner.Plan(
			[CreateProduct("elasticsearch"), CreateProduct("kibana"), CreateProduct("cloudbeat"), CreateProduct("logstash")],
			"9.5.5",
			Manifest("9.5.5", ("elasticsearch", ElasticsearchEnd), ("kibana", KibanaCommit), ("cloudbeat", ElasticsearchEnd)),
			Manifest("9.5.4", ("elasticsearch", ElasticsearchStart), ("kibana", KibanaCommit))
		);

		plan.IsComplete.Should().BeFalse();
		plan.Ranges.Select(r => r.ProductId).Should().Equal("elasticsearch");
		plan.Unchanged.Should().Equal("kibana");
		plan
			.Problems
			.Select(p => (p.ProductId, p.Kind))
			.Should()
			.Equal(("cloudbeat", PlanProblemKind.MissingFromPreviousManifest), ("logstash", PlanProblemKind.MissingFromManifest));
	}
}
