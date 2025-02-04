// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reflection;
using Documentation.Builder.Diagnostics;
using Documentation.Builder.Diagnostics.Console;
using Documentation.Builder.Diagnostics.LiveMode;
using Elastic.Markdown;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
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
	private readonly ILogger<DocumentationWebHost> _logger;

	public DocumentationWebHost(string? path, int port, ILoggerFactory logger, IFileSystem fileSystem)
	{
		_logger = logger.CreateLogger<DocumentationWebHost>();
		var builder = WebApplication.CreateSlimBuilder();

		builder.Logging.ClearProviders();
		builder.Logging.SetMinimumLevel(LogLevel.Warning)
			.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Error)
			.AddFilter("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogLevel.Error)
			.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information)

			.AddSimpleConsole(o => o.SingleLine = true);

		_context = new BuildContext(fileSystem, fileSystem, path, null)
		{
			Collector = new LiveModeDiagnosticsCollector(logger)
		};
		builder.Services.AddAotLiveReload(s =>
		{
			s.FolderToMonitor = _context.SourcePath.FullName;
			s.ClientFileExtensions = ".md,.yml";
		});
		builder.Services.AddSingleton<ReloadableGeneratorState>(_ => new ReloadableGeneratorState(_context.SourcePath, null, _context, logger));
		builder.Services.AddHostedService<ReloadGeneratorService>();
		if (IsDotNetWatchBuild())
			builder.Services.AddHostedService<ParcelWatchService>();

		builder.WebHost.UseUrls($"http://localhost:{port}");

		_webApplication = builder.Build();
		SetUpRoutes();
	}

	private bool IsDotNetWatchBuild() =>
		Environment.GetEnvironmentVariable("DOTNET_WATCH") is not null;

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
		_webApplication.UseLiveReload();
		_webApplication.UseStaticFiles(new StaticFileOptions
		{
			FileProvider = new EmbeddedOrPhysicalFileProvider(_context),
			RequestPath = "/_static"
		});
		_webApplication.UseRouting();

		_webApplication.MapGet("/", (ReloadableGeneratorState holder, Cancel ctx) =>
			ServeDocumentationFile(holder, "index.md", ctx));

		_webApplication.MapGet("{**slug}", (string slug, ReloadableGeneratorState holder, Cancel ctx) =>
			ServeDocumentationFile(holder, slug, ctx));
	}

	private static async Task<IResult> ServeDocumentationFile(ReloadableGeneratorState holder, string slug, Cancel ctx)
	{
		var generator = holder.Generator;

		// For now, the logic is backwards compatible.
		// Hence, both http://localhost:5000/migration/versioning.html and http://localhost:5000/migration/versioning works,
		// so it's easier to copy links from issues created during the bug bounty.
		// However, we can remove this logic in the future and only support links without the .html extension.
		var s = Path.GetExtension(slug) == string.Empty ? Path.Combine(slug, "index.md") : slug.Replace(".html", ".md");
		if (!generator.DocumentationSet.FlatMappedFiles.TryGetValue(s, out var documentationFile))
		{
			s = Path.GetExtension(slug) == string.Empty ? slug + ".md" : s.Replace("/index.md", ".md");
			if (!generator.DocumentationSet.FlatMappedFiles.TryGetValue(s, out documentationFile))
				return Results.NotFound();
		}

		switch (documentationFile)
		{
			case MarkdownFile markdown:
				{
					var rendered = await generator.RenderLayout(markdown, ctx);

					return Results.Content(rendered, "text/html");
				}
			case ImageFile image:
				return Results.File(image.SourceFile.FullName, image.MimeType);
			default:
				return Results.NotFound();
		}
	}
}


public class EmbeddedOrPhysicalFileProvider : IFileProvider
{
	private readonly EmbeddedFileProvider _embeddedProvider = new(typeof(BuildContext).Assembly, "Elastic.Markdown._static");
	private readonly PhysicalFileProvider? _staticFilesInDocsFolder;

	private readonly PhysicalFileProvider? _staticWebFilesDuringDebug = null;

	public EmbeddedOrPhysicalFileProvider(BuildContext context)
	{
		var documentationStaticFiles = Path.Combine(context.SourcePath.FullName, "_static");
#if DEBUG
		// this attempts to serve files directly from their source rather than the embedded resources during development.
		// this allows us to change js/css files without restarting the webserver
		var solutionRoot = Paths.GetSolutionDirectory();
		if (solutionRoot != null)
		{

			var debugWebFiles = Path.Combine(solutionRoot.FullName, "src", "Elastic.Markdown", "_static");
			_staticWebFilesDuringDebug = new PhysicalFileProvider(debugWebFiles);
		}
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
}
