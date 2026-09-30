// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Codex;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Tests.Domain;

// Property tests document their law and its consequence on purpose: a generated input is invisible and a law is not a
// value, so the doc comment is the only place a reader learns what is guaranteed and why it matters.

/// <summary>Named generators for the domain property tests.</summary>
internal static class Gens
{
	private static readonly string[] Repositories = ["kibana", "elasticsearch", "apm-server"];
	private static readonly string[] Segments = ["reference", "release-notes", "kibana", "setup", "api"];

	/// <summary>A path prefix of one to three segments, sometimes with a trailing slash.</summary>
	public static Gen<string> PathPrefix { get; } = Gen.Select(
		Gen.OneOfConst(Segments).Array[1, 3],
		Gen.Bool,
		(parts, slash) => string.Join('/', parts) + (slash ? "/" : "")
	);

	/// <summary>A navigation.yml mount: a repository and the prefix it publishes under.</summary>
	public static Gen<(string Repository, string Prefix)> Mount { get; } = Gen.Select(Gen.OneOfConst(Repositories), PathPrefix);

	/// <summary>Mounts that no repository repeats: each (repository, prefix without trailing slash) pair occurs once.</summary>
	public static Gen<(string Repository, string Prefix)[]> DistinctMounts { get; } = Mount.Array[0, 8].Select(
		m => m.DistinctBy(x => (x.Repository, x.Prefix.TrimEnd('/'))).ToArray()
	);

	/// <summary>A semantic version with small components, so base and current often collide.</summary>
	public static Gen<SemVersion> Version { get; } = Gen.Select(
		Gen.Int[0, 12],
		Gen.Int[0, 4],
		Gen.Int[0, 3],
		(major, minor, patch) => new SemVersion(major, minor, patch)
	);

	/// <summary>Codex groups whose ids are unique ignoring case, each with a name.</summary>
	public static Gen<CodexGroupDefinition[]> DistinctCodexGroups { get; } = Gen
		.String[Gen.Char["a-z"], 1, 8]
		.Array[0, 6]
		.Select(
			ids => ids
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Select(id => new CodexGroupDefinition { Id = id, Name = id.ToUpperInvariant() })
				.ToArray()
		);

	/// <summary>A docset's registry as authored: absent, the known codex registry, the legacy 'public', or an unknown value.</summary>
	public static Gen<string?> Registry { get; } = Gen.OneOfConst<string?>(null, "", "internal", "INTERNAL", "public", "security");

	/// <summary>The registries of the docsets found at a repository's known locations, in discovery order.</summary>
	public static Gen<string?[]> DiscoveredRegistries { get; } = Registry.Array[1, 4];

	/// <summary>A product id from the test catalog, spelled with '-' or '_' and in any case.</summary>
	public static Gen<string> ProductSpelling { get; } = Gen.Select(Gen.OneOfConst(
		"apm-server",
		"elastic-agent",
		"elasticsearch"
	), Gen.Bool, Gen.Bool, (id, underscore, upper) =>
	{
		var spelled = underscore ? id.Replace('-', '_') : id;
		return upper ? spelled.ToUpperInvariant() : spelled;
	});
}

public class ConstraintProperties
{
	private static MockFileSystem FileSystem => new();

	private static string[] ProductIds => ["apm-server", "elastic-agent", "elasticsearch"];

	/// <summary>
	/// Law: when no repository mounts the same path prefix twice, <c>repository-path-prefixes-unique</c> finds nothing,
	/// whatever the mix of repositories and trailing slashes.
	/// Consequence if broken: valid navigation.yml files get warnings, and strict assembler builds fail for no reason.
	/// </summary>
	[Test]
	public void DistinctRepositoryPrefixes_AreNeverReported() =>
		Gens.DistinctMounts.Sample(mounts => RepeatedPrefixFindings(mounts).Should().BeEmpty(), print: Print);

	/// <summary>
	/// Law: mounting any prefix a second time from the same repository is reported, even when only a trailing slash differs.
	/// Consequence if broken: two tables of contents silently publish to one URL space and overwrite each other's pages.
	/// </summary>
	[Test]
	public void RepeatedRepositoryPrefix_IsReported() =>
		Gen.Select(Gens.DistinctMounts, Gens.Mount).Sample(
			x =>
			{
				var (mounts, extra) = x;
				var repeated = mounts.Append(extra).Append(extra with { Prefix = extra.Prefix.TrimEnd('/') + "/" }).ToArray();
				RepeatedPrefixFindings(repeated).Should().NotBeEmpty();
			},
			print: x => $"{Print(x.Item1)} + {x.Item2}"
		);

