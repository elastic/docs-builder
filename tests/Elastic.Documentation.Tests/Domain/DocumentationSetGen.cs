// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions.TestingHelpers;
using CsCheck;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Generators of the stages of a documentation set in every state a <c>docset.yml</c> can put one in: blank and padded text, entries written with no
/// value (a null list item, a list or section with no value at all), keys that differ only in case, duplicates, unknown registries and schemes,
/// undefined enum values and absurd depths. <c>Hostile</c> generators also break the rules; <c>Lawful</c> generators are as hostile as the rules allow
/// and satisfy every one, which is what the invariant axis of <c>AutoTest</c> needs. Every null is one the loader can produce: nothing the
/// <c>DocumentationSetLoader</c> would map is outside these generators.
/// </summary>
internal static class DocumentationSetGen
{
	private static Gen<T> Just<T>(T value) => Gen.Const<T>(() => value);

	private static Gen<T?> OrNull<T>(Gen<T> gen, int nullWeight = 1) where T : class =>
		Gen.Frequency((nullWeight, Just<T?>(null)), (4, gen.Select(v => (T?)v)));

	private static Gen<IReadOnlyList<T>> ListOrNull<T>(Gen<T> entries, int max) =>
		Gen.Frequency((1, Just<IReadOnlyList<T>>(null!)), (6, entries.List[0, max].Select(l => (IReadOnlyList<T>)l)));

	/// <summary>Entries kept in front of the first one with no value, then that null and anything after it, which no stage reports.</summary>
	private static Gen<IReadOnlyList<T?>> ThenNoValue<T>(Gen<T> lawful, Gen<T?> anything) where T : class =>
		Gen.Select(
			lawful.List[0, 3],
			Gen.Bool,
			anything.List[0, 3],
			(good, stop, rest) => (IReadOnlyList<T?>)(stop ? [.. good.Select(g => (T?)g), null, .. rest] : good.Select(g => (T?)g).ToList())
		);

	private static Gen<IReadOnlyList<T?>> ListOrNullWith<T>(Gen<IReadOnlyList<T?>> list) where T : class =>
		Gen.Frequency((1, Just<IReadOnlyList<T?>>(null!)), (7, list));

	// ---- the files a documentation set resolves against

	// MockFileSystem on Windows needs a drive on every absolute path
	private static readonly string Root = OperatingSystem.IsWindows() ? "C:" : "";

	public static readonly MockFileSystem FileSystem = new(
		new Dictionary<string, MockFileData>
		{
			[Root + "/docs/docset.yml"] = new("project: p"),
			[Root + "/docs/logo.svg"] = new("<svg/>"),
			[Root + "/docs/icon.ico"] = new("ico"),
			[Root + "/docs/img/a.png"] = new("png"),
			[Root + "/docs/spec.json"] = new("{}"),
			[Root + "/docs/sub/spec.json"] = new("{}"),
			[Root + "/docs/api/es/a.md"] = new("# a"),
			[Root + "/docs/api/es/op-1.md"] = new("# op"),
			[Root + "/docs/api/es/sub/b.md"] = new("# b"),
			[Root + "/docs/api/es/sub/op-2.md"] = new("# op"),
			[Root + "/outside.svg"] = new("<svg/>")
		},
		new MockFileSystemOptions { CurrentDirectory = Root + "/docs" }
	);

	public static DocumentationSetDefinition Base =>
		new()
		{
			SourceFile = FileSystem.FileInfo.New(Root + "/docs/docset.yml"),
			SourceDirectory = FileSystem.DirectoryInfo.New(Root + "/docs")
		};

	// ---- the catalog

	private static Product Widget(string id, ReleaseNotesPath releaseNotes) =>
		new() { Id = id, DisplayName = id, Features = new ProductFeatures { PublicReference = false, ReleaseNotes = releaseNotes } };

	private static ProductCatalog CatalogOf(params Product[] products) => new() { Products = products.ToFrozenDictionary(p => p.Id) };

