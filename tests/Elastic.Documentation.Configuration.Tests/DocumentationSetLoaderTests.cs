// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Tests;

public class DocumentationSetLoaderTests
{
	private const string FullDocset =
		"""
		project: settings
		max_toc_depth: 4
		dev_docs: true
		registry: public
		cross_links:
		  - docs-content
		  - public://elasticsearch
		exclude:
		  - '_*.md'
		  - '!drafts/keep.md'
		extensions:
		  - detection-rules
		subs:
		  Product: The Product
		suppress:
		  - AutolinkElasticCoDocs
		icon: gear
		features:
		  primary-nav: true
		  guide-nav: false
		products:
		  - id: elasticsearch
		codex:
		  group: observability
		branding:
		  header-bg: '#112233'
		  copyright: Acme
		cta:
		  custom:
		    button:
		      label: Custom
		      url: https://example.com/custom
		    benefits:
		      - one
		storybook:
		  registry: https://example.com/storybook
		release_notes:
		  - product: elasticsearch
		api:
		  elasticsearch:
		    - spec: elasticsearch.json
		      product: elasticsearch
		      catalog:
		        categories:
		          - self
		toc:
		  - file: index.md
		""";

	[Test]
	public void Resolve_EveryDocsetKey_IsOnTheResolvedSet()
	{
		var fileSystem = DocsetGolden.CreateFileSystem(FullDocset, new DocFile("index.md"));
		var configPath = fileSystem.FileInfo.New($"{DocsetGolden.Root}/docset.yml");
		var context = DocsetGolden.CreateContext(new DiagnosticsCollector([]), fileSystem);
		var definition = DocumentationSetLoader.Load(
			new DiagnosticsCollector([]),
			configPath,
			new ScopedFileSystem(fileSystem, DocsetGolden.Root)
		).Value;

		ResolvedDocumentationSet Resolve() =>
			DocumentationSetResolver.Resolve(definition, context, GoldenConfig.LoadVersions(), GoldenConfig.LoadProducts());

		var resolved = Resolve();

		resolved.Should().Be(Resolve());
		resolved.Project.Should().Be("settings");
		resolved.MaxTocDepth.Should().Be(4);
		resolved.DevelopmentDocs.Should().BeTrue();
		resolved.Exclusions.ExcludeGlobs.Should().Equal("_*.md");
		resolved.Exclusions.IncludeOverrides.Should().Equal("drafts/keep.md");
		resolved.CrossLinkEntries.Select(e => e.Repository).Should().Equal("docs-content", "elasticsearch");
		resolved.Extensions.IsDetectionRulesEnabled.Should().BeTrue();
		resolved.Ctas.Keys.Should().BeEquivalentTo(Cta.DefaultName, "custom");
		resolved.Branding!.HeaderBg.Should().Be("#112233");
		resolved.FeatureToggles.Should().BeEquivalentTo(new Dictionary<string, bool> { ["primary-nav"] = true, ["guide-nav"] = false });
		resolved.ApiConfigurations!["elasticsearch"].CatalogCategories.Should().Equal("self");
		resolved.Products.Select(p => p.Id).Should().Equal("elasticsearch");
	}

	[Test]
	public void Load_EveryDocsetKey_IsOnTheDefinition()
	{
		var fileSystem = DocsetGolden.CreateFileSystem(FullDocset, new DocFile("index.md"));
		var docsetFile = fileSystem.FileInfo.New($"{DocsetGolden.Root}/docset.yml");

		var loaded = DocumentationSetLoader.Load(
			new DiagnosticsCollector([]),
			docsetFile,
			new ScopedFileSystem(fileSystem, DocsetGolden.Root)
		);

		loaded.Findings.Should().BeEmpty();
		DocsetGolden
			.Normalise(ModelDump.ToText(loaded.Value, DocsetGolden.Summarize, nameof(DocumentationSetDefinition.TableOfContents)))
			.Should()
			.MatchSnapshot();
	}

	[Test]
	public void Load_TableOfContents_IsTheResolvedOne()
	{
		var fileSystem = DocsetGolden.CreateFileSystem(FullDocset, new DocFile("index.md"));
		var docsetFile = fileSystem.FileInfo.New($"{DocsetGolden.Root}/docset.yml");
		var collector = new DiagnosticsCollector([]);
		var scoped = new ScopedFileSystem(fileSystem, DocsetGolden.Root);

		var definition = DocumentationSetLoader.Load(collector, docsetFile, scoped).Value;
		var file = DocumentationSetFile.LoadAndResolve(new DiagnosticsCollector([]), docsetFile, scoped);

		definition.TableOfContents.Should().BeEquivalentTo(file.TableOfContents);
	}

