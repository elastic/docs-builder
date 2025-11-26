// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
#if DEBUG
using Elastic.Documentation.Api.Infrastructure;
#endif
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Http;

public class StaticWebHost
{
	public WebApplication WebApplication { get; }

	public StaticWebHost(int port, string? path)
	{
		var contentRoot = path ?? Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly");
		var fs = new FileSystem();
		var dir = fs.DirectoryInfo.New(contentRoot);
		if (!dir.Exists)
			throw new Exception($"Can not serve empty directory: {contentRoot}");
		if (!dir.IsSubPathOf(fs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName)))
			throw new Exception($"Can not serve directory outside of: {Paths.WorkingDirectoryRoot.FullName}");

		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ContentRootPath = contentRoot
		});

		_ = builder.AddDocumentationServiceDefaults();
#if DEBUG
		builder.Services.AddElasticDocsApiUsecases("dev");
#endif

		_ = builder.Logging
			.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Error)
			.AddFilter("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogLevel.Error)
			.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
		_ = builder.WebHost.UseUrls($"http://localhost:{port}");

		WebApplication = builder.Build();
		SetUpRoutes();
	}

	public async Task RunAsync(Cancel ctx) => await WebApplication.RunAsync(ctx);

	public async Task StopAsync(Cancel ctx) => await WebApplication.StopAsync(ctx);

	private void SetUpRoutes()
	{
		_ = WebApplication.Use(async (context, next) =>
		{
			try
			{
				await next(context);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[UNHANDLED EXCEPTION] {ex.GetType().Name}: {ex.Message}");
				Console.WriteLine($"[STACK TRACE] {ex.StackTrace}");
				if (ex.InnerException != null)
					Console.WriteLine($"[INNER EXCEPTION] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");

				throw; // Re-throw to let ASP.NET Core handle it
			}
		});
		_ =
			WebApplication
				.UseDeveloperExceptionPage(new DeveloperExceptionPageOptions())
				.UseRouting();

		_ = WebApplication.MapGet("/", (Cancel _) => Results.Redirect("docs"));

		_ = WebApplication.MapGet("{**slug}", ServeDocumentationFile);


		var apiV1 = WebApplication.MapGroup("/docs/_api/v1");
#if DEBUG
		apiV1.MapElasticDocsApiEndpoints();
#endif

	}

	private async Task<IResult> ServeDocumentationFile(string slug, Cancel _)
	{
		// from the injected top level navigation which expects us to run on elastic.co
		if (slug.StartsWith("static-res/"))
			return Results.NotFound();

		await Task.CompletedTask;
		var path = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly");
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
