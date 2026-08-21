// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

public class CdnChangelogFetcherTests
{
	// language=yaml
	private const string SampleBundle = """
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    repo: elasticsearch
		    owner: elastic
		entries:
		  - type: enhancement
		    title: Sample enhancement
		""";

	private static readonly Uri BaseUri = new("https://cdn.example");

	private static CdnChangelogFetcher CreateFetcher(StubHandler handler) =>
		new(NullLoggerFactory.Instance, new FileSystem(), handler);

	private static (List<string> Errors, List<string> Warnings, Action<string> EmitError, Action<string> EmitWarning) Diagnostics()
	{
		var errors = new List<string>();
		var warnings = new List<string>();
		return (errors, warnings, errors.Add, warnings.Add);
	}

	[Fact]
	public async Task FetchAsync_HappyPath_ReturnsBundlesFromRegistry()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0" } ] }""")
				: Yaml(SampleBundle));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle();
		bundles[0].Version.Should().Be("9.3.0");
		bundles[0].Entries.Should().ContainSingle().Which.Title.Should().Be("Sample enhancement");

		// Artifact-root layout: bundles and their registry live under bundle/{product}/...
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/registry.json");
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/9.3.0.yaml");
	}

	[Fact]
	public async Task FetchAsync_WithVersion_OnlyDownloadsMatchingBundle()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.4.0.yaml", "target": "9.4.0" }, { "file": "9.3.0.yaml", "target": "9.3.0" } ] }""")
				: Yaml(SampleBundle));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: "9.3.0", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle();
		handler.RequestedPaths.Should().NotContain(p => p.EndsWith("/9.4.0.yaml", StringComparison.Ordinal),
			"only the requested version should be downloaded");
		handler.RequestedPaths.Should().Contain(p => p.EndsWith("/9.3.0.yaml", StringComparison.Ordinal));
	}

	[Fact]
	public async Task FetchAsync_WithVersion_DownloadsAmendCarryingParentProducts()
	{
		// Amend materialized by a current docs-builder: it carries the parent's complete products,
		// so its registry entry has a target and matches the version on its own.
		// language=yaml
		const string amendBundle = """
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    repo: elasticsearch
			    owner: elastic
			entries:
			  - type: bug-fix
			    title: Amended fix
			""";
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) =>
				Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.4.0.yaml", "target": "9.4.0" }, { "file": "9.3.0.yaml", "target": "9.3.0" }, { "file": "9.3.0.amend-1.yaml", "target": "9.3.0" } ] }"""),
			var p when p.EndsWith("/9.3.0.amend-1.yaml", StringComparison.Ordinal) => Yaml(amendBundle),
			_ => Yaml(SampleBundle)
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: "9.3.0", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/9.3.0.amend-1.yaml");
		handler.RequestedPaths.Should().NotContain(p => p.EndsWith("/9.4.0.yaml", StringComparison.Ordinal));

		bundles.Should().ContainSingle("the amend merges into its parent");
		bundles[0].Version.Should().Be("9.3.0");
		bundles[0].Entries.Select(e => e.Title)
			.Should().BeEquivalentTo("Sample enhancement", "Amended fix");
	}

	[Fact]
	public async Task FetchAsync_WithVersion_DownloadsLegacyAmendWhoseParentMatches()
	{
		// Amend published before products were copied from the parent: null registry target and a
		// file name the version can never equal. It must still be fetched when its parent matches.
		// language=yaml
		const string amendBundle = """
			entries:
			  - type: bug-fix
			    title: Amended fix
			""";
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) =>
				Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0" }, { "file": "9.3.0.amend-1.yaml", "target": null } ] }"""),
			var p when p.EndsWith("/9.3.0.amend-1.yaml", StringComparison.Ordinal) => Yaml(amendBundle),
			_ => Yaml(SampleBundle)
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: "9.3.0", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/9.3.0.amend-1.yaml");

		bundles.Should().ContainSingle();
		bundles[0].Entries.Select(e => e.Title)
			.Should().BeEquivalentTo("Sample enhancement", "Amended fix");
	}

	[Fact]
	public async Task FetchAsync_WithOtherVersion_DoesNotDownloadUnrelatedAmend()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.4.0.yaml", "target": "9.4.0" }, { "file": "9.3.0.yaml", "target": "9.3.0" }, { "file": "9.3.0.amend-1.yaml", "target": null } ] }""")
				: Yaml(SampleBundle));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		_ = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: "9.4.0", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		handler.RequestedPaths.Should().Contain(p => p.EndsWith("/9.4.0.yaml", StringComparison.Ordinal));
		handler.RequestedPaths.Should().NotContain(p => p.EndsWith("/9.3.0.yaml", StringComparison.Ordinal));
		handler.RequestedPaths.Should().NotContain(p => p.EndsWith("/9.3.0.amend-1.yaml", StringComparison.Ordinal));
	}

	[Fact]
	public async Task FetchAsync_WithVersion_FileIdentityRetractionApplies()
	{
		// A resolved parent whose entries carry file identities, and a legacy amend that retracts one
		// of them by file identity: the version-filtered fetch must return the amended result.
		// language=yaml
		const string parentBundle = """
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    repo: elasticsearch
			    owner: elastic
			entries:
			  - file:
			      name: 1-old.yaml
			      checksum: deadbeef
			    type: bug-fix
			    title: Retracted fix
			  - file:
			      name: 2-keep.yaml
			      checksum: c0ffee
			    type: enhancement
			    title: Kept enhancement
			""";
		// language=yaml
		const string amendBundle = """
			exclude-entries:
			  - file:
			      name: 1-old.yaml
			      checksum: deadbeef
			""";
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) =>
				Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0" }, { "file": "9.3.0.amend-1.yaml", "target": null } ] }"""),
			var p when p.EndsWith("/9.3.0.amend-1.yaml", StringComparison.Ordinal) => Yaml(amendBundle),
			_ => Yaml(parentBundle)
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: "9.3.0", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle();
		bundles[0].Entries.Select(e => e.Title)
			.Should().BeEquivalentTo(["Kept enhancement"], "the amend retracts the entry by file identity");
	}

	[Fact]
	public async Task FetchAsync_RegistryNotFound_EmitsErrorAndReturnsEmpty()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		bundles.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("registry");
	}

	[Fact]
	public async Task FetchAsync_BundleNotFound_EmitsWarningAndSkipsBundle()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0" } ] }""")
				: new HttpResponseMessage(HttpStatusCode.NotFound));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		bundles.Should().BeEmpty();
		errors.Should().BeEmpty();
		warnings.Should().ContainSingle().Which.Should().Contain("9.3.0.yaml");
	}

	[Fact]
	public async Task FetchAsync_SchemaVersionTooNew_EmitsError()
	{
		var handler = new StubHandler(_ =>
			Json(/*lang=json,strict*/ """{ "schema_version": 999, "product": "elasticsearch", "bundles": [] }"""));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		bundles.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("schema version");
	}

	[Theory]
	[InlineData("")]
	[InlineData(".")]
	[InlineData("..")]
	// Products never contain dots or spaces; the producer would have refused to upload such a bundle key.
	[InlineData("foo.bar")]
	[InlineData("elastic search")]
	public async Task FetchAsync_InvalidProduct_EmitsErrorAndDoesNotHitCdn(string product)
	{
		// A malformed product must be rejected before any request, mirroring the entry fetcher's pool
		// validation, so URI normalization can't redirect the fetch outside the bundle layout.
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var bundles = await fetcher.FetchAsync(BaseUri, product, version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		bundles.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("Invalid changelog product");
		handler.RequestedPaths.Should().BeEmpty("validation must happen before any CDN request");
	}

	[Fact]
	public async Task FetchAsync_WithETag_UsesCachedBundleOnSecondCall()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": "abc123" } ] }""")
				: Yaml(SampleBundle));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();
		var fs = new MockFileSystem();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);

		// First call — should fetch from CDN
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		bundles.Should().ContainSingle();
		handler.CallCount.Should().Be(2, "registry + bundle");

		// Second call — bundle should come from cache (only registry re-fetched)
		var bundles2 = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		bundles2.Should().ContainSingle();
		handler.CallCount.Should().Be(3, "only registry fetched again; bundle served from memory cache");
		errors.Should().BeEmpty();
	}

	[Fact]
	public async Task FetchAsync_WithETag_WritesCacheToDisk()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": "deadbeef" } ] }""")
				: Yaml(SampleBundle));
		var (errors, _, emitError, emitWarning) = Diagnostics();
		var fs = new MockFileSystem();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);
		_ = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		var expectedPath = Path.Join(Paths.ApplicationData.FullName, "changelog-bundles", "changelog-elasticsearch-9.3.0.yaml-deadbeef");
		fs.File.Exists(expectedPath).Should().BeTrue("bundle should be written to disk cache");
		errors.Should().BeEmpty();
	}

	[Fact]
	public async Task FetchAsync_WithETag_ReadsCacheFromDisk()
	{
		// Pre-populate the disk cache
		var fs = new MockFileSystem();
		var cachePath = Path.Join(Paths.ApplicationData.FullName, "changelog-bundles", "changelog-elasticsearch-9.3.0.yaml-cached1");
		fs.Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
		fs.File.WriteAllText(cachePath, SampleBundle);

		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": "cached1" } ] }""")
				: throw new InvalidOperationException("Should not fetch bundle from CDN"));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		bundles.Should().ContainSingle();
		handler.CallCount.Should().Be(1, "only registry should be fetched; bundle served from disk");
		errors.Should().BeEmpty();
	}

	[Fact]
	public async Task FetchNamedBundleAsync_DownloadsParentAndSiblingAmendsOnly()
	{
		// language=yaml
		const string amendBundle = """
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - type: bug-fix
			    title: Amended fix
			""";
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) =>
				Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.4.0.yaml", "target": "9.4.0" }, { "file": "9.3.0.yaml", "target": "9.3.0" }, { "file": "9.3.0.amend-1.yaml", "target": "9.3.0" } ] }"""),
			var p when p.EndsWith("/9.3.0.amend-1.yaml", StringComparison.Ordinal) => Yaml(amendBundle),
			_ => Yaml(SampleBundle)
		});
		var (errors, _, emitError, _) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var named = await fetcher.FetchNamedBundleAsync(BaseUri, "elasticsearch", "9.3.0.yaml", emitError, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		named.Should().NotBeNull();
		named!.Value.FileName.Should().Be("9.3.0.yaml");
		named.Value.Content.Should().Contain("Sample enhancement");
		named.Value.AmendSidecars.Should().ContainSingle().Which.FileName.Should().Be("9.3.0.amend-1.yaml");
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/registry.json");
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/9.3.0.yaml");
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/9.3.0.amend-1.yaml");
		handler.RequestedPaths.Should().NotContain(p => p.EndsWith("/9.4.0.yaml", StringComparison.Ordinal));
	}

	[Fact]
	public async Task FetchNamedBundleAsync_UnknownFile_EmitsErrorWithoutDownloadingYaml()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0" } ] }""")
				: Yaml(SampleBundle));
		var (errors, _, emitError, _) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var named = await fetcher.FetchNamedBundleAsync(BaseUri, "elasticsearch", "missing.yaml", emitError, TestContext.Current.CancellationToken);

		named.Should().BeNull();
		errors.Should().ContainSingle(e => e.Contains("missing.yaml") && e.Contains("not listed"));
		handler.RequestedPaths.Should().Equal("/bundle/elasticsearch/registry.json");
	}

	[Fact]
	public async Task FetchAsync_NullETag_AlwaysFetchesFromCdn()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": null } ] }""")
				: Yaml(SampleBundle));
		var (errors, _, emitError, emitWarning) = Diagnostics();
		var fs = new MockFileSystem();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);

		// First call
		_ = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		handler.CallCount.Should().Be(2);

		// Second call — no caching, so bundle is fetched again
		_ = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		handler.CallCount.Should().Be(4, "without ETag, both registry and bundle are fetched each time");
		errors.Should().BeEmpty();
	}

	[Fact]
	public async Task FetchAsync_ChangedETag_FetchesNewBundle()
	{
		var etag = "v1";
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json($$"""{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": "{{etag}}" } ] }""")
				: Yaml(SampleBundle));
		var (errors, _, emitError, emitWarning) = Diagnostics();
		var fs = new MockFileSystem();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);

		_ = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		handler.CallCount.Should().Be(2);

		// Simulate a new etag by creating a new fetcher (in real usage the registry returns a different etag)
		etag = "v2";
		_ = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		handler.CallCount.Should().Be(4, "new ETag means cache miss, bundle re-downloaded");
		errors.Should().BeEmpty();
	}

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private static HttpResponseMessage Yaml(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/yaml") };

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public int CallCount { get; private set; }

		public List<string> RequestedPaths { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return Task.FromResult(responder(request));
		}
	}
}
