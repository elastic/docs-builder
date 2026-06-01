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

	// A unique base per test keeps the fetcher's process-wide memoization cache from leaking between tests.
	private static Uri UniqueBase() => new($"https://cdn.example/{Guid.NewGuid():N}");

	private CdnChangelogFetcher CreateFetcher(StubHandler handler) =>
		new(NullLoggerFactory.Instance, new FileSystem(), handler);

	private static (List<string> Errors, List<string> Warnings, Action<string> EmitError, Action<string> EmitWarning) Diagnostics()
	{
		var errors = new List<string>();
		var warnings = new List<string>();
		return (errors, warnings, errors.Add, warnings.Add);
	}

	[Fact]
	public void Fetch_HappyPath_ReturnsBundlesFromRegistry()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0" } ] }""")
				: Yaml(SampleBundle));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		var bundles = CreateFetcher(handler).Fetch(UniqueBase(), "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle();
		bundles[0].Version.Should().Be("9.3.0");
		bundles[0].Entries.Should().ContainSingle().Which.Title.Should().Be("Sample enhancement");
	}

	[Fact]
	public void Fetch_WithVersion_OnlyDownloadsMatchingBundle()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.4.0.yaml", "target": "9.4.0" }, { "file": "9.3.0.yaml", "target": "9.3.0" } ] }""")
				: Yaml(SampleBundle));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		var bundles = CreateFetcher(handler).Fetch(UniqueBase(), "elasticsearch", version: "9.3.0", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle();
		handler.RequestedPaths.Should().NotContain(p => p.EndsWith("/9.4.0.yaml", StringComparison.Ordinal),
			"only the requested version should be downloaded");
		handler.RequestedPaths.Should().Contain(p => p.EndsWith("/9.3.0.yaml", StringComparison.Ordinal));
	}

	[Fact]
	public void Fetch_RegistryNotFound_EmitsErrorAndReturnsEmpty()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		var bundles = CreateFetcher(handler).Fetch(UniqueBase(), "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		bundles.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("registry");
	}

	[Fact]
	public void Fetch_BundleNotFound_EmitsWarningAndSkipsBundle()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0" } ] }""")
				: new HttpResponseMessage(HttpStatusCode.NotFound));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		var bundles = CreateFetcher(handler).Fetch(UniqueBase(), "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		bundles.Should().BeEmpty();
		errors.Should().BeEmpty();
		warnings.Should().ContainSingle().Which.Should().Contain("9.3.0.yaml");
	}

	[Fact]
	public void Fetch_SchemaVersionTooNew_EmitsError()
	{
		var handler = new StubHandler(_ =>
			Json(/*lang=json,strict*/ """{ "schema_version": 999, "product": "elasticsearch", "bundles": [] }"""));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		var bundles = CreateFetcher(handler).Fetch(UniqueBase(), "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		bundles.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("schema version");
	}

	[Fact]
	public void Fetch_SecondCallForSameProduct_UsesCache()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "9.3.0.yaml", "target": "9.3.0" } ] }""")
				: Yaml(SampleBundle));
		var (_, _, emitError, emitWarning) = Diagnostics();
		var fetcher = CreateFetcher(handler);
		var baseUri = UniqueBase();

		_ = fetcher.Fetch(baseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);
		var callsAfterFirst = handler.CallCount;
		_ = fetcher.Fetch(baseUri, "elasticsearch", version: null, emitError, emitWarning, TestContext.Current.CancellationToken);

		callsAfterFirst.Should().Be(2, "first fetch reads the registry and one bundle");
		handler.CallCount.Should().Be(callsAfterFirst, "the second fetch is served from the in-memory cache");
	}

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private static HttpResponseMessage Yaml(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/yaml") };

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public int CallCount { get; private set; }

		public List<string> RequestedPaths { get; } = [];

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return responder(request);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
