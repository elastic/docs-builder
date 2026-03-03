// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.ApiExplorer;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Http;

/// <summary>Singleton behavior enforced by registration on <see cref="IServiceCollection"/></summary>
public class ReloadableGeneratorState : IDisposable
{
	private IDirectoryInfo SourcePath { get; }
	private IDirectoryInfo OutputPath { get; }
	public IDirectoryInfo ApiPath { get; }

	private DocumentationGenerator _generator;
	private readonly ILoggerFactory _logFactory;
	private readonly BuildContext _context;
	private readonly bool _isWatchBuild;
	private readonly DocSetConfigurationCrossLinkFetcher _crossLinkFetcher;
	private readonly ILinkIndexReader? _codexReader;

	public ReloadableGeneratorState(ILoggerFactory logFactory,
		IDirectoryInfo sourcePath,
		IDirectoryInfo outputPath,
		BuildContext context,
		bool isWatchBuild
	)
	{
		_logFactory = logFactory;
		_context = context;
		_isWatchBuild = isWatchBuild;
		SourcePath = sourcePath;
		OutputPath = outputPath;
		ApiPath = context.WriteFileSystem.DirectoryInfo.New(Path.Combine(outputPath.FullName, "api"));

		if (context.Configuration.Registry != DocSetRegistry.Public)
			_codexReader = new GitLinkIndexReader(context.Configuration.Registry.ToStringFast(true), context.ReadFileSystem);

		_crossLinkFetcher = new DocSetConfigurationCrossLinkFetcher(logFactory, _context.Configuration, codexLinkIndexReader: _codexReader);
		// we pass NoopCrossLinkResolver.Instance here because `ReloadAsync` will always be called when the <see cref="ReloadableGeneratorState"/> is started.
		_generator = new DocumentationGenerator(new DocumentationSet(context, logFactory, NoopCrossLinkResolver.Instance), logFactory);
	}

	public DocumentationGenerator Generator => _generator;

	// Track OpenAPI spec file modification times to detect changes
	private readonly Dictionary<string, DateTimeOffset> _openApiSpecLastModified = [];

	public async Task ReloadAsync(Cancel ctx)
	{
		SourcePath.Refresh();
		OutputPath.Refresh();
		var crossLinks = await _crossLinkFetcher.FetchCrossLinks(ctx);
		IUriEnvironmentResolver? uriResolver = crossLinks.CodexRepositories is not null
			? new CodexAwareUriResolver(crossLinks.CodexRepositories)
			: null;
		var crossLinkResolver = new CrossLinkResolver(crossLinks, uriResolver);
		var docSet = new DocumentationSet(_context, _logFactory, crossLinkResolver);

		// Add LLM markdown export for dev server
		var markdownExporters = new List<IMarkdownExporter>();
		if (!_isWatchBuild)
			markdownExporters.AddLlmMarkdownExport(); // Consistent LLM-optimized output

		var generator = new DocumentationGenerator(docSet, _logFactory, markdownExporters: markdownExporters.ToArray());
		await generator.ResolveDirectoryTree(ctx);
		_ = Interlocked.Exchange(ref _generator, generator);

		// Only regenerate OpenAPI if spec files have changed
		if (HaveOpenApiSpecsChanged(docSet.Configuration))
		{
			await ReloadApiReferences(generator.MarkdownStringRenderer, ctx);
			UpdateOpenApiSpecTimestamps(docSet.Configuration);
		}
	}

	private bool HaveOpenApiSpecsChanged(ConfigurationFile config)
	{
		if (_isWatchBuild)
			return false;
		if (config.OpenApiSpecifications is null)
			return false;

		// First run - no timestamps yet
		if (_openApiSpecLastModified.Count == 0)
			return true;

		foreach (var (_, fileInfo) in config.OpenApiSpecifications)
		{
			fileInfo.Refresh();
			if (!_openApiSpecLastModified.TryGetValue(fileInfo.FullName, out var lastModified))
				return true; // New file
			if (fileInfo.LastWriteTimeUtc > lastModified)
				return true; // File modified
		}

		return false;
	}

	private void UpdateOpenApiSpecTimestamps(ConfigurationFile config)
	{
		if (config.OpenApiSpecifications is null)
			return;

		_openApiSpecLastModified.Clear();
		foreach (var (_, fileInfo) in config.OpenApiSpecifications)
		{
			fileInfo.Refresh();
			_openApiSpecLastModified[fileInfo.FullName] = fileInfo.LastWriteTimeUtc;
		}
	}

	public async Task ReloadApiReferences(Cancel ctx) => await ReloadApiReferences(_generator.MarkdownStringRenderer, ctx);

	private async Task ReloadApiReferences(IMarkdownStringRenderer markdownStringRenderer, Cancel ctx)
	{
		if (_isWatchBuild)
			return;

		if (ApiPath.Exists)
			ApiPath.Delete(true);
		ApiPath.Create();
		var generator = new OpenApiGenerator(_logFactory, _context, markdownStringRenderer);
		await generator.Generate(ctx);
	}

	public void Dispose()
	{
		(_codexReader as IDisposable)?.Dispose();
		GC.SuppressFinalize(this);
	}
}
