// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Build.Tests;

public class UnifiedReleaseNotesTests : IDisposable
{
	private readonly FileSystem _fileSystem = new();
	private readonly ScopedTempDirectory _temp;

	public UnifiedReleaseNotesTests() => _temp = new ScopedTempDirectory(_fileSystem, "unified-release-notes");

	public void Dispose() => _temp.Dispose();

	private static Product CreateProduct(string id, string display, ReleaseNotesPath path) =>
		new()
		{
			Id = id,
			DisplayName = display,
			VersioningSystem = new VersioningSystem
			{
				Id = VersioningSystemId.Stack,
				Current = new SemVersion(9, 5, 0),
				Base = new SemVersion(9, 0, 0)
			},
			Features = new ProductFeatures { PublicReference = true, ReleaseNotes = path }
		};

	private static readonly Product[] Products =
	[
		CreateProduct("kibana", "Kibana", ReleaseNotesPath.DailyReleasableArtifacts),
		CreateProduct("elasticsearch", "Elasticsearch", ReleaseNotesPath.DailyReleasableArtifacts),
		CreateProduct("auditbeat", "Auditbeat", ReleaseNotesPath.OnRelease),
		CreateProduct("curator", "Curator", ReleaseNotesPath.None)
	];

	private static string Bundle(string product, string version, string title) =>
		$"""
		products:
		- product: {product}
		  target: {version}
		  repo: {product}
		  owner: elastic
		entries:
		- title: {title}
		  type: enhancement
		  products:
		  - product: {product}
		    target: {version}
		  prs:
		  - "100"
		""";

	private string WriteBundle(string product, string fileName, string version, string title)
	{
		var folder = _fileSystem.Path.Join(_temp.FullName, "bundles", product);
		_ = _fileSystem.Directory.CreateDirectory(folder);
		_fileSystem.File.WriteAllText(_fileSystem.Path.Join(folder, fileName), Bundle(product, version, title));
		return folder;
	}

	private string WriteFile(string product, string fileName, string content)
	{
		var folder = _fileSystem.Path.Join(_temp.FullName, "bundles", product);
		_ = _fileSystem.Directory.CreateDirectory(folder);
		_fileSystem.File.WriteAllText(_fileSystem.Path.Join(folder, fileName), content);
		return folder;
	}

	private UnifiedReleaseNotesBuildOptions Options(params string[] requiredVersions) =>
		new()
		{
			BundlesDirectory = new DirectoryInfo(_fileSystem.Path.Join(_temp.FullName, "bundles")),
			Output = new DirectoryInfo(_fileSystem.Path.Join(_temp.FullName, "site")),
			RequiredVersions = requiredVersions
		};

	private UnifiedReleaseNotesBuildService Service()
	{
		var products = Products.ToDictionary(p => p.Id).ToFrozenDictionary();
		var productsConfiguration = new ProductsConfiguration
		{
			Products = products,
			PublicReferenceProducts = products,
			ProductDisplayNames = products.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
		};
		var configurationContext = TestHelpers.CreateConfigurationContext(_fileSystem, productsConfiguration: productsConfiguration);
		return new UnifiedReleaseNotesBuildService(
			new TestLoggerFactory(),
			configurationContext,
			new NullCoreService(),
			MockEnvironmentVariables.CreateLocal()
		);
	}

	[Test]
	public void SelectProducts_MixedReleaseNotesPaths_KeepsOnlyDraProductsInIdOrder() =>
		UnifiedReleaseNotesDocset.SelectProducts(Products).Select(p => p.Id).Should().Equal("elasticsearch", "kibana");

	[Test]
	public void Generate_DraProducts_CreatesFourPagesAndTocPerProduct()
	{
		var files = UnifiedReleaseNotesDocset.Generate(UnifiedReleaseNotesDocset.SelectProducts(Products));

		foreach (var product in new[] { "elasticsearch", "kibana" })
		{
			files.Should().ContainKey($"release-notes/{product}/index.md");
			files.Should().ContainKey($"release-notes/{product}/known-issues.md");
			files.Should().ContainKey($"release-notes/{product}/breaking-changes.md");
			files.Should().ContainKey($"release-notes/{product}/deprecations.md");
			files.Should().ContainKey($"release-notes/{product}/toc.yml");
		}

		files.Keys.Should().NotContain(k => k.Contains("auditbeat") || k.Contains("curator"));
	}

	[Test]
	public void Generate_Docset_DeclaresTheTocWithProductsInOrder()
	{
		var files = UnifiedReleaseNotesDocset.Generate(UnifiedReleaseNotesDocset.SelectProducts(Products));

		files["docset.yml"]
			.Should()
			.Be(
				"project: unified-release-notes\ntoc:\n  - file: release-notes/index.md\n    children:\n      - toc: release-notes/elasticsearch\n      - toc: release-notes/kibana\n"
			);
		files.Should().NotContainKey("toc.yml");
	}

