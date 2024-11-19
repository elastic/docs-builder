// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Cysharp.IO;
using Elastic.Markdown.Myst.Comments;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Myst.InlineParsers;
using Elastic.Markdown.Myst.Substitution;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst;

public class MarkdownParser(IDirectoryInfo sourcePath, BuildContext context, Func<string, string?>? getTitle)
{
	public IDirectoryInfo SourcePath { get; } = sourcePath;
	public BuildContext Context { get; } = context;

	public MarkdownPipeline MinimalPipeline { get; } =
		new MarkdownPipelineBuilder()
			.UseDiagnosticLinks()
			.UseSubstitution()
			.UseYamlFrontMatter()
			.Build();

	public MarkdownPipeline Pipeline =>
		new MarkdownPipelineBuilder()
			.EnableTrackTrivia()
			.UsePreciseSourceLocation()
			.UseDiagnosticLinks()
			.UseGenericAttributes()
			.UseEmphasisExtras(EmphasisExtraOptions.Default)
			.UseSoftlineBreakAsHardlineBreak()
			.UseSubstitution()
			.UseComments()
			.UseYamlFrontMatter()
			.UseGridTables()
			.UsePipeTables()
			.UseDirectives()
			.DisableHtml()
			.Build();


	public Task<MarkdownDocument> MinimalParseAsync(IFileInfo path, Cancel ctx)
	{
		var context = new ParserContext(this, path, null, Context)
		{
			SkipValidation = true,
			GetTitle = getTitle
		};
		return ParseAsync(path, context, MinimalPipeline, ctx);
	}

	public Task<MarkdownDocument> ParseAsync(IFileInfo path, YamlFrontMatter? matter, Cancel ctx)
	{
		var context = new ParserContext(this, path, matter, Context)
		{
			GetTitle = getTitle
		};
		return ParseAsync(path, context, Pipeline, ctx);
	}

	private async Task<MarkdownDocument> ParseAsync(
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
}
