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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Http;

public class DocumentationWebHost
{
	private readonly WebApplication _webApplication;

	private readonly string _staticFilesDirectory =
		Path.Combine(Paths.Root.FullName, "docs", "source", "_static");

	public DocumentationWebHost(string? path, ILoggerFactory logger, IFileSystem fileSystem)
	{
		var builder = WebApplication.CreateSlimBuilder();
		var sourcePath = path != null ? fileSystem.DirectoryInfo.New(path) : null;
		var context = new BuildContext
		{
			ReadFileSystem = fileSystem,
			WriteFileSystem = fileSystem,
			Collector = new ConsoleDiagnosticsCollector(logger)
		};
		builder.Services.AddSingleton<ReloadableGeneratorState>(_ => new ReloadableGeneratorState(sourcePath, null, context, logger));
		builder.Services.AddHostedService<ReloadGeneratorService>();
		builder.Services.AddSingleton(logger);
		builder.Logging.SetMinimumLevel(LogLevel.Warning);

		_webApplication = builder.Build();
		SetUpRoutes();
	}

	public async Task RunAsync(Cancel ctx) => await _webApplication.RunAsync(ctx);

	private void SetUpRoutes()
	{
		_webApplication.UseStaticFiles(new StaticFileOptions
		{
			FileProvider = new PhysicalFileProvider(_staticFilesDirectory),
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
				await markdown.ParseAsync(ctx);
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
