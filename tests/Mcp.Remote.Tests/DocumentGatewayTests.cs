// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Search.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mcp.Remote.Tests;

public class DocumentGatewayTests
{
	// The indexed endpoint keys are lowercased, slash-stripped search variants; the response must not expose them.
	private const string ApiHit =
		"""
		{
		  "took": 1,
		  "timed_out": false,
		  "hits": {
		    "total": { "value": 1, "relation": "eq" },
		    "hits": [
		      {
		        "_index": "test-index",
		        "_id": "/docs/api/doc/logstash/operation/operation-nodestats",
		        "_source": {
		          "path": "/docs/api/doc/logstash/operation/operation-nodestats",
		          "title": "Get node statistics",
		          "search_title": "Get node statistics",
		          "content_type": "api",
		          "section": "api",
		          "api": {
		            "key": "logstash",
		            "method": "GET",
		            "path": "/_node/stats",
		            "operation_id": "nodestats",
		            "deprecated": false,
		            "endpoints": ["_node/stats", "node/stats"],
		            "routes": [ { "path": "/_node/stats", "methods": ["GET"] } ]
		          }
		        }
		      }
		    ]
		  }
		}
		""";

	private const string ApiLandingHit =
		"""
		{
		  "took": 1,
		  "timed_out": false,
		  "hits": {
		    "total": { "value": 1, "relation": "eq" },
		    "hits": [
		      {
		        "_index": "test-index",
		        "_id": "/docs/api/doc/logstash",
		        "_source": {
		          "path": "/docs/api/doc/logstash",
		          "title": "Logstash API",
		          "search_title": "Logstash API",
		          "content_type": "api",
		          "section": "api",
		          "api": {
		            "key": "logstash",
		            "landing": true,
		            "method": "",
		            "path": "",
		            "operation_id": "",
		            "endpoints": [],
		            "routes": []
		          }
		        }
		      }
		    ]
		  }
		}
		""";

	private const string NarrativeHit =
		"""
		{
		  "took": 1,
		  "timed_out": false,
		  "hits": {
		    "total": { "value": 1, "relation": "eq" },
		    "hits": [
		      {
		        "_index": "test-index",
		        "_id": "/docs/some-page",
		        "_source": { "path": "/docs/some-page", "title": "Some Page", "search_title": "Some Page", "section": "reference" }
		      }
		    ]
		  }
		}
		""";

	[Test]
	public async Task GetByUrl_ApiDocument_ReturnsApiTypeAndVerbatimRoutes()
	{
		using var stub = ElasticsearchStub.Start(ApiHit);
		using var accessor = CreateAccessor(stub);
		var gateway = new DocumentGateway(accessor, NullLogger<DocumentGateway>.Instance);

		var result = await gateway.GetByUrlAsync(
			"/docs/api/doc/logstash/operation/operation-nodestats",
			TestContext.Current!.Execution.CancellationToken
		);

		result.Should().NotBeNull();
		result.Type.Should().Be("api");
		result.Api.Should().NotBeNull();
		result.Api.Method.Should().Be("GET");
		result.Api.Path.Should().Be("/_node/stats");
		result.Api.OperationId.Should().Be("nodestats");
		result.Api.Endpoints.Should().Equal("GET /_node/stats");
	}

	[Test]
	public async Task GetByUrl_ApiLandingPage_ReturnsApiTypeAndNoOperation()
	{
		using var stub = ElasticsearchStub.Start(ApiLandingHit);
		using var accessor = CreateAccessor(stub);
		var gateway = new DocumentGateway(accessor, NullLogger<DocumentGateway>.Instance);

		var result = await gateway.GetByUrlAsync("/docs/api/doc/logstash", TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.Type.Should().Be("api");
		result.Api.Should().BeNull();
	}

	[Test]
	public async Task GetByUrl_RequestsContentTypeAndApiFromSource()
	{
		using var stub = ElasticsearchStub.Start(ApiHit);
		using var accessor = CreateAccessor(stub);
		var gateway = new DocumentGateway(accessor, NullLogger<DocumentGateway>.Instance);

		_ = await gateway.GetByUrlAsync(
			"/docs/api/doc/logstash/operation/operation-nodestats",
			TestContext.Current!.Execution.CancellationToken
		);

		stub.LastRequestBody.Should().Contain("\"content_type\"").And.Contain("\"api\"");
	}

	[Test]
	public async Task GetByUrl_NarrativeDocument_ReturnsDocsTypeAndNoApi()
	{
		using var stub = ElasticsearchStub.Start(NarrativeHit);
		using var accessor = CreateAccessor(stub);
		var gateway = new DocumentGateway(accessor, NullLogger<DocumentGateway>.Instance);

		var result = await gateway.GetByUrlAsync("/docs/some-page", TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.Type.Should().Be("docs");
		result.Api.Should().BeNull();
	}

	private static ElasticsearchClientAccessor CreateAccessor(ElasticsearchStub stub) =>
		new(
			new DocumentationEndpoints
			{
				Elasticsearch = new ElasticsearchEndpoint { Uri = stub.Uri, ApiKey = "test" },
				SearchIndexOverride = "test-index"
			},
			new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] }
		);

	private sealed class ElasticsearchStub : IDisposable
	{
		private readonly HttpListener _listener = new();
		private readonly string _response;

		private ElasticsearchStub(string response, int port)
		{
			_response = response;
			Uri = new Uri($"http://127.0.0.1:{port}/");
			_listener.Prefixes.Add(Uri.ToString());
		}

		public Uri Uri { get; }
		public string LastRequestBody { get; private set; } = string.Empty;

		public static ElasticsearchStub Start(string response)
		{
			// Bind a free port; retry if another process takes it between probe and bind.
			for (var attempt = 0; ; attempt++)
			{
				var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
				probe.Start();
				var port = ((IPEndPoint)probe.LocalEndpoint).Port;
				probe.Stop();

				var stub = new ElasticsearchStub(response, port);
				try
				{
					stub._listener.Start();
				}
				catch (HttpListenerException) when (attempt < 5)
				{
					stub.Dispose();
					continue;
				}

				_ = Task.Run(stub.ServeAsync);
				return stub;
			}
		}

		private async Task ServeAsync()
		{
			while (_listener.IsListening)
			{
				HttpListenerContext context;
				try
				{
					context = await _listener.GetContextAsync();
				}
				catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
				{
					return;
				}

				// The client gzips request bodies by default.
				var body = context.Request.Headers["Content-Encoding"] == "gzip"
					? new System.IO.Compression.GZipStream(context.Request.InputStream, System.IO.Compression.CompressionMode.Decompress)
					: context.Request.InputStream;
				using (var reader = new StreamReader(body, Encoding.UTF8))
					LastRequestBody = await reader.ReadToEndAsync();

				var bytes = Encoding.UTF8.GetBytes(_response);
				context.Response.StatusCode = 200;
				context.Response.ContentType = "application/json";
				// The Elasticsearch client rejects responses without the product header.
				context.Response.Headers["X-Elastic-Product"] = "Elasticsearch";
				await context.Response.OutputStream.WriteAsync(bytes);
				context.Response.Close();
			}
		}

		public void Dispose() => _listener.Close();
	}
}
