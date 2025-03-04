// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Documentation.Builder.Diagnostics.LiveMode;
using Elastic.Documentation.Tooling;
using Elastic.Markdown;
using Elastic.Markdown.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Westwind.AspNetCore.LiveReload;
using IFileInfo = Microsoft.Extensions.FileProviders.IFileInfo;

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
		_context = new BuildContext(collector, fileSystem, fileSystem, path, null);
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

		_ = builder.WebHost.UseUrls($"http://localhost:{port}");

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

		var s = Path.GetExtension(slug) == string.Empty ? slug + ".md" : slug;
		if (!generator.DocumentationSet.FlatMappedFiles.TryGetValue(s, out var documentationFile))
		{
			foreach (var extension in generator.Context.Configuration.EnabledExtensions)
			{
				if (extension.TryGetDocumentationFileBySlug(generator.DocumentationSet, slug, out documentationFile))
					break;
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
				return Results.NotFound();
		}
	}
}


public sealed class EmbeddedOrPhysicalFileProvider : IFileProvider, IDisposable
{
	private readonly EmbeddedFileProvider _embeddedProvider = new(typeof(BuildContext).Assembly, "Elastic.Markdown._static");
	private readonly PhysicalFileProvider? _staticFilesInDocsFolder;

	private readonly PhysicalFileProvider? _staticWebFilesDuringDebug;

	public EmbeddedOrPhysicalFileProvider(BuildContext context)
	{
		var documentationStaticFiles = Path.Combine(context.DocumentationSourceDirectory.FullName, "_static");
#if DEBUG
		// this attempts to serve files directly from their source rather than the embedded resources during development.
		// this allows us to change js/css files without restarting the webserver
		var solutionRoot = Paths.GetSolutionDirectory();
		if (solutionRoot != null)
		{

			var debugWebFiles = Path.Combine(solutionRoot.FullName, "src", "Elastic.Markdown", "_static");
			_staticWebFilesDuringDebug = new PhysicalFileProvider(debugWebFiles);
		}
#else
		_staticWebFilesDuringDebug = null;
#endif
		if (context.ReadFileSystem.Directory.Exists(documentationStaticFiles))
			_staticFilesInDocsFolder = new PhysicalFileProvider(documentationStaticFiles);
	}

	private T? FirstYielding<T>(string arg, Func<string, PhysicalFileProvider, T?> predicate) =>
		Yield(arg, predicate, _staticWebFilesDuringDebug) ?? Yield(arg, predicate, _staticFilesInDocsFolder);

	private static T? Yield<T>(string arg, Func<string, PhysicalFileProvider, T?> predicate, PhysicalFileProvider? provider)
	{
		if (provider is null)
			return default;
		var result = predicate(arg, provider);
		return result ?? default;
	}

	public IDirectoryContents GetDirectoryContents(string subpath)
	{
		var contents = FirstYielding(subpath, static (a, p) => p.GetDirectoryContents(a));
		if (contents is null || !contents.Exists)
			contents = _embeddedProvider.GetDirectoryContents(subpath);
		return contents;
	}

	public IFileInfo GetFileInfo(string subpath)
	{
		var path = subpath.Replace("/_static", "");
		var fileInfo = FirstYielding(path, static (a, p) => p.GetFileInfo(a));
		if (fileInfo is null || !fileInfo.Exists)
			fileInfo = _embeddedProvider.GetFileInfo(subpath);
		return fileInfo;
	}

	public IChangeToken Watch(string filter)
	{
		var changeToken = FirstYielding(filter, static (f, p) => p.Watch(f));
		if (changeToken is null or NullChangeToken)
			changeToken = _embeddedProvider.Watch(filter);
		return changeToken;
	}

	public void Dispose()
	{
		_staticFilesInDocsFolder?.Dispose();
		_staticWebFilesDuringDebug?.Dispose();
	}
}
