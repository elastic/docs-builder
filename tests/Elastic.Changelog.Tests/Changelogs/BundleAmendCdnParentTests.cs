// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundleAmendCdnParentTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	// language=yaml
	private const string ExistingEntry =
		"""
		title: Existing feature
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.3.0
		""";

	// language=yaml
	private const string LateEntry =
		"""
		title: Late addition
		type: enhancement
		products:
		  - product: elasticsearch
		    target: 9.3.0
		""";

	[Fact]
	public async Task Amend_CdnParent_WritesAmend1UnderOutput()
	{
		var outputDir = CreateDir();
		var handler = CombinedHandler(parentYaml: ParentBundleYaml("existing.yaml", ExistingEntry), lateYaml: LateEntry);
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments
			{
				BundlePath = "/bundle/elasticsearch/9.3.0.yaml",
				AddFiles = ["/changelog/elastic/elasticsearch/main/late.yaml"],
				Output = outputDir
			},
			TestContext.Current.CancellationToken
		);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/registry.json");
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/9.3.0.yaml");
		handler.RequestedPaths.Should().NotContain(p => p.EndsWith("/9.4.0.yaml", StringComparison.Ordinal));
		var amendPath = FileSystem.Path.Join(outputDir, "9.3.0.amend-1.yaml");
		FileSystem.File.Exists(amendPath).Should().BeTrue();
		var amend = await FileSystem.File.ReadAllTextAsync(amendPath, TestContext.Current.CancellationToken);
		amend.Should().Contain("title: Late addition");
		amend.Should().Contain("name: late.yaml");
	}

	[Fact]
	public async Task Amend_CdnParent_ExistingCdnAmend_WritesAmend2()
	{
		var outputDir = CreateDir();
		var handler = CombinedHandler(
			parentYaml: ParentBundleYaml("existing.yaml", ExistingEntry),
			lateYaml: LateEntry,
			cdnAmendYaml: AmendSidecarYaml("prior.yaml")
		);
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments { BundlePath = "/bundle/elasticsearch/9.3.0.yaml", AddFiles = ["late.yaml"], Output = outputDir },
			TestContext.Current.CancellationToken
		);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/9.3.0.amend-1.yaml");
		FileSystem.File.Exists(FileSystem.Path.Join(outputDir, "9.3.0.amend-1.yaml")).Should().BeFalse();
		FileSystem.File.Exists(FileSystem.Path.Join(outputDir, "9.3.0.amend-2.yaml")).Should().BeTrue();
	}

	[Fact]
	public async Task Amend_CdnParent_LocalSiblingAmend_WritesAmend2()
	{
		var outputDir = CreateDir();
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(outputDir, "9.3.0.amend-1.yaml"),
			AmendSidecarYaml("local-prior.yaml"),
			TestContext.Current.CancellationToken
		);
		var handler = CombinedHandler(parentYaml: ParentBundleYaml("existing.yaml", ExistingEntry), lateYaml: LateEntry);
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments { BundlePath = "/bundle/elasticsearch/9.3.0.yaml", AddFiles = ["late.yaml"], Output = outputDir },
			TestContext.Current.CancellationToken
		);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		FileSystem.File.Exists(FileSystem.Path.Join(outputDir, "9.3.0.amend-2.yaml")).Should().BeTrue();
	}

	[Fact]
	public async Task Amend_LocalParent_StillWritesBesideParent_IgnoringOutput()
	{
		var bundlePath = await WriteLocalParentAsync();
		var outputDir = CreateDir();
		var localDir = CreateDir();
		var localFile = FileSystem.Path.Join(localDir, "late.yaml");
		await FileSystem.File.WriteAllTextAsync(localFile, LateEntry, TestContext.Current.CancellationToken);
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments { BundlePath = bundlePath, AddFiles = [localFile], ForceLocal = true, Output = outputDir },
			TestContext.Current.CancellationToken
		);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		handler.RequestedPaths.Should().BeEmpty("a local parent with --force-local must not reach the CDN");
		FileSystem.File.Exists(FileSystem.Path.Join(FileSystem.Path.GetDirectoryName(bundlePath), "bundle.amend-1.yaml")).Should().BeTrue();
		FileSystem.Directory.GetFiles(outputDir).Should().BeEmpty();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Warning && d.Message.Contains("--output is ignored"));
	}

	[Fact]
	public async Task Amend_LocalParent_MissingFile_FailsWithoutWriting()
	{
		var outputDir = CreateDir();
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments
			{
				BundlePath = FileSystem.Path.Join(outputDir, "missing.yaml"),
				AddFiles = ["late.yaml"],
				Output = outputDir
			},
			TestContext.Current.CancellationToken
		);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("does not exist"));
		FileSystem.Directory.GetFiles(outputDir).Should().BeEmpty();
	}

	[Fact]
	public async Task Amend_CdnParent_UnknownBundleFile_FailsWithoutWriting()
	{
		var outputDir = CreateDir();
		var handler = CombinedHandler(parentYaml: ParentBundleYaml("existing.yaml", ExistingEntry), lateYaml: LateEntry);
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments { BundlePath = "/bundle/elasticsearch/missing.yaml", AddFiles = ["late.yaml"], Output = outputDir },
			TestContext.Current.CancellationToken
		);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("not listed"));
		FileSystem.Directory.GetFiles(outputDir).Should().BeEmpty();
	}

	[Fact]
	public async Task Amend_CdnParent_AmendSidecarAsParent_FailsWithoutWriting()
	{
		var outputDir = CreateDir();
		var handler = CombinedHandler(parentYaml: ParentBundleYaml("existing.yaml", ExistingEntry), lateYaml: LateEntry);
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments
			{
				BundlePath = "/bundle/elasticsearch/9.3.0.amend-1.yaml",
				AddFiles = ["late.yaml"],
				Output = outputDir
			},
			TestContext.Current.CancellationToken
		);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("amend sidecar"));
		handler.RequestedPaths.Should().BeEmpty();
		FileSystem.Directory.GetFiles(outputDir).Should().BeEmpty();
	}

	[Fact]
	public void DiscoverAmendFiles_IgnoresSiblingSidecarWithDifferentExtension()
	{
		var bundleDir = CreateDir();
		var parentPath = FileSystem.Path.Join(bundleDir, "9.3.0.yml");
		FileSystem.File.WriteAllText(parentPath, "products: []\nentries: []\n");
		// Same stem, wrong extension: belongs to a different (same-stem) .yaml bundle and must not
		// be treated as this .yml parent's amend history.
		FileSystem.File.WriteAllText(FileSystem.Path.Join(bundleDir, "9.3.0.amend-2.yaml"), "products: []\nentries: []\n");
		FileSystem.File.WriteAllText(FileSystem.Path.Join(bundleDir, "9.3.0.amend-1.yml"), "products: []\nentries: []\n");

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, parentPath);

		amendFiles.Should().ContainSingle().Which.Should().EndWith("9.3.0.amend-1.yml");
	}

	[Fact]
	public async Task Amend_CdnParent_YmlExtension_IgnoresMismatchedExtensionSidecar_WritesAmend1()
	{
		var outputDir = CreateDir();
		// A stray same-stem .yaml sidecar in the output directory must not be mistaken for existing
		// history of the .yml parent (it would otherwise bump the next amend number to 3 and merge
		// its entries/exclusions in).
		FileSystem.File.WriteAllText(FileSystem.Path.Join(outputDir, "9.3.0.amend-2.yaml"), AmendSidecarYaml("unrelated.yaml"));
		var handler = CombinedHandler(
			parentYaml: ParentBundleYaml("existing.yaml", ExistingEntry),
			lateYaml: LateEntry,
			parentExtension: ".yml"
		);
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments { BundlePath = "/bundle/elasticsearch/9.3.0.yml", AddFiles = ["late.yaml"], Output = outputDir },
			TestContext.Current.CancellationToken
		);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		var amendPath = FileSystem.Path.Join(outputDir, "9.3.0.amend-1.yml");
		FileSystem.File.Exists(amendPath).Should().BeTrue();
		var amend = await FileSystem.File.ReadAllTextAsync(amendPath, TestContext.Current.CancellationToken);
		amend.Should().NotContain("unrelated.yaml", "the mismatched-extension sidecar must not be merged in");
	}

	[Fact]
	public async Task Amend_LocalFileMatchingLocatorShape_PrefersCdnParent()
	{
		var outputDir = CreateDir();
		var shadowedDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, "bundle", "elasticsearch");
		FileSystem.Directory.CreateDirectory(shadowedDir);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(shadowedDir, "9.3.0.yaml"),
			"this on-disk file must not be read; locator syntax takes precedence",
			TestContext.Current.CancellationToken
		);
		var handler = CombinedHandler(parentYaml: ParentBundleYaml("existing.yaml", ExistingEntry), lateYaml: LateEntry);
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments { BundlePath = "bundle/elasticsearch/9.3.0.yaml", AddFiles = ["late.yaml"], Output = outputDir },
			TestContext.Current.CancellationToken
		);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		handler
			.RequestedPaths
			.Should()
			.Contain(
				"/bundle/elasticsearch/registry.json",
				"a path matching the locator shape must resolve via the CDN even if a local file exists at that relative path"
			);
		FileSystem.File.Exists(FileSystem.Path.Join(outputDir, "9.3.0.amend-1.yaml")).Should().BeTrue();
	}

	[Fact]
	public async Task Amend_LocalParent_Symlink_FailsWithoutWriting()
	{
		var bundleDir = CreateDir();
		var realBundlePath = FileSystem.Path.Join(bundleDir, "real-bundle.yaml");
		await FileSystem.File.WriteAllTextAsync(
			realBundlePath,
			ParentBundleYaml("existing.yaml", ExistingEntry),
			TestContext.Current.CancellationToken
		);
		var symlinkPath = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		FileSystem.File.CreateSymbolicLink(symlinkPath, realBundlePath);
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments { BundlePath = symlinkPath, AddFiles = ["late.yaml"], ForceLocal = true },
			TestContext.Current.CancellationToken
		);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("symlink"));
		handler.RequestedPaths.Should().BeEmpty();
	}

	[Fact]
	public async Task Amend_BadPathShape_FailsWithoutWriting()
	{
		var outputDir = CreateDir();
		var handler = CombinedHandler(parentYaml: ParentBundleYaml("existing.yaml", ExistingEntry), lateYaml: LateEntry);
		var service = Service(handler);

		var result = await service.AmendBundle(
			Collector,
			new AmendBundleArguments
			{
				BundlePath = "/changelog/elastic/elasticsearch/main/9.3.0.yaml",
				AddFiles = ["late.yaml"],
				Output = outputDir
			},
			TestContext.Current.CancellationToken
		);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("/bundle/{product}/{file}.yaml"));
		handler.RequestedPaths.Should().BeEmpty();
		FileSystem.Directory.GetFiles(outputDir).Should().BeEmpty();
	}

	private ChangelogBundleAmendService Service(StubHandler handler)
	{
		var entryFetcher = new CdnChangelogEntryFetcher(LoggerFactory, handler, sleep: (_, _) => Task.CompletedTask);
		var bundleFetcher = new CdnChangelogFetcher(LoggerFactory, FileSystem, handler);
		return new ChangelogBundleAmendService(LoggerFactory, FileSystem, entryFetcher: entryFetcher, bundleFetcher: bundleFetcher);
	}

	private string CreateDir()
	{
		var dir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir);
		return dir;
	}

	private async Task<string> WriteLocalParentAsync()
	{
		var bundleDir = CreateDir();
		var bundlePath = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		await FileSystem.File.WriteAllTextAsync(
			bundlePath,
			ParentBundleYaml("existing.yaml", ExistingEntry),
			TestContext.Current.CancellationToken
		);
		return bundlePath;
	}

	private string ParentBundleYaml(string fileName, string changelogYaml)
	{
		var checksum = ComputeSha1(changelogYaml);
		return $"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  repo: elasticsearch
			  owner: elastic
			entries:
			- file:
			    name: {fileName}
			    checksum: {checksum}
			  type: feature
			  title: Existing feature
			""";
	}

	private static string AmendSidecarYaml(string entryName) =>
		$"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		  repo: elasticsearch
		  owner: elastic
		entries:
		- file:
		    name: {entryName}
		    checksum: placeholder
		  type: bug-fix
		  title: Prior amend
		""";

	private static StubHandler CombinedHandler(
		string parentYaml,
		string lateYaml,
		string? cdnAmendYaml = null,
		string parentExtension = ".yaml"
	)
	{
		var parentFile = $"9.3.0{parentExtension}";
		var amendFile = $"9.3.0.amend-1{parentExtension}";
		var bundleRegistry = cdnAmendYaml is null
			? /*lang=json,strict*/  $$"""{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.4.0.yaml", "target": "9.4.0" }, { "file": "{{parentFile}}", "target": "9.3.0" } ] }"""
			: /*lang=json,strict*/  $$"""{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.4.0.yaml", "target": "9.4.0" }, { "file": "{{parentFile}}", "target": "9.3.0" }, { "file": "{{amendFile}}", "target": "9.3.0" } ] }""";
		var poolRegistry = /*lang=json,strict*/
			"""{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "late.yaml" } ] }""";
		return new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.Contains("/bundle/", StringComparison.Ordinal) && path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Json(bundleRegistry);
			if (path.Contains("/changelog/", StringComparison.Ordinal) && path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Json(poolRegistry);
			if (path.EndsWith("/" + parentFile, StringComparison.Ordinal))
				return Yaml(parentYaml);
			if (path.EndsWith("/" + amendFile, StringComparison.Ordinal) && cdnAmendYaml is not null)
				return Yaml(cdnAmendYaml);
			if (path.EndsWith("/late.yaml", StringComparison.Ordinal))
				return Yaml(lateYaml);
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
	}

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private static HttpResponseMessage Yaml(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/yaml") };

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public List<string> RequestedPaths { get; } = [];

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return responder(request);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
