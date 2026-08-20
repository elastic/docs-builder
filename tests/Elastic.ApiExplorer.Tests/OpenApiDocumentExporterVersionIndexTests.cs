// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Versions;
using FakeItEasy;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class OpenApiDocumentExporterVersionIndexTests
{
	private static readonly Uri BaseUri = new("https://cdn.example/");

	private static readonly VersionsConfiguration StackVersions = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9, currentMinor: 2);

	private static GitCheckoutInformation GitForElasticsearch() => new()
	{
		Branch = "main",
		Remote = "https://github.com/elastic/elasticsearch.git",
		Ref = "refs/heads/main"
	};

	private static OpenApiDocument SpecWithOperation(string operationId) => new()
	{
		Info = new OpenApiInfo { Title = operationId, Version = "1.0" },
		Paths = new OpenApiPaths
		{
			["/ping"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Get] = new() { OperationId = operationId, Summary = "Ping" }
				}
			}
		}
	};

	private static ResolvedApiConfiguration ApiConfig(Product product, string specFileName = "elasticsearch-openapi.json", string? repository = "elastic/elasticsearch") =>
		new()
		{
			ProductKey = product.Id,
			Product = product,
			SpecFileName = specFileName,
			Repository = repository
		};

	private static HttpMessageHandler IndexHandler(string indexJson) =>
		new StubHandler(request =>
		{
			if (request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal))
			{
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(indexJson, Encoding.UTF8, "application/json")
				};
			}

			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(/*lang=json,strict*/ """{"openapi":"3.1.0","info":{"title":"Spec","version":"1.0"},"paths":{}}""", Encoding.UTF8, "application/json")
			};
		});

	[Fact]
	public async Task ExportDocuments_MultiMajorIndex_EmitsMainAndVersionedPaths()
	{
		var product = TestHelpers.CreateProduct("elasticsearch", StackVersions.GetVersioningSystem(VersioningSystemId.Stack), "Elasticsearch");
		var handler = IndexHandler(/*lang=json,strict*/ """
			{
				"elastic/elasticsearch": {
					"elasticsearch-openapi.json": {
						"main": { "version": "main" },
						"9": { "version": "9.4" },
						"8": { "version": "8.19" }
					}
				}
			}
			""");
		using var client = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var queue = new Queue<OpenApiDocument>(
		[
			SpecWithOperation("ping-main"),
			SpecWithOperation("ping-9"),
			SpecWithOperation("ping-8")
		]);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._))
			.ReturnsLazily(_ => Task.FromResult<OpenApiDocument?>(queue.Dequeue()));
		var exporter = new OpenApiDocumentExporter(StackVersions, versionIndexClient: client, openApiReader: reader, collector: new DiagnosticsCollector([]));
		var source = new OpenApiExportSource("elasticsearch", ApiConfig(product), GitForElasticsearch());

		var docs = new List<Elastic.Documentation.Search.Contract.DocumentationDocument>();
		await foreach (var doc in exporter.ExportDocuments([source], TestContext.Current.CancellationToken))
			docs.Add(doc);

		docs.Select(d => d.Path).Should().BeEquivalentTo(
		[
			"/docs/api/doc/elasticsearch/operation/operation-ping-main",
			"/docs/api/doc/elasticsearch/v9/operation/operation-ping-9",
			"/docs/api/doc/elasticsearch/v8/operation/operation-ping-8"
		]);
	}

	[Fact]
	public async Task ExportDocuments_VersionlessProduct_EmitsMainOnly()
	{
		var versionless = TestHelpers.CreateVersionlessConfiguration();
		var product = TestHelpers.CreateProduct("cloud-serverless", versionless.GetVersioningSystem(VersioningSystemId.Serverless), "Cloud Serverless");
		var handler = IndexHandler(/*lang=json,strict*/ """
			{
				"elastic/serverless-api-specification": {
					"elastic-cloud-serverless.yml": {
						"main": { "version": "main" },
						"8": { "version": "8.19" }
					}
				}
			}
			""");
		using var client = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._))
			.Returns(Task.FromResult<OpenApiDocument?>(SpecWithOperation("ping")));
		var exporter = new OpenApiDocumentExporter(versionless, versionIndexClient: client, openApiReader: reader, collector: new DiagnosticsCollector([]));
		var source = new OpenApiExportSource(
			"cloud-serverless",
			ApiConfig(product, specFileName: "elastic-cloud-serverless.yml", repository: "elastic/serverless-api-specification"),
			GitForElasticsearch());

		var docs = new List<Elastic.Documentation.Search.Contract.DocumentationDocument>();
		await foreach (var doc in exporter.ExportDocuments([source], TestContext.Current.CancellationToken))
			docs.Add(doc);

		docs.Should().ContainSingle();
		docs[0].Path.Should().Be("/docs/api/doc/cloud-serverless/operation/operation-ping");
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(responder(request));
	}
}
