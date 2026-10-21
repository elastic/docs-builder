// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using NSeverity = Nullean.Entities.Severity;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>Operation laws of the links aggregates: a registry only grows, a manifest is what its pages say, a key either names an entry or is refused.</summary>
public class LinkOperationProperties
{
	private static Gen<LinkRegistry> Registries { get; } = DomainGen.Of<LinkRegistry>(
		o => o.Normalize<LinkRegistry>(r => LinkExamples.Registry(r))
	);

	private static Gen<LinkRegistryEntry> Entries { get; } = Gen.Select(
		Gen.OneOfConst("kibana", "elasticsearch", "docs-content", "newcomer"),
		Gen.OneOfConst("main", "9.0", "8.19", "release-x"),
		Gen.Int[0, 5000],
		(repository, branch, days) => LinkExamples.Entry(repository, branch) with { UpdatedAt = LinkExamples.Published.AddDays(days) }
	);

	private static List<(string Repository, string Branch, LinkRegistryEntry Entry)> Flatten(LinkRegistry r) =>
		[
			.. r
				.Repositories
				.SelectMany(repo => repo.Value.Select(b => (Repository: repo.Key, Branch: b.Key, Entry: b.Value)))
				.OrderBy(x => x.Repository, StringComparer.Ordinal)
				.ThenBy(x => x.Branch, StringComparer.Ordinal)
		];

	/// <summary>
	/// Law: adding an entry to a registry never removes another, keeps the newer entry of a branch, is idempotent, and leaves the registry it started from as it was.
	/// Consequence if broken: publishing one repository's links makes another repository's cross-links vanish, or a stale upload overwrites a newer one.
	/// </summary>
	[Test]
	public void WithLinkRegistryEntry_IsMonotonic_Idempotent_AndKeepsTheNewer() =>
		Gen.Select(Registries, Entries).Check(x =>
		{
			var (registry, entry) = x;
			var before = Flatten(registry);

			var once = registry.WithLinkRegistryEntry(entry);
			once.Value.Should().NotBeNull();
			var added = once.Value!;
			var twice = added.WithLinkRegistryEntry(entry).Value!;

			Flatten(registry).Should().Equal(before, "the registry it started from is not touched");
			var keys = Flatten(added).Select(x => (x.Repository, x.Branch)).ToHashSet();
			before.Select(x => (x.Repository, x.Branch)).Should().BeSubsetOf(keys);
			keys.Should().Contain((entry.Repository, entry.Branch));
			Flatten(twice).Should().Equal(Flatten(added));

			var kept = added.Repositories[entry.Repository][entry.Branch];
			var existing = before
				.Where(x => x.Repository == entry.Repository && x.Branch == entry.Branch)
				.Select(x => x.Entry)
				.FirstOrDefault();
			kept
				.UpdatedAt
				.Ticks
				.Should()
				.Be(existing is null ? entry.UpdatedAt.Ticks : Math.Max(existing.UpdatedAt.Ticks, entry.UpdatedAt.Ticks));
			// an entry that is not the newest of its branch leaves every other branch as it was
			foreach (var other in before.Where(x => x.Repository != entry.Repository || x.Branch != entry.Branch))
				added.Repositories[other.Repository][other.Branch].Should().Be(other.Entry);
		});

	/// <summary>
	/// Law: the entry of an S3 key is either made (repository and branch are the second and third segments) or refused with an ArgumentException, whatever the key says.
	/// Consequence if broken: one stray object in the bucket crashes the registry update for every repository.
	/// </summary>
	[Test]
	public void FromS3Key_OfAnyKey_IsAnEntryOrAnArgumentException() =>
		Gen.String.Check(key =>
		{
			LinkRegistryEntry? entry = null;
			var act = () => entry = LinkRegistryEntry.FromS3Key(key, "etag", LinkExamples.Published, "abc");
			if (key.Split('/').Length < 3)
				act.Should().Throw<ArgumentException>();
			else
			{
				act.Should().NotThrow();
				var parts = key.Split('/');
				entry!.Repository.Should().Be(parts[1]);
				entry.Branch.Should().Be(parts[2]);
				entry.Path.Should().Be(key);
			}
		});

	/// <summary>
	/// Law: a manifest built from pages has one entry per distinct page path, its cross-links distinct and sorted, and is returned (with its findings) whatever the pages are.
	/// Consequence if broken: a build publishes a links.json that lost a page, or throws instead of reporting.
	/// </summary>
	[Test]
	public void ManifestFrom_HasOnePageOfEachPath_AndNeverThrows() =>
		Gen.Select(
			Gen.OneOfConst("a.md", "b/c.md", "d.md", "e/f/g.md", "").Select(
				Gen.Bool,
				Gen.OneOfConst("x", "y", "x").Array[0, 3],
				(p, h, a) => new LinkPage(p, a, h, [])
			).Array[0, 8],
			Gen.OneOfConst("scheme://page.md", "other://page.md").Array[0, 4],
			Gen.OneOfConst<string?>(null, "", "/docs")
		).Check(x =>
		{
			var (pages, crossLinks, prefix) = x;
			var origin = GitCheckoutInformation.ForElasticRepository("kibana", "abc", "main");

			var result = LinkManifest.From(origin, prefix, pages, crossLinks, null);

			result.Value.Should().NotBeNull();
			result.Value!.Links.Keys.Should().BeEquivalentTo(pages.Select(p => p.Path).Distinct());
			result.Value.CrossLinks.Should().Equal(crossLinks.Distinct().Order(StringComparer.Ordinal));
			result.HasErrors.Should().Be(LinkManifestConstraints.All.Validate(result.Value).Any(v => v.Severity == NSeverity.Error));
		});

