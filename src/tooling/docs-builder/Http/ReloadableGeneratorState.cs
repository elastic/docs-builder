// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.ApiExplorer;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.Links.CrossLinks;
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
	private readonly DocSetConfigurationCrossLinkFetcher _crossLinkFetcher;

	public ReloadableGeneratorState(ILoggerFactory logFactory,
		IDirectoryInfo sourcePath,
		IDirectoryInfo outputPath,
		BuildContext context)
	{
		_logFactory = logFactory;
		_context = context;
		SourcePath = sourcePath;
		OutputPath = outputPath;
		ApiPath = context.WriteFileSystem.DirectoryInfo.New(Path.Combine(outputPath.FullName, "api"));
		_crossLinkFetcher = new DocSetConfigurationCrossLinkFetcher(logFactory, _context.Configuration);
		// we pass NoopCrossLinkResolver.Instance here because `ReloadAsync` will always be called when the <see cref="ReloadableGeneratorState"/> is started.
		_generator = new DocumentationGenerator(new DocumentationSet(context, logFactory, NoopCrossLinkResolver.Instance), logFactory);
	}

	public DocumentationGenerator Generator => _generator;

	public async Task ReloadAsync(Cancel ctx)
	{
		SourcePath.Refresh();
		OutputPath.Refresh();
		var crossLinks = await _crossLinkFetcher.FetchCrossLinks(ctx);
		var crossLinkResolver = new CrossLinkResolver(crossLinks);
		var docSet = new DocumentationSet(_context, _logFactory, crossLinkResolver);

		// Add LLM markdown export for dev server
		var markdownExporters = new List<IMarkdownExporter>();
		markdownExporters.AddLlmMarkdownExport(); // Consistent LLM-optimized output

		var generator = new DocumentationGenerator(docSet, _logFactory, markdownExporters: markdownExporters.ToArray());
		await generator.ResolveDirectoryTree(ctx);
		_ = Interlocked.Exchange(ref _generator, generator);

		await ReloadApiReferences(generator.MarkdownStringRenderer, ctx);
	}

	public async Task ReloadApiReferences(Cancel ctx) => await ReloadApiReferences(_generator.MarkdownStringRenderer, ctx);

	private async Task ReloadApiReferences(IMarkdownStringRenderer markdownStringRenderer, Cancel ctx)
	{
		if (ApiPath.Exists)
			ApiPath.Delete(true);
		ApiPath.Create();
		var generator = new OpenApiGenerator(_logFactory, _context, markdownStringRenderer);
		await generator.Generate(ctx);
	}

	public void Dispose()
	{
		_crossLinkFetcher.Dispose();
		GC.SuppressFinalize(this);
	}
}
