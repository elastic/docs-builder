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

	private static Uri Base() => new($"https://cdn.example/{Guid.NewGuid():N}");

	// A no-op sleeper keeps retry-exercising tests instant; a small attempt budget keeps them deterministic.
	private CdnChangelogEntryFetcher CreateFetcher(StubHandler handler, int maxAttempts = 3) =>
		new(NullLoggerFactory.Instance, handler, maxAttempts, sleep: (_, _) => { });

	private static (List<string> Errors, List<string> Warnings, Action<string> EmitError, Action<string> EmitWarning) Diagnostics()
	{
		var errors = new List<string>();
		var warnings = new List<string>();
		return (errors, warnings, errors.Add, warnings.Add);
	}

	[Fact]
	public void Fetch_HappyPath_ReturnsAllEntriesFromRegistry()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/changelog/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "1-a.yaml" }, { "file": "2-b.yaml" } ] }""")
				: Yaml(SampleEntry));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		var entries = CreateFetcher(handler).Fetch(Base(), "elasticsearch", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		entries.Select(e => e.FileName).Should().BeEquivalentTo("1-a.yaml", "2-b.yaml");
		entries.Should().OnlyContain(e => e.Content.Contains("Sample enhancement"));
		handler.RequestedPaths.Should().Contain(p => p.EndsWith("/elasticsearch/changelog/1-a.yaml", StringComparison.Ordinal));
	}

	[Fact]
	public void Fetch_RegistryNotFound_EmitsErrorAndReturnsEmpty()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		var entries = CreateFetcher(handler).Fetch(Base(), "elasticsearch", emitError, emitWarning, TestContext.Current.CancellationToken);

		entries.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("registry");
	}

	[Fact]
	public void Fetch_EntryMissingAfterRetries_EmitsErrorAndReturnsEmpty()
	{
		// A registry-listed entry that never appears on the CDN is retried, then escalated to an error
		// (not skipped) so the bundle fails rather than silently dropping a release entry.
		var handler = new StubHandler(req =>
		{
			if (req.RequestUri!.AbsolutePath.EndsWith("/changelog/registry.json", StringComparison.Ordinal))
				return Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "1-a.yaml" }, { "file": "2-missing.yaml" } ] }""");
			return req.RequestUri!.AbsolutePath.EndsWith("/2-missing.yaml", StringComparison.Ordinal)
				? new HttpResponseMessage(HttpStatusCode.NotFound)
				: Yaml(SampleEntry);
		});
		var (errors, _, emitError, emitWarning) = Diagnostics();

		var entries = CreateFetcher(handler, maxAttempts: 3).Fetch(Base(), "elasticsearch", emitError, emitWarning, TestContext.Current.CancellationToken);

		entries.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("2-missing.yaml");
		handler.RequestedPaths.Count(p => p.EndsWith("/2-missing.yaml", StringComparison.Ordinal))
			.Should().Be(3, "the missing entry should be attempted up to the retry budget before failing");
	}

	[Fact]
	public void Fetch_EntryRecoversAfterRetry_ReturnsEntry()
	{
		// The common scrub/propagation race: the first GET 404s, a retry succeeds. No error, no skip.
		var entryAttempts = 0;
		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/changelog/registry.json", StringComparison.Ordinal))
				return Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "1-a.yaml" } ] }""");
			if (path.EndsWith("/1-a.yaml", StringComparison.Ordinal))
				return Interlocked.Increment(ref entryAttempts) == 1
					? new HttpResponseMessage(HttpStatusCode.NotFound)
					: Yaml(SampleEntry);
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		var entries = CreateFetcher(handler).Fetch(Base(), "elasticsearch", emitError, emitWarning, TestContext.Current.CancellationToken);

		errors.Should().BeEmpty();
		warnings.Should().BeEmpty();
		entries.Select(e => e.FileName).Should().BeEquivalentTo("1-a.yaml");
		entryAttempts.Should().Be(2, "the first 404 should be retried and then succeed");
	}

	[Fact]
	public void Fetch_SchemaVersionTooNew_EmitsError()
	{
		var handler = new StubHandler(_ =>
			Json(/*lang=json,strict*/ """{ "schema_version": 999, "product": "elasticsearch", "bundles": [] }"""));
		var (errors, _, emitError, emitWarning) = Diagnostics();

		var entries = CreateFetcher(handler).Fetch(Base(), "elasticsearch", emitError, emitWarning, TestContext.Current.CancellationToken);

		entries.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("schema version");
	}

	[Fact]
	public void Fetch_UnsafeFileName_EmitsWarningAndSkips()
	{
		var handler = new StubHandler(req =>
			req.RequestUri!.AbsolutePath.EndsWith("/changelog/registry.json", StringComparison.Ordinal)
				? Json(/*lang=json,strict*/ """{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "../escape.yaml" }, { "file": "ok.yaml" } ] }""")
				: Yaml(SampleEntry));
		var (errors, warnings, emitError, emitWarning) = Diagnostics();

		var entries = CreateFetcher(handler).Fetch(Base(), "elasticsearch", emitError, emitWarning, TestContext.Current.CancellationToken);

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

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return responder(request);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
