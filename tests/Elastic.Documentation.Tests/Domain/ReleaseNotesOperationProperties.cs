// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities.CsCheck;
using NSeverity = Nullean.Entities.Severity;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>Generators for the release-notes operation properties: references to repositories some of which the allow-list does not name.</summary>
internal static class ReleaseNotesGen
{
	private static readonly string[] Repositories = ["elastic/kibana", "elastic/elasticsearch", "acme/secret", "acme/internal-tools"];

	/// <summary>The allow-list the scrubbing properties run against.</summary>
	public static readonly IReadOnlyList<string> Allow = ["elastic/kibana", "elastic/elasticsearch"];

	/// <summary>A PR or issue reference in a form a file carries: a GitHub URL, <c>owner/repo#n</c> or a bare number.</summary>
	public static Gen<string> Reference { get; } = Gen.Select(
		Gen.OneOfConst(Repositories),
		Gen.Int[1, 500],
		Gen.Int[0, 2],
		(repo, number, form) =>
			form switch
			{
				0 => $"https://github.com/{repo}/pull/{number}",
				1 => $"{repo}#{number}",
				_ => number.ToString(CultureInfo.InvariantCulture)
			}
	);

	/// <summary>A reference that carries its repository (a bare number does not, and is read against the default).</summary>
	private static Gen<string> QualifiedReference { get; } = Gen.Select(
		Gen.OneOfConst(Repositories),
		Gen.Int[1, 500],
		Gen.Bool,
		(repo, number, url) => url ? $"https://github.com/{repo}/issues/{number}" : $"{repo}#{number}"
	);

	/// <summary>Free text that mentions zero to three qualified references, or none at all.</summary>
	public static Gen<string?> Prose { get; } = Gen.Select(
		QualifiedReference.Array[0, 3],
		Gen.Int[0, 3],
		(refs, shape) => shape == 0 ? null : string.Join(" and ", refs.Select(r => $"see {r} for details"))
	);

	public static Gen<ChangelogEntry> Entries { get; } = ChangelogEntryExamples.Generated<ChangelogEntry>();

	/// <summary>A valid entry whose references and prose point at allowed and disallowed repositories alike.</summary>
	public static Gen<ChangelogEntry> EntriesWithReferences { get; } = Gen.Select(
		Entries,
		Reference.Array[0, 4],
		Reference.Array[0, 4],
		Prose,
		Prose,
		Prose,
		(e, prs, issues, description, impact, action) =>
			e with { Prs = prs, Issues = issues, Description = description, Impact = impact, Action = action }
	);

	public static Gen<BundledEntry> BundledEntriesWithReferences { get; } = Gen.Select(
		EntriesWithReferences,
		Gen.Int[0, 9999],
		(e, i) => e.ToBundledEntry($"{i}.yaml", "sum")
	);

	public static Gen<Bundle> BundlesWithReferences { get; } = Gen.Select(
		BundleExamples.Generated<Bundle>(),
		BundledEntriesWithReferences.Array[0, 4],
		Prose,
		(b, entries, description) => b with { Entries = entries, Description = description }
	);

	public static Gen<PullRequestFacts> Facts { get; } = Gen.Select(
		DomainGen.Of<PullRequestFacts>(),
		Reference,
		Gen.OneOfConst<string?>(null, "feature", "bug-fix", "breaking-change", "not-a-type", ""),
		(f, url, type) => f with { Url = url, Type = type }
	);

	/// <summary>Arbitrary text for every free field: blanks, long text, anything the CLI could be handed.</summary>
	public static Gen<CreateEntryFacts> AnyCreateFacts { get; } = Gen.Select(
		Props.OptionalString,
		Gen.OneOfConst<string?>(null, "", "feature", "breaking-change", "nope"),
		Gen.OneOfConst<string?>(null, "", "api", "nope"),
		Props.OptionalString,
		Props.OptionalString.Array[0, 3],
		(title, type, subtype, description, any) => new CreateEntryFacts(
			title,
			type,
			subtype,
			description,
			any.ElementAtOrDefault(0),
			any.ElementAtOrDefault(1),
			any.ElementAtOrDefault(2),
			null,
			any,
			[],
			any,
			any
		)
	);
}

public class ChangelogEntryOperationProperties
{
	private static IEnumerable<Nullean.Entities.Violation> Errors<T>(Nullean.Entities.ConstraintSet<T> set, T value) =>
		set.Validate(value).Where(v => v.Severity == NSeverity.Error);

