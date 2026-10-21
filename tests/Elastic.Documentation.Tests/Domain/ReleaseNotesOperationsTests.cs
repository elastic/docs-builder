// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The domain operations of the release-notes records: each is pure, returns the record checked by the aggregate's set, and has a valid result,
/// a violation result and its edge cases.
/// </summary>
public class ChangelogEntryOperationsTests
{
	private static readonly IReadOnlyList<ProductReference> Products = [ProductReference.Parse("elasticsearch", ["9.0.0"], "ga")];

	[Test]
	public void FromPullRequest_IsTheEntryOfThePr()
	{
		var result = ChangelogEntry.FromPullRequest(
			new PullRequestFacts(
				"https://github.com/elastic/elasticsearch/pull/12",
				"Add a thing",
				"feature",
				"Details",
				["search"],
				"flag",
				true,
				["https://x/1"]
			),
			Products
		);

		result.HasErrors.Should().BeFalse();
		var entry = result.Value!;
		entry.Title.Should().Be("Add a thing");
		entry.Type.Should().Be(ChangelogEntryType.Feature);
		entry.Prs.Should().Equal("https://github.com/elastic/elasticsearch/pull/12");
		entry.Areas.Should().Equal("search");
		entry.FeatureId.Should().Be("flag");
		entry.Highlight.Should().BeTrue();
		entry.Issues.Should().Equal("https://x/1");
		entry.Products.Should().BeEquivalentTo(Products);
	}

	[Test]
	public void FromPullRequest_UnknownTypeIsOther_AndEmptyListsAreNone()
	{
		var entry =
			ChangelogEntry.FromPullRequest(
				new PullRequestFacts("https://github.com/elastic/elasticsearch/pull/12", "T", "no-such-type", Areas: [], Issues: []),
				Products
			).Value!;

		entry.Type.Should().Be(ChangelogEntryType.Other);
		entry.Areas.Should().BeNull();
		entry.Issues.Should().BeNull();
	}

	[Test]
	public void FromPullRequest_AMissingTypeIsOther() =>
		ChangelogEntry.FromPullRequest(new PullRequestFacts("u", "T"), Products).Value!.Type.Should().Be(ChangelogEntryType.Other);

	[Test]
	public void Create_BuildsTheEntryTheCliCollected()
	{
		var result = ChangelogEntry.Create(new CreateEntryFacts(
			"Title",
			"bug-fix",
			"api",
			"d",
			"i",
			"a",
			"f",
			false,
			["https://x/pull/1"],
			Products,
			["a1"],
			["https://x/issues/2"]
		));

		var entry = result.Value!;
		result.HasErrors.Should().BeFalse();
		entry.Type.Should().Be(ChangelogEntryType.BugFix);
		entry.Subtype.Should().Be(ChangelogEntrySubtype.Api);
		entry.Areas.Should().Equal("a1");
		entry.Prs.Should().Equal("https://x/pull/1");
	}

	[Test]
	public void Create_AnIncompleteEntryIsStillBuilt_AndUnrecognizedTextFallsBack()
	{
		var result = ChangelogEntry.Create(new CreateEntryFacts(null, "nope", "nope", null, null, null, null, null, null, [], [], null));

		result.HasErrors.Should().BeFalse("completeness is the command's own validation, not a read-stage rule");
		var entry = result.Value!;
		entry.Title.Should().BeEmpty();
		entry.Type.Should().Be(ChangelogEntryType.Other);
		entry.Subtype.Should().BeNull();
		entry.Areas.Should().BeNull();
	}

	[Test]
	public void Marker_CarriesOnlyTheLink()
	{
		var result = ChangelogEntry.Marker(42);

		result.HasErrors.Should().BeFalse();
		result.Value!.IsMarker.Should().BeTrue();
		result.Value.Link.Should().Be("42");
		result.Value.SourceRedirect.Should().BeFalse();
	}

	[Test]
	public void Marker_ALinkThatIsNotAPrNumberIsAViolation()
	{
		var result = ChangelogEntry.Marker("abc");

		result.HasErrors.Should().BeTrue();
		result.Diagnostics.Should().Contain(d => d.RuleId == "marker-link-is-pr-number");
		result.Value.Should().NotBeNull("the value rides along so a caller that tolerates the finding can still write it");
	}

