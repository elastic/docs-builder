// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net;
using AwesomeAssertions;
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
