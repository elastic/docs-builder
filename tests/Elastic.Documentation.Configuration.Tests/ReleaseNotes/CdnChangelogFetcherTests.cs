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

	/// <summary>The shallow per-tree map probed once per run before any per-product registry fetch.</summary>
	private const string ShallowMapPath = "/bundle/registry.json";

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
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath => NotFound(),
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) =>
				Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": "abc123" } ] }"""),
			_ => Yaml(SampleBundle)
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();
		var fs = new MockFileSystem();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);

		// First call — should fetch from CDN
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		bundles.Should().ContainSingle();
		handler.CallCount.Should().Be(3, "shallow map probe + registry + bundle");

		// Second call — bundle should come from cache (only registry re-fetched; the map probe is memoized per run)
		var bundles2 = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		bundles2.Should().ContainSingle();
		handler.CallCount.Should().Be(4, "only registry fetched again; bundle served from memory cache");
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

		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath => NotFound(),
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) =>
				Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": "cached1" } ] }"""),
			_ => throw new InvalidOperationException("Should not fetch bundle from CDN")
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		bundles.Should().ContainSingle();
		handler.CallCount.Should().Be(2, "only the map probe and registry should be fetched; bundle served from disk");
		errors.Should().BeEmpty();
	}

	[Fact]
	public async Task FetchAsync_NullETag_AlwaysFetchesFromCdn()
	{
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath => NotFound(),
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) =>
				Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": null } ] }"""),
			_ => Yaml(SampleBundle)
		});
		var (errors, _, emitError, emitWarning) = Diagnostics();
		var fs = new MockFileSystem();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);

		// First call
		_ = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		handler.CallCount.Should().Be(3, "map probe + registry + bundle");

		// Second call — no caching, so bundle is fetched again (the map probe stays memoized)
		_ = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		handler.CallCount.Should().Be(5, "without ETag, both registry and bundle are fetched each time");
		errors.Should().BeEmpty();
	}

	[Fact]
	public async Task FetchAsync_ChangedETag_FetchesNewBundle()
	{
		var etag = "v1";
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath => NotFound(),
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) =>
				Json($$"""{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": "{{etag}}" } ] }"""),
			_ => Yaml(SampleBundle)
		});
		var (errors, _, emitError, emitWarning) = Diagnostics();
		var fs = new MockFileSystem();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);

		_ = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		handler.CallCount.Should().Be(3, "map probe + registry + bundle");

		// Simulate a new etag by creating a new fetcher (in real usage the registry returns a different etag)
		etag = "v2";
		_ = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		handler.CallCount.Should().Be(5, "new ETag means cache miss, bundle re-downloaded");
		errors.Should().BeEmpty();
	}

	// language=json
	private const string EsRegistryJson =
		"""{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": "abc123" } ] }""";

	private static string CacheFilePath(string cacheKey) =>
		Path.Join(Paths.ApplicationData.FullName, "changelog-bundles", cacheKey);

	/// <summary>Seeds the disk cache as a previous run with shallow token <paramref name="token"/> would have left it.</summary>
	private static MockFileSystem WarmCache(string token)
	{
		var fs = new MockFileSystem();
		fs.Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath("x"))!);
		fs.File.WriteAllText(CacheFilePath($"registry-elasticsearch-{token}"), EsRegistryJson);
		fs.File.WriteAllText(CacheFilePath("changelog-elasticsearch-9.3.0.yaml-abc123"), SampleBundle);
		return fs;
	}

	[Fact]
	public async Task FetchAsync_ShallowMapAbsent_FetchesRegistryAndBundleAsBefore()
	{
		// Pre-cutover CDNs have no bundle/registry.json: a 404 must degrade to the full per-product flow.
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath => NotFound(),
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) => Json(EsRegistryJson),
			_ => Yaml(SampleBundle)
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, new MockFileSystem(), handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle();
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/registry.json");
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/9.3.0.yaml");
	}

	[Fact]
	public async Task FetchAsync_ShallowMapUnparseable_FetchesRegistryAndBundleAsBefore()
	{
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath => Json("{ not valid json"),
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) => Json(EsRegistryJson),
			_ => Yaml(SampleBundle)
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, new MockFileSystem(), handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle();
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/registry.json");
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/9.3.0.yaml");
	}

	[Fact]
	public async Task FetchAsync_ShallowMapTimesOut_DegradesInsteadOfFaultingLaterFetches()
	{
		// HttpClient surfaces its own request timeout as a TaskCanceledException even though the
		// caller's token was never signaled. That must degrade like a transport failure — not fault
		// the shared per-base-URI map lookup and take every later product fetch down with it.
		var mapCalls = 0;
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath when Interlocked.Increment(ref mapCalls) == 1 =>
				throw new TaskCanceledException("The request timed out.", new TimeoutException(), new CancellationToken(canceled: true)),
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) => Json(EsRegistryJson),
			_ => Yaml(SampleBundle)
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, new MockFileSystem(), handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle("a map timeout must degrade to the full per-product registry fetch, not fault the whole run");
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/registry.json");
	}

	[Fact]
	public async Task FetchAsync_ShallowMapTimesOut_DoesNotPoisonLaterProductsInTheSameRun()
	{
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath => throw new TaskCanceledException("The request timed out.", new TimeoutException(), new CancellationToken(canceled: true)),
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) => Json(EsRegistryJson),
			_ => Yaml(SampleBundle)
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, new MockFileSystem(), handler);
		var first = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		var second = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		first.Should().ContainSingle();
		second.Should().ContainSingle("a prior timeout for this base URI must not poison later fetches sharing the cached map lookup");
	}

	[Fact]
	public async Task FetchAsync_CallerCancels_PropagatesCancellationRatherThanDegrading()
	{
		using var cts = new CancellationTokenSource();
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath == ShallowMapPath
			? throw new OperationCanceledException(cts.Token)
			: throw new InvalidOperationException($"Unexpected request: {req.RequestUri}"));

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, new MockFileSystem(), handler);
		await cts.CancelAsync();

		var act = () => fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, _ => { }, _ => { }, cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>("a genuinely canceled caller must not have its cancellation swallowed as a degrade-and-continue");
	}

	[Fact]
	public async Task FetchAsync_ShallowTokenMatchesWarmCache_MakesNoPerProductRequests()
	{
		var fs = WarmCache("tok1");
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath == ShallowMapPath
			? Json(/*lang=json,strict*/ """{ "elasticsearch": "tok1" }""")
			: throw new InvalidOperationException($"Unexpected per-folder request: {req.RequestUri}"));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle();
		bundles[0].Entries.Should().ContainSingle().Which.Title.Should().Be("Sample enhancement");
		handler.RequestedPaths.Should().Equal(ShallowMapPath);
	}

	[Fact]
	public async Task FetchAsync_ShallowTokenMismatch_FetchesRegistryAndRecordsNewToken()
	{
		var fs = WarmCache("tok-old");
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath => Json(/*lang=json,strict*/ """{ "elasticsearch": "tok-new" }"""),
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) => Json(EsRegistryJson),
			_ => Yaml(SampleBundle)
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);
		var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle();
		handler.RequestedPaths.Should().Contain("/bundle/elasticsearch/registry.json");
		fs.File.Exists(CacheFilePath("registry-elasticsearch-tok-new"))
			.Should().BeTrue("the fresh registry should be recorded under the new token for the next run");
	}

	[Fact]
	public async Task FetchAsync_ShallowTokenWithColdCache_FetchesAsUsualThenSkipsOnNextRun()
	{
		var fs = new MockFileSystem();
		var mapJson = /*lang=json,strict*/ """{ "elasticsearch": "tok1" }""";
		var coldHandler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath => Json(mapJson),
			var p when p.EndsWith("/registry.json", StringComparison.Ordinal) => Json(EsRegistryJson),
			_ => Yaml(SampleBundle)
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		// Cold cache: the token alone cannot satisfy a skip, so the flow is identical to today.
		using (var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, coldHandler))
		{
			var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
			bundles.Should().ContainSingle();
			coldHandler.RequestedPaths.Should().Contain("/bundle/elasticsearch/registry.json");
		}

		// Next run (new fetcher, same disk cache): the unchanged token skips every per-product request.
		var warmHandler = new StubHandler(req => req.RequestUri!.AbsolutePath == ShallowMapPath
			? Json(mapJson)
			: throw new InvalidOperationException($"Unexpected per-folder request: {req.RequestUri}"));
		using (var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, warmHandler))
		{
			var bundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
			bundles.Should().ContainSingle();
			warmHandler.RequestedPaths.Should().Equal(ShallowMapPath);
		}

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
	}

	[Fact]
	public async Task FetchAsync_ShallowMapPartialMatch_SkipsOnlyUnchangedFolders()
	{
		// One run over two products: elasticsearch has a warm cache and a matching token, kibana does
		// not — only kibana's registry and bundle may hit the CDN, and the map is probed exactly once.
		var fs = WarmCache("tok-es");
		var handler = new StubHandler(req => req.RequestUri!.AbsolutePath switch
		{
			ShallowMapPath => Json(/*lang=json,strict*/ """{ "elasticsearch": "tok-es", "kibana": "tok-kb" }"""),
			"/bundle/kibana/registry.json" =>
				Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "kibana", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0", "etag": "kb1" } ] }"""),
			"/bundle/kibana/9.3.0.yaml" => Yaml(SampleBundle),
			var p => throw new InvalidOperationException($"Unexpected per-folder request: {p}")
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, fs, handler);
		var esBundles = await fetcher.FetchAsync(BaseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		var kibanaBundles = await fetcher.FetchAsync(BaseUri, "kibana", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		esBundles.Should().ContainSingle();
		kibanaBundles.Should().ContainSingle();
		handler.RequestedPaths.Count(p => p == ShallowMapPath).Should().Be(1, "the map is fetched once per run");
		handler.RequestedPaths.Should().NotContain(p => p.StartsWith("/bundle/elasticsearch/", StringComparison.Ordinal));
		handler.RequestedPaths.Should().Contain("/bundle/kibana/registry.json");
		handler.RequestedPaths.Should().Contain("/bundle/kibana/9.3.0.yaml");
		fs.File.Exists(CacheFilePath("registry-kibana-tok-kb"))
			.Should().BeTrue("kibana's registry should be recorded under its token for the next run");
	}

	private static HttpResponseMessage NotFound() => new(HttpStatusCode.NotFound);

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