	[Test]
	public void SourcePointer_IsALinkWithSourceRedirect()
	{
		var result = ChangelogEntry.SourcePointer("14");

		result.HasErrors.Should().BeFalse();
		result.Value!.SourceRedirect.Should().BeTrue();
		result.Value.Link.Should().Be("14");
	}

	[Test]
	public void SourcePointer_AStemThatIsNotANumberIsExemptFromTheLinkRule() =>
		ChangelogEntry.SourcePointer("some-file").HasErrors.Should().BeFalse();

	[Test]
	public void CanonicalKeyAndMarkers_TheLowestPrIsTheFile_TheOthersAreMarkers()
	{
		var entry = new ChangelogEntry
		{
			Prs = ["https://github.com/elastic/x/pull/30", "https://github.com/elastic/x/pull/12", "elastic/x#12"]
		};

		var placement = entry.CanonicalKeyAndMarkers("30-something.yaml")!;

		placement.CanonicalFileName.Should().Be("12.yaml");
		placement.IsCanonical.Should().BeFalse();
		placement.Markers.Select(m => m.FileName).Should().Equal("30.yaml");
		placement.Markers[0].Marker.Link.Should().Be("12");
	}

	[Test]
	public void CanonicalKeyAndMarkers_AlreadyCanonical_HasNoMarkersForASinglePr()
	{
		var placement = new ChangelogEntry { Prs = ["https://github.com/elastic/x/pull/12"] }.CanonicalKeyAndMarkers("12.YAML")!;

		placement.IsCanonical.Should().BeTrue();
		placement.Markers.Should().BeEmpty();
	}

	[Test]
	public void CanonicalKeyAndMarkers_NoPrIsNull()
	{
		new ChangelogEntry().CanonicalKeyAndMarkers("a.yaml").Should().BeNull();
		new ChangelogEntry { Prs = ["not a reference"] }.CanonicalKeyAndMarkers("a.yaml").Should().BeNull();
	}

	[Test]
	public void ToBundledEntry_CarriesTheEntryAndTheFile()
	{
		var entry =
			ChangelogEntry.FromPullRequest(new PullRequestFacts("https://github.com/elastic/x/pull/1", "T", "feature"), Products).Value!;

		var bundled = entry.ToBundledEntry("1.yaml", "sha");

		bundled.File.Should().Be(BundledFile.Of("1.yaml", "sha"));
		bundled.Title.Should().Be("T");
		bundled.Type.Should().Be(ChangelogEntryType.Feature);
		bundled.Prs.Should().Equal(entry.Prs!);
	}

	[Test]
	public void ToBundledEntry_AnInvalidTypeIsNone() => new ChangelogEntry().ToBundledEntry("a.yaml", "c").Type.Should().BeNull();

	[Test]
	public void ToBundledEntry_RoundTripsThroughToNoteShape()
	{
		var entry = new ChangelogEntry
		{
			Title = "T",
			Type = ChangelogEntryType.Feature,
			Products = Products,
			Prs = ["https://github.com/elastic/x/pull/1"]
		};

		var bundled = entry.ToBundledEntry("1.yaml", "c");

		bundled.Title.Should().Be(entry.Title);
		bundled.Products.Should().BeEquivalentTo(entry.Products);
		bundled.Type.Should().Be(entry.Type);
	}
}

public class ProductReferenceOperationsTests
{
	[Test]
	public void Parse_ReadsTheLifecycleText()
	{
		var product = ProductReference.Parse("kibana", ["9.1.0"], "beta");

		product.ProductId.Should().Be("kibana");
		product.Versions.Should().Equal("9.1.0");
		product.Lifecycle.Should().Be(Lifecycle.Beta);
	}

	[Test]
	public void Parse_BlankAndUnknownLifecycleAreNone()
	{
		ProductReference.Parse("kibana", null, " ").Lifecycle.Should().BeNull();
		ProductReference.Parse("kibana", null, "nonsense").Lifecycle.Should().BeNull();
	}