	/// <summary>
	/// Law: an entry made from a pull request's facts is always returned, never throws, and says it has errors exactly when the read rules find one.
	/// Consequence if broken: a backfill or a git range run dies on one odd PR, or writes an entry that a later read rejects without a word.
	/// </summary>
	[Test]
	public void FromPullRequest_AlwaysReturnsTheEntry_AndItsVerdictIsTheReadRules() =>
		Gen.Select(ReleaseNotesGen.Facts, ReleaseNotesGen.Entries).Check(x =>
		{
			var (facts, products) = x;
			var result = ChangelogEntry.FromPullRequest(facts, products.Products!);

			result.Value.Should().NotBeNull();
			result.HasErrors.Should().Be(Errors(ChangelogEntryReadConstraints.All, result.Value!).Any());
			result.Value!.Prs.Should().Equal(facts.Url);
			result.Value.Title.Should().Be(facts.Title);
		});

	/// <summary>
	/// Law: a CLI's facts, whatever text they carry, make an entry without throwing; unrecognized text falls back to other and no subtype.
	/// Consequence if broken: <c>changelog add</c> crashes on input it should have reported.
	/// </summary>
	[Test]
	public void Create_NeverThrows() =>
		ReleaseNotesGen.AnyCreateFacts.Check(facts =>
		{
			var result = ChangelogEntry.Create(facts);

			result.Value.Should().NotBeNull();
			result.HasErrors.Should().Be(Errors(ChangelogEntryReadConstraints.All, result.Value!).Any());
		});

	/// <summary>
	/// Law: the marker of any PR number is a valid marker: it satisfies the full entry rules and the marker rules, and says it is one.
	/// Consequence if broken: the pool writes marker files that its own reader rejects.
	/// </summary>
	[Test]
	public void Marker_OfAPrNumberAlwaysSatisfiesTheMarkerRules() =>
		Gen.Int[0, int.MaxValue].Check(pr =>
		{
			var result = ChangelogEntry.Marker(pr);

			result.HasErrors.Should().BeFalse();
			result.Value!.IsMarker.Should().BeTrue();
			ChangelogEntryConstraints.All.Validate(result.Value).Should().BeEmpty();
			ChangelogEntryReadConstraints.All.Validate(result.Value).Should().BeEmpty();
		});

	/// <summary>
	/// Law: a marker or a source pointer for any text is returned (a link that is no PR number is a violation, not an exception).
	/// Consequence if broken: reading one malformed pool file takes down the whole scrub.
	/// </summary>
	[Test]
	public void MarkerAndSourcePointer_OfAnyText_NeverThrow() =>
		Gen.String.Check(text =>
		{
			ChangelogEntry.Marker(text).Value.Should().NotBeNull();
			ChangelogEntry.SourcePointer(text).Value.Should().NotBeNull();
		});

	/// <summary>
	/// Law: the placement of an entry names the lowest PR as the file, every other PR gets a marker that points at it, and each of those markers is valid.
	/// Consequence if broken: two files claim the same PR, or a marker points at a file that is not there.
	/// </summary>
	[Test]
	public void CanonicalKeyAndMarkers_PointAtTheLowestPr() =>
		Gen.Select(ReleaseNotesGen.Entries, Gen.Int[1, 500].Array[1, 5]).Check(x =>
		{
			var (entry, numbers) = x;
			var prs = numbers.Select(n => $"https://github.com/elastic/kibana/pull/{n}").ToArray();
			var placement = (entry with { Prs = prs }).CanonicalKeyAndMarkers("whatever.yaml")!;

			var distinct = numbers.Distinct().Order().ToArray();
			placement.CanonicalFileName.Should().Be($"{distinct[0]}.yaml");
			placement.Markers.Select(m => m.FileName).Should().Equal(distinct.Skip(1).Select(n => $"{n}.yaml"));
			foreach (var (fileName, marker) in placement.Markers)
			{
				fileName.Should().NotBe(placement.CanonicalFileName);
				marker.Link.Should().Be(distinct[0].ToString(CultureInfo.InvariantCulture));
				Errors(ChangelogEntryConstraints.All, marker).Should().BeEmpty();
			}
		});