	/// <summary>
	/// Law: <c>base-not-after-current</c> reports a versioning system exactly when its base version is after its current one.
	/// Consequence if broken: version dropdowns and applies_to badges are computed from an inverted range.
	/// </summary>
	[Test]
	public void BaseAfterCurrent_IsReportedExactlyWhenInverted() =>
		Gen.Select(Gens.Version, Gens.Version).Sample(
			x =>
			{
				var (@base, current) = x;
				var catalog = new VersioningSystemCatalog
				{
					VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
					{
						[VersioningSystemId.Stack] = new() { Id = VersioningSystemId.Stack, Base = @base, Current = current }
					}
				};
				VersioningSystemConstraints.Check(catalog).Any().Should().Be(@base > current);
			},
			print: x => $"base {x.Item1}, current {x.Item2}"
		);

	/// <summary>
	/// Law: codex groups with unique ids and names break no rule, and repeating any id, in any case, is reported.
	/// Consequence if broken: two groups share an id, so docsets join whichever the codex navigation finds first.
	/// </summary>
	[Test]
	public void CodexGroupIds_AreReportedExactlyWhenRepeated() =>
		Gens.DistinctCodexGroups.Sample(
			groups =>
			{
				CodexConfigurationConstraints.Check(new CodexConfiguration { Groups = groups }).Should().BeEmpty();
				if (groups is [var first, ..])
				{
					var repeated = new CodexConfiguration { Groups = [.. groups, first with { Id = first.Id.ToUpperInvariant() }] };
					CodexConfigurationConstraints.Check(repeated).Select(v => v.ConstraintId).Should().Equal("group-ids-unique");
				}
			},
			print: groups => string.Join(", ", groups.Select(g => g.Id))
		);

	/// <summary>
	/// Law: <c>one-docset-per-registry</c> fires exactly when two docsets at known locations resolve to the same registry.
	/// Consequence if broken: two docsets write the same link-index key, and the last one built wins silently.
	/// </summary>
	[Test]
	public void OneDocsetPerRegistry_FiresExactlyOnSharedRegistries() =>
		Gens.DiscoveredRegistries.Sample(
			registries =>
			{
				var discovery = Discovery(registries);
				var shared = discovery.Candidates.GroupBy(c => c.EffectiveRegistry).Any(g => g.Count() > 1);
				Fired(discovery, "one-docset-per-registry").Should().Be(shared);
			},
			print: Print
		);

	/// <summary>
	/// Law: <c>discovered-docset-is-default</c> fires exactly when the docset discovery builds belongs to a codex registry.
	/// Consequence if broken: an isolated or assembler build publishes a codex docset as the repository's public docs.
	/// </summary>
	[Test]
	public void DiscoveredDocset_IsReportedExactlyWhenItIsACodexDocset() =>
		Gens.DiscoveredRegistries.Sample(
			registries =>
			{
				var discovery = Discovery(registries);
				var codexFirst = discovery.Candidates[0].EffectiveRegistry != DocSetRegistry.Public;
				Fired(discovery, "discovered-docset-is-default").Should().Be(codexFirst);
			},
			print: Print
		);

	/// <summary>
	/// Law: a product in changelog.yml's products.available is known in any spelling products.yml accepts: '-' or '_', any case.
	/// Consequence if broken: a changelog.yml that loads today reports its products as unknown, and the command fails.
	/// </summary>
	[Test]
	public void ProductIds_AreKnownInEitherSpelling() =>
		Gens.ProductSpelling.Sample(spelling =>
		{
			var configuration = ChangelogConfiguration.Default with
			{
				ProductsConfiguration = new ProductsConfig { Available = [spelling] }
			};
			ChangelogConfigurationConstraints.Check(configuration, Products).Should().BeEmpty();
		});

	private static ProductCatalog Products =>
		ProductCatalog.FromPublicProducts(ProductIds.ToDictionary(id => id, id => new Product { Id = id, DisplayName = id }));

	private static IEnumerable<string> RepeatedPrefixFindings((string Repository, string Prefix)[] mounts) =>
		SiteNavigationConstraints
			.Check(new SiteNavigationDefinition
			{
				TableOfContents =
				[
					.. mounts.Select(
						m => new SiteNavigationEntry(new SiteTableOfContentsRef(new Uri($"{m.Repository}://toc"), m.Prefix, []))
					)
				]
			})
			.Where(v => v.ConstraintId == "repository-path-prefixes-unique")
			.Select(v => v.Message);

	private static DocsetDiscovery Discovery(string?[] registries) =>
		new([.. registries.Select((r, i) => new DiscoveredDocset(FileSystem.FileInfo.New($"/repo/candidate-{i}/docset.yml"), r))]);

	private static bool Fired(DocsetDiscovery discovery, string constraintId) =>
		DocsetDiscoveryConstraints.Check(discovery).Any(v => v.ConstraintId == constraintId);

	private static string Print((string Repository, string Prefix)[] mounts) =>
		string.Join(", ", mounts.Select(m => $"{m.Repository}:{m.Prefix}"));

	private static string Print(string?[] registries) => string.Join(", ", registries.Select(r => r ?? "<none>"));
}
