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
}
