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
using Elastic.Markdown.Myst.Renderers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Http;

/// <summary>Singleton behavior enforced by registration on <see cref="IServiceCollection"/></summary>
public class ReloadableGeneratorState(
	ILoggerFactory logFactory,
	IDirectoryInfo sourcePath,
	IDirectoryInfo outputPath,
	BuildContext context)
{
	private IDirectoryInfo SourcePath { get; } = sourcePath;
	private IDirectoryInfo OutputPath { get; } = outputPath;
	public IDirectoryInfo ApiPath { get; } = context.WriteFileSystem.DirectoryInfo.New(Path.Combine(outputPath.FullName, "api"));

	private DocumentationGenerator _generator = new(new DocumentationSet(context, logFactory, context.Collector), logFactory);
	public DocumentationGenerator Generator => _generator;

	public async Task ReloadAsync(Cancel ctx)
	{
		SourcePath.Refresh();
		OutputPath.Refresh();
		var docSet = new DocumentationSet(context, logFactory, context.Collector);
		_ = await docSet.LinkResolver.FetchLinks(ctx);

		// Add LLM markdown export for dev server
		var markdownExporters = new List<IMarkdownExporter>();
		markdownExporters.AddLlmMarkdownExport(); // Consistent LLM-optimized output

		var generator = new DocumentationGenerator(docSet, logFactory, markdownExporters: markdownExporters.ToArray());
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
		var generator = new OpenApiGenerator(logFactory, context, markdownStringRenderer);
		await generator.Generate(ctx);
	}
}
