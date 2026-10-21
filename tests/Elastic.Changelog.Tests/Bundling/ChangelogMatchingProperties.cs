// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using CsCheck;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Tests.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Bundling;

/// <summary>
/// Service-algorithm laws of changelog matching (<see cref="ChangelogEntryMatcher"/>, <see cref="BundleBuilder"/>, <see cref="ProductArgument"/>) over generated
/// entry files. A fixed seed, 200 iterations. The pool is small on purpose, so two files carry the same content often.
/// </summary>
public class ChangelogMatchingProperties
{
	private static readonly string[] Products = ["elasticsearch", "kibana", "cloud-hosted"];
	private static readonly string[] Targets = ["9.3.0", "9.4.0", "10.0.0"];
	private static readonly string[] Lifecycles = ["ga", "beta", "preview"];

	private sealed record Spec(int Pr, int Product, int Target, int Lifecycle, bool WithIssue, bool Comment)
	{
		public string Yaml() =>
			$"""
			{(Comment ? "# a comment\n" : "")}title: Change {Pr}
			type: {(Pr % 2 == 0 ? "feature" : "bug-fix")}
			prs:
			  - "https://github.com/elastic/elasticsearch/pull/{Pr}"
			{(WithIssue ? $"issues:\n  - \"https://github.com/elastic/elasticsearch/issues/{Pr + 100}\"\n" : "")}products:
			  - product: {Products[Product]}
			    target: {Targets[Target]}
			    lifecycle: {Lifecycles[Lifecycle]}
			""";
	}

	private static Gen<Spec> Specs { get; } = Gen.Select(
		Gen.Int[1, 6],
		Gen.Int[0, 2],
		Gen.Int[0, 2],
		Gen.Int[0, 2],
		Gen.Bool,
		Gen.Bool,
		(pr, product, target, lifecycle, issue, comment) => new Spec(pr, product, target, lifecycle, issue, comment)
	);

	/// <summary>Files named <c>f{n}.yaml</c>: the same spec twice is the same content under two names.</summary>
	private static Gen<(string FileName, string Content)[]> Files { get; } = Specs.Array[0, 8].Select(
		specs => specs.Select((s, i) => ($"f{i}.yaml", s.Yaml())).ToArray()
	);

	private static Gen<ProductFilter> Filters { get; } = Gen.Select(
		Gen.OneOfConst<string?>(null, "*", "elasticsearch", "kibana", "ELASTICSEARCH", "elastic*", "cloud*"),
		Gen.OneOfConst<string?>(null, "*", "9.3.0", "9.4.0", "10.0.0", "9.*"),
		Gen.OneOfConst<string?>(null, "*", "ga", "beta", "GA"),
		(product, target, lifecycle) => new ProductFilter { ProductPattern = product, TargetPattern = target, LifecyclePattern = lifecycle }
	);

	private static ChangelogFilterCriteria Criteria(
		bool all = false,
		IReadOnlyList<ProductFilter>? filters = null,
		IEnumerable<string>? prs = null,
		IEnumerable<string>? issues = null
	) =>
		new()
		{
			IncludeAll = all,
			ProductFilters = filters ?? [],
			PrsToMatch = [.. prs ?? []],
			IssuesToMatch = [.. issues ?? []],
			DefaultOwner = "elastic",
			DefaultRepo = "elasticsearch"
		};

	private static ChangelogMatchResult Match((string FileName, string Content)[] files, ChangelogFilterCriteria criteria) =>
		new ChangelogEntryMatcher(new MockFileSystem(), NullLogger.Instance).MatchChangelogContents(
			new TestDiagnosticsCollector(),
			files,
			criteria,
			CancellationToken.None
		);

	private static void Within(string[] subset, string[] superset, string because = "") =>
		subset.Except(superset).Should().BeEmpty(because);

	private static string[] Checksums(ChangelogMatchResult result) => [.. result.Entries.Select(e => e.Checksum)];

