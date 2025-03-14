// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Tooling;
using Elastic.Markdown;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Http;

public class StaticWebHost
{
	private readonly WebApplication _webApplication;

	private readonly BuildContext _context;

	public StaticWebHost(string? path, int port, IFileSystem fileSystem)
	{
		var builder = WebApplication.CreateSlimBuilder();
		DocumentationTooling.CreateServiceCollection(builder.Services, LogLevel.Warning);

		_ = builder.Logging
			.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Error)
			.AddFilter("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogLevel.Error)
			.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
		_ = builder.WebHost.UseUrls($"http://localhost:{port}");

		_webApplication = builder.Build();
		_context = new BuildContext(new DiagnosticsCollector([]), fileSystem, fileSystem, path, null);
		SetUpRoutes();
	}

	public async Task RunAsync(Cancel ctx) => await _webApplication.RunAsync(ctx);

	public async Task StopAsync(Cancel ctx) => await _context.Collector.StopAsync(ctx);

	private void SetUpRoutes()
	{
		_ =
			_webApplication
				.UseRouting();

		_ = _webApplication.MapGet("/", (Cancel _) => Results.Redirect("docs"));

		_ = _webApplication.MapGet("docs/", (Cancel ctx) =>
			ServeDocumentationFile("index.html", ctx));

		_ = _webApplication.MapGet("docs/{**slug}", (string slug, Cancel ctx) =>
			ServeDocumentationFile(slug, ctx));
	}

	private static async Task<IResult> ServeDocumentationFile(string slug, Cancel _)
	{
		// from the injected top level navigation which expects us to run on elastic.co
		if (slug.StartsWith("static-res/"))
			return Results.NotFound();

		await Task.CompletedTask;
		var path = Path.Combine(Paths.Root.FullName, ".artifacts", "assembly");
		var localPath = Path.Combine(path, slug.Replace('/', Path.DirectorySeparatorChar));
		var fileInfo = new FileInfo(localPath);
		var directoryInfo = new DirectoryInfo(localPath);
		if (directoryInfo.Exists)
			fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "index.html"));

		if (fileInfo.Exists)
		{
			var mimetype = fileInfo.Extension switch
			{
				".js" => "text/javascript",
				".css" => "text/css",
				".png" => "image/png",
				".jpg" => "image/jpeg",
				".gif" => "image/gif",
				".svg" => "image/svg+xml",
				".ico" => "image/x-icon",
				".json" => "application/json",
				".map" => "application/json",
				".txt" => "text/plain",
				".xml" => "text/xml",
				".yml" => "text/yaml",
				".md" => "text/markdown",
				_ => "text/html"
			};
			return Results.File(fileInfo.FullName, mimetype);
		}


		return Results.NotFound();
	}
}