	/// <summary>The products the lawful generators name: two that take part in the release notes under their hyphenated ids, one that does not.</summary>
	public static readonly ProductCatalog Standard = CatalogOf(
		Widget("widgets", ReleaseNotesPath.OnRelease),
		Widget("wid-gets", ReleaseNotesPath.OnRelease),
		Widget("nonotes", ReleaseNotesPath.None)
	);

	public static readonly Gen<ProductCatalog> HostileCatalogs = Gen.OneOf(
		Just(Standard),
		Just(CatalogOf()),
		Just(CatalogOf(Widget("Widgets", ReleaseNotesPath.OnRelease), Widget("widgets", ReleaseNotesPath.None))),
		Just(CatalogOf(Widget("widgets", (ReleaseNotesPath)42)))
	);

	// ---- what a docset.yml says that no stage reads

	private static readonly Gen<string?> Words = OrNull(Gen.OneOf("p", "", " ", "  p  ", "ünï", "a/b", "\0"));

	private static Gen<IReadOnlyDictionary<string, string>> Substitutions =>
		Gen
			.Select(Gen.OneOf("Foo", "foo", "FOO", "", " "), Gen.OneOf("bar", "", " "))
			.List[0, 3]
			.Select(pairs =>
			{
				var dictionary = new Dictionary<string, string>();
				foreach (var (key, value) in pairs)
					dictionary[key] = value;
				return (IReadOnlyDictionary<string, string>)dictionary;
			});

	/// <summary>The members no rule of a stage reads: an overlong depth, flags, undefined hint types, exclusions, products and extensions.</summary>
	private static readonly Gen<Func<DocumentationSetDefinition, DocumentationSetDefinition>> Noise = Gen.Select(
		Words,
		Gen.OneOf(-1, 0, 1, 2, 3, 99, int.MaxValue),
		Gen.Bool,
		Gen.OneOf(
			Just<IReadOnlySet<HintType>>(new HashSet<HintType>()),
			Just<IReadOnlySet<HintType>>(new HashSet<HintType> { (HintType)999, (HintType)(-3) })
		),
		Substitutions,
		Gen.OneOf("*.md", "!index.md", "", " ", "DRAFT.md").List[0, 3],
		Gen.OneOf("widgets", "", " ", "Widgets").List[0, 3],
		(project, depth, flag, suppress, subs, exclude, products) =>
			(Func<DocumentationSetDefinition, DocumentationSetDefinition>)(d =>
				d with
				{
					Project = project,
					MaxTocDepth = depth,
					DevDocs = flag,
					Island = !flag,
					SuppressDiagnostics = suppress,
					Substitutions = subs,
					Exclude = exclude,
					Products = products,
					CodexGroup = project
				})
	);

	// ---- cross_links and release_notes

	private static readonly string[] PublicCrossLinks =
	[
		"kibana",
		"KIBANA",
		"  kibana  ",
		"public://kibana",
		"PUBLIC://kibana",
		"Public://Kibana",
		"",
		" ",
		"\t",
		"a/b"
	];

	private static readonly string[] HostileCrossLinks =
	[
		.. PublicCrossLinks,
		"internal://kibana",
		"INTERNAL://x",
		"foo://x",
		"://x",
		"public://",
		"internal://   ",
		"a://b://c",
		"http://x",
		"public:/kibana",
		"a\0"
	];

	public static readonly Gen<string?> Registries = Gen.OneOf(
		null,
		"",
		" ",
		"public",
		"PUBLIC",
		"internal",
		"Internal",
		" internal ",
		"zork",
		"codex"
	);

	private static Gen<string> CrossLink(IEnumerable<string> pool) => Gen.OneOf(Gen.OneOf(pool.ToArray()), Just<string>(null!));