	/// <summary>
	/// Law: product filters widen and narrow as written. Filters are alternatives, so adding one never removes an entry; a pattern that names a target
	/// or a lifecycle never matches more than the same filter with a wildcard there; <c>IncludeAll</c> matches everything any filter matches.
	/// Consequence if broken: a bundle for one product silently gains or loses another product's entries.
	/// </summary>
	[Test]
	public void ProductFilters_AreMonotonic_AndSpecificNarrowsWildcard() =>
		Gen.Select(Files, Filters.Array[0, 3], Filters).Check(x =>
		{
			var (files, filters, extra) = x;

			var before = Checksums(Match(files, Criteria(filters: filters)));
			var after = Checksums(Match(files, Criteria(filters: [.. filters, extra])));
			var wildcard = Checksums(Match(files, Criteria(filters: [extra with { TargetPattern = null, LifecyclePattern = null }])));
			var specific = Checksums(Match(files, Criteria(filters: [extra])));
			var all = Checksums(Match(files, Criteria(all: true)));

			if (filters.Length > 0)
				Within(before, after, "adding an alternative filter removes nothing");
			Within(specific, wildcard, "a filter that names a target and a lifecycle matches no more than one that does not");
			Within(after, all);
			Within(wildcard, all);
		});

	/// <summary>
	/// Law: matching is case-insensitive for product, target and lifecycle patterns: upper-casing every pattern changes nothing.
	/// Consequence if broken: <c>--products Elasticsearch</c> bundles nothing while <c>elasticsearch</c> bundles everything.
	/// </summary>
	[Test]
	public void ProductFilters_AreCaseInsensitive() =>
		Gen.Select(Files, Filters).Check(x =>
		{
			var (files, filter) = x;
			var upper = filter with
			{
				ProductPattern = filter.ProductPattern?.ToUpperInvariant(),
				TargetPattern = filter.TargetPattern?.ToUpperInvariant(),
				LifecyclePattern = filter.LifecyclePattern?.ToUpperInvariant()
			};

			Checksums(Match(files, Criteria(filters: [upper]))).Should().Equal(Checksums(Match(files, Criteria(filters: [filter]))));
		});

	/// <summary>
	/// Law: a product pattern spelled with underscores (<c>cloud_hosted</c>, as the create command accepts it) matches the product <c>cloud-hosted</c> (<see cref="Elastic.Documentation.ProductNames.Normalize"/> is applied to the filter and to the entry's product).
	/// Consequence if broken: the same product is spelled one way to create an entry and another way to bundle it.
	/// </summary>
	[Test]
	public void ProductFilter_UnderscoreSpelling_MatchesTheHyphenatedProduct() =>
		Files.Check(files =>
		{
			var hyphenated = new ProductFilter { ProductPattern = "cloud-hosted" };
			var underscored = new ProductFilter { ProductPattern = "cloud_hosted" };

			Checksums(Match(files, Criteria(filters: [underscored])))
				.Should()
				.Equal(Checksums(Match(files, Criteria(filters: [hyphenated]))));
		});

	/// <summary>
	/// Law: content is deduplicated by checksum: each checksum appears once, the first file of a content keeps it (a later copy, or the same text with
	/// comments added, is dropped), feeding the files twice gives the result of feeding them once, and the set of checksums does not depend on the order.
	/// Consequence if broken: a release note appears twice in a bundle, or which file a bundle names depends on the listing order.
	/// </summary>
	[Test]
	public void Matching_DeduplicatesByChecksum_Idempotently() =>
		Gen.Select(Files, Gen.Int).Check(x =>
		{
			var (files, salt) = x;
			var once = Match(files, Criteria(all: true));
			var twice = Match([.. files, .. files], Criteria(all: true));
			var shuffled = files
				.OrderBy(f => (f.FileName.GetHashCode() ^ salt) & 0xFFFF)
				.ThenBy(f => f.FileName, StringComparer.Ordinal)
				.ToArray();

			Checksums(once).Should().OnlyHaveUniqueItems();
			twice.Entries.Select(e => (e.FileName, e.Checksum)).Should().Equal(once.Entries.Select(e => (e.FileName, e.Checksum)));
			Checksums(Match(shuffled, Criteria(all: true))).Order().Should().Equal(Checksums(once).Order());
			once.Entries.Count.Should().Be(files.Select(f => ChangelogBundlingService.ComputeSha1(f.Content)).Distinct().Count());
		});

