// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.ApiExplorer;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.FileSystems;
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
	private DocSetConfigurationCrossLinkFetcher _crossLinkFetcher;
	private ILinkIndexReader? _codexReader;
	private FetchedCrossLinks? _cachedCrossLinks;

	public ReloadableGeneratorState(
		ILoggerFactory logFactory,
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
		ApiPath = context.WriteFileSystem.DirectoryInfo.New(Path.Join(outputPath.FullName, "api"));

		if (context.Configuration.Registry != DocSetRegistry.Public)
			_codexReader = new GitLinkIndexReader(context.Configuration.Registry.ToStringFast(true), new ApplicationDataFileSystem());

		_crossLinkFetcher = new DocSetConfigurationCrossLinkFetcher(logFactory, _context.Configuration, codexLinkIndexReader: _codexReader);
		// we pass NoopCrossLinkResolver.Instance here because `ReloadAsync` will always be called when the <see cref="ReloadableGeneratorState"/> is started.
		_generator = new DocumentationGenerator(new DocumentationSet(context, logFactory, NoopCrossLinkResolver.Instance), logFactory);
	}

	public DocumentationGenerator Generator => _generator;

	// Track OpenAPI spec file modification times to detect changes
	private readonly Dictionary<string, DateTimeOffset> _openApiSpecLastModified = [];

	// Track API markdown modification times so serve reloads on overlay and children: edits.
	private readonly Dictionary<string, DateTimeOffset> _apiMarkdownFilesLastModified = [];

	private volatile bool _apiReferencesStale = true;
	private readonly SemaphoreSlim _apiSemaphore = new(1, 1);
	private CancellationTokenSource? _apiGenerationCts;

	public async Task ReloadAsync(Cancel ctx, bool reloadConfiguration = true)
	{
		// Content-only changes (e.g. .md edits) don't need a full rebuild:
		// RenderLayout -> ParseFullAsync reads fresh content from disk on each request.
		// API overlay files are an exception: they are baked into generated HTML, so mark
		// refs stale and let EnsureApiReferencesAsync re-check timestamps on the next /api request.
		if (!reloadConfiguration && _cachedCrossLinks is not null)
		{
			_apiReferencesStale = true;
			return;
		}

		SourcePath.Refresh();
		OutputPath.Refresh();
		if (reloadConfiguration)
		{
			_context.ReloadConfiguration();
			(_codexReader as IDisposable)?.Dispose();
			_codexReader = _context.Configuration.Registry != DocSetRegistry.Public
				? new GitLinkIndexReader(_context.Configuration.Registry.ToStringFast(true), new ApplicationDataFileSystem())
				: null;
			_crossLinkFetcher = new DocSetConfigurationCrossLinkFetcher(
				_logFactory,
				_context.Configuration,
				codexLinkIndexReader: _codexReader
			);
		}
		var crossLinks = _cachedCrossLinks;
		if (crossLinks is null || reloadConfiguration)
		{
			crossLinks = await _crossLinkFetcher.FetchCrossLinks(ctx);
			// Only cache successful fetches so transient failures get retried on the next reload.
			_cachedCrossLinks = crossLinks.IsComplete ? crossLinks : null;
		}
		IUriEnvironmentResolver? uriResolver = crossLinks.CodexRepositories is not null
			? new CodexAwareUriResolver(crossLinks.CodexRepositories)
			: null;
		var crossLinkResolver = new CrossLinkResolver(crossLinks, uriResolver);
		var releaseNotesResolver = await ReleaseNotesFetcher.PrefetchAsync(_context, _logFactory, ctx);
		var docSet = new DocumentationSet(_context, _logFactory, crossLinkResolver, releaseNotesResolver);

		// Add LLM markdown export for dev server
		var markdownExporters = new List<IMarkdownExporter>();
		if (!_isWatchBuild)
			markdownExporters.AddLlmMarkdownExport(branded: _context.Configuration.Branding is not null); // Consistent LLM-optimized output

		var generator = new DocumentationGenerator(docSet, _logFactory, markdownExporters: markdownExporters.ToArray());
		await generator.ResolveDirectoryTree(ctx);
		_ = Interlocked.Exchange(ref _generator, generator);
		_apiReferencesStale = true;
	}

	/// <summary>Lazily generates OpenAPI references on the first /api/ request, and regenerates when spec files change.</summary>
	public async Task EnsureApiReferencesAsync(Cancel ctx)
	{
		if (!_apiReferencesStale)
			return;

		// Create isolated cancellation for API generation (or reuse existing)
		_apiGenerationCts ??= new CancellationTokenSource();

		// Use combined token that respects both HTTP cancellation AND generation-specific timeout
		using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(ctx, _apiGenerationCts.Token);
		combinedCts.CancelAfter(TimeSpan.FromMinutes(5)); // Reasonable timeout for API generation

		await _apiSemaphore.WaitAsync(ctx); // Still respect immediate HTTP cancellation for semaphore
		try
		{
			if (!_apiReferencesStale)
				return;

			// Use the isolated token for actual generation
			var config = _generator.DocumentationSet.Configuration;
			if (HaveOpenApiSpecsChanged(config))
			{
				await ReloadApiReferences(_generator.MarkdownStringRenderer, combinedCts.Token);
				UpdateOpenApiSpecTimestamps(config);
			}
			_apiReferencesStale = false;
		}
		finally
		{
			_ = _apiSemaphore.Release();
		}
	}

	private bool HaveOpenApiSpecsChanged(ConfigurationFile config)
	{
		if (_isWatchBuild)
			return false;
		if (config.ApiConfigurations is null)
			return false;

		if (_openApiSpecLastModified.Count == 0 && _apiMarkdownFilesLastModified.Count == 0)
			return true;

		foreach (var apiConfig in config.ApiConfigurations.Values)
		{
			if (apiConfig.LocalSpecFile is { } specFile)
			{
				specFile.Refresh();
				if (!_openApiSpecLastModified.TryGetValue(specFile.FullName, out var lastModified))
					return true;
				if (specFile.LastWriteTimeUtc > lastModified)
					return true;
			}
		}

		return HaveApiMarkdownFilesChanged(config);
	}

	private bool HaveApiMarkdownFilesChanged(ConfigurationFile config)
	{
		var current = CurrentApiMarkdownTimestamps(config);
		if (current.Count != _apiMarkdownFilesLastModified.Count)
			return true;

		foreach (var (path, time) in current)
		{
			if (!_apiMarkdownFilesLastModified.TryGetValue(path, out var lastModified) || time > lastModified)
				return true;
		}

		return false;
	}

	private static Dictionary<string, DateTimeOffset> CurrentApiMarkdownTimestamps(ConfigurationFile config)
	{
		var current = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
		if (config.ApiConfigurations is null)
			return current;

		foreach (var apiConfig in config.ApiConfigurations.Values)
		{
			foreach (var file in apiConfig.EnumerateApiMarkdownFiles().Concat(apiConfig.Children))
			{
				file.Refresh();
				current[file.FullName] = file.LastWriteTimeUtc;
			}
		}

		return current;
	}

	private void UpdateOpenApiSpecTimestamps(ConfigurationFile config)
	{
		_openApiSpecLastModified.Clear();
		_apiMarkdownFilesLastModified.Clear();

		if (config.ApiConfigurations is null)
			return;

		foreach (var apiConfig in config.ApiConfigurations.Values)
		{
			if (apiConfig.LocalSpecFile is { } specFile)
			{
				specFile.Refresh();
				_openApiSpecLastModified[specFile.FullName] = specFile.LastWriteTimeUtc;
			}
		}

		foreach (var (path, time) in CurrentApiMarkdownTimestamps(config))
			_apiMarkdownFilesLastModified[path] = time;
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
		_apiGenerationCts?.Cancel();
		_apiGenerationCts?.Dispose();
		_apiSemaphore.Dispose();
		(_codexReader as IDisposable)?.Dispose();
		GC.SuppressFinalize(this);
	}
}
