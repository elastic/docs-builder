// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using CsCheck;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Generators of catalog records in every state: blank and whitespace text, undefined enum values, versions out of order, keys that differ only in
/// case, duplicates and empty collections. <c>Hostile</c> generators also break the rules; <c>Lawful</c> generators are as hostile as the rules
/// allow and satisfy every one, which is what the invariant axis of <c>AutoTest</c> needs. <c>Nulls</c> generators add the nulls the type
/// annotations forbid (a null product in the dictionary, a null nested record).
/// </summary>
internal static class CatalogGen
{
	private static Gen<T> Just<T>(T value) => Gen.Const<T>(() => value);

	public static readonly Gen<string> Text = Gen.OneOf(
		"widgets",
		"Widgets",
		"WIDGETS",
		"",
		" ",
		"\t\r\n",
		"  widgets  ",
		"cloud_hosted",
		"cloud-hosted",
		"ünï",
		"a/b",
		"\0"
	);

	public static readonly Gen<string> Keys = Gen.OneOf(
		"widgets",
		"Widgets",
		"WIDGETS",
		"",
		" ",
		"gadgets",
		"cloud-hosted",
		"Cloud_Hosted",
		"ünï"
	);

	public static readonly Gen<string> Urls = Gen.OneOf(
		"https://www.elastic.co/training/demo",
		"HTTP://EXAMPLE.TEST/UPPER",
		"http://[::1]/a",
		"https://ü.example/é",
		"",
		" ",
		"/training/demo",
		"ftp://www.elastic.co/x",
		"http:///",
		"https://",
		"http://[",
		"javascript:alert(1)",
		"mailto:a@b.test",
		"C:\\temp\\x",
		"//host/x",
		"http://a b",
		"\0"
	);

	private static readonly string[] AbsoluteUrls =
	[
		"https://www.elastic.co/training/demo",
		"HTTP://EXAMPLE.TEST/UPPER",
		"http://[::1]/a",
		"https://ü.example/é",
		"https://example.test/a?b=c#d"
	];

	private static readonly string[] Titles = ["Demo", "demo", " Demo ", "ünï", "a\0b", "<b>x</b>"];

	private static readonly Gen<int> Parts = Gen.OneOf(0, 1, 8, 9, 99999, int.MaxValue);

	public static readonly Gen<SemVersion> Versions = Gen.OneOf(
		Gen.Select(Parts, Parts, Gen.OneOf(0, 5), (major, minor, patch) => new SemVersion(major, minor, patch)),
		Gen.Select(Parts, Gen.OneOf("beta", "rc.1", "", " "), (major, pre) => new SemVersion(major, 0, 0, pre)),
		Just<SemVersion>(AllVersions.Instance),
		Just<SemVersion>(new ZeroVersion())
	);

	public static readonly Gen<VersioningSystemId> VersioningIds = Gen.OneOf(
		VersioningSystemId.Stack,
		VersioningSystemId.All,
		VersioningSystemId.Ech,
		VersioningSystemId.Serverless,
		VersioningSystemId.ApmAgentJava,
		VersioningSystemId.None,
		(VersioningSystemId)12345,
		(VersioningSystemId)(-5)
	);

	/// <summary>A versioning system; base and current are independent, so half of them are out of order.</summary>
	public static readonly Gen<VersioningSystem> VersioningSystems = Gen.Select(
		VersioningIds,
		Versions,
		Versions,
		(id, baseVersion, current) => new VersioningSystem { Id = id, Base = baseVersion, Current = current }
	);

	/// <summary>Only ids and versions a versioning system can carry in a file, but unordered and with undefined enum values.</summary>
	private static Gen<VersioningSystem?> OrNull(Gen<VersioningSystem> gen) =>
		Gen.Frequency(
			(1, Just<VersioningSystem?>(null)),
			(1, Just<VersioningSystem?>(VersioningSystem.None)),
			(6, gen.Select(v => (VersioningSystem?)v))
		);

