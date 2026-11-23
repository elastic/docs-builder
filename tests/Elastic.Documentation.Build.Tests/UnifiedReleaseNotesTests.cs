// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Changelog.Scrubbing;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Versions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Build.Tests;

public class UnifiedReleaseNotesTests : IDisposable
{
	private readonly FileSystem _fileSystem = new();
	private readonly ScopedTempDirectory _temp;

	public UnifiedReleaseNotesTests() => _temp = new ScopedTempDirectory(_fileSystem, "unified-release-notes");

	public void Dispose()
	{
		_temp.Dispose();
		GC.SuppressFinalize(this);
	}

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

	private UnifiedReleaseNotesBuildOptions Options(params string[] expectedVersions) =>
		new()
		{
			BundlesDirectory = new DirectoryInfo(_fileSystem.Path.Join(_temp.FullName, "bundles")),
			Output = new DirectoryInfo(_fileSystem.Path.Join(_temp.FullName, "site")),
			ExpectedVersions = expectedVersions
		};

	private UnifiedReleaseNotesBuildService Service(params Product[] extraProducts)
	{
		var products = Products.Concat(extraProducts).ToDictionary(p => p.Id).ToFrozenDictionary();
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

	private static IReadOnlyDictionary<string, string> GenerateFiles() =>
		UnifiedReleaseNotesDocset.Generate([
			.. UnifiedReleaseNotesDocset.SelectProducts(Products).Select(p => new ProductVersions(p, ["9.6.0", "9.5.6"]))
		]);

	[Test]
	public void SelectProducts_MixedReleaseNotesPaths_KeepsOnlyDraProductsInIdOrder() =>
		UnifiedReleaseNotesDocset.SelectProducts(Products).Select(p => p.Id).Should().Equal("elasticsearch", "kibana");

	[Test]
	public void Generate_DraProducts_CreatesAPagePerVersionAndTheSharedPagesAndTocPerProduct()
	{
		var files = GenerateFiles();

		foreach (var product in new[] { "elasticsearch", "kibana" })
		{
			files.Should().ContainKey($"{product}/index.md");
			files.Should().ContainKey($"{product}/9.6.0.md");
			files.Should().ContainKey($"{product}/9.5.6.md");
			files.Should().ContainKey($"{product}/9.6.0/known-issues.md");
			files.Should().ContainKey($"{product}/9.6.0/breaking-changes.md");
			files.Should().ContainKey($"{product}/9.6.0/deprecations.md");
			files.Should().ContainKey($"{product}/9.5.6/known-issues.md");
			files.Should().ContainKey($"{product}/toc.yml");
		}

		files.Keys.Should().NotContain(k => k.StartsWith("release-notes/", StringComparison.Ordinal));

		files.Keys.Should().NotContain(k => k.Contains("auditbeat") || k.Contains("curator"));
	}

	private static readonly Product Serverless = CreateProduct("cloud-serverless", "Elastic Cloud Serverless", ReleaseNotesPath.OnRelease);

	private static IReadOnlyDictionary<string, string> GeneratePreviewFiles() =>
		UnifiedReleaseNotesDocset.Generate([
			new ProductVersions(Products[0], ["9.6.0"]),
			new ProductVersions(Serverless, ["staging", "head"])
		]);

	[Test]
	public void SelectProducts_AdditionalProductIds_JoinTheDraProductsInIdOrder() =>
		UnifiedReleaseNotesDocset
			.SelectProducts([.. Products, Serverless], ["cloud-serverless", "curator"])
			.Select(p => p.Id)
			.Should()
			.Equal("cloud-serverless", "curator", "elasticsearch", "kibana");

	[Test]
	public void SelectProducts_AdditionalProductIdOfAnUnknownProduct_IsIgnored() =>
		UnifiedReleaseNotesDocset.SelectProducts(Products, ["nope"]).Select(p => p.Id).Should().Equal("elasticsearch", "kibana");

	[Test]
	public void Generate_PreviewLabels_AreTitledAsTheEnvironmentTheyName()
	{
		var files = GeneratePreviewFiles();

		files["cloud-serverless/index.md"]
			.Should()
			.MatchInlineSnapshot(
				"""
			---
			navigation_title: Elastic Cloud Serverless
			---

			# Elastic Cloud Serverless release notes

			:::{warning}
			This is a preview of an unreleased version. The release notes can change until the version ships.
			:::

			## Versions

			- [Staging](staging.md)
			- [HEAD](head.md)
			"""
			);
		files["cloud-serverless/staging.md"]
			.Should()
			.MatchInlineSnapshot(
				"""
			---
			navigation_title: Staging
			---

			# Elastic Cloud Serverless Staging

			:::{warning}
			This is a preview of an unreleased version. The release notes can change until the version ships.
			:::

			:::{changelog} /changelog/bundles/cloud-serverless
			:version: staging
			:highlights:
			:subsections:
			:::
			"""
			);
		files["cloud-serverless/head/known-issues.md"]
			.Should()
			.MatchInlineSnapshot(
				"""
			---
			navigation_title: Known issues
			---

			# Elastic Cloud Serverless HEAD known issues

			:::{warning}
			This is a preview of an unreleased version. The release notes can change until the version ships.
			:::

			:::{changelog} /changelog/bundles/cloud-serverless
			:version: head
			:type: known-issue
			:subsections:
			:::
			"""
			);
		files["cloud-serverless/toc.yml"]
			.Should()
			.MatchInlineSnapshot(
				"""
			toc:
			  - file: index.md
			  - file: staging.md
			    children:
			      - file: staging/known-issues.md
			      - file: staging/breaking-changes.md
			      - file: staging/deprecations.md
			  - file: head.md
			    children:
			      - file: head/known-issues.md
			      - file: head/breaking-changes.md
			      - file: head/deprecations.md
			"""
			);
	}

	[Test]
	public void Generate_RootIndex_NamesServerlessOnlyWhenAServerlessProductIsOnTheSite()
	{
		GeneratePreviewFiles()["index.md"].Should().Contain("Elastic Cloud Serverless and Elastic Cloud Hosted");
		GenerateFiles()["index.md"].Should().NotContain("Serverless");
	}

	[Test]
	public void Generate_VersionsKeepTheirOwnTitles() => GenerateFiles()["kibana/9.6.0.md"].Should().Contain("# Kibana 9.6.0");

	[Test]
	public void Generate_Docset_DeclaresTheTocWithProductsInOrder()
	{
		var files = GenerateFiles();

		files["docset.yml"]
			.Should()
			.Be("project: unified-release-notes\ntoc:\n  - file: index.md\n  - toc: elasticsearch\n  - toc: kibana\n");
		files.Should().NotContainKey("toc.yml");
	}

	[Test]
	public void Generate_ProductPages_PointTheChangelogDirectiveAtTheProductBundles()
	{
		var files = GenerateFiles();

		files["elasticsearch/index.md"].Should().Contain("# Elasticsearch release notes");
		files["elasticsearch/index.md"].Should().Contain("- [9.6.0](9.6.0.md)\n- [9.5.6](9.5.6.md)");
		files["elasticsearch/9.6.0.md"]
			.Should()
			.Contain(":::{changelog} /changelog/bundles/elasticsearch\n:version: 9.6.0\n:highlights:\n:subsections:\n:::");
		files["elasticsearch/9.6.0.md"].Should().Contain("# Elasticsearch 9.6.0");
		files["kibana/9.6.0/breaking-changes.md"]
			.Should()
			.Contain(":::{changelog} /changelog/bundles/kibana\n:version: 9.6.0\n:type: breaking-change\n:dropdowns:\n:subsections:\n:::");
		files["kibana/9.6.0/known-issues.md"].Should().Contain(":version: 9.6.0\n:type: known-issue\n:subsections:");
		files["kibana/9.6.0/deprecations.md"].Should().Contain(":version: 9.6.0\n:type: deprecation\n:subsections:");
		files["kibana/9.6.0/deprecations.md"].Should().Contain("# Kibana 9.6.0 deprecations");
	}

	[Test]
	public void Generate_ProductPages_AreNamedInTheNavigationAfterTheProductAndTheVersion()
	{
		var files = GenerateFiles();

		files["kibana/9.6.0.md"].Should().Contain("navigation_title: 9.6.0\n");
		files["kibana/toc.yml"]
			.Should()
			.Be(
				"""
				toc:
				  - file: index.md
				  - file: 9.6.0.md
				    children:
				      - file: 9.6.0/known-issues.md
				      - file: 9.6.0/breaking-changes.md
				      - file: 9.6.0/deprecations.md
				  - file: 9.5.6.md
				    children:
				      - file: 9.5.6/known-issues.md
				      - file: 9.5.6/breaking-changes.md
				      - file: 9.5.6/deprecations.md

				""".ReplaceLineEndings(
					"\n"
				)
			);

		files["kibana/index.md"].Should().Contain("navigation_title: Kibana\n");
		files["kibana/9.6.0/known-issues.md"].Should().Contain("navigation_title: Known issues\n");
		files["index.md"].Should().Contain("navigation_title: Release notes\n");
	}

	[Test]
	public void Generate_EveryPage_WarnsThatTheVersionsAreUnreleased()
	{
		var files = GenerateFiles();

		foreach (var (path, content) in files.Where(f => f.Key.EndsWith(".md", StringComparison.Ordinal)))
			content.Should().Contain(":::{warning}\nThis is a preview of an unreleased version.", $"{path} shows unreleased versions");
	}

	[Test]
	public void Generate_RootIndex_LinksEveryProductByDisplayName()
	{
		var files = GenerateFiles();

		files["index.md"].Should().Contain("- [Elasticsearch](elasticsearch/index.md)");
		files["index.md"].Should().Contain("- [Kibana](kibana/index.md)");
	}

	[Test]
	public async Task Build_BundlesForEveryProduct_WritesAPageSetPerProduct()
	{
		_ = WriteBundle("elasticsearch", "elasticsearch-elasticsearch-9.5.0.yaml", "9.5.0", "Elasticsearch unified change");
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("9.5.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		collector.Errors.Should().Be(0);
		collector.Warnings.Should().Be(0);
		collector.Hints.Should().Be(0, "the generated toc should not trigger structure hints");
		var output = _fileSystem.Path.Join(_temp.FullName, "site");
		_fileSystem.File.Exists(_fileSystem.Path.Join(output, "elasticsearch", "index.html")).Should().BeTrue();
		_fileSystem.File.Exists(_fileSystem.Path.Join(output, "kibana", "9.5.0", "breaking-changes", "index.html")).Should().BeTrue();
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(output, "kibana", "breaking-changes")).Should().BeFalse();
		_fileSystem
			.File
			.ReadAllText(_fileSystem.Path.Join(output, "elasticsearch", "9.5.0", "index.html"))
			.Should()
			.Contain("Elasticsearch unified change")
			.And
			.Contain("This is a preview of an unreleased version");
		_fileSystem
			.File
			.ReadAllText(_fileSystem.Path.Join(output, "elasticsearch", "index.html"))
			.Should()
			.Contain("9.5.0")
			.And
			.NotContain("Elasticsearch unified change");
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(output, "auditbeat")).Should().BeFalse();
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(output, "release-notes")).Should().BeFalse();
	}

	[Test]
	public async Task Build_AdditionalProductWithLabelBundles_MergesTheServicesOfALabelAndKeepsTheLabelsApart()
	{
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		_ = WriteBundle("cloud-serverless", "kibana-cloud-serverless-staging.yaml", "staging", "Kibana staging change");
		_ = WriteBundle(
			"cloud-serverless",
			"elasticsearch-serverless-cloud-serverless-staging.yaml",
			"staging",
			"Elasticsearch staging change"
		);
		_ = WriteBundle("cloud-serverless", "kibana-cloud-serverless-head.yaml", "head", "Kibana head change");
		var collector = new DiagnosticsCollector([]);
		var options = Options("9.5.0") with { AdditionalProductIds = ["cloud-serverless"] };

		var succeeded = await Service(Serverless).Build(collector, options, TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		collector.Errors.Should().Be(0);
		collector.Warnings.Should().Be(0);
		var site = _fileSystem.Path.Join(_temp.FullName, "site");
		var staging = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(site, "cloud-serverless", "staging", "index.html"));
		staging.Should().Contain("Kibana staging change").And.Contain("Elasticsearch staging change").And.NotContain("Kibana head change");
		var head = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(site, "cloud-serverless", "head", "index.html"));
		head.Should().Contain("Kibana head change").And.NotContain("staging change");
		var index = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(site, "cloud-serverless", "index.html"));
		index.IndexOf("Staging", StringComparison.Ordinal).Should().BeLessThan(index.IndexOf("HEAD", StringComparison.Ordinal));
		_fileSystem.File.Exists(_fileSystem.Path.Join(site, "kibana", "9.5.0", "index.html")).Should().BeTrue();
	}

	[Test]
	public async Task Build_AdditionalProductBundlesWithoutOptingIn_AreLeftOutOfTheSite()
	{
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		_ = WriteBundle("cloud-serverless", "kibana-cloud-serverless-staging.yaml", "staging", "Kibana staging change");
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service(Serverless).Build(collector, Options("9.5.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(_temp.FullName, "site", "cloud-serverless")).Should().BeFalse();
	}

	private const string PrivateReference = "https://github.com/elastic/private-repo/pull/12";
	private const string PublicReference = "https://github.com/elastic/kibana/pull/99";

	// The renderer shows the description of an entry only for a bundle with a private repository in it, which is what the
	// Elastic Cloud Hosted preview has. edot-cloud-forwarder-aws is private in assembler.yml.
	private string WriteBundleWithReferences() =>
		WriteFile(
			"kibana",
			"kibana-kibana-9.5.0.yaml",
			$$"""
			products:
			- product: kibana
			  target: 9.5.0
			  repo: edot-cloud-forwarder-aws
			  owner: elastic
			entries:
			- title: Entry with references
			  type: enhancement
			  description: Follows {{PrivateReference}} and {{PublicReference}}.
			  products:
			  - product: kibana
			    target: 9.5.0
			  prs:
			  - "{{PrivateReference}}"
			  - "{{PublicReference}}"
			"""
		);

	private static async Task<string> ScrubLikePublication(string product, string fileName, string content, Cancel ctx) =>
		(await new ChangelogContentScrubber(new TestLoggerFactory(), ["elastic/kibana"]).ScrubAsync(
			$"{ChangelogKeys.BundlePrefix}{product}/{fileName}",
			content,
			ctx
		)).Content;

	[Test]
	public async Task Build_WithAScrubber_RemovesReferencesToRepositoriesThatAreNotAllowed()
	{
		_ = WriteBundleWithReferences();
		var collector = new DiagnosticsCollector([]);
		var options = Options("9.5.0") with { ScrubBundle = (p, f, c, ctx) => ScrubLikePublication(p, f, c, ctx) };

		var succeeded = await Service().Build(collector, options, TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		var html = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(_temp.FullName, "site", "kibana", "9.5.0", "index.html"));
		html.Should().Contain("Entry with references").And.Contain("kibana/pull/99").And.NotContain("private-repo");
		html.Should().Contain("Follows", "the description renders as a highlight, so its text is scrubbed in place and not hidden");
	}

	[Test]
	public async Task Build_WithoutAScrubber_KeepsTheReferencesAsWritten()
	{
		_ = WriteBundleWithReferences();
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("9.5.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		var html = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(_temp.FullName, "site", "kibana", "9.5.0", "index.html"));
		html.Should().Contain("private-repo").And.Contain("Follows");
	}

	[Test]
	public async Task Build_WhenTheScrubberFails_FailsInsteadOfPublishingTheBundleAsItIs()
	{
		_ = WriteBundleWithReferences();
		var collector = new DiagnosticsCollector([]);
		var options = Options("9.5.0") with { ScrubBundle = (_, _, _, _) => throw new InvalidOperationException("cannot scrub") };

		var act = async () => await Service().Build(collector, options, TestContext.Current!.Execution.CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(_temp.FullName, "site", "kibana")).Should().BeFalse();
	}

	[Test]
	public async Task Build_ProductWithoutBundlesFolder_LeavesItOutOfTheSite()
	{
		_ = WriteBundle("elasticsearch", "elasticsearch-elasticsearch-9.5.0.yaml", "9.5.0", "Elasticsearch unified change");
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("9.5.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		collector.Errors.Should().Be(0);
		var output = _fileSystem.Path.Join(_temp.FullName, "site");
		_fileSystem.File.Exists(_fileSystem.Path.Join(output, "elasticsearch", "index.html")).Should().BeTrue();
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(output, "kibana")).Should().BeFalse("kibana has no bundles");
		_fileSystem.File.ReadAllText(_fileSystem.Path.Join(output, "index.html")).Should().NotContain("kibana/");
	}

	[Test]
	public async Task Build_NoProductHasBundles_FailsWithoutWritingASite()
	{
		_ = _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Join(_temp.FullName, "bundles"));
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("9.5.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeFalse();
		collector.Errors.Should().Be(1);
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(_temp.FullName, "site")).Should().BeFalse();
	}

	[Test]
	public async Task Build_MultipleBundleVersionsSuppliedOutOfOrder_EachVersionPageHasOnlyItsOwnEntriesAndNavigationIsNewestFirst()
	{
		// Written oldest-first, and with the 'v' prefix a release tag actually has, to prove that
		// neither enumeration order nor the target/filename version mismatch affects the result.
		_ = WriteBundle("elasticsearch", "elasticsearch-elasticsearch-v9.5.0.yaml", "v9.5.0", "Elasticsearch 9.5.0 change");
		_ = WriteBundle("elasticsearch", "elasticsearch-elasticsearch-v9.6.0.yaml", "v9.6.0", "Elasticsearch 9.6.0 change");
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("v9.5.0", "v9.6.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		collector
			.Warnings
			.Should()
			.Be(0, "each version page's :version: filter should match its own bundle despite the bundle target's 'v' prefix");

		var output = _fileSystem.Path.Join(_temp.FullName, "site", "elasticsearch");
		_fileSystem
			.File
			.ReadAllText(_fileSystem.Path.Join(output, "9.5.0", "index.html"))
			.Should()
			.Contain("Elasticsearch 9.5.0 change")
			.And
			.NotContain("Elasticsearch 9.6.0 change");
		_fileSystem
			.File
			.ReadAllText(_fileSystem.Path.Join(output, "9.6.0", "index.html"))
			.Should()
			.Contain("Elasticsearch 9.6.0 change")
			.And
			.NotContain("Elasticsearch 9.5.0 change");

		var index = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(output, "index.html"));
		index.IndexOf("9.6.0", StringComparison.Ordinal).Should().BeLessThan(index.IndexOf("9.5.0", StringComparison.Ordinal));
	}

	[Test]
	public async Task Build_ExpectedVersionMissingForAProduct_StillBuilds()
	{
		// A product that did not change in a release has no bundle for it.
		_ = WriteBundle("elasticsearch", "elasticsearch-elasticsearch-9.5.0.yaml", "9.5.0", "Elasticsearch unified change");
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("9.5.0", "9.6.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		collector.Errors.Should().Be(0);
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
		var html = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(_temp.FullName, "site", "elasticsearch", "9.5.0", "index.html"));
		html.Should().Contain("Original change");
		html.Should().Contain("Change added by the amend");
		html.Should().NotContain("Change removed by the amend");
	}
}
