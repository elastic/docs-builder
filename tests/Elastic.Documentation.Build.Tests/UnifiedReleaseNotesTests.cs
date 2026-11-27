// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Changelog.Scrubbing;
using Elastic.Documentation.Catalog;
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
		var productsConfiguration = new ProductCatalog { Products = products };
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
			new ProductVersions(Serverless, ["staging", "edge"])
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
			- [Edge](edge.md)
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

			These are the changes running on the staging environment that are not in production yet. They are next to be released.

			:::{changelog} /changelog/bundles/cloud-serverless
			:version: staging
			:highlights:
			:subsections:
			:::
			"""
			);
		files["cloud-serverless/edge/known-issues.md"]
			.Should()
			.MatchInlineSnapshot(
				"""
			---
			navigation_title: Known issues
			---

			# Elastic Cloud Serverless Edge known issues

			:::{warning}
			This is a preview of an unreleased version. The release notes can change until the version ships.
			:::

			:::{changelog} /changelog/bundles/cloud-serverless
			:version: edge
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
			  - file: edge.md
			    children:
			      - file: edge/known-issues.md
			      - file: edge/breaking-changes.md
			      - file: edge/deprecations.md
			"""
			);
	}

	[Test]
	public void Generate_PreviewLabelsWithADate_ExplainEachReleaseAndNameTheVersionItWouldGetToday()
	{
		var files = UnifiedReleaseNotesDocset.Generate(
			[new ProductVersions(Products[0], ["9.5.0"]), new ProductVersions(Serverless, ["staging", "edge"])],
			null,
			new DateOnly(2026, 11, 24)
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

			These are the changes running on the staging environment that are not in production yet. They are next to be released. If released today, this release would be dated **2026-11-24**.

			:::{changelog} /changelog/bundles/cloud-serverless
			:version: staging
			:highlights:
			:subsections:
			:::
			"""
			);
		files["cloud-serverless/edge.md"]
			.Should()
			.MatchInlineSnapshot(
				"""
			---
			navigation_title: Edge
			---

			# Elastic Cloud Serverless Edge

			:::{warning}
			This is a preview of an unreleased version. The release notes can change until the version ships.
			:::

			These are all the changes merged to the default branch that are not in production yet. If released today, this release would be dated **2026-11-24**.

			:::{changelog} /changelog/bundles/cloud-serverless
			:version: edge
			:highlights:
			:subsections:
			:::
			"""
			);
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

			- [Staging](staging.md), dated 2026-11-24 if released today
			- [Edge](edge.md), dated 2026-11-24 if released today
			"""
			);
		files["kibana/9.5.0.md"].Should().NotContain("If released today", "a version is its own name");
		files["kibana/index.md"].Should().NotContain("if released today");
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
		_ = WriteBundle("cloud-serverless", "kibana-cloud-serverless-edge.yaml", "edge", "Kibana edge change");
		var collector = new DiagnosticsCollector([]);
		var options = Options("9.5.0") with { AdditionalProductIds = ["cloud-serverless"] };

		var succeeded = await Service(Serverless).Build(collector, options, TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		collector.Errors.Should().Be(0);
		collector.Warnings.Should().Be(0);
		var site = _fileSystem.Path.Join(_temp.FullName, "site");
		var staging = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(site, "cloud-serverless", "staging", "index.html"));
		staging.Should().Contain("Kibana staging change").And.Contain("Elasticsearch staging change").And.NotContain("Kibana edge change");
		var head = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(site, "cloud-serverless", "edge", "index.html"));
		head.Should().Contain("Kibana edge change").And.NotContain("staging change");
		var index = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(site, "cloud-serverless", "index.html"));
		index.IndexOf("Staging", StringComparison.Ordinal).Should().BeLessThan(index.IndexOf("Edge", StringComparison.Ordinal));
		_fileSystem.File.Exists(_fileSystem.Path.Join(site, "kibana", "9.5.0", "index.html")).Should().BeTrue();
	}

	[Test]
	public async Task Build_WithAPreviewDate_ThePagesOfThePreviewReleasesSayWhatTheyWouldBeNamedToday()
	{
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		_ = WriteBundle("cloud-serverless", "kibana-cloud-serverless-staging.yaml", "staging", "Kibana staging change");
		_ = WriteBundle("cloud-serverless", "kibana-cloud-serverless-edge.yaml", "edge", "Kibana edge change");
		var collector = new DiagnosticsCollector([]);
		var options = Options("9.5.0") with { AdditionalProductIds = ["cloud-serverless"], PreviewDate = new DateOnly(2026, 11, 24) };

		var succeeded = await Service(Serverless).Build(collector, options, TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		var site = _fileSystem.Path.Join(_temp.FullName, "site");
		foreach (var release in new[] { "staging", "edge" })
			_fileSystem
				.File
				.ReadAllText(_fileSystem.Path.Join(site, "cloud-serverless", release, "index.html"))
				.Should()
				.Contain("If released today, this release would be dated")
				.And
				.Contain("2026-11-24");
		_fileSystem
			.File
			.ReadAllText(_fileSystem.Path.Join(site, "cloud-serverless", "index.html"))
			.Should()
			.Contain("dated 2026-11-24 if released today");
		_fileSystem.File.ReadAllText(_fileSystem.Path.Join(site, "kibana", "9.5.0", "index.html")).Should().NotContain("If released today");
	}

	[Test]
	public async Task Build_WithoutAPreviewDate_ThePreviewReleasesSayWhatTheyAreButNotWhenTheyWouldShip()
	{
		_ = WriteBundle("cloud-serverless", "kibana-cloud-serverless-staging.yaml", "staging", "Kibana staging change");
		var collector = new DiagnosticsCollector([]);
		var options = Options() with { AdditionalProductIds = ["cloud-serverless"] };

		var succeeded = await Service(Serverless).Build(collector, options, TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		_fileSystem
			.File
			.ReadAllText(_fileSystem.Path.Join(_temp.FullName, "site", "cloud-serverless", "staging", "index.html"))
			.Should()
			.Contain("running on the staging environment")
			.And
			.NotContain("If released today");
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

	private static readonly Product Hosted = CreateProduct("cloud-hosted", "Elastic Cloud Hosted", ReleaseNotesPath.OnRelease);

	private static readonly PreviewProblem[] DiagnosticsProblems =
	[
		new()
		{
			Product = "cloud-hosted",
			Release = "edge",
			Kind = PreviewProblemKind.MissingProductLabel,
			Owner = "elastic",
			Repo = "private-example",
			Number = 1048,
			Labels = "@Product:ECE, @Product:ECH"
		},
		new()
		{
			Product = "cloud-hosted",
			Release = "edge",
			Kind = PreviewProblemKind.MissingTypeLabel,
			Owner = "elastic",
			Repo = "private-example",
			Number = 1081
		},
		new()
		{
			Product = "kibana",
			Release = "9.5.0",
			Kind = PreviewProblemKind.MissingTypeLabel,
			Owner = "elastic",
			Repo = "kibana",
			Number = 7,
			Url = "https://github.com/elastic/kibana/pull/7",
			Title = "Fix [a link](https://evil.example) and *bold* | a pipe and x^2^",
			Labels = ">bug, >enhancement"
		}
	];

	[Test]
	public void Generate_WithProblems_ListsThemByProductReleaseAndKind()
	{
		var files = UnifiedReleaseNotesDocset.Generate(
			[new ProductVersions(Products[0], ["9.5.0"]), new ProductVersions(Hosted, ["edge"])],
			DiagnosticsProblems
		);

		files["diagnostics.md"]
			.Should()
			.MatchInlineSnapshot(
				"""
			---
			navigation_title: Diagnostics
			---

			# Release notes diagnostics

			This page lists the pull requests whose release notes end up in the wrong place because a label is missing.
			A pull request without a product label may show under the wrong product. One without an area label has no area.
			One without a type label shows under Other changes. Add the label to the pull request, and the next hourly rebuild of this site picks it up.

			3 pull requests lack a label. They are listed below, by product and release.

			:::{note}
			A pull request of a private repository is named by repository and number, without a link or a title.
			:::

			## Elastic Cloud Hosted

			### Elastic Cloud Hosted Edge

			:::{dropdown} No product label (1)
			:open:

			The release note shows under the product of its bundle, which may not be its product.

			Add one of these labels: `@Product:ECE`, `@Product:ECH`.

			- `elastic/private-example#1048`

			:::

			:::{dropdown} No type label (1)

			The release note shows under Other changes.

			- `elastic/private-example#1081`

			:::

			## Kibana

			### Kibana 9.5.0

			:::{dropdown} No type label (1)

			The release note shows under Other changes.

			Add one of these labels: `>bug`, `>enhancement`.

			- [elastic/kibana\#7](https://github.com/elastic/kibana/pull/7) Fix \[a link\]\(https\://evil.example\) and \*bold\* \| a pipe and x\^2\^

			:::
			"""
			);
	}

	[Test]
	public void Generate_TheDiagnosticsPage_UsesLineFeedsOnEveryPlatform()
	{
		var withProblems = UnifiedReleaseNotesDocset.Generate(
			[new ProductVersions(Products[0], ["9.5.0"])],
			DiagnosticsProblems
		)["diagnostics.md"];
		var withoutProblems = UnifiedReleaseNotesDocset.Generate([new ProductVersions(Products[0], ["9.5.0"])], [])["diagnostics.md"];

		withProblems.Should().NotContain("\r");
		withoutProblems.Should().NotContain("\r");
	}

	[Test]
	public void Generate_WithProblems_AddsTheDiagnosticsPageToTheTocAndTheIndex()
	{
		var files = UnifiedReleaseNotesDocset.Generate([new ProductVersions(Products[0], ["9.5.0"])], DiagnosticsProblems);

		files["docset.yml"].Should().Contain("  - file: index.md\n  - file: diagnostics.md\n  - toc: kibana");
		files["index.md"].Should().Contain("## Diagnostics").And.Contain("(diagnostics.md)");
	}

	[Test]
	public void Generate_WithoutProblems_HasNoDiagnosticsPage()
	{
		var files = UnifiedReleaseNotesDocset.Generate([new ProductVersions(Products[0], ["9.5.0"])]);

		files.Should().NotContainKey("diagnostics.md");
		files["docset.yml"].Should().NotContain("diagnostics");
		files["index.md"].Should().NotContain("Diagnostics");
	}

	[Test]
	public void Generate_WithNoProblemsFound_HasAPageThatSaysWhatItIsForAndThatAllIsWell()
	{
		var files = UnifiedReleaseNotesDocset.Generate([new ProductVersions(Products[0], ["9.5.0"])], []);

		files["diagnostics.md"]
			.Should()
			.Contain("This page lists the pull requests whose release notes end up in the wrong place because a label is missing.")
			.And
			.Contain("**No pull request lacks a label right now.** There is nothing to fix.")
			.And
			.NotContain("## ");
		files["docset.yml"].Should().Contain("  - file: diagnostics.md");
		files["index.md"].Should().Contain("## Diagnostics").And.Contain("No pull request lacks a label right now.");
	}

	[Test]
	public async Task Build_WithAProblemsFileThatFoundNothing_RendersAPageThatSaysSo()
	{
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		PreviewProblemsFile.Write(_fileSystem, _fileSystem.Path.Join(_temp.FullName, "bundles"), []);
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(
			collector,
			Options("9.5.0") with { ShowDiagnostics = true },
			TestContext.Current!.Execution.CancellationToken
		);

		succeeded.Should().BeTrue();
		collector.Errors.Should().Be(0);
		_fileSystem
			.File
			.ReadAllText(_fileSystem.Path.Join(_temp.FullName, "site", "diagnostics", "index.html"))
			.Should()
			.Contain("No pull request lacks a label right now.")
			.And
			.Contain("end up in the wrong place");
	}

	[Test]
	public async Task Build_WithAProblemsFile_RendersTheDiagnosticsPageAndLinksOnlyPublicPullRequests()
	{
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		PreviewProblemsFile.Write(_fileSystem, _fileSystem.Path.Join(_temp.FullName, "bundles"), DiagnosticsProblems);
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service(Hosted).Build(
			collector,
			Options("9.5.0") with { ShowDiagnostics = true },
			TestContext.Current!.Execution.CancellationToken
		);

		succeeded.Should().BeTrue();
		collector.Errors.Should().Be(0);
		var site = _fileSystem.Path.Join(_temp.FullName, "site");
		var page = _fileSystem.File.ReadAllText(_fileSystem.Path.Join(site, "diagnostics", "index.html"));
		page
			.Should()
			.Contain("kibana/pull/7")
			.And
			.Contain("elastic/private-example#1048")
			.And
			.NotContain("pull/1048")
			.And
			.NotContain("href=\"https://evil.example");
		page.Should().Contain("a pipe", "the title is text, so the pipe does not end the line").And.Contain("@Product:ECH");
		page.Should().Contain("x^2^", "a caret is text, not superscript").And.NotContain("<sup>");
		_fileSystem.File.ReadAllText(_fileSystem.Path.Join(site, "index.html")).Should().Contain("diagnostics");
	}

	[Test]
	public async Task Build_WithAProblemsFileButDiagnosticsNotRequested_LeavesThePrivatePullRequestsOffTheSite()
	{
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		PreviewProblemsFile.Write(_fileSystem, _fileSystem.Path.Join(_temp.FullName, "bundles"), DiagnosticsProblems);
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service(Hosted).Build(collector, Options("9.5.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(_temp.FullName, "site", "diagnostics")).Should().BeFalse();
		_fileSystem.File.ReadAllText(_fileSystem.Path.Join(_temp.FullName, "site", "index.html")).Should().NotContain("iagnostics");
	}

	[Test]
	public void Generate_ProblemsOfSeveralRepositoriesInOneRelease_EachRepositoryNamesItsOwnLabels()
	{
		PreviewProblem Problem(string repo, int number, string labels) =>
			new()
			{
				Product = "cloud-serverless",
				Release = "edge",
				Kind = PreviewProblemKind.MissingAreaLabel,
				Owner = "elastic",
				Repo = repo,
				Number = number,
				Url = $"https://github.com/elastic/{repo}/pull/{number}",
				Title = $"Change {number}",
				Labels = labels
			};

		var files = UnifiedReleaseNotesDocset.Generate(
			[new ProductVersions(Serverless, ["edge"])],
			[Problem("kibana", 1, ":Search"), Problem("elasticsearch", 2, ":Analytics, :Ingest")]
		);

		var page = files["diagnostics.md"].ReplaceLineEndings("\n");
		page.Should().Contain(
			"**elastic/elasticsearch**\n\nAdd one of these labels: `:Analytics`, `:Ingest`.\n\n- [elastic/elasticsearch\\#2]"
		);
		page.Should().Contain("**elastic/kibana**\n\nAdd one of these labels: `:Search`.\n\n- [elastic/kibana\\#1]");
	}

	[Test]
	public async Task Build_WithoutAProblemsFile_HasNoDiagnosticsPage()
	{
		_ = WriteBundle("kibana", "kibana-kibana-9.5.0.yaml", "9.5.0", "Kibana unified change");
		var collector = new DiagnosticsCollector([]);

		var succeeded = await Service().Build(collector, Options("9.5.0"), TestContext.Current!.Execution.CancellationToken);

		succeeded.Should().BeTrue();
		_fileSystem.Directory.Exists(_fileSystem.Path.Join(_temp.FullName, "site", "diagnostics")).Should().BeFalse();
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