	/// <summary>
	/// Law: the stand-in manifest of any repository name is returned without throwing and has no pages.
	/// Consequence if broken: one repository whose links could not be fetched stops the cross-link check for the others.
	/// </summary>
	[Test]
	public void ManifestEmpty_OfAnyName_NeverThrows() =>
		Gen.String.Check(name =>
		{
			var result = LinkManifest.Empty(name);

			result.Value.Should().NotBeNull();
			result.Value!.Links.Should().BeEmpty();
			result.Value.Origin.RepositoryName.Should().Be(name);
		});
}

/// <summary>Operation laws of the publishing aggregates.</summary>
public class PublishingOperationProperties
{
	private static Gen<SiteNavigationDefinition> Navigations { get; } = DomainGen.Of<SiteNavigationDefinition>(
		o => o.Normalize<SiteNavigationDefinition>(SiteNavigationConstraintsTests.Valid)
	);

	private static IEnumerable<string> Schemes(SiteNavigationDefinition n) =>
		SiteNavigationDefinition.GetAllDeclaredSources(n).Select(u => u.Scheme);

	private static Gen<(SiteNavigationDefinition Navigation, HashSet<string> Removed)> NavigationsWithRemoval
	{
		get;
	} = Navigations.SelectMany(n =>
	{
		var all = Schemes(n).Concat(n.Phantoms.Select(p => p.Source[..p.Source.IndexOf(':')])).Distinct().Order().ToArray();
		return Gen.Bool.Array[all.Length].Select(pick => (n, all.Where((_, i) => pick[i]).ToHashSet()));
	});

	/// <summary>
	/// Law: removing repositories from the navigation drops their mounts and phantoms, keeps everything else in order, and returns the navigation checked by its rules.
	/// The one thing that stays is a child promoted out of a removed mount, which is kept as it is, even when it is itself from a removed repository (documented).
	/// Consequence if broken: a private repository stays in the published navigation, or a public one disappears with it.
	/// </summary>
	[Test]
	public void WithoutRepositories_RemovesTheirMounts_AndRechecks() =>
		NavigationsWithRemoval.Check(x =>
		{
			var (navigation, removed) = x;
			var result = navigation.WithoutRepositories(removed);

			result.Value.Should().NotBeNull();
			var derived = result.Value!;
			derived.Phantoms.Select(p => p.Source[..p.Source.IndexOf(':')]).Should().NotIntersectWith(removed);

			var promoted = SiteNavigationDefinition
				.GetAllDeclaredSources(navigation)
				.Where(u => removed.Contains(u.Scheme))
				.SelectMany(u => ChildrenOf(navigation, u))
				.Select(c => c.Source.Scheme)
				.ToHashSet();
			Schemes(derived).Where(removed.Contains).Should().BeSubsetOf(promoted);
			Schemes(derived)
				.Where(s => !removed.Contains(s))
				.Order()
				.Should()
				.Equal(Schemes(navigation).Where(s => !removed.Contains(s)).Order());

			result.HasErrors.Should().Be(SiteNavigationConstraints.All.Validate(derived).Any(v => v.Severity == NSeverity.Error));
		});

	private static IEnumerable<SiteTableOfContentsRef> ChildrenOf(SiteNavigationDefinition navigation, Uri source)
	{
		IEnumerable<SiteTableOfContentsRef> Walk(IEnumerable<SiteTableOfContentsRef> tocs) =>
			tocs.SelectMany(t => new[] { t }.Concat(Walk(t.Children)));

		return Walk(
			navigation.TableOfContents.SelectMany(
				e => e.Value is SiteTableOfContentsRef toc ? [toc] : e.Value is SiteSectionRef s ? s.Children : []
			)
		).Where(t => t.Source == source).SelectMany(t => t.Children);
	}

	/// <summary>
	/// Law: removing nothing and appending nothing leaves the navigation as it was.
	/// Consequence if broken: building the navigation for an environment with no private repositories changes it.
	/// </summary>
	[Test]
	public void WithoutRepositories_OfNone_IsTheIdentity() =>
		Navigations.Check(n => n.WithoutRepositories(new HashSet<string>()).Value.Should().Be(n));

	/// <summary>
	/// Law: the configuration checkout of a repository changes the sparse paths and nothing else, and doing it twice is doing it once.
	/// Consequence if broken: cloning the configuration repository alters its branch, origin or strategy.
	/// </summary>
	[Test]
	public void AsConfigurationCheckout_ChangesOnlyTheSparsePaths_AndIsIdempotent() =>
		DomainGen.Of<Repository>().Check(repository =>
		{
			var checkout = repository.AsConfigurationCheckout();

			checkout.SparsePaths.Should().Equal("config");
			(checkout with { SparsePaths = repository.SparsePaths }).Should().Be(repository);
			checkout.AsConfigurationCheckout().Should().Be(checkout);
		});
}
