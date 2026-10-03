// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Constraints;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities.CsCheck;
using Nullean.Entities.CsCheck.FileSystem;

namespace Elastic.Documentation.Entities.Tests;

/// <summary>The real aggregates, generated: every one builds, their own rules survive hostile data, and ours find what theirs do not.</summary>
public partial class GeneratedAggregatesTests
{
	private static readonly AutoMode[] Modes = [AutoMode.Plausible, AutoMode.Mixed, AutoMode.Adversarial];

	// A repository name as a URI scheme: mounts refer to repositories by it.
	[GeneratedRegex("[^a-z0-9]")]
	private static partial Regex NotSchemeCharacters();

	private static string Scheme(string name)
	{
		var clean = NotSchemeCharacters().Replace(name.ToLowerInvariant(), "");
		return clean.Length == 0 || !char.IsLetter(clean[0]) ? "x" + clean : clean;
	}

	/// <summary>The relations these aggregates need declared (the names alone do not give them away) and the loader's own derivation as a normalizer.</summary>
	internal static void Declared(AutoOptions o) =>
		o
			.UseFileSystem()
			.KeyedBy<AssemblyConfiguration, string, Repository>(a => a.ReferenceRepositories, r => r.Name)
			.KeyedBy<ProductCatalog, string, Product>(c => c.Products, p => p.Id)
			.Reference<SiteTableOfContentsRef, Repository, string, Uri>(t => t.Source, r => r.Name, name => new Uri($"{Scheme(name)}://x"))
			.References<DocumentationSetDefinition, Product, string>(d => d.Products, p => p.Id)
			.Identity<SiteTableOfContentsRef, string>(t => t.PathPrefix)
			.Normalize<AssemblyConfiguration>(Support.Derive);

	private static List<T> Draw<T>(AutoMode mode, int count, Action<AutoOptions>? configure = null) =>
		[
			.. Enumerable.Range(0, count).Select(
				i => GenAuto.Of<T>(mode, configure ?? Declared).Generate(new PCG(11u, (ulong)i + 1), null, out _)
			)
		];

	[Test]
	[Arguments(typeof(GlobalCatalog))]
	[Arguments(typeof(ProductCatalog))]
	[Arguments(typeof(DocumentationSetDefinition))]
	[Arguments(typeof(ResolvedDocumentationSet))]
	[Arguments(typeof(SiteNavigationDefinition))]
	[Arguments(typeof(AssemblyConfiguration))]
	[Arguments(typeof(LinkRegistry))]
	[Arguments(typeof(LinkManifest))]
	[Arguments(typeof(ChangelogConfiguration))]
	[Arguments(typeof(CodexConfiguration))]
	[Arguments(typeof(DocsetDiscovery))]
	public void Every_aggregate_is_generated_in_every_mode(Type aggregate)
	{
		foreach (var mode in Modes)
		{
			for (var i = 0; i < 25; i++)
			{
				var generate = () => GenerateObject(aggregate, mode, i);

				generate.Should().NotThrow($"{aggregate.Name} in {mode}");
			}
		}
	}

	private static object? GenerateObject(Type aggregate, AutoMode mode, int i) =>
		typeof(GeneratedAggregatesTests).GetMethod(
			nameof(One),
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
		)!.MakeGenericMethod(aggregate).Invoke(null, [mode, i]);

	private static T One<T>(AutoMode mode, int i) => GenAuto.Of<T>(mode, Declared).Generate(new PCG(2u, (ulong)i + 1), null, out _);

	// ── their rules must report, not throw ──────────────────────────────────────────────────

	[Test]
	[Arguments(AutoMode.Mixed)]
	[Arguments(AutoMode.Adversarial)]
	public void Site_navigation_rules_do_not_throw_on_hostile_data(AutoMode mode)
	{
		// a path_prefix that is not a legal URL path (a space) made `path-prefixes-unique` throw UriFormatException
		foreach (var navigation in Draw<SiteNavigationDefinition>(mode, 300))
		{
			var check = () => SiteNavigationConstraints.Check(navigation);

			check.Should().NotThrow();
		}
	}

	[Test]
	[Arguments(AutoMode.Mixed)]
	[Arguments(AutoMode.Adversarial)]
	public void Documentation_set_rules_do_not_throw_on_hostile_data(AutoMode mode)
	{
		// a default_cta whose value is null made `default-cta-known` throw ArgumentNullException
		foreach (var pair in Draw<DocumentationSetInCatalog>(mode, 300))
		{
			var check = () => DocumentationSetConstraints.All.CheckAll(pair).ToList();

			check.Should().NotThrow();
		}
	}

	// ── ours, on generated data ─────────────────────────────────────────────────────────────

	[Test]
	public void With_the_loaders_derivation_plausible_assemblies_satisfy_every_assembly_rule_and_hostile_ones_do_not()
	{
		Draw<AssemblyConfiguration>(AutoMode.Plausible, 300).SelectMany(a => EntityRules.Assembly.Validate(a)).Should().BeEmpty();
		Draw<AssemblyConfiguration>(AutoMode.Adversarial, 300).Any(a => EntityRules.Assembly.Validate(a).Count > 0).Should().BeTrue();
	}

	[Test]
	public void Generated_data_breaks_every_cross_aggregate_rule_with_the_relations_declared()
	{
		var mixed = Gen.OneOf(
			GenAuto.Of<DocumentationSetInCatalog>(AutoMode.Mixed, Declared),
			GenAuto.Of<DocumentationSetInCatalog>(AutoMode.Adversarial, Declared)
		);
		EntityRules.Products.Probe(mixed).NeverViolated.Should().BeEmpty();

		var toc = Gen.OneOf(
			GenAuto.Of<DocumentationSetDefinition>(AutoMode.Mixed, Declared),
			GenAuto.Of<DocumentationSetDefinition>(AutoMode.Adversarial, Declared)
		);
		EntityRules.Toc.Probe(toc, samples: 3000, throwOnContradiction: false).Entries.Should().NotBeEmpty();
	}

	[Test]
	public void An_unknown_docset_product_is_found_by_our_rule_and_by_none_of_theirs()
	{
		var pairs = Draw<DocumentationSetInCatalog>(AutoMode.Mixed, 300);

		var ours = pairs.Count(p => EntityRules.Products.Validate(p).Count > 0);
		var theirs = pairs.Count(
			p => DocumentationSetConstraints
				.All
				.CheckAll(p)
				.Any(v => v.ConstraintId.Contains("product", StringComparison.Ordinal) && v.ConstraintId != "release-notes-product-known")
		);

		ours.Should().BeGreaterThan(0);
		theirs.Should().Be(0, "no rule of theirs checks a docset's `products` against the catalog");
	}
}
