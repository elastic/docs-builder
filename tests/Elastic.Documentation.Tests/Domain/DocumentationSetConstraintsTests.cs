// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using CsCheck;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Nullean.Entities.CsCheck;
using Nullean.Entities.CsCheck.FileSystem;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The documentation set constraints are checked on definitions built directly, with no loader involved: the rules
/// hold for any model. Every constraint needs a counter-example that breaks it, and only it, so none is dead or
/// duplicated; a valid definition breaks none.
/// </summary>
public class DocumentationSetConstraintsTests
{
	private static ProductCatalog Products =>
		ProductCatalog.FromPublicProducts(new Dictionary<string, Product>
		{
			["elasticsearch"] = new() { Id = "elasticsearch", DisplayName = "Elasticsearch" }
		});

	private static DeclaringFilePath Declared => DeclaringFilePath.From("/docs/docset.yml");

	private static DocumentationSetDefinition ValidDocset()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["/docs/docset.yml"] = new("project: test"),
			["/docs/index.md"] = new("# Test"),
			["/docs/setup.md"] = new("# Setup")
		});
		return new DocumentationSetDefinition
		{
			SourceFile = fs.FileInfo.New("/docs/docset.yml"),
			SourceDirectory = fs.DirectoryInfo.New("/docs"),
			FileSystem = fs,
			Project = "test",
			CrossLinks = ["elasticsearch"],
			ReleaseNotesProducts = ["elasticsearch"],
			Substitutions = [new("stack", "Elastic Stack")],
			Ctas = [new CtaDeclaration("serverless", "Try it", "https://example.test", ["Fast"])],
			TocDefaultCtas = [new("index.md", "serverless")],
			TableOfContents = [Page("index.md")]
		};
	}

	private static TocItem Page(string path, string? defaultCta = null, params TocItem[] children) =>
		new FileRef(DocsetPath.From(path), ContainerPath.From(path), false, children, Declared, defaultCta);

	private static FrozenDictionary<string, Func<DocumentationSetDefinition, DocumentationSetDefinition>> DocsetCounterExamples =>
		new Dictionary<string, Func<DocumentationSetDefinition, DocumentationSetDefinition>>
		{
			["docset-file-exists"] =
				d =>
					d with
					{
						SourceFile = d.FileSystem.FileInfo.New("/docs/missing/docset.yml"),
						CrossLinks = [],
						ReleaseNotesProducts = [],
						Substitutions = [],
						Ctas = [],
						TocDefaultCtas = []
					},
			["nested-toc-has-children"] = d =>
			{
				((MockFileSystem)d.FileSystem).AddFile("/docs/guides/toc.yml", new MockFileData("toc: []"));
				return d with
				{
					TableOfContents =
					[
						new IsolatedTableOfContentsRef(DocsetPath.From("guides"), ContainerPath.From("guides"), [], Declared)
					]
				};
			},
			["cli-supplemental-folder-exists"] =
				d =>
					d with
					{
						TableOfContents =
						[
							new CliReferenceRef(
								"cli.json",
								"missing-folder",
								null,
								null,
								DocsetPath.From("cli"),
								ContainerPath.From("cli"),
								Declared,
								[]
							)
						]
					},
			["listing-glob-has-extension"] =
				d =>
					d with
					{
						TableOfContents =
						[
							new ListingRef(
								DocsetPath.From("posts"),
								ContainerPath.From("posts"),
								[],
								Declared,
								new ListingOptions(glob: "**/*.yaml")
							)
						]
					},
			["cross-link-well-formed"] = d => d with { CrossLinks = ["internal://secret"] },
			["release-notes-product-known"] = d => d with { ReleaseNotesProducts = ["kibana"] },
			["substitution-keys-unique"] = d => d with { Substitutions = [new("stack", "a"), new("STACK", "b")] },
			["api-entry-valid"] = d => d with { Api = [new ApiDeclaration("elasticsearch", [])] },
			["storybook-variables-allowed"] = d => d with { HasStorybook = true, StorybookRegistry = "${NOT_ALLOWED}/registry" },
			["branding-images-valid"] = d => d with { Branding = new BrandingDeclaration(icon: "logo.bmp") },
			["cta-template-valid"] = d => d with { Ctas = [new CtaDeclaration("broken", null, null, [])], TocDefaultCtas = [] },
			["default-cta-known"] = d => d with { TocDefaultCtas = [new("index.md", "missing")] },
			["primary-nav-without-branding"] =
				d => d with { Branding = new BrandingDeclaration(), Features = new DeclaredFeatures(primaryNav: true) },
			["file-stays-in-folder"] =
				d =>
					d with
					{
						TableOfContents =
						[
							new FileRef(
								DocsetPath.From("guides/../setup.md"),
								ContainerPath.From("guides/../setup.md"),
								false,
								[],
								Declared
							)
						]
					},
			["registry-known"] = d => d with { Registry = "public" },
			["docset-file-not-symlink"] = d =>
			{
				d.FileSystem.File.CreateSymbolicLink("/docs/linked.yml", "/docs/docset.yml");
				return d with { SourceFile = d.FileSystem.FileInfo.New("/docs/linked.yml") };
			},
			["toc-not-empty"] = d => d with { TableOfContents = [] },
			["toc-nesting-within-max-depth"] =
				d =>
					d with
					{
						MaxTocDepth = 1,
						TableOfContents =
						[
							new IsolatedTableOfContentsRef(
								DocsetPath.From("guides"),
								ContainerPath.From("guides"),
								[
									new IsolatedTableOfContentsRef(
										DocsetPath.From("guides/inner"),
										ContainerPath.From("inner"),
										[Page("index.md")],
										Declared
									)
								],
								Declared
							)
						]
					},
			["dev-docs-has-no-effect"] = d => d with { DevDocs = true },
			["cross-links-unique"] = d => d with { CrossLinks = ["elasticsearch", "elasticsearch"] },
			["cross-links-not-blank"] = d => d with { CrossLinks = ["elasticsearch", " "] },
			["exclude-overrides-literal"] = d => d with { Exclude = ["!docs/*.md"] },
			["exclude-not-blank"] = d => d with { Exclude = [""] },
			["extension-known"] = d => d with { Extensions = ["unknown-extension"] },
			["substitution-key-not-generated"] = d => d with { Substitutions = [new("version.stack", "9.0")] },
			["products-known"] = d => d with { Products = ["kibana"] },
			["release-notes-products-unique"] = d => d with { ReleaseNotesProducts = ["elasticsearch", "elasticsearch"] },
#pragma warning disable CS0618 // The rule is about the obsolete fields
			["docset-obsolete-fields-unused"] = d => d with { DisplayName = "Test", Description = "A test set" },
#pragma warning restore CS0618
			["branding-header-bg-valid"] = d => d with { Branding = new BrandingDeclaration(headerBg: "not a colour!") },
			["cta-names-unique"] =
				d =>
					d with
					{
						Ctas =
						[
							new CtaDeclaration("serverless", "Try it", "https://example.test", ["Fast"]),
							new CtaDeclaration("SERVERLESS", "Try it", "https://example.test", ["Fast"])
						]
					},
			["cta-replaces-default"] =
				d => d with { Ctas = [.. d.Ctas, new CtaDeclaration("trial", "Try it", "https://example.test", ["Fast"])] },
			["default-cta-one-per-page"] = d => d with { TableOfContents = [Page("index.md", "serverless"), Page("./index.md", "other")] },
			["default-cta-page-exists"] = d => d with { TocDefaultCtas = [new("missing.md", "serverless")] },
			["toc-file-exists-and-markdown"] = d => d with { TableOfContents = [Page("missing.md")] },
			["toc-file-not-empty"] = d =>
			{
				((MockFileSystem)d.FileSystem).AddFile("/docs/blank.md", new MockFileData(""));
				return d with { TableOfContents = [Page("blank.md")] };
			},
			["toc-deep-linked-file-has-no-children"] = d =>
			{
				((MockFileSystem)d.FileSystem).AddFile("/docs/guides/intro.md", new MockFileData("# Intro"));
				((MockFileSystem)d.FileSystem).AddFile("/docs/guides/other.md", new MockFileData("# Other"));
				return d with { TableOfContents = [Page("guides/intro.md", null, Page("guides/other.md"))] };
			},
			["toc-file-children-creatable"] = d =>
			{
				Mkdir(d, "empty");
				return d with
				{
					TableOfContents =
					[
						Page("index.md", null, new FolderRef(DocsetPath.From("empty"), ContainerPath.From("empty"), [], Declared))
					]
				};
			},
			["api-key-unique-ignoring-case"] = d => d with { Api = [Api("es"), Api("ES")] },
			["api-key-url-segment"] = d => d with { Api = [Api("not a segment")] },
			["api-child-slugs-unique"] = d => d with { Api = [Api("es", Child(d, "es", "a_b.md"), Child(d, "es", "a-b.md"))] },
			["api-child-slug-not-reserved"] = d => d with { Api = [Api("es", Child(d, "es", "types.md"))] },
			["folder-index-file-name-matches-folder"] = d =>
			{
				Mkdir(d, "guides/intro");
				Touch(d, "guides/intro/other.md");
				return d with
				{
					TableOfContents =
					[
						new FolderRef(
							DocsetPath.From("guides/intro"),
							ContainerPath.From("guides/intro"),
							[
								new FolderIndexFileRef(
									DocsetPath.From("guides/intro/other.md"),
									ContainerPath.From("other.md"),
									false,
									[],
									Declared
								)
							],
							Declared
						)
					]
				};
			},
			["toc-folder-exists"] =
				d => d with { TableOfContents = [new FolderRef(DocsetPath.From("missing"), ContainerPath.From("missing"), [], Declared)] },
			["nested-toc-paths-unique"] = d =>
			{
				Touch(d, "guides/a.md");
				Touch(d, "guides/b.md");
				return d with
				{
					TableOfContents =
					[
						new IsolatedTableOfContentsRef(
							DocsetPath.From("guides"),
							ContainerPath.From("guides"),
							[Page("guides/a.md")],
							Declared
						),
						new IsolatedTableOfContentsRef(
							DocsetPath.From("guides"),
							ContainerPath.From("guides"),
							[Page("guides/b.md")],
							Declared
						)
					]
				};
			},
			["toc-node-has-index-page"] = d =>
			{
				Mkdir(d, "guides");
				return d with
				{
					TableOfContents =
					[
						new FolderRef(
							DocsetPath.From("guides"),
							ContainerPath.From("guides"),
							[new CrossLinkRef(new Uri("docs-content://index.md"), null, false, [], Declared)],
							Declared
						)
					]
				};
			},
			["toc-file-listed-once"] = d => d with { TableOfContents = [Page("index.md"), Page("setup.md"), Page("setup.md")] },
			["page-file-name-chars"] = d =>
			{
				Touch(d, "Upper Case.md");
				return d with { TableOfContents = [Page("Upper Case.md")] };
			},
			["listing-island-needs-visual"] =
				d =>
					d with
					{
						TableOfContents =
						[
							new ListingRef(
								DocsetPath.From("posts"),
								ContainerPath.From("posts"),
								[],
								Declared,
								new ListingOptions(island: true, visual: ListingVisual.None)
							)
						]
					},
			["detection-rule-folders-exist"] = d =>
			{
				Touch(d, "rules/index.md");
				return d with
				{
					TableOfContents =
					[
						new DetectionRuleOverviewRef(
							DocsetPath.From("rules/index.md"),
							ContainerPath.From("rules/index.md"),
							["missing-rules"],
							[],
							Declared
						)
					]
				};
			},
			["redirect-target-valid"] = d => d with { Redirects = Redirects(("old.md", new LinkRedirect { To = "missing.md" })) },
			["redirect-many-targets-valid"] =
				d =>
					d with
					{
						Redirects = Redirects(
							("old.md", new LinkRedirect { To = "index.md", Many = [new LinkSingleRedirect { To = "missing.md" }] })
						)
					},
			["redirect-not-to-itself"] = d => d with { Redirects = Redirects(("index.md", new LinkRedirect { To = "index.md" })) },
			["redirect-many-single-target"] =
				d =>
					d with
					{
						Redirects = Redirects(
							("old.md", new LinkRedirect
							{
								Many = [new LinkSingleRedirect { To = "index.md" }, new LinkSingleRedirect { To = "setup.md" }]
							})
						)
					},
			["redirects-file-named-for-docset"] = d => d with { RedirectsFile = d.FileSystem.FileInfo.New("/docs/_redirects.yml") },
			["suppress-hint-known"] = d => d with { UnknownSuppressions = ["NotAHint"] },
			["api-entry-keys-known"] =
				d =>
					d with
					{
						Api =
						[
							new ApiDeclaration(
								"elasticsearch",
								[new ApiDeclarationEntry { Product = "elasticsearch", Spec = "spec.yaml", UnknownKeys = ["spce"] }]
							)
						]
					},
			["redirect-not-dropped"] =
				d => d with { DroppedRedirects = [new DroppedRedirect("old.md"), new DroppedRedirect("older.md", 1)] },
			["toc-entry-selects-a-shape"] = d => d with { TocAuthoring = [new TocEntryAuthoring(TocEntryKind.Dropped, "{flie}")] },
			["toc-entry-keys-known"] =
				d => d with { TocAuthoring = [new TocEntryAuthoring(TocEntryKind.File, "file: index.md") { UnknownKeys = ["flie"] }] },
			["toc-entry-single-shape"] =
				d => d with { TocAuthoring = [new TocEntryAuthoring(TocEntryKind.File, "file: index.md") { IgnoredKeys = ["toc"] }] },
			["toc-file-hidden-title-ignored"] =
				d =>
					d with
					{
						TocAuthoring = [new TocEntryAuthoring(TocEntryKind.File, "file: index.md") { IgnoredKeys = ["hidden", "title"] }]
					},
			["toc-detection-rules-needs-file"] =
				d =>
					d with
					{
						TocAuthoring = [new TocEntryAuthoring(TocEntryKind.Folder, "folder: rules") { IgnoredKeys = ["detection_rules"] }]
					},
			["toc-folder-sort-exclude-need-no-children"] =
				d =>
					d with
					{
						TocAuthoring =
						[
							new TocEntryAuthoring(TocEntryKind.Folder, "folder: guides")
							{
								WrittenKeys = ["folder", "children", "sort"],
								ExplicitChildren = true
							}
						]
					},
			["toc-island-is-boolean"] =
				d => d with { TocAuthoring = [new TocEntryAuthoring(TocEntryKind.NestedToc, "toc: guides") { IslandRaw = "yes" }] },
			["toc-crosslink-hidden-is-boolean"] =
				d =>
					d with
					{
						TocAuthoring =
						[
							new TocEntryAuthoring(TocEntryKind.CrossLink, "crosslink: docs-content://a.md") { HiddenRaw = "maybe" }
						]
					},
			["listing-visual-known"] =
				d => d with { TocAuthoring = [new TocEntryAuthoring(TocEntryKind.Listing, "listing: posts") { VisualRaw = "everything" }] },
			["listing-sort-known"] =
				d => d with { TocAuthoring = [new TocEntryAuthoring(TocEntryKind.Listing, "listing: posts") { SortRaw = "newest" }] }
		}.ToFrozenDictionary();

	private static void Touch(DocumentationSetDefinition d, string path) =>
		((MockFileSystem)d.FileSystem).AddFile($"/docs/{path}", new MockFileData("# Page"));

	private static void Mkdir(DocumentationSetDefinition d, string path) => ((MockFileSystem)d.FileSystem).AddDirectory($"/docs/{path}");

	/// <summary>An api entry that satisfies <c>api-entry-valid</c>; <paramref name="children"/> are the child pages, which must exist.</summary>
	private static ApiDeclaration Api(string key, params string[] children) =>
		new(key, [new ApiDeclarationEntry { Product = "elasticsearch", Spec = "spec.yaml", Children = children }]);

	/// <summary>Creates the child page under <c>api/&lt;key&gt;/</c> and returns its name.</summary>
	private static string Child(DocumentationSetDefinition d, string key, string name)
	{
		Touch(d, $"api/{key}/{name}");
		return name;
	}

	private static IReadOnlyDictionary<string, LinkRedirect> Redirects(params (string From, LinkRedirect Redirect)[] redirects) =>
		redirects.ToDictionary(r => r.From, r => r.Redirect);

	[Test]
	// Stays hand-written (5a: not generated). A valid docset is a table of contents and a file system that agree: about 25 of its 70 rules say a toc entry's file, folder, index page,
	// api child or default-cta page exists on disk, so a generated tree is valid only when the generator builds the world from the tree (or the tree from the world) and the entries are
	// unique, well-nested and well-named. GenAuto draws the file system and the tree independently; that coupling is not a repair of members but a model-first generator (a set of page
	// paths, then both the files and the toc built from it).
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest
			.For(DocumentationSetConstraints.All, Gen.Const(0).Select(_ => Valid()))
			.Options(o => o.UseFileSystem())
			.CounterExamples(CounterExamples)
			.Axes();

	private static DocumentationSetInCatalog Valid() => new(ValidDocset(), Products);

	[Test]
	public void ObsoleteFieldsAreReportedByName()
	{
#pragma warning disable CS0618 // The rule is about the obsolete fields
		var docset = ValidDocset() with { DisplayName = "Test", Description = "A test set" };
#pragma warning restore CS0618
		var violations = DocumentationSetConstraints
			.All
			.Report(new DocumentationSetInCatalog(docset, Products))
			.Of(DocumentationSetConstraints.DocsetObsoleteFieldsUnused);

		violations.Select(v => v.Member).Should().BeEquivalentTo("display_name", "description");
		violations.Should().OnlyContain(v => v.Severity == Nullean.Entities.Severity.Warning);
	}

	[Test]
	public void ConflictingDefaultCtasPointAtTheConflictingEntry()
	{
		var docset = ValidDocset() with { TableOfContents = [Page("index.md", "serverless"), Page("./index.md", "other")] };

		var violation = DocumentationSetConstraints
			.All
			.Report(new DocumentationSetInCatalog(docset, Products))
			.Of(DocumentationSetConstraints.DefaultCtaOnePerPage)
			.Single();

		violation
			.Message
			.Should()
			.Be("'index.md' is registered with default CTA 'serverless' and 'other'. Each page can only have one default CTA.");
		violation.File.Should().Be("/docs/docset.yml");
	}

	private static IReadOnlyDictionary<string, Func<DocumentationSetInCatalog, DocumentationSetInCatalog>> CounterExamples =>
		DocsetCounterExamples.ToDictionary(
			kv => kv.Key,
			kv => (Func<DocumentationSetInCatalog, DocumentationSetInCatalog>)(m => m with { Docset = kv.Value(m.Docset) })
		);
}