	[Test]
	public void Parse_ANullProductIsBlank_ForTheEntrysRuleToReport()
	{
		var entry = new ChangelogEntry { Title = "T", Type = ChangelogEntryType.Feature, Products = [ProductReference.Parse(null)] };

		ProductReference.Parse(null).ProductId.Should().BeEmpty();
		ChangelogEntryConstraints.All.Validate(entry).Should().Contain(v => v.ConstraintId == "entry-product-id-required");
	}
}

public class BundledEntryOperationsTests
{
	[Test]
	public void Inline_IsAnEntryOfOneProduct()
	{
		var entry = BundledEntry.Inline(ChangelogEntryType.BugFix, "Fix it", "kibana", ["https://github.com/elastic/kibana/pull/1"], "ui");

		entry.Type.Should().Be(ChangelogEntryType.BugFix);
		entry.Products!.Select(p => p.ProductId).Should().Equal("kibana");
		entry.Areas.Should().Equal("ui");
		entry.Prs.Should().Equal("https://github.com/elastic/kibana/pull/1");
	}

	[Test]
	public void Inline_NoPrsAndNoAreaAreNone()
	{
		var entry = BundledEntry.Inline(ChangelogEntryType.Other, "T", "kibana", [], null);

		entry.Prs.Should().BeNull();
		entry.Areas.Should().BeNull();
	}

	[Test]
	public void AppendContinuation_MergesPrsOnce()
	{
		var entry = BundledEntry.Inline(ChangelogEntryType.Other, "T", "kibana", ["a", "b"], null);

		var merged = entry.AppendContinuation("T and more", ["b", "c"]);

		merged.Title.Should().Be("T and more");
		merged.Prs.Should().Equal("a", "b", "c");
	}

	[Test]
	public void Exclusion_IsAFileAndAChecksum() => BundledEntry.Exclusion("1.yaml", "").File.Should().Be(BundledFile.Of("1.yaml", ""));

	[Test]
	public void ToNote_CarriesTheReleaseInVersions()
	{
		var bundled = BundledEntry.Inline(ChangelogEntryType.Feature, "T", "kibana", ["https://github.com/elastic/kibana/pull/1"], "ui");

		var result = bundled.ToNote("kibana", "9.1.0");

		result.HasErrors.Should().BeFalse();
		var note = result.Value!;
		note.Title.Should().Be("T");
		note.Type.Should().Be(ChangelogEntryType.Feature);
		note.Products!.Single().Versions.Should().Equal("9.1.0");
		note.Areas.Should().Equal("ui");
	}

	[Test]
	public void ToNote_ANoteWithoutATypeOrTitleIsOtherAndBlank()
	{
		var note = new BundledEntry().ToNote("kibana", "9.1.0").Value!;

		note.Type.Should().Be(ChangelogEntryType.Other);
		note.Title.Should().BeEmpty();
	}
}

public class BundleOperationsTests
{
	private static readonly ChangelogEntry Entry = new()
	{
		Title = "Add a thing",
		Type = ChangelogEntryType.Feature,
		Products = [ProductReference.Parse("elasticsearch", ["9.0.0"], "ga")]
	};

