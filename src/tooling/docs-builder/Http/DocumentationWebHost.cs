// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net;
using System.Runtime.InteropServices;
using Documentation.Builder.Diagnostics.LiveMode;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Tooling;
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

	private readonly IHostedService _hostedService;
	private readonly IFileSystem _writeFileSystem;

	public DocumentationWebHost(ILoggerFactory logFactory, string? path, int port, IFileSystem readFs, IFileSystem writeFs,
		VersionsConfiguration versionsConfig)
	{
		_writeFileSystem = writeFs;
		var builder = WebApplication.CreateSlimBuilder();
		DocumentationTooling.CreateServiceCollection(builder.Services, LogLevel.Warning);

		_ = builder.Logging
			.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Error)
			.AddFilter("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogLevel.Error)
			.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);

		var collector = new LiveModeDiagnosticsCollector(logFactory);

		var hostUrl = $"http://localhost:{port}";

		_hostedService = collector;
		Context = new BuildContext(collector, readFs, writeFs, versionsConfig, path, null)
		{
			CanonicalBaseUrl = new Uri(hostUrl),
		};
		GeneratorState = new ReloadableGeneratorState(logFactory, Context.DocumentationSourceDirectory, Context.DocumentationOutputDirectory, Context);
		_ = builder.Services
			.AddAotLiveReload(s =>
			{
				s.FolderToMonitor = Context.DocumentationSourceDirectory.FullName;
				s.ClientFileExtensions = ".md,.yml";
			})
			.AddSingleton<ReloadableGeneratorState>(_ => GeneratorState)
			.AddHostedService<ReloadGeneratorService>();

		if (IsDotNetWatchBuild())
			_ = builder.Services.AddHostedService<ParcelWatchService>();

		_ = builder.WebHost.UseUrls(hostUrl);

		_webApplication = builder.Build();
		SetUpRoutes();
	}

	public ReloadableGeneratorState GeneratorState { get; }

	public BuildContext Context { get; }

	private static bool IsDotNetWatchBuild() => Environment.GetEnvironmentVariable("DOTNET_WATCH") is not null;

	public async Task RunAsync(Cancel ctx)
	{
		_ = _hostedService.StartAsync(ctx);
		await _webApplication.RunAsync(ctx);
	}

	public async Task StopAsync(Cancel ctx)
	{
		await _webApplication.StopAsync(ctx);
		await _hostedService.StopAsync(ctx);
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
					FileProvider = new EmbeddedOrPhysicalFileProvider(Context),
					RequestPath = "/_static"
				})
			.UseRouting();

		_ = _webApplication.MapGet("/", (ReloadableGeneratorState holder, Cancel ctx) =>
			ServeDocumentationFile(holder, "index.md", ctx));

		_ = _webApplication.MapGet("/api/", (ReloadableGeneratorState holder, Cancel ctx) =>
			ServeApiFile(holder, "", ctx));

		_ = _webApplication.MapGet("/api/{**slug}", (string slug, ReloadableGeneratorState holder, Cancel ctx) =>
			ServeApiFile(holder, slug, ctx));

		_ = _webApplication.MapGet("{**slug}", (string slug, ReloadableGeneratorState holder, Cancel ctx) =>
			ServeDocumentationFile(holder, slug, ctx));
	}

	private async Task<IResult> ServeApiFile(ReloadableGeneratorState holder, string slug, Cancel ctx)
	{
#if DEBUG
		// only reload when actually debugging
		if (System.Diagnostics.Debugger.IsAttached)
			await holder.ReloadApiReferences(ctx);
#endif
		var path = Path.Combine(holder.ApiPath.FullName, slug.Trim('/'), "index.html");
		var info = _writeFileSystem.FileInfo.New(path);
		if (info.Exists)
		{
			//TODO STREAM
			var contents = await _writeFileSystem.File.ReadAllTextAsync(info.FullName, ctx);
			return Results.Content(contents, "text/html");
		}

		return Results.NotFound();
	}

	private static async Task<IResult> ServeDocumentationFile(ReloadableGeneratorState holder, string slug, Cancel ctx)
	{
		var generator = holder.Generator;
		const string navPartialSuffix = ".nav.html";
		if (slug.EndsWith(navPartialSuffix))
		{
			var segments = slug.Split("/");
			var lastSegment = segments.Last();
			var navigationId = lastSegment.Replace(navPartialSuffix, "");
			return generator.DocumentationSet.NavigationRenderResults.TryGetValue(navigationId, out var rendered)
				? Results.Content(rendered.Html, "text/html")
				: Results.NotFound();
		}

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			slug = slug.Replace('/', Path.DirectorySeparatorChar);

		var s = Path.GetExtension(slug) == string.Empty ? Path.Combine(slug, "index.md") : slug;

		if (!generator.DocumentationSet.FlatMappedFiles.TryGetValue(s, out var documentationFile))
		{
			s = Path.GetExtension(slug) == string.Empty ? slug + ".md" : s.Replace($"{Path.DirectorySeparatorChar}index.md", ".md");
			if (!generator.DocumentationSet.FlatMappedFiles.TryGetValue(s, out documentationFile))
			{
				foreach (var extension in holder.Generator.DocumentationSet.EnabledExtensions)
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
				return Results.Content(rendered.Html, "text/html");

			case ImageFile image:
				return Results.File(image.SourceFile.FullName, image.MimeType);
			default:
				if (s == "index.md")
					return Results.Redirect(generator.DocumentationSet.MarkdownFiles.First().Url);

				if (!generator.DocumentationSet.FlatMappedFiles.TryGetValue("404.md", out var notFoundDocumentationFile))
					return Results.NotFound();

				if (Path.GetExtension(s) is "" or not ".md")
					return Results.NotFound();

				var renderedNotFound = await generator.RenderLayout((notFoundDocumentationFile as MarkdownFile)!, ctx);
				return Results.Content(renderedNotFound.Html, "text/html", null, (int)HttpStatusCode.NotFound);
		}
	}
}
