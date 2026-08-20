// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;

namespace Elastic.ApiExplorer.Tests;

public class VersionIndexClientTests
{
	private static readonly Uri BaseUri = new("https://cdn.example/");

	private static VersionIndexClient CreateClient(StubHandler handler, int maxAttempts = 3) =>
		new(BaseUri, handler, maxAttempts, sleep: (_, _) => Task.CompletedTask);

	private static GitCheckoutInformation GitFor(string? remote) =>
		new() { Branch = "main", Remote = remote ?? "unavailable", Ref = "refs/heads/main" };

	private static ResolvedApiConfiguration ApiConfig(IFileInfo? localSpecFile = null, string? repository = null) =>
		new()
		{
			ProductKey = "elasticsearch",
			Product = new Product { Id = "elasticsearch", DisplayName = "Elasticsearch" },
			SpecFileName = "elasticsearch-openapi.json",
			LocalSpecFile = localSpecFile,
			Repository = repository
		};

	private static IFileInfo LocalSpecFile()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/docs/elasticsearch-openapi.json", new MockFileData("{}"));
		return fs.FileInfo.New("/docs/elasticsearch-openapi.json");
	}

	[Fact]
	public async Task ResolveVersionsAsync_AlwaysFetchesTheRootIndexAtBucketRoot()
	{
		var handler = new StubHandler(_ => IndexResponse("{}"));
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();

		_ =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/elasticsearch.git"),
				"elasticsearch",
				ApiConfig(),
				collector,
				TestContext.Current.CancellationToken
			);

		handler.RequestedPaths.Should().ContainSingle().Which.Should().Be("/index.json");
	}

	[Fact]
	public async Task ResolveVersionsAsync_NoLocalSpec_NoRepository_EmitsErrorAndReturnsEmpty()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();

		var versions =
			await client.ResolveVersionsAsync(GitFor(null), "elasticsearch", ApiConfig(), collector, TestContext.Current.CancellationToken);

		versions.Should().BeEmpty();
		collector.ErrorMessages.Should().ContainSingle(m => m.Contains("its repository could not be determined"));
		handler.RequestedPaths.Should().BeEmpty("no repository means there is nothing to look up, so the index is never fetched");
	}

	[Fact]
	public async Task ResolveVersionsAsync_LocalSpec_NoRepository_ReturnsLocalMainOnly()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();
		var localFile = LocalSpecFile();

		var versions =
			await client.ResolveVersionsAsync(
				GitFor(null),
				"elasticsearch",
				ApiConfig(localFile),
				collector,
				TestContext.Current.CancellationToken
			);

		versions.Should()
			.ContainSingle()
			.Which
			.Should()
			.Match<ResolvedApiVersion>(v => v.Moniker == "main" && v.IsLocal && v.LocalFile == localFile);
		collector.Errors.Should().Be(0);
		collector.Warnings.Should().Be(0);
	}

	[Fact]
	public async Task ResolveVersionsAsync_MultipleRemoteVersions_ResolvesAllFromIndex()
	{
		var handler = new StubHandler(
			_ =>
				IndexResponse(
					/*lang=json,strict*/
					"""
			{
				"elastic/elasticsearch": {
					"elasticsearch-openapi.json": {
						"main": { "version": "main" },
						"9": { "version": "9.4" },
						"8": { "version": "8.19" }
					}
				}
			}
			"""
				)
		);
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();

		var versions =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/elasticsearch.git"),
				"elasticsearch",
				ApiConfig(),
				collector,
				TestContext.Current.CancellationToken
			);

		versions.Should().HaveCount(3);
		versions.Should().ContainSingle(
			v => v.Moniker == "main" && !v.IsLocal && v.ObjectKey == "elastic/elasticsearch/main/elasticsearch-openapi.json"
		);
		versions.Should().ContainSingle(v => v.Moniker == "9" && v.Version == "9.4");
		versions.Should().ContainSingle(v => v.Moniker == "8" && v.Version == "8.19");
		collector.Errors.Should().Be(0);
		collector.Warnings.Should().Be(0);
	}

	[Fact]
	public async Task ResolveVersionsAsync_RepositoryOverride_UsedInsteadOfGitRemote()
	{
		var handler = new StubHandler(
			_ =>
				IndexResponse(
					/*lang=json,strict*/
					"""
			{
				"elastic/elasticsearch-specification": {
					"elasticsearch-openapi.json": {
						"main": { "version": "main" }
					}
				}
			}
			"""
				)
		);
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();

		// The git remote is a docs repo that never publishes this spec; only the explicit override
		// (elastic/elasticsearch-specification) has a matching entry.
		var versions =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/docs-builder.git"),
				"elasticsearch",
				ApiConfig(repository: "elastic/elasticsearch-specification"),
				collector,
				TestContext.Current.CancellationToken
			);

		versions.Should().ContainSingle(
			v => v.Moniker == "main" && v.ObjectKey == "elastic/elasticsearch-specification/main/elasticsearch-openapi.json"
		);
		collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task ResolveVersionsAsync_RepositoryNotInIndex_NoLocalSpec_EmitsErrorAndReturnsEmpty()
	{
		var handler = new StubHandler(
			_ =>
				IndexResponse(
					/*lang=json,strict*/
					"""{ "elastic/kibana": { "kibana.yaml": { "main": { "version": "main" } } } }"""
				)
		);
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();

		var versions =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/elasticsearch.git"),
				"elasticsearch",
				ApiConfig(),
				collector,
				TestContext.Current.CancellationToken
			);

		versions.Should().BeEmpty();
		collector.ErrorMessages.Should().ContainSingle(m => m.Contains("no entry for repository 'elastic/elasticsearch'"));
	}

	[Fact]
	public async Task ResolveVersionsAsync_SpecNotUnderRepository_NoLocalSpec_EmitsErrorAndReturnsEmpty()
	{
		var handler = new StubHandler(
			_ =>
				IndexResponse(
					/*lang=json,strict*/
					"""
			{
				"elastic/elasticsearch": {
					"elasticsearch-serverless.json": {
						"main": { "version": "main" }
					}
				}
			}
			"""
				)
		);
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();

		var versions =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/elasticsearch.git"),
				"elasticsearch",
				ApiConfig(),
				collector,
				TestContext.Current.CancellationToken
			);

		versions.Should().BeEmpty();
		collector.ErrorMessages
			.Should()
			.ContainSingle(m => m.Contains("no entry for spec 'elasticsearch-openapi.json'") && m.Contains("elastic/elasticsearch"));
	}

	[Fact]
	public async Task ResolveVersionsAsync_LocalSpecPresent_MainUsesLocalFileNotRemoteKey()
	{
		var handler = new StubHandler(
			_ =>
				IndexResponse(
					/*lang=json,strict*/
					"""
			{
				"elastic/elasticsearch": {
					"elasticsearch-openapi.json": {
						"main": { "version": "main" },
						"8": { "version": "8.19" }
					}
				}
			}
			"""
				)
		);
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();
		var localFile = LocalSpecFile();

		var versions =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/elasticsearch.git"),
				"elasticsearch",
				ApiConfig(localFile),
				collector,
				TestContext.Current.CancellationToken
			);

		var main = versions.Should().ContainSingle(v => v.Moniker == "main").Subject;
		main.IsLocal.Should().BeTrue();
		main.LocalFile.Should().Be(localFile);
		main.ObjectKey.Should().BeNull();

		var v8 = versions.Should().ContainSingle(v => v.Moniker == "8").Subject;
		v8.IsLocal.Should().BeFalse();
		v8.ObjectKey.Should().Be("elastic/elasticsearch/8.19/elasticsearch-openapi.json");
	}

	[Fact]
	public async Task ResolveVersionsAsync_EmptyIndex_NoLocalSpec_EmitsErrorAndReturnsEmpty()
	{
		var handler = new StubHandler(_ => IndexResponse("{}"));
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();

		var versions =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/elasticsearch.git"),
				"elasticsearch",
				ApiConfig(),
				collector,
				TestContext.Current.CancellationToken
			);

		versions.Should().BeEmpty();
		collector.ErrorMessages.Should().ContainSingle(m => m.Contains("declares no repositories"));
	}

	[Fact]
	public async Task ResolveVersionsAsync_EmptyIndex_WithLocalSpec_ReturnsLocalMainWithWarning()
	{
		var handler = new StubHandler(_ => IndexResponse("{}"));
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();
		var localFile = LocalSpecFile();

		var versions =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/elasticsearch.git"),
				"elasticsearch",
				ApiConfig(localFile),
				collector,
				TestContext.Current.CancellationToken
			);

		versions.Should().ContainSingle().Which.Should().Match<ResolvedApiVersion>(v => v.Moniker == "main" && v.IsLocal);
		collector.WarningMessages.Should().ContainSingle(m => m.Contains("declares no repositories"));
	}

	[Fact]
	public async Task ResolveVersionsAsync_IndexFetchFails_NoLocalSpec_EmitsErrorAndReturnsEmpty()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();

		var versions =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/elasticsearch.git"),
				"elasticsearch",
				ApiConfig(),
				collector,
				TestContext.Current.CancellationToken
			);

		versions.Should().BeEmpty();
		collector.ErrorMessages.Should().ContainSingle(m => m.Contains("could not be fetched"));
		handler.RequestedPaths.Should().ContainSingle("a missing index is not a transient failure");
	}

	[Fact]
	public async Task ResolveVersionsAsync_IndexFetchFails_WithLocalSpec_FallsBackToLocalMainWithWarning()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();
		var localFile = LocalSpecFile();

		var versions =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/elasticsearch.git"),
				"elasticsearch",
				ApiConfig(localFile),
				collector,
				TestContext.Current.CancellationToken
			);

		versions.Should()
			.ContainSingle()
			.Which
			.Should()
			.Match<ResolvedApiVersion>(v => v.Moniker == "main" && v.IsLocal && v.LocalFile == localFile);
		collector.Errors.Should().Be(0);
		collector.WarningMessages.Should().ContainSingle(m => m.Contains("could not be fetched"));
	}

	[Fact]
	public async Task ResolveVersionsAsync_IndexRecoversAfterRetry_ReturnsVersions()
	{
		var attempts = 0;
		var handler = new StubHandler(
			_ =>
				Interlocked.Increment(ref attempts) == 1
					? new HttpResponseMessage(HttpStatusCode.InternalServerError)
					: IndexResponse(
						/*lang=json,strict*/
						"""{ "elastic/elasticsearch": { "elasticsearch-openapi.json": { "main": { "version": "main" } } } }"""
					)
		);
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();

		var versions =
			await client.ResolveVersionsAsync(
				GitFor("https://github.com/elastic/elasticsearch.git"),
				"elasticsearch",
				ApiConfig(),
				collector,
				TestContext.Current.CancellationToken
			);

		versions.Should().ContainSingle();
		collector.Errors.Should().Be(0);
		attempts.Should().Be(2);
	}

	[Fact]
	public async Task ResolveVersionsAsync_CalledForMultipleApis_FetchesTheRootIndexOnlyOnce()
	{
		var handler = new StubHandler(
			_ =>
				IndexResponse(
					/*lang=json,strict*/
					"""
			{
				"elastic/elasticsearch": {
					"elasticsearch-openapi.json": { "main": { "version": "main" } }
				},
				"elastic/kibana": {
					"kibana.yaml": { "main": { "version": "main" } }
				}
			}
			"""
				)
		);
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();
		var git = GitFor("https://github.com/elastic/elasticsearch.git");

		var esVersions =
			await client.ResolveVersionsAsync(git, "elasticsearch", ApiConfig(), collector, TestContext.Current.CancellationToken);
		var kibanaConfig = new ResolvedApiConfiguration
		{
			ProductKey = "kibana",
			Product = new Product { Id = "kibana", DisplayName = "Kibana" },
			SpecFileName = "kibana.yaml",
			Repository = "elastic/kibana"
		};
		var kibanaVersions =
			await client.ResolveVersionsAsync(git, "kibana", kibanaConfig, collector, TestContext.Current.CancellationToken);

		esVersions.Should().ContainSingle();
		kibanaVersions.Should().ContainSingle();
		handler.RequestedPaths
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("/index.json", "the second call should reuse the first call's cached root index");
	}

	[Fact]
	public async Task FetchSpecStreamAsync_HappyPath_ReturnsContent()
	{
		var handler = new StubHandler(
			_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(/*lang=json,strict*/ """{"openapi":"3.1.0"}""") }
		);
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();
		var version = new ResolvedApiVersion
		{
			Moniker = "8",
			Version = "8.19",
			IsLocal = false,
			ObjectKey = "elastic/elasticsearch/8.19/elasticsearch-openapi.json"
		};

		var stream = await client.FetchSpecStreamAsync("elasticsearch", version, collector, TestContext.Current.CancellationToken);

		stream.Should().NotBeNull();
		using var reader = new StreamReader(stream);
		(await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Contain("openapi");
		handler.RequestedPaths.Should().ContainSingle().Which.Should().Be("/elastic/elasticsearch/8.19/elasticsearch-openapi.json");
		collector.Warnings.Should().Be(0);
	}

	[Fact]
	public async Task FetchSpecStreamAsync_PersistentFailure_EmitsWarningAndReturnsNull()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		using var client = CreateClient(handler);
		var collector = new CapturingDiagnosticsCollector();
		var version = new ResolvedApiVersion
		{
			Moniker = "8",
			Version = "8.19",
			IsLocal = false,
			ObjectKey = "elastic/elasticsearch/8.19/elasticsearch-openapi.json"
		};

		var stream = await client.FetchSpecStreamAsync("elasticsearch", version, collector, TestContext.Current.CancellationToken);

		stream.Should().BeNull();
		collector.WarningMessages.Should().ContainSingle(m => m.Contains('8') && m.Contains("Skipping this version"));
		handler.RequestedPaths.Should().ContainSingle("a missing spec is not a transient failure");
	}

	private static HttpResponseMessage IndexResponse(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public List<string> RequestedPaths { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return Task.FromResult(responder(request));
		}
	}

	/// <summary>Bypasses the channel/background-reader machinery so tests can assert on emitted messages synchronously.</summary>
	private sealed class CapturingDiagnosticsCollector() : DiagnosticsCollector([])
	{
		private readonly List<Diagnostic> _captured = [];

		public IEnumerable<string> ErrorMessages => _captured.Where(d => d.Severity == Severity.Error).Select(d => d.Message);
		public IEnumerable<string> WarningMessages => _captured.Where(d => d.Severity == Severity.Warning).Select(d => d.Message);

		public override void Write(Diagnostic diagnostic)
		{
			IncrementSeverityCount(diagnostic);
			_captured.Add(diagnostic);
		}

		public override DiagnosticsCollector StartAsync(Cancel ctx) => this;
		public override Task StopAsync(Cancel cancellationToken) => Task.CompletedTask;
	}
}
