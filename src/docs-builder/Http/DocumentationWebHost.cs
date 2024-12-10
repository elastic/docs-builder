// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Documentation.Builder.Diagnostics;
using Elastic.Markdown;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Microsoft.AspNetCore.Builder;
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

	private readonly string _staticFilesDirectory;

	public DocumentationWebHost(string? path, ILoggerFactory logger, IFileSystem fileSystem)
	{
		var builder = WebApplication.CreateSlimBuilder();
		var context = new BuildContext(fileSystem, fileSystem, path, null)
		{
			Collector = new ConsoleDiagnosticsCollector(logger)
		};
		builder.Services.AddLiveReload(s =>
		{
			s.FolderToMonitor = context.SourcePath.FullName;
			s.ClientFileExtensions = ".md,.yml";
		});
		builder.Services.AddSingleton<ReloadableGeneratorState>(_ => new ReloadableGeneratorState(context.SourcePath, null, context, logger));
		builder.Services.AddHostedService<ReloadGeneratorService>();
		builder.Services.AddSingleton(logger);
		builder.Logging.SetMinimumLevel(LogLevel.Warning);

		_staticFilesDirectory = Path.Combine(context.SourcePath.FullName, "_static");
		#if DEBUG
		// this attempts to serve files directly from their source rather than the embedded resourses during development.
		// this allows us to change js/css files without restarting the webserver
		var solutionRoot = Paths.GetSolutionDirectory();
		if (solutionRoot != null)
			_staticFilesDirectory = Path.Combine(solutionRoot.FullName, "src", "Elastic.Markdown", "_static");
		#endif

		_webApplication = builder.Build();
		SetUpRoutes();
	}


	public async Task RunAsync(Cancel ctx) => await _webApplication.RunAsync(ctx);

	private void SetUpRoutes()
	{
		_webApplication.UseLiveReload();
		_webApplication.UseStaticFiles(new StaticFileOptions
		{
			FileProvider = new EmbeddedOrPhysicalFileProvider(_staticFilesDirectory),
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
		slug = slug.Replace(".html", ".md");
		if (!generator.DocumentationSet.FlatMappedFiles.TryGetValue(slug, out var documentationFile))
			return Results.NotFound();

		switch (documentationFile)
		{
			case MarkdownFile markdown:
			{
				await markdown.ParseFullAsync(ctx);
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
	private readonly EmbeddedFileProvider _embeddedProvider;
	private readonly PhysicalFileProvider _fileProvider;

	public EmbeddedOrPhysicalFileProvider(string root)
	{
		_embeddedProvider = new EmbeddedFileProvider(typeof(BuildContext).Assembly, "Elastic.Markdown._static");
		_fileProvider = new PhysicalFileProvider(root);
	}

	public IDirectoryContents GetDirectoryContents(string subpath)
	{
		var contents = _fileProvider.GetDirectoryContents(subpath);
		if (!contents.Exists)
			contents = _embeddedProvider.GetDirectoryContents(subpath);
		return contents;
	}

	public IFileInfo GetFileInfo(string subpath)
	{
		var fileInfo = _fileProvider.GetFileInfo(subpath.Replace("/_static", ""));
		if (!fileInfo.Exists)
			fileInfo = _embeddedProvider.GetFileInfo(subpath);
		return fileInfo;
	}

	public IChangeToken Watch(string filter)
	{
		var changeToken = _fileProvider.Watch(filter);
		if (changeToken is NullChangeToken)
			changeToken = _embeddedProvider.Watch(filter);
		return changeToken;
	}
}
