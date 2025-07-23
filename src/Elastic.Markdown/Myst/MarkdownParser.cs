// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Cysharp.IO;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.Comments;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Myst.FrontMatter;
using Elastic.Markdown.Myst.InlineParsers;
using Elastic.Markdown.Myst.InlineParsers.Substitution;
using Elastic.Markdown.Myst.Linters;
using Elastic.Markdown.Myst.Renderers;
using Elastic.Markdown.Myst.Roles.AppliesTo;
using Elastic.Markdown.Myst.Roles.Icons;
using Elastic.Markdown.Myst.Roles.Kbd;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst;

public partial class MarkdownParser(BuildContext build, IParserResolvers resolvers)
{
	private BuildContext Build { get; } = build;
	public IParserResolvers Resolvers { get; } = resolvers;

	public Task<MarkdownDocument> ParseAsync(IFileInfo path, YamlFrontMatter? matter, Cancel ctx) =>
		ParseFromFile(path, matter, Pipeline, false, ctx);

	public Task<MarkdownDocument> MinimalParseAsync(IFileInfo path, Cancel ctx) =>
		ParseFromFile(path, null, MinimalPipeline, true, ctx);

	private Task<MarkdownDocument> ParseFromFile(
		IFileInfo path, YamlFrontMatter? matter, MarkdownPipeline pipeline, bool skip, Cancel ctx
	)
	{
		var state = new ParserState(Build)
		{
			MarkdownSourcePath = path,
			YamlFrontMatter = matter,
			DocumentationFileLookup = Resolvers.DocumentationFileLookup,
			CrossLinkResolver = Resolvers.CrossLinkResolver,
			SkipValidation = skip
		};
		var context = new ParserContext(state);
		return ParseAsync(path, context, pipeline, ctx);
	}

	public MarkdownDocument ParseStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter) =>
		ParseMarkdownStringAsync(markdown, path, matter, Pipeline);

	public MarkdownDocument MinimalParseStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter) =>
		ParseMarkdownStringAsync(markdown, path, matter, MinimalPipeline);

	private MarkdownDocument ParseMarkdownStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter, MarkdownPipeline pipeline) =>
		ParseMarkdownStringAsync(Build, Resolvers, markdown, path, matter, pipeline);

	public static MarkdownDocument ParseMarkdownStringAsync(BuildContext build, IParserResolvers resolvers, string markdown, IFileInfo path, YamlFrontMatter? matter, MarkdownPipeline pipeline)
	{
		var state = new ParserState(build)
		{
			MarkdownSourcePath = path,
			YamlFrontMatter = matter,
			DocumentationFileLookup = resolvers.DocumentationFileLookup,
			CrossLinkResolver = resolvers.CrossLinkResolver
		};
		var context = new ParserContext(state);

		// Preprocess substitutions in link patterns before Markdig parsing
		var preprocessedMarkdown = PreprocessLinkSubstitutions(markdown, context);

		var markdownDocument = Markdig.Markdown.Parse(preprocessedMarkdown, pipeline, context);
		return markdownDocument;
	}

	public static Task<MarkdownDocument> ParseSnippetAsync(BuildContext build, IParserResolvers resolvers, IFileInfo path, IFileInfo parentPath,
		YamlFrontMatter? matter, Cancel ctx)
	{
		var state = new ParserState(build)
		{
			MarkdownSourcePath = path,
			YamlFrontMatter = matter,
			DocumentationFileLookup = resolvers.DocumentationFileLookup,
			CrossLinkResolver = resolvers.CrossLinkResolver,
			ParentMarkdownPath = parentPath
		};
		var context = new ParserContext(state);
		return ParseAsync(path, context, Pipeline, ctx);
	}


	private static async Task<MarkdownDocument> ParseAsync(
		IFileInfo path,
		MarkdownParserContext context,
		MarkdownPipeline pipeline,
		Cancel ctx)
	{
		string inputMarkdown;
		if (path.FileSystem is FileSystem)
		{
			//real IO optimize through UTF8 stream reader.
			await using var streamReader = new Utf8StreamReader(path.FullName, fileOpenMode: FileOpenMode.Throughput);
			inputMarkdown = await streamReader.AsTextReader().ReadToEndAsync(ctx);
		}
		else
			inputMarkdown = await path.FileSystem.File.ReadAllTextAsync(path.FullName, ctx);

		// Preprocess substitutions in link patterns before Markdig parsing
		var preprocessedMarkdown = PreprocessLinkSubstitutions(inputMarkdown, (ParserContext)context);

		var markdownDocument = Markdig.Markdown.Parse(preprocessedMarkdown, pipeline, context);
		return markdownDocument;
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
				.UseInlineAppliesTo()
				.UseInlineIcons()
				.UseInlineKbd()
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
				.UseWhiteSpaceNormalizer()
				.UseHardBreaks();
			_ = builder.BlockParsers.TryRemove<IndentedCodeBlockParser>();
			PipelineCached = builder.Build();
			return PipelineCached;
		}
	}

	[System.Text.RegularExpressions.GeneratedRegex(@"\[([^\]]+)\]\(([^\)]+)\)", System.Text.RegularExpressions.RegexOptions.Multiline)]
	private static partial System.Text.RegularExpressions.Regex LinkPattern();

	/// <summary>
	/// Preprocesses substitutions specifically in link patterns [text](url) before Markdig parsing
	/// </summary>
	private static string PreprocessLinkSubstitutions(string markdown, ParserContext context) =>
		LinkPattern().Replace(markdown, match =>
		{
			var linkText = match.Groups[1].Value;
			var linkUrl = match.Groups[2].Value;

			// Only preprocess external links to preserve internal link validation behavior
			// Check if URL contains substitutions and looks like it might resolve to an external URL
			if (linkUrl.Contains("{{") && (linkUrl.Contains("http") || linkText.Contains("{{")))
			{
				// Apply substitutions to both link text and URL
				var processedText = linkText.ReplaceSubstitutions(context);
				var processedUrl = linkUrl.ReplaceSubstitutions(context);
				return $"[{processedText}]({processedUrl})";
			}

			// Return original match for internal links
			return match.Value;
		});
}