	/// <summary>
	/// Law: an entry carried into a bundle and back out as a note keeps what both can carry: its type, title, areas, PRs and issues.
	/// Consequence if broken: release notes lose a PR link or change their section between the pool and the page.
	/// </summary>
	[Test]
	public void ToBundledEntry_ThenToNote_KeepsTheSharedFields() =>
		ReleaseNotesGen.EntriesWithReferences.Check(entry =>
		{
			var bundled = entry.ToBundledEntry("12.yaml", "sum");
			var note = bundled.ToNote("elasticsearch", "9.0.0").Value!;

			note.Type.Should().Be(entry.Type);
			note.Title.Should().Be(entry.Title);
			note.Areas.Should().BeEquivalentTo(entry.Areas);
			note.Prs.Should().BeEquivalentTo(entry.Prs);
			note.Issues.Should().BeEquivalentTo(entry.Issues);
			bundled.File.Should().Be(BundledFile.Of("12.yaml", "sum"));
		});

	/// <summary>
	/// Law: scrubbing an entry of private links is idempotent, never throws, and leaves nothing outside the allow-list (a bare number stays only when the default repository is allowed).
	/// Consequence if broken: a public release note links a private repository, or a second scrub changes what the first published.
	/// </summary>
	[Test]
	public void WithoutPrivateLinks_Entry_IsIdempotent_AndLeavesNothingOutsideTheAllowList() =>
		ReleaseNotesGen.EntriesWithReferences.Check(entry =>
		{
			var first = entry.WithoutPrivateLinks(ReleaseNotesGen.Allow, "elastic", "kibana");
			first.Value.Should().NotBeNull();
			var once = first.Value!.Value;

			var text = string.Join(
				'\n',
				(once.Prs ?? []).Concat(once.Issues ?? []).Append(once.Description).Append(once.Impact).Append(once.Action)
			);
			var leak = () => LinkAllowlist.ValidateNoPrivateReferences(text, ReleaseNotesGen.Allow);
			leak.Should().NotThrow();

			var second = once.WithoutPrivateLinks(ReleaseNotesGen.Allow, "elastic", "kibana").Value!;
			second.Value.Should().Be(once);
			second.Changed.Should().BeFalse();

			once.Title.Should().Be(entry.Title);
			once.Type.Should().Be(entry.Type);
			(once.Prs ?? []).Should().BeSubsetOf(entry.Prs ?? []);
		});

	/// <summary>The same law for an entry of a bundle.</summary>
	[Test]
	public void WithoutPrivateLinks_BundledEntry_IsIdempotent_AndLeavesNothingOutsideTheAllowList() =>
		ReleaseNotesGen.BundledEntriesWithReferences.Check(entry =>
		{
			var once = entry.WithoutPrivateLinks(ReleaseNotesGen.Allow, "elastic", "kibana").Value!.Value;

			var text = string.Join(
				'\n',
				(once.Prs ?? []).Concat(once.Issues ?? []).Append(once.Description).Append(once.Impact).Append(once.Action)
			);
			var leak = () => LinkAllowlist.ValidateNoPrivateReferences(text, ReleaseNotesGen.Allow);
			leak.Should().NotThrow();
			once.WithoutPrivateLinks(ReleaseNotesGen.Allow, "elastic", "kibana").Value!.Value.Should().Be(once);
		});
}

public class BundleOperationProperties
{
	private static readonly DateOnly Today = new(2026, 3, 1);

	/// <summary>
	/// Law: scrubbing a bundle of private links is idempotent and leaves no reference outside the allow-list in any entry or in the intro.
	/// Consequence if broken: a published bundle carries a private link in a place the scrub did not look.
	/// </summary>
	[Test]
	public void WithoutPrivateLinks_Bundle_IsIdempotent_AndLeavesNothingOutsideTheAllowList() =>
		ReleaseNotesGen.BundlesWithReferences.Check(bundle =>
		{
			var once = bundle.WithoutPrivateLinks(ReleaseNotesGen.Allow, "elastic", "kibana").Value!.Value;

			foreach (var e in once.Entries)
			{
				var text = string.Join('\n', (e.Prs ?? []).Concat(e.Issues ?? []).Append(e.Description).Append(e.Impact).Append(e.Action));
				var leak = () => LinkAllowlist.ValidateNoPrivateReferences(text, ReleaseNotesGen.Allow);
				leak.Should().NotThrow();
			}
			var intro = () => LinkAllowlist.ValidateNoPrivateReferences(once.Description ?? "", ReleaseNotesGen.Allow);
			intro.Should().NotThrow();

			var twice = once.WithoutPrivateLinks(ReleaseNotesGen.Allow, "elastic", "kibana").Value!;
			twice.Value.Should().Be(once);
			twice.Changed.Should().BeFalse();
			once.Entries.Count.Should().Be(bundle.Entries.Count);
		});