	private static readonly Gen<ReleaseNotesDeclaration?> HostileReleaseNote = OrNull(
		Gen.OneOf(
			"widgets",
			"  widgets  ",
			"wid_gets",
			"wid-gets",
			"Widgets",
			"WIDGETS",
			"nonotes",
			"nope",
			"",
			"  ",
			"a b",
			"a/b",
			"wid.gets",
			"ünï",
			"\0"
		).Select(p => new ReleaseNotesDeclaration { Product = p })
	);

	private static readonly Gen<ReleaseNotesDeclaration> NoProductReleaseNote = Just(new ReleaseNotesDeclaration { Product = null });

	private static readonly Gen<ReleaseNotesDeclaration> LawfulReleaseNote = Gen.OneOf(
		"widgets",
		"  widgets  ",
		"wid_gets",
		"wid-gets"
	).Select(p => new ReleaseNotesDeclaration { Product = p });

	public static readonly Gen<DocumentationSetInCatalog> HostileInCatalog = Gen.Select(
		Noise,
		Registries,
		ListOrNull(CrossLink(HostileCrossLinks), 6),
		ListOrNullWith(
			Gen
				.OneOf(HostileReleaseNote, NoProductReleaseNote.Select(n => (ReleaseNotesDeclaration?)n))
				.List[0, 5]
				.Select(l => (IReadOnlyList<ReleaseNotesDeclaration?>)l)
		),
		HostileCatalogs,
		(noise, registry, crossLinks, releaseNotes, catalog) => new DocumentationSetInCatalog(
			noise(Base) with { Registry = registry, CrossLinks = crossLinks, ReleaseNotes = releaseNotes },
			catalog
		)
	);

	/// <summary>
	/// Cross-links that name a repository (a codex docset may link to internal ones, a public one only to public ones), release notes that name a
	/// product of the catalog, blank entries, and after the first release note with no value anything at all.
	/// </summary>
	public static readonly Gen<DocumentationSetInCatalog> LawfulInCatalog = Registries.SelectMany(registry =>
	{
		var isInternal = DocumentationSetRules.ParseRegistry(registry) != DocSetRegistry.Public;
		var pool = isInternal ? [.. PublicCrossLinks, "internal://kibana", "INTERNAL://x"] : PublicCrossLinks;
		return Gen.Select(
			Noise,
			ListOrNull(CrossLink(pool), 6),
			ListOrNullWith(ThenNoValue(LawfulReleaseNote, HostileReleaseNote)),
			(noise, crossLinks, releaseNotes) => new DocumentationSetInCatalog(
				noise(Base) with { Registry = registry, CrossLinks = crossLinks, ReleaseNotes = releaseNotes },
				Standard
			)
		);
	});

	// ---- api and storybook

	private static Gen<ApiDeclarationEntry> ApiEntry(
		Gen<string?> spec,
		Gen<string?> product,
		Gen<string?> repository,
		Gen<IReadOnlyList<string?>> children,
		Gen<IReadOnlyList<string>> categories
	) =>
		Gen.Select(
			spec,
			product,
			repository,
			children,
			categories,
			Gen.Bool,
			(s, p, r, c, k, located) => new ApiDeclarationEntry
			{
				Spec = s,
				Product = p,
				Repository = r,
				Children = c,
				CatalogCategories = k,
				Location = located ? new SourceLocation(3, 5) : null,
				ProductLocation = located ? new SourceLocation(4, 7) : null,
				SpecLocation = new SourceLocation(5, 7),
				RepositoryLocation = null,
				CatalogLocation = located ? new SourceLocation(6, 7) : null
			}
		);

	private static Gen<IReadOnlyList<string?>> Children(params string?[] pool) =>
		pool.Length == 0 ? Just<IReadOnlyList<string?>>([]) : Gen.OneOf(pool).List[0, 4].Select(l => (IReadOnlyList<string?>)l);