	private static readonly Gen<ProductFeatures> Features = Gen.Select(
		Gen.Bool,
		Gen.OneOf(
			ReleaseNotesPath.None,
			ReleaseNotesPath.OnRelease,
			ReleaseNotesPath.DailyReleasableArtifacts,
			(ReleaseNotesPath)42,
			(ReleaseNotesPath)(-1)
		),
		(publicReference, releaseNotes) => new ProductFeatures { PublicReference = publicReference, ReleaseNotes = releaseNotes }
	);

	private static readonly Gen<string?> OptionalText = Gen.Frequency((1, Just<string?>(null)), (4, Text.Select(t => (string?)t)));

	/// <summary>A product that breaks <c>public-reference-product-has-versioning</c> about as often as it satisfies it.</summary>
	public static readonly Gen<Product> HostileProducts = Gen.Select(
		Text,
		Text,
		OrNull(VersioningSystems),
		OptionalText,
		OptionalText,
		Features,
		(id, displayName, versioning, repository, dra, features) => new Product
		{
			Id = id,
			DisplayName = displayName,
			VersioningSystem = versioning,
			Repository = repository,
			DraArtifact = dra,
			Features = features
		}
	);

	/// <summary>A product whose text members and features are hostile and whose versioning satisfies the rule: public reference products have one.</summary>
	public static readonly Gen<Product> LawfulProducts = HostileProducts.Select(
		p => p.Features.PublicReference && (p.VersioningSystem is null || p.VersioningSystem.Id == VersioningSystemId.None)
			? p with
			{
				VersioningSystem = new VersioningSystem
				{
					Id = VersioningSystemId.Stack,
					Base = new SemVersion(9, 0, 0),
					Current = new SemVersion(8, 0, 0)
				}
			}
			: p
	);

	/// <summary>Products under keys that may differ from their id, differ from each other only in case, or repeat (the last one wins).</summary>
	private static Gen<FrozenDictionary<string, Product>> ProductsByKey(Gen<Product> products) =>
		Gen.Select(Keys, products).List[0, 6].Select(pairs => ToFrozen(pairs.Select(p => (p.Item1, p.Item2))));

	public static readonly Gen<ProductCatalog> HostileProductCatalogs = ProductsByKey(HostileProducts).Select(
		p => new ProductCatalog { Products = p }
	);
	public static readonly Gen<ProductCatalog> LawfulProductCatalogs = ProductsByKey(LawfulProducts).Select(
		p => new ProductCatalog { Products = p }
	);

	/// <summary>A catalog the type annotations forbid: null products, a product without features, and no products at all.</summary>
	public static readonly Gen<ProductCatalog> NullProductCatalogs = Gen.Frequency(
		(3, ProductsByKey(
			Gen.Frequency((1, Just<Product>(null!)), (1, HostileProducts), (1, HostileProducts.Select(p => p with { Features = null! })))
		).Select(p => new ProductCatalog { Products = p })),
		(1, Just(new ProductCatalog { Products = null! }))
	);

	public static FrozenDictionary<string, T> ToFrozen<T>(IEnumerable<(string Key, T Value)> pairs)
	{
		var dictionary = new Dictionary<string, T>();
		foreach (var (key, value) in pairs)
			dictionary[key] = value;
		return dictionary.ToFrozenDictionary();
	}

	private static Gen<RelatedLearningLink> Link(Gen<string> title, Gen<string> url) =>
		Gen.Select(Text, title, url, (id, t, u) => new RelatedLearningLink { Id = id, Title = t, Url = u });

	private static Gen<RelatedLearningCatalog> LinkCatalog(Gen<RelatedLearningLink> links) =>
		Gen
			.Select(Keys, links)
			.List[0, 5]
			.Select(pairs => new RelatedLearningCatalog { Links = ToFrozen(pairs.Select(p => (p.Item1, p.Item2))) });

	public static readonly Gen<RelatedLearningCatalog> HostileRelatedLearning = LinkCatalog(Link(Gen.OneOf(Gen.OneOf(Titles), Text), Urls));

	public static readonly Gen<RelatedLearningCatalog> LawfulRelatedLearning = LinkCatalog(
		Link(Gen.OneOf(Titles), Gen.OneOf(AbsoluteUrls))
	);