	[Test]
	public void Assemble_TakesTheProductsOfTheEntries()
	{
		var result = Bundle.Assemble(
			[new BundleSource("1.yaml", "c1", Entry), new BundleSource("2.yaml", "c2", Entry)],
			null,
			"elasticsearch",
			"elastic",
			["flag"]
		);

		result.HasErrors.Should().BeFalse();
		var bundle = result.Value!;
		bundle
			.Products
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new BundledProduct("elasticsearch", "9.0.0", Lifecycle.Ga, "elasticsearch", "elastic"));
		bundle.Entries.Select(e => e.File!.Name).Should().Equal("1.yaml", "2.yaml");
		bundle.HideFeatures.Should().Equal("flag");
	}

	[Test]
	public void Assemble_OutputProductsWinAndAreOrdered()
	{
		var bundle =
			Bundle.Assemble(
				[new BundleSource("1.yaml", "c", Entry)],
				[new BundleProductSpec("kibana", "9.1.0", "preview"), new BundleProductSpec("elasticsearch", "*", "*")],
				null,
				null,
				null
			).Value!;

		bundle.Products.Select(p => p.ProductId).Should().Equal("elasticsearch", "kibana");
		bundle.Products[0].Target.Should().BeNull();
		bundle.Products[0].Lifecycle.Should().BeNull();
		bundle.Products[1].Lifecycle.Should().Be(Lifecycle.Preview);
	}

	[Test]
	public void Assemble_AProductWithSeveralTargetsIsAWarning()
	{
		var result = Bundle.Assemble(
			[new BundleSource("1.yaml", "c", Entry)],
			[new BundleProductSpec("elasticsearch", "9.0.0", null), new BundleProductSpec("elasticsearch", "9.1.0", null)],
			null,
			null,
			null
		);

		result.HasErrors.Should().BeFalse();
		result
			.Diagnostics
			.Should()
			.ContainSingle(d => d.RuleId == "bundle-product-single-target")
			.Which
			.Message
			.Should()
			.Be("Product 'elasticsearch' has multiple targets in bundle: 9.0.0, 9.1.0");
	}

	[Test]
	public void Assemble_NothingIsAnEmptyBundle() =>
		Bundle.Assemble([], null, null, null, null).Value!.Should().BeEquivalentTo(new Bundle());

	[Test]
	public void ForRelease_IsOneProductAtTheVersion()
	{
		var entries = new[] { BundledEntry.Inline(ChangelogEntryType.Feature, "T", "kibana", [], null) };

		var result = Bundle.ForRelease("kibana", "9.1.0", Lifecycle.Ga, "kibana", "elastic", "Intro", new DateOnly(2026, 1, 2), entries);

		result.HasErrors.Should().BeFalse();
		var bundle = result.Value!;
		bundle.Products.Should().ContainSingle().Which.Target.Should().Be("9.1.0");
		bundle.Description.Should().Be("Intro");
		bundle.ReleaseDate.Should().Be(new DateOnly(2026, 1, 2));
		bundle.Entries.Should().HaveCount(1);
	}

	[Test]
	public void ForRelease_ABlankDescriptionIsNone() =>
		Bundle.ForRelease("kibana", "9.1.0", null, null, null, "", null, []).Value!.Description.Should().BeNull();

	private static readonly Bundle Assembled =
		Bundle.Assemble([new BundleSource("1.yaml", "c", Entry)], null, "elasticsearch", "elastic", null).Value!;
	private static readonly DateOnly Today = new(2026, 10, 6);

	[Test]
	public void Finalize_TodayWhenNothingElseSaysOtherwise_AndDeterministicForADay()
	{
		var first = Assembled.Finalize(null, null, null, Today).Value!;
		var second = Assembled.Finalize(null, null, null, Today).Value!;

		first.ReleaseDate.Should().Be(Today);
		first.Should().Be(second);
		Assembled.ReleaseDate.Should().BeNull("a bundle is immutable");
	}

	[Test]
	public void Finalize_AnExplicitDateBeatsTheExistingOne_WhichBeatsToday()
	{
		var dated = Assembled with { ReleaseDate = new DateOnly(2025, 5, 5) };

		dated.Finalize(null, null, null, Today).Value!.ReleaseDate.Should().Be(new DateOnly(2025, 5, 5));
		dated.Finalize(null, new DateOnly(2024, 1, 1), null, Today).Value!.ReleaseDate.Should().Be(new DateOnly(2024, 1, 1));
	}

	[Test]
	public void Finalize_SuppressedLeavesNoDate_ButAnExplicitOneStillApplies()
	{
		Assembled.Finalize(null, null, null, Today, suppressReleaseDate: true).Value!.ReleaseDate.Should().BeNull();
		Assembled.Finalize(null, new DateOnly(2024, 1, 1), null, Today, suppressReleaseDate: true).Value!
			.ReleaseDate
			.Should()
			.Be(new DateOnly(2024, 1, 1));
	}

	[Test]
	public void Finalize_DescriptionAndGitRefOnlyWhenGiven()
	{
		var finalized = Assembled.Finalize("Intro", null, "v9.0.0", Today).Value!;
		var untouched = finalized.Finalize(null, null, " ", Today).Value!;

		finalized.Description.Should().Be("Intro");
		finalized.GitRef.Should().Be("v9.0.0");
		untouched.Description.Should().Be("Intro");
		untouched.GitRef.Should().Be("v9.0.0");
	}

	[Test]
	public void AmendOf_CopiesTheParentProductsAndRecordsTheChange()
	{
		var added = Entry.ToBundledEntry("3.yaml", "c3");
		var exclusion = BundledEntry.Exclusion("1.yaml", "c1");

		var result = Bundle.AmendOf(Assembled.Products, [added], [exclusion], "");

		result.HasErrors.Should().BeFalse();
		var amend = result.Value!;
		amend.Products.Should().Equal(Assembled.Products);
		amend.Entries.Should().Equal(added);
		amend.ExcludeEntries.Should().Equal(exclusion);
		amend.Description.Should().BeEmpty("an empty description clears the parent's intro");
	}

	[Test]
	public void AmendOf_NoDescriptionInherits() => Bundle.AmendOf([], [], []).Value!.Description.Should().BeNull();

	[Test]
	public void ApplyAmends_ExclusionsThenAdditions_InOrder_AndIsIdempotentOnTheResult()
	{
		var parent =
			Bundle.Assemble(
				[new BundleSource("1.yaml", "c1", Entry), new BundleSource("2.yaml", "c2", Entry)],
				null,
				"elasticsearch",
				"elastic",
				null
			).Value!;
		var amend1 =
			Bundle.AmendOf(
				parent.Products,
				[Entry.ToBundledEntry("3.yaml", "c3")],
				[BundledEntry.Exclusion("1.yaml", "c1")],
				"Patched"
			).Value!;
		var amend2 = Bundle.AmendOf(parent.Products, [], [BundledEntry.Exclusion("2.yaml", "")]).Value!;

		var applied = parent.ApplyAmends([new BundleAmend(amend1), new BundleAmend(amend2)]);

		applied.HasErrors.Should().BeFalse();
		applied.Value!.Entries.Select(e => e.File!.Name).Should().Equal("3.yaml");
		applied.Value.Description.Should().Be("Patched");
		parent.Entries.Should().HaveCount(2, "the parent is not changed");
	}

	[Test]
	public void ApplyAmends_TheNotesSidecarDoesNotPatchTheIntro()
	{
		var amend = Bundle.AmendOf([], [], [], "From the notes sidecar").Value!;

		var applied = Assembled.ApplyAmends([new BundleAmend(amend, PatchesDescription: false)]).Value!;

		applied.Description.Should().BeNull();
	}

	[Test]
	public void ApplyAmends_NoAmendsIsTheSameBundle() => Assembled.ApplyAmends([]).Value.Should().Be(Assembled);

	[Test]
	public void WithoutPrivateLinks_DropsWhatIsNotAllowed_AndSaysSo()
	{
		var entry = BundledEntry.Inline(
			ChangelogEntryType.Feature,
			"T",
			"kibana",
			["https://github.com/elastic/kibana/pull/1", "https://github.com/elastic/private/pull/2"],
			null
		) with
		{ Description = "See https://github.com/elastic/private/pull/2 for more" };
		var bundle = Bundle.ForRelease("kibana", "9.1.0", null, null, null, "Intro elastic/private#5", null, [entry]).Value!;

		var result = bundle.WithoutPrivateLinks(["elastic/kibana"], "elastic", "kibana");

		var scrubbed = result.Value!;
		scrubbed.Changed.Should().BeTrue();
		scrubbed.Value.Entries[0].Prs.Should().Equal("https://github.com/elastic/kibana/pull/1");
		scrubbed.Value.Entries[0].Description.Should().Be("See  for more");
		scrubbed.Value.Description.Should().Be("Intro ");
		result
			.Diagnostics
			.Should()
			.ContainSingle(d => d.Severity == Severity.Warning)
			.Which
			.Message
			.Should()
			.Contain("elastic/private")
			.And
			.Contain("removed from public output");

		// idempotent: nothing is left to remove
		var again = scrubbed.Value.WithoutPrivateLinks(["elastic/kibana"], "elastic", "kibana");
		again.Value!.Changed.Should().BeFalse();
		again.Value.Value.Should().Be(scrubbed.Value);
		again.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public void WithoutPrivateLinks_AReferenceThatCannotBeReadIsAnErrorAndNoBundle()
	{
		var entry = BundledEntry.Inline(ChangelogEntryType.Feature, "T", "kibana", ["https://example.com/not-github"], null);
		var bundle = Bundle.ForRelease("kibana", "9.1.0", null, null, null, null, null, [entry]).Value!;

		var result = bundle.WithoutPrivateLinks(["elastic/kibana"]);

		result.Value.Should().BeNull();
		result.HasErrors.Should().BeTrue();
		result.Diagnostics.Single().Message.Should().Contain("could not parse PR reference");
	}

	[Test]
	public void WithoutPrivateLinks_ABareNumberIsKeptWithAWarning()
	{
		var entry = BundledEntry.Inline(ChangelogEntryType.Feature, "T", "kibana", ["155"], null);

		var result = entry.WithoutPrivateLinks(["elastic/kibana"]);

		result.Value!.Value.Prs.Should().Equal("155");
		result.Diagnostics.Should().ContainSingle().Which.Severity.Should().Be(Severity.Warning);
	}

	[Test]
	public void ChangelogEntry_WithoutPrivateLinks_ScrubsTheEntry()
	{
		var entry = new ChangelogEntry
		{
			Title = "T",
			Type = ChangelogEntryType.Feature,
			Products = [ProductReference.Parse("kibana")],
			Prs = ["https://github.com/elastic/private/pull/2"],
			Impact = "see elastic/private#7"
		};

		var result = entry.WithoutPrivateLinks(["elastic/kibana"]);

		result.Value!.Changed.Should().BeTrue();
		result.Value.Value.Prs.Should().BeEmpty();
		result.Value.Value.Impact.Should().Be("see ");
	}
}

