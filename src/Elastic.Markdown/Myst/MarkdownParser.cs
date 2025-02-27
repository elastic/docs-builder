// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Cysharp.IO;
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

public class MarkdownParser(BuildContext build, IParserResolvers resolvers)
{
	private BuildContext Build { get; } = build;
	private IParserResolvers Resolvers { get; } = resolvers;

	public Task<MarkdownDocument> MinimalParseAsync(IFileInfo path, Cancel ctx)
	{
		var state = new ParserState(Build)
		{
			MarkdownSourcePath = path,
			YamlFrontMatter = null,
			DocumentationFileLookup = Resolvers.DocumentationFileLookup,
			CrossLinkResolver = Resolvers.CrossLinkResolver,
			SkipValidation = true
		};
		var context = new ParserContext(state);
		return ParseAsync(path, context, MinimalPipeline, ctx);
	}

	public Task<MarkdownDocument> ParseAsync(IFileInfo path, YamlFrontMatter? matter, Cancel ctx)
	{
		var state = new ParserState(Build)
		{
			MarkdownSourcePath = path,
			YamlFrontMatter = matter,
			DocumentationFileLookup = Resolvers.DocumentationFileLookup,
			CrossLinkResolver = Resolvers.CrossLinkResolver
		};
		var context = new ParserContext(state);
		return ParseAsync(path, context, Pipeline, ctx);
	}

	public Task<MarkdownDocument> ParseSnippetAsync(IFileInfo path, IFileInfo parentPath, YamlFrontMatter? matter, Cancel ctx)
	{
		var state = new ParserState(Build)
		{
			MarkdownSourcePath = path,
			YamlFrontMatter = matter,
			DocumentationFileLookup = Resolvers.DocumentationFileLookup,
			CrossLinkResolver = Resolvers.CrossLinkResolver,
			ParentMarkdownPath = parentPath
		};
		var context = new ParserContext(state);
		return ParseAsync(path, context, Pipeline, ctx);
	}

	public MarkdownDocument ParseEmbeddedMarkdown(string markdown, IFileInfo path, YamlFrontMatter? matter)
	{
		var state = new ParserState(Build)
		{
			MarkdownSourcePath = path,
			YamlFrontMatter = matter,
			DocumentationFileLookup = Resolvers.DocumentationFileLookup,
			CrossLinkResolver = Resolvers.CrossLinkResolver
		};
		var context = new ParserContext(state);
		var markdownDocument = Markdig.Markdown.Parse(markdown, Pipeline, context);
		return markdownDocument;
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
	private MarkdownPipeline? _pipelineCached;
	public MarkdownPipeline Pipeline
	{
		get
		{
			if (_pipelineCached is not null)
				return _pipelineCached;

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
				.UseHtmxLinkInlineRenderer(Build)
				.DisableHtml()
				.UseHardBreaks();
			_ = builder.BlockParsers.TryRemove<IndentedCodeBlockParser>();
			_pipelineCached = builder.Build();
			return _pipelineCached;
		}
	}

}