	[Test]
	public void Generate_ProductPages_PointTheChangelogDirectiveAtTheProductBundles()
	{
		var files = UnifiedReleaseNotesDocset.Generate(UnifiedReleaseNotesDocset.SelectProducts(Products));

		files["release-notes/elasticsearch/index.md"].Should().Contain("# Elasticsearch release notes");
		files["release-notes/elasticsearch/index.md"]
			.Should()
			.Contain(":::{changelog} /changelog/bundles/elasticsearch\n:highlights:\n:::");
		files["release-notes/kibana/breaking-changes.md"].Should().Contain(":type: breaking-change\n:dropdowns:");
		files["release-notes/kibana/known-issues.md"].Should().Contain(":type: known-issue");
		files["release-notes/kibana/deprecations.md"].Should().Contain(":type: deprecation");
	}

	[Test]
	public void Generate_RootIndex_LinksEveryProductByDisplayName()
	{
		var files = UnifiedReleaseNotesDocset.Generate(UnifiedReleaseNotesDocset.SelectProducts(Products));

		files["release-notes/index.md"].Should().Contain("- [Elasticsearch](elasticsearch/index.md)");
		files["release-notes/index.md"].Should().Contain("- [Kibana](kibana/index.md)");
	}

	[Test]
	public async Task Build_BundlesForEveryProduct_WritesAPageSetPerProduct()
	{
		_ = WriteBundle("elasticsearch", "elasticsearch-elasticsearch-9.5.0.yaml", "9.5.0", "Elasticsearch unified change");
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("9.5.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		var output = _fileSystem.Path.Join(_temp.FullName, "site");
		_fileSystem.File.Exists(_fileSystem.Path.Join(output, "release-notes", "elasticsearch", "index.html")).Should().BeTrue();
		_fileSystem
			.File
			.Exists(_fileSystem.Path.Join(output, "release-notes", "kibana", "breaking-changes", "index.html"))
			.Should()
			.BeTrue();
		_fileSystem
			.File
			.ReadAllText(_fileSystem.Path.Join(output, "release-notes", "elasticsearch", "index.html"))
			.Should()
			.Contain("Elasticsearch unified change");
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(output, "release-notes", "auditbeat")).Should().BeFalse();
	}

	[Test]
	public async Task Build_ProductWithoutBundlesFolder_FailsWithoutWritingASite()
	{
		_ = WriteBundle("elasticsearch", "elasticsearch-elasticsearch-9.5.0.yaml", "9.5.0", "Elasticsearch unified change");
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("9.5.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeFalse();
		collector.Errors.Should().Be(1);
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(_temp.FullName, "site")).Should().BeFalse();
	}

	[Test]
	public async Task Build_RequiredVersionMissingForAProduct_Fails()
	{
		_ = WriteBundle("elasticsearch", "elasticsearch-elasticsearch-9.5.0.yaml", "9.5.0", "Elasticsearch unified change");
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("9.5.0", "9.6.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeFalse();
		collector.Errors.Should().Be(2);
	}

	[Test]
	public async Task Build_BundleWithAmendSidecar_RendersTheAmendedEntries()
	{
		// language=yaml
		const string parent =
			"""
			products:
			- product: elasticsearch
			  target: 9.5.0
			  repo: elasticsearch
			  owner: elastic
			entries:
			- title: Original change
			  type: enhancement
			  file:
			    name: original.yaml
			    checksum: aaa
			  products:
			  - product: elasticsearch
			    target: 9.5.0
			  prs:
			  - "100"
			- title: Change removed by the amend
			  type: enhancement
			  file:
			    name: removed.yaml
			    checksum: bbb
			  products:
			  - product: elasticsearch
			    target: 9.5.0
			  prs:
			  - "101"
			""";
		// language=yaml
		const string amend =
			"""
			exclude-entries:
			- file:
			    name: removed.yaml
			    checksum: bbb
			entries:
			- title: Change added by the amend
			  type: enhancement
			  products:
			  - product: elasticsearch
			    target: 9.5.0
			  prs:
			  - "102"
			""";
		_ = WriteFile("elasticsearch", "elasticsearch-elasticsearch-9.5.0.yaml", parent);
		_ = WriteFile("elasticsearch", "elasticsearch-elasticsearch-9.5.0.amend-1.yaml", amend);
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("9.5.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		var html = _fileSystem.File.ReadAllText(
			_fileSystem.Path.Join(_temp.FullName, "site", "release-notes", "elasticsearch", "index.html")
		);
		html.Should().Contain("Original change");
		html.Should().Contain("Change added by the amend");
		html.Should().NotContain("Change removed by the amend");
	}
}
