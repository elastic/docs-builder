// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown;
using Elastic.Markdown.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Http;

/// <summary>Singleton behaviour enforced by registration on <see cref="IServiceCollection"/></summary>
public class ReloadableGeneratorState(
	IDirectoryInfo? sourcePath,
	IDirectoryInfo? outputPath,
	BuildContext context,
	ILoggerFactory logger
)
{
	private IDirectoryInfo? SourcePath { get; } = sourcePath;
	private IDirectoryInfo? OutputPath { get; } = outputPath;

	private DocumentationGenerator _generator = new(new DocumentationSet(context), logger);
	public DocumentationGenerator Generator => _generator;

	public async Task ReloadAsync(Cancel ctx)
	{
		SourcePath?.Refresh();
		OutputPath?.Refresh();
		var docSet = new DocumentationSet(context);
		var generator = new DocumentationGenerator(docSet, logger);
		await generator.ResolveDirectoryTree(ctx);
		Interlocked.Exchange(ref _generator, generator);
	}
}
