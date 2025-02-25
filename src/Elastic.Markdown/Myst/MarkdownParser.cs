// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Cysharp.IO;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.Comments;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Myst.FrontMatter;
using Elastic.Markdown.Myst.InlineParsers;
using Elastic.Markdown.Myst.Renderers;
using Elastic.Markdown.Myst.Substitution;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst;

public class MarkdownParser(
	IDirectoryInfo sourcePath,
	BuildContext context,
	Func<IFileInfo, DocumentationFile?> getDocumentationFile,
	ConfigurationFile configuration,
	ICrossLinkResolver linksResolver
	)
{
	public IDirectoryInfo SourcePath { get; } = sourcePath;

	private BuildContext Context { get; } = context;

	private ICrossLinkResolver LinksResolver { get; } = linksResolver;

	// ReSharper disable once InconsistentNaming
	private static MarkdownPipeline? MinimalPipelineCached;

	private static MarkdownPipeline MinimalPipeline
	{
		get
		{
			if (MinimalPipelineCached is not null)
				return MinimalPipelineCached;
			var builder = new MarkdownPipelineBuilder()
				.UseYamlFrontMatter()
				.UseInlineAnchors()
				.UseHeadingsWithSlugs()
				.UseDirectives();

			_ = builder.BlockParsers.TryRemove<IndentedCodeBlockParser>();
			MinimalPipelineCached = builder.Build();
			return MinimalPipelineCached;

		}
	}

	// ReSharper disable once InconsistentNaming
	private static MarkdownPipeline? PipelineCached;
	public static MarkdownPipeline Pipeline
	{
		get
		{
			if (PipelineCached is not null)
				return PipelineCached;

			var builder = new MarkdownPipelineBuilder()
				.UseInlineAnchors()
				.UsePreciseSourceLocation()
				.UseDiagnosticLinks()
				.UseHeadingsWithSlugs()
				.UseEmphasisExtras(EmphasisExtraOptions.Default)
				.UseSoftlineBreakAsHardlineBreak()
				.UseSubstitution()
				.UseComments()
				.UseYamlFrontMatter()
				.UseGridTables()
				.UsePipeTables()
				.UseDirectives()
				.UseDefinitionLists()
				.UseEnhancedCodeBlocks()
				.UseHtmxLinkInlineRenderer()
				.DisableHtml()
				.UseHardBreaks();
			_ = builder.BlockParsers.TryRemove<IndentedCodeBlockParser>();
			PipelineCached = builder.Build();
			return PipelineCached;
		}
	}

	public ConfigurationFile Configuration { get; } = configuration;

	public Task<MarkdownDocument> MinimalParseAsync(IFileInfo path, Cancel ctx)
	{
		var context = new ParserContext(this, path, null, Context, Configuration, LinksResolver, getDocumentationFile)
		{
			SkipValidation = true
		};
		return ParseAsync(path, context, MinimalPipeline, ctx);
	}

	public Task<MarkdownDocument> ParseAsync(IFileInfo path, YamlFrontMatter? matter, Cancel ctx)
	{
		var context = new ParserContext(this, path, matter, Context, Configuration, LinksResolver, getDocumentationFile);
		return ParseAsync(path, context, Pipeline, ctx);
	}

	private static async Task<MarkdownDocument> ParseAsync(
		IFileInfo path,
		MarkdownParserContext context,
		MarkdownPipeline pipeline,
		Cancel ctx)
	{
		if (path.FileSystem is FileSystem)
		{
			//real IO optimize through UTF8 stream reader.
			await using var streamReader = new Utf8StreamReader(path.FullName, fileOpenMode: FileOpenMode.Throughput);
			var inputMarkdown = await streamReader.AsTextReader().ReadToEndAsync(ctx);
			var markdownDocument = Markdig.Markdown.Parse(inputMarkdown, pipeline, context);
			return markdownDocument;
		}
		else
		{
			var inputMarkdown = await path.FileSystem.File.ReadAllTextAsync(path.FullName, ctx);
			var markdownDocument = Markdig.Markdown.Parse(inputMarkdown, pipeline, context);
			return markdownDocument;
		}
	}

	public MarkdownDocument Parse(string yaml, IFileInfo parent, YamlFrontMatter? matter)
	{
		var context = new ParserContext(this, parent, matter, Context, Configuration, LinksResolver, getDocumentationFile);
		var markdownDocument = Markdig.Markdown.Parse(yaml, Pipeline, context);
		return markdownDocument;
	}
}
