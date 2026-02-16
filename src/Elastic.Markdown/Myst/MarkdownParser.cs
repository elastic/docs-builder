// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
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
using Elastic.Documentation.Site;
using Elastic.Markdown.Myst.Roles.AppliesTo;
using Elastic.Markdown.Myst.Roles.Icons;
using Elastic.Markdown.Myst.Roles.Kbd;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Parsers;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Myst;

public partial class MarkdownParser(BuildContext build, IParserResolvers resolvers)
{
	private BuildContext Build { get; } = build;
	public IParserResolvers Resolvers { get; } = resolvers;

	public Task<MarkdownDocument> ParseAsync(IFileInfo path, YamlFrontMatter? matter, Cancel ctx) =>
		ParseFromFile(path, matter, Pipeline, false, ctx);

	public Task<MarkdownDocument> MinimalParseAsync(IFileInfo path, Cancel ctx) =>
		ParseFromFile(path, null, MinimalPipeline, true, ctx);

	private Task<MarkdownDocument> ParseFromFile(IFileInfo path, YamlFrontMatter? matter, MarkdownPipeline pipeline, bool skip, Cancel ctx)
	{
		var state = new ParserState(Build)
		{
			MarkdownSourcePath = path,
			YamlFrontMatter = matter,
			TryFindDocument = Resolvers.TryFindDocument,
			TryFindDocumentByRelativePath = Resolvers.TryFindDocumentByRelativePath,
			CrossLinkResolver = Resolvers.CrossLinkResolver,
			NavigationTraversable = Resolvers.NavigationTraversable,
			SkipValidation = skip
		};
		var context = new ParserContext(state);
		return ParseAsync(path, context, pipeline, ctx);
	}

