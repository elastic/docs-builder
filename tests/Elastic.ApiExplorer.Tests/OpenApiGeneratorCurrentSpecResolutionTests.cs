// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Nullean.ScopedFileSystem;

namespace Elastic.ApiExplorer.Tests;

public class OpenApiGeneratorCurrentSpecResolutionTests
{
	private static readonly Uri BaseUri = new("https://cdn.example/");

	private static BuildContext CreateContext(DiagnosticsCollector collector, GitCheckoutInformation? git = null)
	{
		var fs = FileSystemFactory.RealGitRootForPath(null);
		return new BuildContext(collector, fs, fs, TestHelpers.CreateConfigurationContext(new FileSystem()),
			ExportOptions.Default, null, null, gitCheckoutInformation: git);
	}

	private static ResolvedApiConfiguration ApiConfig(IFileInfo? localSpecFile) => new()
	{
		ProductKey = "elasticsearch",
		Product = new Product { Id = "elasticsearch", DisplayName = "Elasticsearch" },
		SpecFileName = "elasticsearch-openapi.json",
		LocalSpecFile = localSpecFile
	};

	[Fact]
	public async Task ResolveCurrentDocument_LocalSpecPresent_RendersLocalFileWithoutAnyNetworkCall()
	{
		var collector = new DiagnosticsCollector([]);
		var context = CreateContext(collector);
		var localFile = new FileSystem().FileInfo.New(
			Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "elasticsearch-openapi-docs.json"));
		var expectedDocument = SpecDocument();
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(localFile)).Returns(expectedDocument);

		var handler = new ThrowingHandler();
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			reader);

		var document = await generator.ResolveCurrentDocument("elasticsearch", ApiConfig(localFile), TestContext.Current.CancellationToken);

		document.Should().BeSameAs(expectedDocument);
		handler.CallCount.Should().Be(0, "a local spec file must short-circuit remote version resolution entirely");
		A.CallTo(() => reader.ReadAsync(localFile)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task ResolveCurrentDocument_NoLocalSpec_ResolvesRemoteMainThroughVersionIndex()
	{
		var collector = new CapturingDiagnosticsCollector();
		var git = new GitCheckoutInformation { Branch = "main", Remote = "https://github.com/elastic/elasticsearch.git", Ref = "refs/heads/main" };
		var context = CreateContext(collector, git);

		var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal)
			? IndexResponse(/*lang=json,strict*/ """
				{
					"elastic/elasticsearch": {
						"elasticsearch-openapi.json": {
							"main": { "version": "main" }
						}
					}
				}
				""")
			: SpecResponse());
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var expectedDocument = SpecDocument();
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, "elasticsearch-openapi.json")).Returns(expectedDocument);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			reader);

		// The sample docset's own kibana/dashboard entries fail product validation against this
		// test's minimal products config; only care about diagnostics from resolving 'elasticsearch'.
		var errorsBeforeResolution = collector.Errors;

		var document = await generator.ResolveCurrentDocument("elasticsearch", ApiConfig(null), TestContext.Current.CancellationToken);

		document.Should().BeSameAs(expectedDocument);
		handler.RequestedPaths.Should().BeEquivalentTo(
		[
			"/index.json",
			"/elastic/elasticsearch/main/elasticsearch-openapi.json"
		]);
		collector.Errors.Should().Be(errorsBeforeResolution, string.Join("; ", collector.ErrorMessages));
		A.CallTo(() => reader.ReadAsync(A<Stream>._, "elasticsearch-openapi.json")).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task ResolveCurrentDocument_NoLocalSpecAndIndexUnreachable_ReturnsNullAndEmitsError()
	{
		var collector = new DiagnosticsCollector([]);
		var git = new GitCheckoutInformation { Branch = "main", Remote = "https://github.com/elastic/elasticsearch.git", Ref = "refs/heads/main" };
		var context = CreateContext(collector, git);

		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, maxAttempts: 1, sleep: (_, _) => Task.CompletedTask);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			reader);
		var errorsBeforeResolution = collector.Errors;

		var document = await generator.ResolveCurrentDocument("elasticsearch", ApiConfig(null), TestContext.Current.CancellationToken);

		document.Should().BeNull();
		collector.Errors.Should().BeGreaterThan(errorsBeforeResolution);
		A.CallTo(() => reader.ReadAsync(A<IFileInfo>._)).MustNotHaveHappened();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._)).MustNotHaveHappened();
	}

	private static OpenApiDocument SpecDocument() => new()
	{
		Info = new OpenApiInfo { Title = "Elasticsearch API", Version = "9.4" }
	};

	private static HttpResponseMessage IndexResponse(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private static HttpResponseMessage SpecResponse() => new(HttpStatusCode.OK)
	{
		Content = new StringContent(
			/*lang=json,strict*/ """{"openapi":"3.1.0","info":{"title":"Elasticsearch API","version":"9.4"},"paths":{}}""",
			System.Text.Encoding.UTF8, "application/json")
	};

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public List<string> RequestedPaths { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return Task.FromResult(responder(request));
		}
	}

	private sealed class ThrowingHandler : HttpMessageHandler
	{
		public int CallCount { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;
			throw new InvalidOperationException("No network calls expected when a local spec file is present.");
		}
	}

	/// <summary>Bypasses the channel/background-reader machinery so tests can assert on emitted messages synchronously.</summary>
	private sealed class CapturingDiagnosticsCollector() : DiagnosticsCollector([])
	{
		private readonly List<Diagnostic> _captured = [];

		public IEnumerable<string> ErrorMessages => _captured.Where(d => d.Severity == Severity.Error).Select(d => d.Message);

		public override void Write(Diagnostic diagnostic)
		{
			IncrementSeverityCount(diagnostic);
			_captured.Add(diagnostic);
		}

		public override DiagnosticsCollector StartAsync(Cancel ctx) => this;
		public override Task StopAsync(Cancel cancellationToken) => Task.CompletedTask;
	}
}
