// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Constraints;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The documentation set constraints are checked on definitions built directly, with no loader involved: the rules
/// hold for any model. Every constraint needs a counter-example that breaks it, and only it, so none is dead or
/// duplicated; a valid definition breaks none.
/// </summary>
[InheritsTests]
public class DocumentationSetConstraintsTests : ConstraintSetTests<DocumentationSetInCatalog>
{
	private static ProductCatalog Products =>
		ProductCatalog.FromPublicProducts(new Dictionary<string, Product>
		{
			["elasticsearch"] = new() { Id = "elasticsearch", DisplayName = "Elasticsearch" }
		});

	private static DeclaringFilePath Declared => DeclaringFilePath.From("/docs/docset.yml");

	private static DocumentationSetDefinition ValidDocset()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData> { ["/docs/docset.yml"] = new("project: test") });
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
			TocDefaultCtas = [new("index.md", "serverless")]
		};
	}

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
								new ListingOptions(Glob: "**/*.yaml")
							)
						]
					},
			["cross-link-well-formed"] = d => d with { CrossLinks = ["internal://secret"] },
			["release-notes-product-known"] = d => d with { ReleaseNotesProducts = ["kibana"] },
			["substitution-keys-unique"] = d => d with { Substitutions = [new("stack", "a"), new("STACK", "b")] },
			["api-entry-valid"] = d => d with { Api = [new ApiDeclaration("elasticsearch", [])] },
			["storybook-variables-allowed"] = d => d with { HasStorybook = true, StorybookRegistry = "${NOT_ALLOWED}/registry" },
			["branding-images-valid"] = d => d with { Branding = new BrandingDeclaration(Icon: "logo.bmp") },
			["cta-template-valid"] = d => d with { Ctas = [new CtaDeclaration("broken", null, null, [])], TocDefaultCtas = [] },
			["default-cta-known"] = d => d with { TocDefaultCtas = [new("index.md", "missing")] },
			["primary-nav-without-branding"] =
				d => d with { Branding = new BrandingDeclaration(), Features = new DeclaredFeatures(PrimaryNav: true) },
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
			["registry-known"] = d => d with { Registry = "public" }
		}.ToFrozenDictionary();

	protected override IReadOnlyList<Constraint<DocumentationSetInCatalog>> Constraints => DocumentationSetConstraints.All;

	protected override DocumentationSetInCatalog Valid() => new(ValidDocset(), Products);

	protected override IReadOnlyDictionary<string, Func<DocumentationSetInCatalog, DocumentationSetInCatalog>> CounterExamples =>
		DocsetCounterExamples.ToDictionary(
			kv => kv.Key,
			kv => (Func<DocumentationSetInCatalog, DocumentationSetInCatalog>)(m => m with { Docset = kv.Value(m.Docset) })
		);
}

[InheritsTests]
public class DocsetDiscoveryConstraintsTests : ConstraintSetTests<DocsetDiscovery>
{
	private static MockFileSystem FileSystem => new();

	private static DiscoveredDocset Docset(string path, string? registry) => new(FileSystem.FileInfo.New(path), registry);

	protected override IReadOnlyList<Constraint<DocsetDiscovery>> Constraints => DocsetDiscoveryConstraints.All;

	protected override DocsetDiscovery Valid() => new([Docset("/repo/docset.yml", null), Docset("/repo/docs/docset.yml", "internal")]);

	protected override IReadOnlyDictionary<string, Func<DocsetDiscovery, DocsetDiscovery>> CounterExamples =>
		new Dictionary<string, Func<DocsetDiscovery, DocsetDiscovery>>
		{
			["one-docset-per-registry"] = d => new([.. d.Candidates, Docset("/repo/_docset.yml", "public")]),
			["discovered-docset-is-default"] = d => new([.. d.Candidates.Reverse()])
		};
}