	private static readonly Gen<ApiDeclaration> HostileApi = Gen.Select(
		Gen.OneOf("es", "es", "ES", "Es", "other", "", " "),
		ApiEntry(
			OrNull(
				Gen.OneOf(
					"spec.json",
					"sub/spec.json",
					"",
					" ",
					"dir/",
					"../spec.json",
					"/abs/spec.json",
					"SPEC.JSON",
					"a\0.json",
					"missing.json",
					"sub/",
					"."
				)
			),
			OrNull(Gen.OneOf("widgets", "wid_gets", "Widgets", "nope", "", "  ", "wid-gets", " widgets ")),
			OrNull(Gen.OneOf("elastic/x", "x", "/x", "x/", "a/b/c", " elastic/x ", "", "  ")),
			Children(null, "", "  ", "a.md", "sub/b.md", "op-1.md", "sub/op-2.md", "../x.md", "missing.md", "a\0.md", "A.MD"),
			Gen
				.OneOf("self", "ech", "ess", "ece", "serverless", "SERVERLESS", "  ece  ", "nope", "", "  ", "self_managed", "stack")
				.List[0, 4]
				.Select(l => (IReadOnlyList<string>)l)
		).List[0, 3],
		(key, entries) => new ApiDeclaration { ProductKey = key, Entries = entries }
	);

	private static readonly Gen<ApiDeclaration> LawfulApi = Gen.OneOf(
		Gen.Select(
			Gen.Const("es"),
			ApiEntry(
				Gen.OneOf<string?>("spec.json", "sub/spec.json", "missing.json"),
				Gen.OneOf("widgets", "wid_gets", " widgets ").Select(p => (string?)p),
				OrNull(Gen.OneOf("elastic/x", " elastic/x ", "a/b/c", "", "  ")),
				Children("a.md", "sub/b.md", "sub/op-2.md"),
				Gen
					.OneOf("self", "ech", "ess", "ece", "serverless", "SERVERLESS", "  ece  ")
					.List[0, 4]
					.Select(l => (IReadOnlyList<string>)l)
			).List[1, 1],
			(key, entries) => new ApiDeclaration { ProductKey = key, Entries = entries }
		),
		Gen.Select(
			Gen.OneOf("other", "ES", "Es"),
			ApiEntry(
				Just<string?>("spec.json"),
				Just<string?>("widgets"),
				Just<string?>(null),
				Children(),
				Just<IReadOnlyList<string>>([])
			).List[1, 1],
			(key, entries) => new ApiDeclaration { ProductKey = key, Entries = entries }
		)
	);

	private static IReadOnlyList<ApiResolution> Resolve(IReadOnlyList<ApiDeclaration>? api, ProductCatalog catalog) =>
		DocumentationSetRules.ResolveApis(api, catalog, FileSystem, Base.SourceDirectory);

	private static readonly Gen<string?> HostileStorybook = OrNull(
		Gen.OneOf(
			"",
			"  ",
			"${A}",
			"${KIBANA_STORYBOOK_REGISTRY}",
			"https://x/${A}/${B}",
			"${A",
			"$A",
			"${a}",
			"${}",
			"${KIBANA_STORYBOOK_REGISTRY}${B}",
			"${A}\0"
		)
	);

	private static readonly Gen<string?> LawfulStorybook = OrNull(
		Gen.OneOf("", "  ", "https://x", "${KIBANA_STORYBOOK_REGISTRY}", " ${KIBANA_STORYBOOK_REGISTRY} ")
	);

	public static readonly Gen<DocumentationSetApis> HostileApis = Gen.Select(
		Noise,
		Gen.Frequency(
			(1, Just<IReadOnlyList<ApiDeclaration>>(null!)),
			(6, HostileApi.List[0, 3].Select(l => (IReadOnlyList<ApiDeclaration>)l))
		),
		Gen.Bool,
		HostileStorybook,
		HostileCatalogs,
		(noise, api, hasStorybook, registry, catalog) => new DocumentationSetApis(
			noise(Base) with { Api = api, HasStorybook = hasStorybook, StorybookRegistry = registry },
			Resolve(api, catalog)
		)
	);

