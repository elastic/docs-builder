// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;

using Cysharp.IO;

using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.Comments;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Myst.FrontMatter;
using Elastic.Markdown.Myst.InlineParsers;
using Elastic.Markdown.Myst.InlineParsers.Substitution;
using Elastic.Markdown.Myst.Renderers;
using Elastic.Markdown.Myst.Roles;
using Elastic.Markdown.Myst.Roles.AppliesTo;

using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst;

public class MarkdownParser(BuildContext build, IParserResolvers resolvers)
{
	private BuildContext Build { get; } = build;
	private IParserResolvers Resolvers { get; } = resolvers;

	// Collection of irregular whitespace characters that may impair Markdown rendering
	private static readonly char[] IrregularWhitespaceChars =
	[
		'\u000B', // Line Tabulation (\v) - <VT>
		'\u000C', // Form Feed (\f) - <FF>
		'\u00A0', // No-Break Space - <NBSP>
		'\u0085', // Next Line
		'\u1680', // Ogham Space Mark
		'\u180E', // Mongolian Vowel Separator - <MVS>
		'\ufeff', // Zero Width No-Break Space - <BOM>
		'\u2000', // En Quad
		'\u2001', // Em Quad
		'\u2002', // En Space - <ENSP>
		'\u2003', // Em Space - <EMSP>
		'\u2004', // Tree-Per-Em
		'\u2005', // Four-Per-Em
		'\u2006', // Six-Per-Em
		'\u2007', // Figure Space
		'\u2008', // Punctuation Space - <PUNCSP>
		'\u2009', // Thin Space
		'\u200A', // Hair Space
		'\u200B', // Zero Width Space - <ZWSP>
		'\u2028', // Line Separator
		'\u2029', // Paragraph Separator
		'\u202F', // Narrow No-Break Space
		'\u205F', // Medium Mathematical Space
		'\u3000'  // Ideographic Space
	];

	// Detects irregular whitespace in the markdown content and reports diagnostics
	private void DetectIrregularWhitespace(string content, string filePath)
	{
		var lines = content.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);

		for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
		{
			var line = lines[lineIndex];
			for (var columnIndex = 0; columnIndex < line.Length; columnIndex++)
			{
				var c = line[columnIndex];
				if (Array.IndexOf(IrregularWhitespaceChars, c) >= 0)
				{
					var charName = GetCharacterName(c);
					Build.Collector.Write(new Diagnostic
					{
						Severity = Severity.Warning,
						File = filePath,
						Line = lineIndex + 1, // 1-based line number
						Column = columnIndex + 1, // 1-based column number
						Length = 1,
						Message = $"Irregular whitespace character detected: U+{(int)c:X4} ({charName}). This may impair Markdown rendering."
					});
				}
			}
		}
	}

	// Helper to get a friendly name for the whitespace character
	private static string GetCharacterName(char c) => c switch
	{
		'\u000B' => "Line Tabulation (VT)",
		'\u000C' => "Form Feed (FF)",
		'\u00A0' => "No-Break Space (NBSP)",
		'\u0085' => "Next Line",
		'\u1680' => "Ogham Space Mark",
		'\u180E' => "Mongolian Vowel Separator (MVS)",
		'\ufeff' => "Zero Width No-Break Space (BOM)",
		'\u2000' => "En Quad",
		'\u2001' => "Em Quad",
		'\u2002' => "En Space (ENSP)",
		'\u2003' => "Em Space (EMSP)",
		'\u2004' => "Tree-Per-Em",
		'\u2005' => "Four-Per-Em",
		'\u2006' => "Six-Per-Em",
		'\u2007' => "Figure Space",
		'\u2008' => "Punctuation Space (PUNCSP)",
		'\u2009' => "Thin Space",
		'\u200A' => "Hair Space",
		'\u200B' => "Zero Width Space (ZWSP)",
		'\u2028' => "Line Separator",
		'\u2029' => "Paragraph Separator",
		'\u202F' => "Narrow No-Break Space",
		'\u205F' => "Medium Mathematical Space",
		'\u3000' => "Ideographic Space",
		_ => "Unknown"
	};


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

	public MarkdownDocument ParseStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter)
	{
		DetectIrregularWhitespace(markdown, path.FullName);
		return ParseMarkdownStringAsync(markdown, path, matter, Pipeline);
	}

	public MarkdownDocument MinimalParseStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter)
	{
		DetectIrregularWhitespace(markdown, path.FullName);
		return ParseMarkdownStringAsync(markdown, path, matter, MinimalPipeline);
	}

	private MarkdownDocument ParseMarkdownStringAsync(string markdown, IFileInfo path, YamlFrontMatter? matter, MarkdownPipeline pipeline)
	{
		var state = new ParserState(Build)
		{
			MarkdownSourcePath = path,
			YamlFrontMatter = matter,
			DocumentationFileLookup = Resolvers.DocumentationFileLookup,
			CrossLinkResolver = Resolvers.CrossLinkResolver
		};
		var context = new ParserContext(state);
		var markdownDocument = Markdig.Markdown.Parse(markdown, pipeline, context);
		return markdownDocument;
	}

	private async Task<MarkdownDocument> ParseAsync(
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
		{
			inputMarkdown = await path.FileSystem.File.ReadAllTextAsync(path.FullName, ctx);
		}

		// Check for irregular whitespace characters
		DetectIrregularWhitespace(inputMarkdown, path.FullName);

		var markdownDocument = Markdig.Markdown.Parse(inputMarkdown, pipeline, context);
		return markdownDocument;
	}

	// ReSharper disable once InconsistentNaming
	private MarkdownPipeline? _minimalPipelineCached;

	private MarkdownPipeline MinimalPipeline
	{
		get
		{
			if (_minimalPipelineCached is not null)
				return _minimalPipelineCached;
			var builder = new MarkdownPipelineBuilder()
				.UseYamlFrontMatter()
				.UseInlineAnchors()
				.UseHeadingsWithSlugs()
				.UseDirectives(this);

			_ = builder.BlockParsers.TryRemove<IndentedCodeBlockParser>();
			_minimalPipelineCached = builder.Build();
			return _minimalPipelineCached;
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
				.UseInlineAppliesTo()
				.UseSubstitution()
				.UseComments()
				.UseYamlFrontMatter()
				.UseGridTables()
				.UsePipeTables()
				.UseDirectives(this)
				.UseDefinitionLists()
				.UseEnhancedCodeBlocks()
				.UseHtmxLinkInlineRenderer()
				.DisableHtml()
				.UseHardBreaks();
			_ = builder.BlockParsers.TryRemove<IndentedCodeBlockParser>();
			_pipelineCached = builder.Build();
			return _pipelineCached;
		}
	}
}