public class DocsetDiscoveryConstraintsTests
{
	private static MockFileSystem FileSystem => new();

	private static DiscoveredDocset Docset(string path, string? registry) => new(FileSystem.FileInfo.New(path), registry);

	/// <summary>
	/// What discovery returns for a repository: its default docset first (the public registry), then at most one docset for the internal codex registry, each file named
	/// docset.yml or _docset.yml. The generator gives the number of candidates; the registries and files are the ones discovery finds them under.
	/// </summary>
	private static DocsetDiscovery Discovered(DocsetDiscovery d) =>
		new([Docset("/repo/docset.yml", null), .. d.Candidates.Skip(1).Take(1).Select(_ => Docset("/repo/docs/_docset.yml", "internal"))]);

	[Test]
	[MethodDataSource(nameof(DiscoveryAxes))]
	public void DiscoveryAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> DiscoveryAxes() =>
		DomainAutoTest
			.For(
				DocsetDiscoveryConstraints.All,
				DomainGen.Of<DocsetDiscovery>(o => o.UseFileSystem().Normalize<DocsetDiscovery>(Discovered))
			)
			.Options(o => o.UseFileSystem())
			.CounterExamples(CounterExamples)
			.Axes();

	// each case reorders or extends what discovery found: the default docset moved behind a codex one, a second public docset, a file the glob matched by accident
	private static IReadOnlyDictionary<string, Func<DocsetDiscovery, DocsetDiscovery>> CounterExamples =>
		new Dictionary<string, Func<DocsetDiscovery, DocsetDiscovery>>
		{
			["one-docset-per-registry"] = d => new([.. d.Candidates, Docset("/repo/_docset.yml", "public")]),
			["discovered-docset-is-default"] =
				d => new([
					Docset("/repo/codex/docset.yml", "internal"),
					.. d.Candidates.Where(c => c.EffectiveRegistry != DocSetRegistry.Internal)
				]),
			["discovered-docset-name-exact"] =
				d => new([
					.. d.Candidates.Where(c => c.EffectiveRegistry != DocSetRegistry.Internal),
					Docset("/repo/docs/foo-docset.yml", "internal")
				]),
			["docset-discovered"] = _ => new([])
		};
}