	/// <summary>
	/// Law: applying a parent's amends twice gives what applying them once does (an added entry already in the bundle, by file name and checksum, is not added again).
	/// Consequence if broken: re-running a render over a bundle that was already amended duplicates the added entries or re-patches the intro.
	/// </summary>
	[Test]
	public void ApplyAmends_IsIdempotent() =>
		BundleExamples
			.Generated<BundleAmendOfParent>()
			.Array[1, 3]
			.Check(pairs =>
			{
				var parent = pairs[0].Parent;
				var amends = pairs.Select(p => new BundleAmend(p.Amend)).ToList();

				var once = parent.ApplyAmends(amends).Value!;
				var twice = once.ApplyAmends(amends).Value!;

				twice.Entries.Should().Equal(once.Entries);
				twice.Description.Should().Be(once.Description);
				twice.Should().Be(once);
			});

	/// <summary>
	/// Law: the exclusions and the intro patches of an amend are idempotent: applying an amend that adds nothing twice gives what applying it once does.
	/// Consequence if broken: re-applying an amend resurrects or removes entries it should not touch.
	/// </summary>
	[Test]
	public void ApplyAmends_ExclusionsAndDescriptionPatches_AreIdempotent() =>
		BundleExamples
			.Generated<BundleAmendOfParent>()
			.Array[1, 3]
			.Check(pairs =>
			{
				var parent = pairs[0].Parent;
				var amends = pairs.Select(p => new BundleAmend(p.Amend with { Entries = [] })).ToList();

				var once = parent.ApplyAmends(amends).Value!;
				var twice = once.ApplyAmends(amends).Value!;

				twice.Should().Be(once);
			});

	/// <summary>
	/// Law: applying amends never throws, keeps the parent's products and returns the bundle with every error the amended bundle breaks.
	/// Consequence if broken: a bad amend file stops a render instead of being reported.
	/// </summary>
	[Test]
	public void ApplyAmends_NeverThrows_AndKeepsTheParentsProducts() =>
		BundleExamples
			.Generated<BundleAmendOfParent>()
			.Array[0, 3]
			.Check(pairs =>
			{
				var parent = pairs.Length > 0 ? pairs[0].Parent : BundleExamples.Bundle();
				var result = parent.ApplyAmends(pairs.Select(p => new BundleAmend(p.Amend)).ToList());

				result.Value.Should().NotBeNull();
				result.Value!.Products.Should().Equal(parent.Products);
				result.HasErrors.Should().Be(BundleReadConstraints.All.Validate(result.Value).Any(v => v.Severity == NSeverity.Error));
			});

	/// <summary>
	/// Law: an amend made of a parent's products, additions and exclusions, and a bundle assembled from entries, are always returned and agree with the read rules.
	/// Consequence if broken: building a bundle throws where it should report.
	/// </summary>
	[Test]
	public void AmendOf_Assemble_ForRelease_NeverThrow_AndAgreeWithTheReadRules() =>
		Gen.Select(BundleExamples.Generated<Bundle>(), ReleaseNotesGen.Entries.Array[0, 4], Props.OptionalString).Check(x =>
		{
			var (bundle, entries, text) = x;
			var amend = Bundle.AmendOf(bundle.Products, bundle.Entries, [], text);
			var assembled = Bundle.Assemble(
				[.. entries.Select((e, i) => new BundleSource($"{i}-a.yaml", "sum", e))],
				null,
				"kibana",
				"elastic",
				null
			);
			var release = Bundle.ForRelease("kibana", "9.0.0", Lifecycle.Ga, "kibana", "elastic", text, null, bundle.Entries);

			foreach (var result in new[] { amend, assembled, release })
			{
				result.Value.Should().NotBeNull();
				result.HasErrors.Should().Be(BundleReadConstraints.All.Validate(result.Value!).Any(v => v.Severity == NSeverity.Error));
			}
			assembled.Value!.Entries.Select(e => e.File!.Name).Should().Equal(entries.Select((_, i) => $"{i}-a.yaml"));
		});

