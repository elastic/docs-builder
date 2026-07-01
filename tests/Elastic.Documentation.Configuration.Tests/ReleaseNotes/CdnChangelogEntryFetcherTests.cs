// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

public class CdnChangelogEntryFetcherTests
{
	// language=yaml
	private const string SampleEntry = """
		title: Sample enhancement
		type: enhancement
		products:
		  - product: elasticsearch
		    target: 9.3.0
		""";

	private static readonly Uri BaseUri = new("https://cdn.example");

	// A no-op async sleeper keeps retry-exercising tests instant; a small attempt budget keeps them deterministic.
	private static CdnChangelogEntryFetcher CreateFetcher(StubHandler handler, int maxAttempts = 3) =>
		new(NullLoggerFactory.Instance, handler, maxAttempts, sleep: (_, _) => Task.CompletedTask);

	private static (List<string> Errors, List<string> Warnings, Action<string> EmitError, Action<string> EmitWarning) Diagnostics()
	{
		var errors = new List<string>();
		var warnings = new List<string>();
		return (errors, warnings, errors.Add, warnings.Add);
	}

	[Fact]
	public async Task FetchAsync_HappyPath_ReturnsAllEntriesFromRegistry()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "1-a.yaml" }, { "file": "2-b.yaml" } ] }""")
				: Yaml(SampleEntry));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var entries = await fetcher.FetchAsync(BaseUri, "elastic", "elasticsearch", "main", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		entries.Select(e => e.FileName).Should().BeEquivalentTo("1-a.yaml", "2-b.yaml");
		entries.Should().OnlyContain(e => e.Content.Contains("Sample enhancement"));
		// Artifact-root layout: entries and their registry live under changelog/{org}/{repo}/{branch}/...
		handler.RequestedPaths.Should().Contain("/changelog/elastic/elasticsearch/main/registry.json");
		handler.RequestedPaths.Should().Contain(p => p.EndsWith("/changelog/elastic/elasticsearch/main/1-a.yaml", StringComparison.Ordinal));
	}

	[Fact]
	public async Task FetchAsync_BranchWithSlashes_KeepsBranchSeparatorsInPath()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elastic/elasticsearch/feature/foo", "bundles": [ { "file": "1-a.yaml" } ] }""")
				: Yaml(SampleEntry));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var entries = await fetcher.FetchAsync(BaseUri, "elastic", "elasticsearch", "feature/foo", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		entries.Select(e => e.FileName).Should().BeEquivalentTo("1-a.yaml");
		// The branch's '/' stays a real path separator (not percent-encoded into one segment).
		handler.RequestedPaths.Should().Contain("/changelog/elastic/elasticsearch/feature/foo/registry.json");
		handler.RequestedPaths.Should().Contain(p => p.EndsWith("/changelog/elastic/elasticsearch/feature/foo/1-a.yaml", StringComparison.Ordinal));
	}

	[Theory]
	[InlineData("..")]
	[InlineData("feature/..")]
	[InlineData("")]
	public async Task FetchAsync_UnsafeBranch_EmitsErrorAndDoesNotHitCdn(string branch)
	{
		// A traversal/empty branch segment must be rejected before any request, so URI normalization
		// can't redirect the fetch to a different pool.
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var entries = await fetcher.FetchAsync(BaseUri, "elastic", "elasticsearch", branch, emitError, emitWarning, TestContext.Current.CancellationToken);

		entries.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("Invalid changelog pool");
		handler.RequestedPaths.Should().BeEmpty("validation must happen before any CDN request");
	}

	[Fact]
	public async Task FetchAsync_RegistryNotFound_EmitsErrorAndReturnsEmpty()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var entries = await fetcher.FetchAsync(BaseUri, "elastic", "elasticsearch", "main", emitError, emitWarning, TestContext.Current.CancellationToken);

		entries.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("registry");
	}

	[Fact]
	public async Task FetchAsync_EntryMissingAfterRetries_EmitsErrorAndReturnsEmpty()
	{
		// A registry-listed entry that never appears on the CDN is retried, then escalated to an error
		// (not skipped) so the bundle fails rather than silently dropping a release entry.
		var handler = new StubHandler(req =>
		{
			if (req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal))
				return Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "1-a.yaml" }, { "file": "2-missing.yaml" } ] }""");
			return req.RequestUri!.AbsolutePath.EndsWith("/2-missing.yaml", StringComparison.Ordinal)
				? new HttpResponseMessage(HttpStatusCode.NotFound)
				: Yaml(SampleEntry);
		});
		var (errors, _, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler, maxAttempts: 3);
		var entries = await fetcher.FetchAsync(BaseUri, "elastic", "elasticsearch", "main", emitError, emitWarning, TestContext.Current.CancellationToken);

		entries.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("2-missing.yaml");
		handler.RequestedPaths.Count(p => p.EndsWith("/2-missing.yaml", StringComparison.Ordinal))
			.Should().Be(3, "the missing entry should be attempted up to the retry budget before failing");
	}

	[Fact]
	public async Task FetchAsync_EntryRecoversAfterRetry_ReturnsEntry()
	{
		// The common scrub/propagation race: the first GET 404s, a retry succeeds. No error, no skip.
		var entryAttempts = 0;
		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "1-a.yaml" } ] }""");
			if (path.EndsWith("/1-a.yaml", StringComparison.Ordinal))
				return Interlocked.Increment(ref entryAttempts) == 1
					? new HttpResponseMessage(HttpStatusCode.NotFound)
					: Yaml(SampleEntry);
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var entries = await fetcher.FetchAsync(BaseUri, "elastic", "elasticsearch", "main", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		entries.Select(e => e.FileName).Should().BeEquivalentTo("1-a.yaml");
		entryAttempts.Should().Be(2, "the first 404 should be retried and then succeed");
	}

	[Fact]
	public async Task FetchAsync_SchemaVersionTooNew_EmitsError()
	{
		var handler = new StubHandler(_ =>
			Json(/*lang=json,strict*/ """{ "schema_version": 999, "product": "elasticsearch", "bundles": [] }"""));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var entries = await fetcher.FetchAsync(BaseUri, "elastic", "elasticsearch", "main", emitError, emitWarning, TestContext.Current.CancellationToken);

		entries.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("schema version");
	}

	[Fact]
	public async Task FetchAsync_UnsafeFileName_EmitsWarningAndSkips()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "../escape.yaml" }, { "file": "ok.yaml" } ] }""")
				: Yaml(SampleEntry));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		using var fetcher = CreateFetcher(handler);
		var entries = await fetcher.FetchAsync(BaseUri, "elastic", "elasticsearch", "main", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		entries.Select(e => e.FileName).Should().BeEquivalentTo("ok.yaml");
		warnings.Should().ContainSingle().Which.Should().Contain("escape.yaml");
	}

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private static HttpResponseMessage Yaml(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/yaml") };

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public List<string> RequestedPaths { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return Task.FromResult(responder(request));
		}
	}
}