	[Test]
	[Arguments("exclude")]
	[Arguments("cross_links")]
	[Arguments("subs")]
	[Arguments("release_notes")]
	[Arguments("features")]
	public void Configure_KeyWithoutValue_FailsAsItAlwaysDid(string key)
	{
		var yaml = $"project: p\n{key}:\ntoc:\n  - file: index.md\n";
		var collector = new CapturingDiagnosticsCollector();
		var fileSystem = DocsetGolden.CreateFileSystem(yaml, new DocFile("index.md"));
		var dto = DocsetGolden.LoadAndResolve(collector, yaml, new DocFile("index.md"));

		var act = () => DocsetGolden.CreateConfiguration(dto, collector, fileSystem, withGlobalConfig: false);

		_ = act.Should().Throw<Exception>();
		collector.Captured.Should().ContainSingle(d => d.Message.StartsWith("Could not load docset.yml: "));
	}

	// what main does, per shape: the exception, and the diagnostics in order
	[Test]
	[Arguments("release_notes:\n  - \n", new string[0])]
	[Arguments("release_notes:\n  - product: a\n  - \n", new[]
	{
		"Unknown 'release_notes' product 'a'. It must be a product id defined in products.yml."
	})]
	[Arguments("cta:\n  x:\n", new string[0])]
	[Arguments("extensions:\n", new string[0])]
	[Arguments("extensions:\napi:\n  p:\n    - spec: a\nbranding:\n  icon: x.exe\n", new string[0])]
	[Arguments("products:\n  - \n", new string[0])]
	[Arguments("products:\n  - id:\n", new string[0])]
	public void Configure_NullInsideAList_FailsWhileTheConfigurationIsBuilt(string declaration, string[] before)
	{
		var yaml = $"project: p\n{declaration}toc:\n  - file: index.md\n";
		var collector = new CapturingDiagnosticsCollector();
		var fileSystem = DocsetGolden.CreateFileSystem(yaml, new DocFile("index.md"));
		var dto = DocsetGolden.LoadAndResolve(collector, yaml, new DocFile("index.md"));

		var act = () => DocsetGolden.CreateConfiguration(dto, collector, fileSystem, withGlobalConfig: false);

		_ = act.Should().Throw<NullReferenceException>().WithMessage("Object reference not set to an instance of an object.");
		collector
			.Captured
			.Select(d => d.Message)
			.Should()
			.Equal([.. before, "Could not load docset.yml: Object reference not set to an instance of an object."]);
	}

	// what main's metadata reader returned: a declaration the metadata does not need never stops it
	[Test]
	[Arguments("release_notes: [null]\n")]
	[Arguments("release_notes:\n  - \n")]
	[Arguments("release_notes:\n")]
	[Arguments("products:\n  - \n")]
	[Arguments("cta:\n  x:\n")]
	[Arguments("exclude:\n")]
	[Arguments("cross_links:\n")]
	public void LoadMetadata_AnUnrelatedDeclarationWithNoValue_StillReturnsRegistryGroupAndIcon(string declaration)
	{
		var yaml = $"project: p\nregistry: internal\nicon: gear\ncodex:\n  group: g\n{declaration}toc:\n  - file: index.md\n";
		var fileSystem = DocsetGolden.CreateFileSystem(yaml, new DocFile("index.md"));

		var metadata = DocumentationSetLoader.LoadMetadata(fileSystem.FileInfo.New($"{DocsetGolden.Root}/docset.yml"));

		metadata.Registry.Should().Be("internal");
		metadata.CodexGroup.Should().Be("g");
		metadata.Icon.Should().Be("gear");
	}

	// the folder-level exclude is matched ignoring case (the set CollectFolderExcludedFiles returns), as it always was
	[Test]
	[Arguments("DRAFT.md")]
	[Arguments("draft.md")]
	public void Configure_FolderExclude_IsMatchedIgnoringCase(string excluded)
	{
		var yaml = $"project: p\ntoc:\n  - file: index.md\n  - folder: sub\n    exclude: [{excluded}]\n";
		var files = new[] { new DocFile("index.md"), new DocFile("sub/index.md"), new DocFile("sub/draft.md") };
		var collector = new CapturingDiagnosticsCollector();
		var dto = DocsetGolden.LoadAndResolve(collector, yaml, files);

		var config = DocsetGolden.CreateConfiguration(dto, collector, DocsetGolden.CreateFileSystem(yaml, files), withGlobalConfig: false);

		config.IsExcluded("sub/draft.md").Should().BeTrue();
		config.IsExcluded("sub/DRAFT.md").Should().BeTrue();
		config.IsExcluded("sub/index.md").Should().BeFalse();
	}
}
