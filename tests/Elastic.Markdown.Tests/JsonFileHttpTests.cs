// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net;
using AwesomeAssertions;
using Elastic.Markdown.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Tests;

public class JsonFileHttpTests
{
	[Test]
	public async Task ServesDocsetJsonWithJsonContentTypeAndUnchangedBody()
	{
		const string body = """{"high_cpu_on_node":{"tags":["deployment:hosted"]}}""";
		var directory = Directory.CreateTempSubdirectory("docset-json-");
		var cancellation = TestContext.Current!.Execution.CancellationToken;
		try
		{
			var path = Path.Join(directory.FullName, "autoops-insights.json");
			await File.WriteAllTextAsync(path, body, cancellation);
			var fileSystem = new FileSystem();
			var json = new JsonFile(
				fileSystem.FileInfo.New(path),
				fileSystem.DirectoryInfo.New(directory.FullName),
				"repo"
			);

			var builder = WebApplication.CreateBuilder();
			builder.Logging.ClearProviders();
			builder.WebHost.UseUrls("http://127.0.0.1:0");
			await using var app = builder.Build();
			app.MapGet("/autoops-insights.json", () => DocumentationFileResults.Serve(json));
			await app.StartAsync(cancellation);
			var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();

			using var client = new HttpClient();
			var response = await client.GetAsync(new Uri(new Uri(address), "autoops-insights.json"), cancellation);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
			(await response.Content.ReadAsStringAsync(cancellation)).Should().Be(body);

			await app.StopAsync(cancellation);
		}
		finally
		{
			directory.Delete(true);
		}
	}
}