	/// <summary>Api entries that name a product of the catalog, a spec and files that are where they say, and a storybook that interpolates only what is allowed.</summary>
	public static readonly Gen<DocumentationSetApis> LawfulApis = Gen.Select(
		Noise,
		Gen.Frequency(
			(1, Just<IReadOnlyList<ApiDeclaration>>(null!)),
			(6, LawfulApi.List[0, 3].Select(l => (IReadOnlyList<ApiDeclaration>)l))
		),
		Gen.Bool,
		LawfulStorybook,
		(noise, api, hasStorybook, registry) => new DocumentationSetApis(
			noise(Base) with { Api = api, HasStorybook = hasStorybook, StorybookRegistry = registry },
			Resolve(api, Standard)
		)
	);

	// ---- branding and cta

	private static Gen<string?> BrandingImage(params string?[] pool) => Gen.OneOf(pool);

	private static Gen<BrandingDeclaration?> Branding(Gen<string?> image) =>
		OrNull(
			Gen.Select(
				image,
				image,
				image,
				image,
				Words,
				(icon, og, favicon, apple, copyright) => new BrandingDeclaration
				{
					Icon = icon,
					OgImage = og,
					Favicon = favicon,
					AppleTouchIcon = apple,
					HeaderBg = copyright,
					Copyright = copyright
				}
			),
			nullWeight: 2
		);

	private static readonly Gen<BrandingDeclaration?> HostileBranding = Branding(
		BrandingImage(
			null,
			"",
			"  ",
			"logo.svg",
			"LOGO.SVG",
			"img/a.png",
			"x.exe",
			"noext",
			"missing.svg",
			"../outside.svg",
			"/abs.svg",
			"img/../logo.svg",
			"icon.ICO",
			"a\0.svg",
			"\\x.svg"
		)
	);

	private static readonly Gen<BrandingDeclaration?> LawfulBranding = Branding(
		BrandingImage(null, "", "logo.svg", "img/a.png", "icon.ico", "img/../logo.svg")
	);

	private static Gen<IReadOnlyList<string>> Benefits(int max) =>
		Gen.OneOf("one", "", " ", "ünï").List[0, max].Select(l => (IReadOnlyList<string>)l);

	private static Gen<CtaDeclaration> Cta(Gen<string> name, Gen<string?> label, Gen<string?> url, Gen<IReadOnlyList<string>> benefits) =>
		Gen.Select(name, label, url, benefits, (n, l, u, b) => new CtaDeclaration { Name = n, Label = l, Url = u, Benefits = b });

	private static readonly Gen<string> CtaNames = Gen.OneOf("trial", "Trial", "TRIAL", "a", "A", "mp", "", "  ");

	private static readonly Gen<CtaDeclaration?> HostileCta = OrNull(
		Cta(
			CtaNames,
			OrNull(Gen.OneOf("L", " L ", "", "  ")),
			OrNull(
				Gen.OneOf(
					"u",
					"http://x",
					"HTTPS://x",
					"/rel",
					"ftp://x",
					"javascript:x",
					"mailto:a@b.test",
					"relative/path",
					"http://[",
					"http:///",
					"u\0",
					"",
					"  "
				)
			),
			Gen.Frequency((1, Just<IReadOnlyList<string>>(null!)), (6, Benefits(5)))
		)
	);

	private static readonly Gen<CtaDeclaration> LawfulCta = Cta(
		CtaNames,
		Gen.OneOf<string?>("L", " L "),
		Gen.OneOf<string?>("u", "http://x", "HTTPS://x", "/rel", "relative/path"),
		Benefits(Elastic.Documentation.DocumentationSets.Cta.MaxBenefits)
	);

	/// <summary>A template that is valid under a name nothing else in the generated data uses, so <c>default_cta</c> can point at it.</summary>
	private static readonly CtaDeclaration KnownCta = new() { Name = "mp", Label = "Go", Url = "http://example.test", Benefits = [] };