public class LoadedBundleOperationsTests
{
	private static LoadedBundle Loaded(string version, string repo, string? description, DateOnly? date, params string[] files) =>
		new(
			version,
			repo,
			"elastic",
			new Bundle { Products = [new BundledProduct(repo, version)], Description = description, ReleaseDate = date },
			$"/b/{repo}-{repo}-{version}.yaml",
			[.. files.Select(f => new ChangelogEntry { Title = f, Type = ChangelogEntryType.Feature, FileName = f })]
		);

	[Test]
	public void MergeByTarget_TheSameVersionIsOneBundle()
	{
		var result = LoadedBundle.MergeByTarget([
			Loaded("9.0.0", "kibana", "A", new DateOnly(2026, 1, 1), "1"),
			Loaded("9.0.0", "elasticsearch", "B", new DateOnly(2026, 2, 2), "2")
		]);

		result.HasErrors.Should().BeFalse();
		var merged = result.Value!.Should().ContainSingle().Which;
		merged.Repo.Should().Be("elasticsearch+kibana");
		merged.Data.Description.Should().Be("A\n\nB");
		merged.Data.ReleaseDate.Should().Be(new DateOnly(2026, 1, 1));
		merged.Entries.Select(e => e.Title).Should().Equal("1", "2");
	}

	[Test]
	public void MergeByTarget_DifferentVersionsStayApart_NewestFirst_AndNoEntryIsLost()
	{
		var result = LoadedBundle.MergeByTarget([
			Loaded("9.0.0", "kibana", null, null, "1"),
			Loaded("9.1.0", "kibana", null, null, "2", "3"),
			Loaded("9.0.0", "kibana", null, null, "4")
		]);

		result.Value!.Select(b => b.Version).Should().Equal("9.1.0", "9.0.0");
		result.Value.SelectMany(b => b.Entries).Select(e => e.Title).Order().Should().Equal("1", "2", "3", "4");
	}

	[Test]
	public void MergeByTarget_AnEmptyOrSingleListIsReturnedAsIs()
	{
		LoadedBundle.MergeByTarget([]).Value.Should().BeEmpty();
		var one = Loaded("9.0.0", "kibana", null, null, "1");
		LoadedBundle.MergeByTarget([one]).Value.Should().Equal(one);
	}
}