	/// <summary>
	/// Law: a PR filter keeps an entry only when it names a requested PR, adding a PR to the filter only adds entries, and every requested PR that
	/// matched is reported as matched.
	/// Consequence if broken: a bundle by PR leaves out an entry the PR owns, or takes one it does not.
	/// </summary>
	[Test]
	public void PrFilter_IsMonotonic_AndReportsWhatMatched() =>
		Gen.Select(Files, Gen.Int[1, 8].Array[0, 3], Gen.Int[1, 8]).Check(x =>
		{
			var (files, prs, extra) = x;

			string Url(int pr) => $"https://github.com/elastic/elasticsearch/pull/{pr}";

			var before = Match(files, Criteria(prs: prs.Select(Url)));
			var after = Match(files, Criteria(prs: prs.Append(extra).Select(Url)));

			if (prs.Length > 0)
				Within(Checksums(before), Checksums(after));
			var requested = prs.Select(Url).ToList();
			before.MatchedPrs.Should().OnlyContain(pr => requested.Contains(pr));
			foreach (var entry in prs.Length == 0 ? [] : before.Entries)
				prs.Select(p => $"/pull/{p}").Any(p => entry.Data.Prs!.Any(e => e.EndsWith(p, StringComparison.Ordinal))).Should().BeTrue();
		});

	/// <summary>
	/// Law: building a bundle from matched entries never throws, keeps every entry once (the bundle holds as many entries as were matched), and is valid
	/// for any entries the matcher accepted.
	/// Consequence if broken: a release's bundle drops a note, or an accepted note makes the bundle fail.
	/// </summary>
	[Test]
	public void BuildBundle_KeepsEveryMatchedEntry() =>
		Files.Check(files =>
		{
			var matched = Match(files, Criteria(all: true));

			var result = new BundleBuilder().BuildBundle(
				new TestDiagnosticsCollector(),
				matched.Entries,
				outputProducts: null,
				repo: "elasticsearch",
				owner: "elastic"
			);

			result.IsValid.Should().BeTrue();
			result.Data!.Entries.Should().HaveCount(matched.Entries.Count);
			result.Data.Entries.Select(e => e.File!.Checksum).Should().Equal(Checksums(matched));
		});

	private static Gen<ProductArgument[]> Arguments { get; } = Gen.Select(
		Gen.OneOfConst("elasticsearch", "kibana", "cloud-hosted", "*"),
		Gen.OneOfConst<string?>(null, "9.3.0", "*", "9.3.0|9.4.0"),
		Gen.OneOfConst<string?>(null, "ga", "beta", "*"),
		(p, t, l) => new ProductArgument { Product = p, Target = t, Lifecycle = l is not null && t is null ? null : l }
	).Array[0, 4];

	/// <summary>
	/// Law: a list of product arguments printed as specs and parsed again is the list that was printed, and printing the parse of any text prints a
	/// fixed point.
	/// Consequence if broken: the spec a bundle records for its products is not the filter that built it.
	/// </summary>
	[Test]
	public void ProductSpecs_PrintAndParse_RoundTrip() =>
		Gen.Select(Arguments, Gen.String).Check(x =>
		{
			var (arguments, text) = x;

			ProductArgument.ParseProductSpecs(ProductArgument.FormatProductSpecs(arguments)).Should().Equal(arguments);

			var printed = ProductArgument.FormatProductSpecs(ProductArgument.ParseProductSpecs(text));
			ProductArgument.FormatProductSpecs(ProductArgument.ParseProductSpecs(printed)).Should().Be(printed);
		});
}