	public static readonly Gen<DocumentationSetPresentation> HostilePresentation = Gen.Select(
		Noise,
		HostileBranding,
		ListOrNullWith(HostileCta.List[0, 5].Select(l => (IReadOnlyList<CtaDeclaration?>)l)),
		(noise, branding, ctas) => new DocumentationSetPresentation(
			noise(Base) with { Branding = branding, Ctas = ctas },
			FileSystem,
			Base.SourceDirectory
		)
	);

	/// <summary>Branding images that are where they say (or unset) and templates that are valid, then a template with no value and anything after it.</summary>
	public static readonly Gen<DocumentationSetPresentation> LawfulPresentation = Gen.Select(
		Noise,
		LawfulBranding,
		ListOrNullWith(ThenNoValue(LawfulCta, HostileCta)),
		(noise, branding, ctas) => new DocumentationSetPresentation(
			noise(Base) with { Branding = branding, Ctas = ctas },
			FileSystem,
			Base.SourceDirectory
		)
	);

	// ---- default_cta and features

	private static readonly Gen<DeclaredFeatures?> HostileFeatures = Gen.Frequency(
		(1, Just<DeclaredFeatures?>(null)),
		(5, Gen.Select(
			Gen.OneOf<bool?>(null, true, false),
			Gen.OneOf<bool?>(null, true, false),
			Gen.OneOf<bool?>(null, true, false),
			(nav, edit, guide) =>
				(DeclaredFeatures?)new DeclaredFeatures { PrimaryNav = nav, DisableGithubEditLink = edit, GuideNav = guide }
		))
	);

	private static Gen<IReadOnlyDictionary<string, string>> TocDefaultCtas(Gen<string> names) =>
		Gen
			.Select(Gen.OneOf("index.md", "a.md", "sub/b.md", "INDEX.md", ""), names)
			.List[0, 4]
			.Select(pairs =>
			{
				var dictionary = new Dictionary<string, string>();
				foreach (var (page, name) in pairs)
					dictionary[page] = name;
				return (IReadOnlyDictionary<string, string>)dictionary;
			});

	public static readonly Gen<DocumentationSetFeatures> HostileFeaturesStage = Gen.Select(
		Noise,
		HostileBranding,
		ListOrNullWith(HostileCta.List[0, 5].Select(l => (IReadOnlyList<CtaDeclaration?>)l)),
		HostileFeatures,
		TocDefaultCtas(Gen.OneOf("trial", "TRIAL", "a", "A", "mp", "nope", "", "  ", "q\0")),
		(noise, branding, ctas, features, defaults) => new DocumentationSetFeatures(noise(Base) with
		{
			Branding = branding,
			Ctas = ctas,
			Features = features!,
			TocDefaultCtas = defaults
		})
	);

	/// <summary>
	/// Every default cta is the built-in one or the template valid under its own name (in any case) that is appended after whatever else the list holds,
	/// and primary-nav is never set where branding is.
	/// </summary>
	public static readonly Gen<DocumentationSetFeatures> LawfulFeaturesStage = Gen.Select(Noise, HostileBranding, ListOrNullWith(
		HostileCta.List[0, 5].Select(l => (IReadOnlyList<CtaDeclaration?>)l)
	), HostileFeatures, TocDefaultCtas(Gen.OneOf("trial", "Trial", "TRIAL", "mp", "MP")), (noise, branding, ctas, features, defaults) =>
	{
		var hasBranding = branding is not null;
		var safe = features is { PrimaryNav: true } && hasBranding ? features with { PrimaryNav = false } : features;
		// null entries and invalid templates are tolerated; the known one is appended so that "mp" is always usable
		var all = (ctas ?? []).Append(KnownCta).ToList();
		return new DocumentationSetFeatures(noise(Base) with
		{
			Branding = branding,
			Ctas = all,
			Features = safe!,
			TocDefaultCtas = defaults
		});
	});
}
