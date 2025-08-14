// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Documentation.Builder.Diagnostics.LiveMode;
using Elastic.Documentation;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure;
using Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;
using Elastic.Documentation.Api.Infrastructure.Gcp;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.ServiceDefaults;
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

	public DocumentationWebHost(
		ILoggerFactory logFactory,
		string? path,
		int port,
		IFileSystem readFs,
		IFileSystem writeFs,
		IConfigurationContext configurationContext
	)
	{
		_writeFileSystem = writeFs;
		var builder = WebApplication.CreateSlimBuilder();
		_ = builder.AddDocumentationServiceDefaults();

		builder.Services.AddElasticDocsApiUsecases("dev");
		_ = builder.Logging
			.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Error)
			.AddFilter("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogLevel.Error)
			.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning)
			.AddFilter("Microsoft.AspNetCore.Http.Result.ContentResult", LogLevel.Warning)
			.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);

		var collector = new LiveModeDiagnosticsCollector(logFactory);

		var hostUrl = $"http://localhost:{port}";

		_hostedService = collector;
		Context = new BuildContext(collector, readFs, writeFs, configurationContext, ExportOptions.Default, path, null)
		{
			CanonicalBaseUrl = new Uri(hostUrl),
		};
		GeneratorState = new ReloadableGeneratorState(logFactory, Context.DocumentationSourceDirectory, Context.OutputDirectory, Context);
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
			.UseLiveReloadWithManualScriptInjection(_webApplication.Lifetime)
			.UseDeveloperExceptionPage(new DeveloperExceptionPageOptions())
			.Use(async (context, next) =>
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
					{
						Console.WriteLine($"[INNER EXCEPTION] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
					}

					throw; // Re-throw to let ASP.NET Core handle it
				}
			})
			.UseStaticFiles(
				new StaticFileOptions
				{
					FileProvider = new EmbeddedOrPhysicalFileProvider(Context),
					RequestPath = "/_static"
				})
			.UseRouting();

		_ = _webApplication.MapGet("/", (ReloadableGeneratorState holder, Cancel ctx) =>
			ServeDocumentationFile(holder, "index", ctx));

		_ = _webApplication.MapGet("/api/", (ReloadableGeneratorState holder, Cancel ctx) =>
			ServeApiFile(holder, "", ctx));

		_ = _webApplication.MapGet("/api/{**slug}", (string slug, ReloadableGeneratorState holder, Cancel ctx) =>
			ServeApiFile(holder, slug, ctx));

		var apiV1 = _webApplication.MapGroup("/docs/_api/v1");
		apiV1.MapElasticDocsApiEndpoints();

		_ = _webApplication.MapGet("{**slug}", (string slug, ReloadableGeneratorState holder, Cancel ctx) =>
			ServeDocumentationFile(holder, slug, ctx));
	}

	private async Task<IResult> ServeApiFile(ReloadableGeneratorState holder, string slug, Cancel ctx)
	{
		var x = LiveReloadConfiguration.Current.LiveReloadScriptUrl;

		var path = Path.Combine(holder.ApiPath.FullName, slug.Trim('/'), "index.html");
		var info = _writeFileSystem.FileInfo.New(path);
		if (info.Exists)
		{
			//TODO STREAM
			var contents = await _writeFileSystem.File.ReadAllTextAsync(info.FullName, ctx);
			return LiveReloadHtml(contents, Encoding.UTF8, 200);
		}

		return Results.NotFound();
	}

	private static async Task<IResult> ServeDocumentationFile(ReloadableGeneratorState holder, string slug, Cancel ctx)
	{
		if (slug == ".well-known/appspecific/com.chrome.devtools.json")
			return Results.NotFound();

		var generator = holder.Generator;
		const string navPartialSuffix = ".nav.html";

		// Check if the original request is asking for LLM-rendered markdown
		var requestLlmMarkdown = slug.EndsWith(".md");

		// If requesting .md output, remove the .md extension to find the source file
		if (requestLlmMarkdown)
			slug = slug[..^3]; // Remove ".md" extension

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
				if (requestLlmMarkdown)
				{
					// Render using LLM pipeline for CommonMark output
					var llmRendered = await generator.RenderLlmMarkdown(markdown, ctx);
					return Results.Content(llmRendered, "text/markdown; charset=utf-8");
				}

				// Regular HTML rendering
				var rendered = await generator.RenderLayout(markdown, ctx);
				return LiveReloadHtml(rendered.Html);

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

	private static IResult LiveReloadHtml(string content, Encoding? encoding = null, int? statusCode = null)
	{
		if (LiveReloadConfiguration.Current.LiveReloadEnabled)
		{
			//var script = WebsocketScriptInjectionHelper.GetWebSocketClientJavaScript(context, true);
			//var html = $"<script>\n{script}\n</script>";
			var html = "\n" + $@"<script src=""{LiveReloadConfiguration.Current.LiveReloadScriptUrl}"" defer></script>";
			content += html;
		}

		return Results.Content(content, "text/html", encoding, statusCode);
	}
}