	public static readonly Gen<RelatedLearningCatalog> NullRelatedLearning = Gen.Frequency(
		(3, LinkCatalog(
			Gen.Frequency(
				(1, Just<RelatedLearningLink>(null!)),
				(1, Link(Gen.OneOf(Text, Just<string>(null!)), Gen.OneOf(Urls, Just<string>(null!))))
			)
		)),
		(1, Just(new RelatedLearningCatalog { Links = null! }))
	);

	private static Gen<VersioningSystemCatalog> VersionCatalogOf(Gen<VersioningSystem> systems) =>
		Gen
			.Select(VersioningIds, systems)
			.List[0, 5]
			.Select(pairs =>
			{
				var dictionary = new Dictionary<VersioningSystemId, VersioningSystem>();
				foreach (var (id, system) in pairs)
					dictionary[id] = system;
				return new VersioningSystemCatalog { VersioningSystems = dictionary };
			});

	/// <summary>Versioning catalogs keyed by an id that may disagree with the system's own id, with undefined enum values and empty catalogs.</summary>
	/// <summary>Versioning catalogs whose keys are the systems' own ids, so a product can point at one that is defined.</summary>
	private static readonly Gen<VersioningSystemCatalog> KeyedVersioningCatalogs = VersioningSystems.List[0, 5].Select(
		systems => new VersioningSystemCatalog { VersioningSystems = systems.GroupBy(v => v.Id).ToDictionary(g => g.Key, g => g.Last()) }
	);

	public static readonly Gen<VersioningSystemCatalog> HostileVersioningCatalogs = VersionCatalogOf(VersioningSystems);

	public static readonly Gen<VersioningSystemCatalog> NullVersioningCatalogs = Gen.Frequency(
		(3, VersionCatalogOf(Gen.OneOf(VersioningSystems, Just<VersioningSystem>(null!)))),
		(1, Just(new VersioningSystemCatalog { VersioningSystems = null! }))
	);

	public static readonly Gen<ProductCatalogInVersioningSystemCatalog> HostileProductsInVersioning = Gen.Select(
		HostileProductCatalogs,
		HostileVersioningCatalogs,
		(products, versions) => new ProductCatalogInVersioningSystemCatalog(products, versions)
	);

	/// <summary>Every product points at a versioning system of the catalog (or at none), whatever else is hostile about it.</summary>
	public static readonly Gen<ProductCatalogInVersioningSystemCatalog> LawfulProductsInVersioning = KeyedVersioningCatalogs.SelectMany(versions =>
	{
		var defined = versions
			.VersioningSystems
			.Where(v => v.Key == v.Value.Id && v.Key != VersioningSystemId.None)
			.Select(v => v.Value)
			.ToArray();
		var known = defined.Length == 0 ? Just<VersioningSystem?>(null) : Gen.OneOf(defined).Select(v => (VersioningSystem?)v);
		return Gen
			.Select(Keys, HostileProducts, known)
			.List[0, 5]
			.Select(
				pairs => new ProductCatalogInVersioningSystemCatalog(
					new ProductCatalog
					{
						Products = ToFrozen(
							pairs.Select(
								p =>
									(p.Item1, p.Item2 with
									{
										VersioningSystem = p.Item2.VersioningSystem is { } own && own.Id != VersioningSystemId.None
											? p.Item3
											: p.Item2.VersioningSystem
									})
							)
						)
					},
					versions
				)
			);
	});

	public static readonly Gen<ProductCatalogInVersioningSystemCatalog> NullProductsInVersioning = Gen.OneOf(
		Gen.Select(NullProductCatalogs, HostileVersioningCatalogs, (p, v) => new ProductCatalogInVersioningSystemCatalog(p, v)),
		Gen.Select(HostileProductCatalogs, NullVersioningCatalogs, (p, v) => new ProductCatalogInVersioningSystemCatalog(p, v)),
		Just(new ProductCatalogInVersioningSystemCatalog(null!, null!))
	);

	private static Gen<LegacyUrlMapping> Mapping(Gen<Product> products) =>
		Gen.Select(
			Gen.OneOf("en/widgets/", "EN/Widgets/", "", " ", "en/widgets/", "ünï/"),
			products,
			Gen.OneOf("1.0", "", " ", "9.0.0-beta", "1.0").List[0, 4],
			(url, product, versions) => new LegacyUrlMapping { BaseUrl = url, Product = product, LegacyVersions = versions }
		);

