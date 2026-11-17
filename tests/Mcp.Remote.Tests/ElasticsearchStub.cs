// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Search.Common;

namespace Mcp.Remote.Tests;

/// <summary>Answers every request with one canned Elasticsearch response and keeps the last request body.</summary>
internal sealed class ElasticsearchStub : IDisposable
{
	private readonly HttpListener _listener = new();
	private readonly string _response;
	private readonly int _statusCode;
	private ElasticsearchClientAccessor? _accessor;

	private ElasticsearchStub(string response, int port, int statusCode)
	{
		_response = response;
		_statusCode = statusCode;
		Uri = new Uri($"http://127.0.0.1:{port}/");
		_listener.Prefixes.Add(Uri.ToString());
	}

	public Uri Uri { get; }
	public string LastRequestBody { get; private set; } = string.Empty;

	/// <summary>A client pointed at this stub, disposed with it.</summary>
	public ElasticsearchClientAccessor Accessor =>
		_accessor ??= new(
			new DocumentationEndpoints
			{
				Elasticsearch = new ElasticsearchEndpoint { Uri = Uri, ApiKey = "test" },
				SearchIndexOverride = "test-index"
			},
			new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] }
		);

	public static ElasticsearchStub Start(string response, int statusCode = 200)
	{
		// Bind a free port; retry if another process takes it between probe and bind.
		for (var attempt = 0; ; attempt++)
		{
			var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
			probe.Start();
			var port = ((IPEndPoint)probe.LocalEndpoint).Port;
			probe.Stop();

			var stub = new ElasticsearchStub(response, port, statusCode);
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
			context.Response.StatusCode = _statusCode;
			context.Response.ContentType = "application/json";
			// The Elasticsearch client rejects responses without the product header.
			context.Response.Headers["X-Elastic-Product"] = "Elasticsearch";
			await context.Response.OutputStream.WriteAsync(bytes);
			context.Response.Close();
		}
	}

	public void Dispose()
	{
		_accessor?.Dispose();
		_listener.Close();
	}
}
