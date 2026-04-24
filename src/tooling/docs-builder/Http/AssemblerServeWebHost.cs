// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Assembler;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.ServiceDefaults;
using Elastic.Documentation.Site.FileProviders;
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

public class AssemblerServeWebHost
{
	private readonly WebApplication _webApplication;

	// Sorted longest-prefix-first for greedy matching
	private readonly (string Prefix, string FileSlugBase, AssemblerDocumentationSet Set, DocumentationGenerator Generator)[] _prefixMap;

	// Shortest top-level prefix → first real content section (e.g. "/docs/get-started")
	private readonly string _rootRedirectUrl;

	public AssemblerServeWebHost(
		int port,
		AssembleSources assembleSources,
		AssemblerBuilder assemblerBuilder,
		ILoggerFactory logFactory,
		bool watchMarkdown = true
	)
	{
		_prefixMap = BuildPrefixMap(assembleSources, assemblerBuilder);
		_rootRedirectUrl = "/" + (_prefixMap
			.OrderBy(e => e.Prefix.Length)
			.Select(e => e.Prefix)
			.FirstOrDefault() ?? assembleSources.AssembleContext.Environment.PathPrefix ?? "");

		var urlPathPrefix = assembleSources.AssembleSets.Values.FirstOrDefault()?.BuildContext.UrlPathPrefix ?? "";

		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ContentRootPath = Paths.WorkingDirectoryRoot.FullName
		});

		_ = builder.AddDocumentationServiceDefaults();

		_ = builder.Logging
			.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Error)
			.AddFilter("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogLevel.Error)
			.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);

		_ = builder.Services.AddAotLiveReload(s =>
		{
			s.FolderToMonitor = assembleSources.AssembleContext.CheckoutDirectory.FullName;
			s.ClientFileExtensions = ".md,.yml";
		});

		var sets = assembleSources.AssembleSets.Values.ToList();
		_ = builder.Services.AddHostedService<AssemblerReloadService>(_ =>
			new AssemblerReloadService(sets, watchMarkdown, logFactory.CreateLogger<AssemblerReloadService>())
		);

		_ = builder.WebHost.UseUrls($"http://localhost:{port}");

		_webApplication = builder.Build();
		SetUpRoutes(assembleSources, urlPathPrefix);
	}

	public async Task RunAsync(Cancel ctx) => await _webApplication.RunAsync(ctx);

	public async Task StopAsync(Cancel ctx) => await _webApplication.StopAsync(ctx);

	private static (string Prefix, string FileSlugBase, AssemblerDocumentationSet Set, DocumentationGenerator Generator)[] BuildPrefixMap(
		AssembleSources assembleSources, AssemblerBuilder assemblerBuilder)
	{
		var envPathPrefix = assembleSources.AssembleContext.Environment.PathPrefix; // e.g. "docs"
		var entries = new List<(string, string, AssemblerDocumentationSet, DocumentationGenerator)>();
		var generatorCache = new Dictionary<string, DocumentationGenerator>();

		foreach (var (uri, mapping) in assembleSources.NavigationTocMappings)
		{
			var repoName = uri.Scheme;
			if (!assembleSources.AssembleSets.TryGetValue(repoName, out var set))
				continue;

			// Reconstruct the path within SourceDirectory for this TOC mapping.
			// The URI host is the subfolder of the repo's content root that this TOC covers.
			var fileSlugBase = Path.Join(uri.Host, uri.AbsolutePath.Trim('/')).TrimStart('/');

			// The URL slug includes the global path prefix (e.g. "docs/release-notes/elasticsearch").
			// SourcePathPrefix is just the suffix (e.g. "release-notes/elasticsearch"), so prepend.
			var urlPrefix = string.IsNullOrEmpty(envPathPrefix)
				? mapping.SourcePathPrefix
				: string.IsNullOrEmpty(mapping.SourcePathPrefix)
					? envPathPrefix
					: $"{envPathPrefix}/{mapping.SourcePathPrefix}";

			// Reuse the same generator for the same repo across multiple TOC entries
			if (!generatorCache.TryGetValue(repoName, out var generator))
			{
				generator = assemblerBuilder.CreateGenerator(set);
				generatorCache[repoName] = generator;
			}

			entries.Add((urlPrefix, fileSlugBase, set, generator));
		}
		return [.. entries.OrderByDescending(e => e.Item1.Length)];
	}

	private void SetUpRoutes(AssembleSources assembleSources, string urlPathPrefix)
	{
		var firstBuildContext = assembleSources.AssembleSets.Values.FirstOrDefault()?.BuildContext;

		// Static assets MUST come before UseRouting so they're served before the {**slug} catch-all matches
		var staticRequestPath = string.IsNullOrEmpty(urlPathPrefix) ? "/_static" : $"{urlPathPrefix}/_static";
		var pipeline = _webApplication
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
						Console.WriteLine($"[INNER EXCEPTION] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
					throw;
				}
			});

		if (firstBuildContext != null)
		{
			_ = pipeline.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new EmbeddedOrPhysicalFileProvider(firstBuildContext),
				RequestPath = staticRequestPath
			});
		}

		_ = pipeline.UseRouting();

		_ = _webApplication.MapGet("/", (Cancel ctx) => ServeRoot(ctx));
		_ = _webApplication.MapGet("{**slug}", (string slug, Cancel ctx) => ServeDocumentationFile(slug, ctx));
	}

	private Task<IResult> ServeRoot(Cancel _) =>
		Task.FromResult(Results.Redirect(_rootRedirectUrl));

	private async Task<IResult> ServeDocumentationFile(string slug, Cancel ctx)
	{
		if (slug == ".well-known/appspecific/com.chrome.devtools.json")
			return Results.NotFound();

		// Static asset requests that UseStaticFiles couldn't serve → 404, not a redirect
		if (slug.Contains("/_static/") || slug.Contains("\\_static\\"))
			return Results.NotFound();

		var match = FindRepo(slug);
		if (match is null)
			return Results.Redirect(_rootRedirectUrl);

		var (set, generator, relFileSlug) = match.Value;

		// Mirror the file lookup logic from DocumentationWebHost.ServeDocumentationFile
		var s = Path.GetExtension(relFileSlug) == string.Empty
			? Path.Join(relFileSlug, "index.md")
			: relFileSlug;
		var fp = new FilePath(s, generator.DocumentationSet.SourceDirectory);

		if (!generator.DocumentationSet.Files.TryGetValue(fp, out var documentationFile))
		{
			s = Path.GetExtension(relFileSlug) == string.Empty
				? relFileSlug + ".md"
				: s.Replace($"{Path.DirectorySeparatorChar}index.md", ".md");
			fp = new FilePath(s, generator.DocumentationSet.SourceDirectory);
			_ = generator.DocumentationSet.Files.TryGetValue(fp, out documentationFile);
		}

		switch (documentationFile)
		{
			case MarkdownFile markdown:
				var rendered = await generator.RenderLayout(markdown, ctx);
				return LiveReloadHtml(rendered.Html);
			case ImageFile image:
				return Results.File(image.SourceFile.FullName, image.MimeType);
			default:
				// If this is a navigation node (index.md not found), try redirect to first child
				var fp404 = new FilePath("404.md", generator.DocumentationSet.SourceDirectory);
				if (generator.DocumentationSet.Files.TryGetValue(fp404, out var notFound) && notFound is MarkdownFile notFoundMd)
				{
					var renderedNotFound = await generator.RenderLayout(notFoundMd, ctx);
					return Results.Content(renderedNotFound.Html, "text/html", null, 404);
				}
				return Results.NotFound();
		}
	}

	private (AssemblerDocumentationSet Set, DocumentationGenerator Generator, string RelFileSlug)? FindRepo(string slug)
	{
		foreach (var (prefix, fileSlugBase, set, generator) in _prefixMap)
		{
			if (!slug.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				continue;
			var afterPrefix = slug[prefix.Length..].TrimStart('/');
			var relFileSlug = string.IsNullOrEmpty(fileSlugBase)
				? afterPrefix
				: string.IsNullOrEmpty(afterPrefix) ? fileSlugBase : $"{fileSlugBase}/{afterPrefix}";
			return (set, generator, relFileSlug);
		}
		return null;
	}

	private static IResult LiveReloadHtml(string content)
	{
		if (LiveReloadConfiguration.Current.LiveReloadEnabled)
		{
			var script = $"\n<script src=\"{LiveReloadConfiguration.Current.LiveReloadScriptUrl}\" defer></script>";
			content += script;
		}
		return Results.Content(content, "text/html");
	}
}
