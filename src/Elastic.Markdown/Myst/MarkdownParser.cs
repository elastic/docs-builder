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
using Elastic.Markdown.Myst.InlineParsers.SubstitutionInlineCode;
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
				.UseSubstitutionInlineCode()
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
	/// Only processes links that are not inside code blocks with subs=false
	/// </summary>
	private static string PreprocessLinkSubstitutions(string markdown, ParserContext context)
	{
		// Find all code block boundaries to avoid processing links inside subs=false blocks
		var codeBlockRanges = GetCodeBlockRanges(markdown);

		return LinkPattern().Replace(markdown, match =>
		{
			// Check if this link is inside a code block with subs=false
			if (IsInsideSubsDisabledCodeBlock(match.Index, codeBlockRanges))
			{
				return match.Value; // Don't process links in subs=false code blocks
			}

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

	private static List<(int start, int end, bool subsDisabled)> GetCodeBlockRanges(string markdown)
	{
		var ranges = new List<(int start, int end, bool subsDisabled)>();
		var lines = markdown.Split('\n');
		var currentPos = 0;

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];

			// Check for code block start (``` or ````)
			if (line.TrimStart().StartsWith("```"))
			{
				// Check if this line contains subs=false
				var subsDisabled = line.Contains("subs=false");
				var blockStart = currentPos;

				// Find the end of the code block
				var blockEnd = currentPos + line.Length;
				for (var j = i + 1; j < lines.Length; j++)
				{
					blockEnd += lines[j].Length + 1; // +1 for newline
					if (lines[j].TrimStart().StartsWith("```"))
						break;
				}

				ranges.Add((blockStart, blockEnd, subsDisabled));
			}

			currentPos += line.Length + 1; // +1 for newline
		}

		return ranges;
	}

	private static bool IsInsideSubsDisabledCodeBlock(int index, List<(int start, int end, bool subsDisabled)> codeBlockRanges)
	{
		foreach (var (start, end, subsDisabled) in codeBlockRanges)
		{
			if (index >= start && index <= end && subsDisabled)
			{
				return true;
			}
		}
		return false;
	}
}