	/// <summary>
	/// Law: finalizing is deterministic for a given day, and the release date is the one given, else the one the bundle has, else today unless suppressed.
	/// Consequence if broken: two runs of one command write different bundles, or a bundle loses the date it was cut with.
	/// </summary>
	[Test]
	public void Finalize_IsDeterministic_AndFollowsTheReleaseDateRule() =>
		Gen.Select(BundleExamples.Generated<Bundle>(), Props.OptionalDate, Props.OptionalString, Gen.Bool).Check(x =>
		{
			var (bundle, explicitDate, gitRef, suppress) = x;
			var a = bundle.Finalize(null, explicitDate, gitRef, Today, suppress).Value!;
			var b = bundle.Finalize(null, explicitDate, gitRef, Today, suppress).Value!;

			a.Should().Be(b);
			a.ReleaseDate.Should().Be(explicitDate ?? bundle.ReleaseDate ?? (suppress ? null : Today));
			a.GitRef.Should().Be(string.IsNullOrWhiteSpace(gitRef) ? bundle.GitRef : gitRef);
			a.Entries.Should().Equal(bundle.Entries);
		});
}

public class LoadedBundleOperationProperties
{
	private static readonly string[] Versions = ["9.0.0", "9.1.0", "8.19.2", "9.10.0"];

	/// <summary>Loaded bundles that share targets: several of the same version, each with entries of its own.</summary>
	private static Gen<LoadedBundle[]> Bundles { get; } = BundleExamples
		.Generated<LoadedBundle>()
		.Array[0, 6]
		.Select(
			bundles => bundles.Select(
				(b, i) => b with { Version = Versions[i % Versions.Length], FilePath = $"/bundles/{i}.yaml" }
			).ToArray()
		);

	private static Gen<(LoadedBundle[] Bundles, LoadedBundle[] Permuted)> WithPermutation { get; } = Bundles.SelectMany(
		bundles => Gen
			.Int
			.Array[bundles.Length]
			.Select(keys => (bundles, bundles.Zip(keys).OrderBy(p => p.Second).Select(p => p.First).ToArray()))
	);

	private static Dictionary<ChangelogEntry, int> Multiset(IEnumerable<ChangelogEntry> entries) =>
		entries.GroupBy(e => e).ToDictionary(g => g.Key, g => g.Count());

	/// <summary>
	/// Law: merging by target never throws, never loses or duplicates an entry, has one bundle per distinct version, newest first, and joins the repositories in the same way in any order.
	/// Consequence if broken: release notes drop a change or print one twice when two repositories publish the same release.
	/// </summary>
	[Test]
	public void MergeByTarget_PreservesEntries_AndGroupsByVersion() =>
		WithPermutation.Check(x =>
		{
			var (bundles, permuted) = x;
			var merged = LoadedBundle.MergeByTarget(bundles).Value!;
			var mergedPermuted = LoadedBundle.MergeByTarget(permuted).Value!;

			Multiset(merged.SelectMany(b => b.Entries)).Should().BeEquivalentTo(Multiset(bundles.SelectMany(b => b.Entries)));
			merged.Select(b => b.Version).Should().BeEquivalentTo(bundles.Select(b => b.Version).Distinct());
			merged.Select(b => b.Version).Should().OnlyHaveUniqueItems();

			// the same merge in any order: the same versions in the same order, the same repositories, the same entries per version
			mergedPermuted.Select(b => b.Version).Should().Equal(merged.Select(b => b.Version));
			mergedPermuted.Select(b => b.Repo).Should().Equal(merged.Select(b => b.Repo));
			foreach (var (a, b) in merged.Zip(mergedPermuted))
				Multiset(a.Entries).Should().BeEquivalentTo(Multiset(b.Entries));
		});

	/// <summary>
	/// Law: merging the same bundles in any order gives an equal result (the bundles of one version are taken in canonical order: release date ascending with undated last, then file path, repository, owner).
	/// Consequence if broken: the page for a release depends on which repository's bundle the file system listed first.
	/// </summary>
	[Test]
	public void MergeByTarget_IsOrderIndependent() =>
		WithPermutation.Check(x =>
		{
			var (bundles, permuted) = x;
			LoadedBundle.MergeByTarget(permuted).Value.Should().Equal(LoadedBundle.MergeByTarget(bundles).Value!);
		});
}