	public MarkdownDocument ParseStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter) =>
		ParseMarkdownStringAsync(markdown, path, matter, Pipeline);

	public MarkdownDocument ParseStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter, IFileInfo? originalSourcePath) =>
		ParseMarkdownStringAsync(markdown, path, matter, originalSourcePath, Pipeline);

	public MarkdownDocument MinimalParseStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter) =>
		ParseMarkdownStringAsync(markdown, path, matter, MinimalPipeline);

	public MarkdownDocument MinimalParseStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter, IFileInfo? originalSourcePath) =>
		ParseMarkdownStringAsync(markdown, path, matter, originalSourcePath, MinimalPipeline);

	private MarkdownDocument ParseMarkdownStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter, MarkdownPipeline pipeline) =>
		ParseMarkdownStringAsync(Build, Resolvers, markdown, path, matter, null, pipeline);

	private MarkdownDocument ParseMarkdownStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter, IFileInfo? originalSourcePath, MarkdownPipeline pipeline) =>
		ParseMarkdownStringAsync(Build, Resolvers, markdown, path, matter, originalSourcePath, pipeline);

	public static MarkdownDocument ParseMarkdownStringAsync(BuildContext build, IParserResolvers resolvers, string markdown, IFileInfo path,
		YamlFrontMatter? matter, MarkdownPipeline pipeline) =>
		ParseMarkdownStringAsync(build, resolvers, markdown, path, matter, null, pipeline);

	public static MarkdownDocument ParseMarkdownStringAsync(BuildContext build, IParserResolvers resolvers, string markdown, IFileInfo path,
		YamlFrontMatter? matter, IFileInfo? originalSourcePath, MarkdownPipeline pipeline)
	{
		var state = new ParserState(build)
		{
			MarkdownSourcePath = path,
			OriginalSourcePath = originalSourcePath,
			YamlFrontMatter = matter,
			TryFindDocument = resolvers.TryFindDocument,
			TryFindDocumentByRelativePath = resolvers.TryFindDocumentByRelativePath,
			NavigationTraversable = resolvers.NavigationTraversable,
			CrossLinkResolver = resolvers.CrossLinkResolver
		};
		var context = new ParserContext(state);

		// Preprocess substitutions in link patterns before Markdig parsing
		var preprocessedMarkdown = PreprocessLinkSubstitutions(markdown, context);

		return Markdig.Markdown.Parse(preprocessedMarkdown, pipeline, context);
	}

	public static Task<MarkdownDocument> ParseSnippetAsync(BuildContext build, IParserResolvers resolvers, IFileInfo path, IFileInfo parentPath,
		YamlFrontMatter? matter, Cancel ctx, int? includeLine = null)
	{
		var state = new ParserState(build)
		{
			MarkdownSourcePath = path,
			YamlFrontMatter = matter,
			TryFindDocument = resolvers.TryFindDocument,
			TryFindDocumentByRelativePath = resolvers.TryFindDocumentByRelativePath,
			CrossLinkResolver = resolvers.CrossLinkResolver,
			NavigationTraversable = resolvers.NavigationTraversable,
			ParentMarkdownPath = parentPath,
			IncludeLine = includeLine
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

		return Markdig.Markdown.Parse(preprocessedMarkdown, pipeline, context);
	}

	[field: AllowNull, MaybeNull]
	private static MarkdownPipeline MinimalPipeline
	{
		get
		{
			if (field is not null)
				return field;
			var builder = new MarkdownPipelineBuilder()
				.UseYamlFrontMatter()
				.UseFootnotes() // Must match Pipeline to avoid inconsistent footnote handling
				.UseInlineAnchors()
				.UseHeadingsWithSlugs()
				.UseDirectives();

			_ = builder.BlockParsers.TryRemove<IndentedCodeBlockParser>();
			field = builder.Build();
			return field;
		}
	}

	[field: AllowNull, MaybeNull]
	public static MarkdownPipeline Pipeline
	{
		get
		{
			if (field is not null)
				return field;

			var builder = new MarkdownPipelineBuilder()
				.UseInlineAnchors()
				.UsePreciseSourceLocation()
				.UseFootnotes() // Must be before UseDiagnosticLinks to ensure FootnoteLinkParser is inserted correctly
				.UseDiagnosticLinks()
				.UseAutoLinks()
				.UseHeadingsWithSlugs()
				.UseEmphasisExtras(EmphasisExtraOptions.Default)
				.UseSubstitutionInlineCode()
				.UseInlineAppliesTo()
				.UseInlineIcons()
				.UseInlineKbd()
				.UseSubstitution()
				.UseComments()
				.UseYamlFrontMatter()
				.UsePipeTables()
				.UseDirectives()
				.UseDefinitionLists()
				.UseEnhancedCodeBlocks()
				.UseHtmxLinkInlineRenderer()
				.DisableHtml()
				.UseSpaceNormalizer()
				.UseHardBreaks();
			_ = builder.BlockParsers.TryRemove<IndentedCodeBlockParser>();
			field = builder.Build();
			return field;
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
				return match.Value; // Don't process links in subs=false code blocks

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
		var span = markdown.AsSpan();
		var pos = 0;

		while (pos < span.Length)
		{
			var lineStart = pos;

			// Skip leading whitespace
			while (pos < span.Length && span[pos] is ' ' or '\t')
				pos++;

			// Check if the line starts with ```
			if (pos + 2 < span.Length && span[pos] == '`' && span[pos + 1] == '`' && span[pos + 2] == '`')
			{
				// Find end of opening line
				var lineEnd = span[pos..].IndexOf('\n');
				lineEnd = lineEnd == -1 ? span.Length : pos + lineEnd;

				var subsDisabled = span[pos..lineEnd].Contains("subs=false".AsSpan(), StringComparison.Ordinal);
				var blockStart = lineStart;

				// Move past an opening fence
				pos = lineEnd < span.Length ? lineEnd + 1 : span.Length;

				// Find closing fence
				while (pos < span.Length)
				{
					// Skip whitespace
					while (pos < span.Length && span[pos] is ' ' or '\t')
						pos++;

					// Check for closing ```
					if (pos + 2 < span.Length && span[pos] == '`' && span[pos + 1] == '`' && span[pos + 2] == '`')
					{
						var closingEnd = span[pos..].IndexOf('\n');
						closingEnd = closingEnd == -1 ? span.Length : pos + closingEnd;
						ranges.Add((blockStart, closingEnd, subsDisabled));
						pos = closingEnd < span.Length ? closingEnd + 1 : span.Length;
						break;
					}

					// Move to the next line
					var nextNewline = span[pos..].IndexOf('\n');
					pos = nextNewline == -1 ? span.Length : pos + nextNewline + 1;
				}
			}
			else
			{
				// Move to the next line
				var nextNewline = span[pos..].IndexOf('\n');
				pos = nextNewline == -1 ? span.Length : pos + nextNewline + 1;
			}
		}

		return ranges;
	}

	private static bool IsInsideSubsDisabledCodeBlock(int index, List<(int start, int end, bool subsDisabled)> codeBlockRanges)
	{
		foreach (var (start, end, subsDisabled) in codeBlockRanges)
		{
			if (index >= start && index <= end && subsDisabled)
				return true;
		}

		return false;
	}

}
