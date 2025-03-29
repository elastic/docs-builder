// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net;
using System.Runtime.InteropServices;
using Documentation.Builder.Diagnostics.LiveMode;
using Elastic.Documentation.Tooling;
using Elastic.Markdown;
using Elastic.Markdown.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Westwind.AspNetCore.LiveReload;

namespace Documentation.Builder.Http;

public class DocumentationWebHost
{
	private readonly WebApplication _webApplication;

	private readonly BuildContext _context;

	public DocumentationWebHost(string? path, int port, ILoggerFactory logger, IFileSystem fileSystem)
	{
		var builder = WebApplication.CreateSlimBuilder();
		DocumentationTooling.CreateServiceCollection(builder.Services, LogLevel.Warning);

		_ = builder.Logging
			.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Error)
			.AddFilter("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogLevel.Error)
			.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);

		var collector = new LiveModeDiagnosticsCollector(logger);

		var hostUrl = $"http://localhost:{port}";

		_context = new BuildContext(collector, fileSystem, fileSystem, path, null)
		{
			CanonicalBaseUrl = new Uri(hostUrl),
		};
		_ = builder.Services
			.AddAotLiveReload(s =>
			{
				s.FolderToMonitor = _context.DocumentationSourceDirectory.FullName;
				s.ClientFileExtensions = ".md,.yml";
			})
			.AddSingleton<ReloadableGeneratorState>(_ =>
				new ReloadableGeneratorState(_context.DocumentationSourceDirectory, null, _context, logger)
			)
			.AddHostedService<ReloadGeneratorService>();

		if (IsDotNetWatchBuild())
			_ = builder.Services.AddHostedService<ParcelWatchService>();

		_ = builder.WebHost.UseUrls(hostUrl);

		_webApplication = builder.Build();
		SetUpRoutes();
	}

	private static bool IsDotNetWatchBuild() => Environment.GetEnvironmentVariable("DOTNET_WATCH") is not null;

	public async Task RunAsync(Cancel ctx)
	{
		_ = _context.Collector.StartAsync(ctx);
		await _webApplication.RunAsync(ctx);
	}

	public async Task StopAsync(Cancel ctx)
	{
		_context.Collector.Channel.TryComplete();
		await _context.Collector.StopAsync(ctx);
	}

	private void SetUpRoutes()
	{
		_ = _webApplication
			.UseExceptionHandler(options =>
			{
				options.Run(async context =>
				{
					try
					{
						var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
						if (exception != null)
						{
							var logger = context.RequestServices.GetRequiredService<ILogger<DocumentationWebHost>>();
							logger.LogError(
								exception.Error,
								"Unhandled exception processing request {Path}. Error: {Error}\nStack Trace: {StackTrace}\nInner Exception: {InnerException}",
								context.Request.Path,
								exception.Error.Message,
								exception.Error.StackTrace,
								exception.Error.InnerException?.ToString() ?? "None"
							);
							logger.LogError(
								"Request Details - Method: {Method}, Path: {Path}, QueryString: {QueryString}",
								context.Request.Method,
								context.Request.Path,
								context.Request.QueryString
							);

							context.Response.StatusCode = 500;
							context.Response.ContentType = "text/html";
							await context.Response.WriteAsync(@"
								<html>
									<head><title>Error</title></head>
									<body>
										<h1>Internal Server Error</h1>
										<p>An error occurred while processing your request.</p>
										<p>Please check the application logs for more details.</p>
									</body>
								</html>");
						}
					}
					catch (Exception handlerEx)
					{
						var logger = context.RequestServices.GetRequiredService<ILogger<DocumentationWebHost>>();
						logger.LogCritical(
							handlerEx,
							"Error handler failed to process exception. Handler Error: {Error}\nStack Trace: {StackTrace}",
							handlerEx.Message,
							handlerEx.StackTrace
						);
						context.Response.StatusCode = 500;
						context.Response.ContentType = "text/plain";
						await context.Response.WriteAsync("A critical error occurred.");
					}
				});
			})
			.UseLiveReload()
			.UseStaticFiles(
				new StaticFileOptions
				{
					FileProvider = new EmbeddedOrPhysicalFileProvider(_context),
					RequestPath = "/_static"
				})
			.UseRouting();

		_ = _webApplication.MapGet("/", (ReloadableGeneratorState holder, Cancel ctx) =>
			ServeDocumentationFile(holder, "index.md", ctx));

		_ = _webApplication.MapGet("{**slug}", (string slug, ReloadableGeneratorState holder, Cancel ctx) =>
			ServeDocumentationFile(holder, slug, ctx));
	}

	private static async Task<IResult> ServeDocumentationFile(ReloadableGeneratorState holder, string slug, Cancel ctx)
	{
		var generator = holder.Generator;

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			slug = slug.Replace('/', Path.DirectorySeparatorChar);

		var s = Path.GetExtension(slug) == string.Empty ? Path.Combine(slug, "index.md") : slug;
		if (!generator.DocumentationSet.FlatMappedFiles.TryGetValue(s, out var documentationFile))
		{
			s = Path.GetExtension(slug) == string.Empty ? slug + ".md" : s.Replace($"{Path.DirectorySeparatorChar}index.md", ".md");
			if (!generator.DocumentationSet.FlatMappedFiles.TryGetValue(s, out documentationFile))
			{
				foreach (var extension in generator.Context.Configuration.EnabledExtensions)
				{
					if (extension.TryGetDocumentationFileBySlug(generator.DocumentationSet, slug, out documentationFile))
						break;
				}
			}
		}

		switch (documentationFile)
		{
			case MarkdownFile markdown:
				var rendered = await generator.RenderLayout(markdown, ctx);
				return Results.Content(rendered, "text/html");
			case ImageFile image:
				return Results.File(image.SourceFile.FullName, image.MimeType);
			default:
				if (generator.DocumentationSet.FlatMappedFiles.TryGetValue("404.md", out var notFoundDocumentationFile))
				{
					var renderedNotFound = await generator.RenderLayout((notFoundDocumentationFile as MarkdownFile)!, ctx);
					return Results.Content(renderedNotFound, "text/html", null, (int)HttpStatusCode.NotFound);
				}
				return Results.NotFound();
		}
	}
}