	private static Gen<LegacyUrlMappingCatalog> MappingCatalog(Gen<LegacyUrlMapping> mappings) =>
		mappings.List[0, 5].Select(list => new LegacyUrlMappingCatalog { Mappings = list });

	public static readonly Gen<LegacyUrlMappingCatalog> HostileLegacyMappings = MappingCatalog(Mapping(HostileProducts));

	public static readonly Gen<LegacyUrlMappingCatalog> NullLegacyMappings = Gen.Frequency(
		(3, MappingCatalog(
			Gen.Frequency((1, Just<LegacyUrlMapping>(null!)), (3, Mapping(HostileProducts)), (1, Mapping(Just<Product>(null!))))
		)),
		(1, Just(new LegacyUrlMappingCatalog { Mappings = null! }))
	);

	public static readonly Gen<LegacyUrlMappingCatalogInProductCatalog> HostileLegacyInProducts = Gen.Select(
		HostileLegacyMappings,
		HostileProductCatalogs,
		(mappings, products) => new LegacyUrlMappingCatalogInProductCatalog(mappings, products)
	);

	/// <summary>Every mapping points at a product the catalog holds under its own id; the mapping text and versions stay hostile.</summary>
	public static readonly Gen<LegacyUrlMappingCatalogInProductCatalog> LawfulLegacyInProducts = HostileProducts.List[0, 5].SelectMany(list =>
	{
		var byId = list.GroupBy(p => p.Id).Select(g => g.Last()).ToArray();
		var catalog = new ProductCatalog { Products = ToFrozen(byId.Select(p => (p.Id, p))) };
		return (byId.Length == 0 ? Just(LegacyUrlMappingCatalog.None) : MappingCatalog(Mapping(Gen.OneOf(byId)))).Select(
			mappings => new LegacyUrlMappingCatalogInProductCatalog(mappings, catalog)
		);
	});

	public static readonly Gen<LegacyUrlMappingCatalogInProductCatalog> NullLegacyInProducts = Gen.OneOf(
		Gen.Select(NullLegacyMappings, HostileProductCatalogs, (m, p) => new LegacyUrlMappingCatalogInProductCatalog(m, p)),
		Gen.Select(HostileLegacyMappings, NullProductCatalogs, (m, p) => new LegacyUrlMappingCatalogInProductCatalog(m, p)),
		Just(new LegacyUrlMappingCatalogInProductCatalog(null!, null!))
	);
}

internal static class CatalogLaw
{
	/// <summary>The public instance methods of <typeparamref name="T"/> that return it: the operations <c>AutoTest</c> would run against every rule.</summary>
	public static IReadOnlyList<string> OperationsOf<T>() =>
		[
			.. typeof(T)
				.GetMethods(
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System
						.Reflection
						.BindingFlags
						.DeclaredOnly
				)
				.Where(m => m.ReturnType == typeof(T) && !m.IsSpecialName && m.Name != "<Clone>$")
				.Select(m => m.Name)
		];

	/// <summary>Runs the rule set over every generated value and writes its violations as diagnostics: neither may throw.</summary>
	public static void ReportsWithoutThrowing<T>(Nullean.Entities.ConstraintSet<T> rules, Gen<T> values, string file) =>
		values.Select(v => new Printable<T>(v)).Sample(
			printable =>
			{
				var report = rules.Report(printable.Value);
				_ = report.Violations.Count;
				_ = report.ToDiagnostics(file).ToList();
			},
			seed: RunSeed.Value,
			iter: 500
		);

	/// <summary>Names a value in a failure without letting its own <c>ToString</c> (which walks members a hostile value left null) hide the real exception.</summary>
	private sealed record Printable<T>(T Value)
	{
		public override string ToString()
		{
			try
			{
				return Value?.ToString() ?? "null";
			}
			catch (Exception e) when (e is NullReferenceException or InvalidOperationException)
			{
				return $"{typeof(T).Name} (ToString throws {e.GetType().Name})";
			}
		}
	}
}
