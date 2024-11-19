// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Slices;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Slugify;

namespace Elastic.Markdown.IO;

public record MarkdownFile : DocumentationFile
{
	private readonly SlugHelper _slugHelper = new();
	private string? _navigationTitle;

	public MarkdownFile(IFileInfo sourceFile, IDirectoryInfo rootPath, MarkdownParser parser, BuildContext context)
		: base(sourceFile, rootPath)
	{
		FileName = sourceFile.Name;
		UrlPathPrefix = context.UrlPathPrefix;
		MarkdownParser = parser;
	}

	public string? UrlPathPrefix { get; }
	private MarkdownParser MarkdownParser { get; }
	public YamlFrontMatter? YamlFrontMatter { get; private set; }
	public string? Title { get; private set; }
	public string? NavigationTitle
	{
		get => !string.IsNullOrEmpty(_navigationTitle) ? _navigationTitle : Title;
		private set => _navigationTitle = value;
	}

	//indexed by slug
	private readonly Dictionary<string, PageTocItem> _tableOfContent = new();
	public IReadOnlyDictionary<string, PageTocItem> TableOfContents => _tableOfContent;

	public string FileName { get; }
	public string Url => $"{UrlPathPrefix}/{RelativePath.Replace(".md", ".html")}";

	private bool _instructionsParsed;

	public async Task<MarkdownDocument> MinimalParse(Cancel ctx)
	{
		var document = await MarkdownParser.MinimalParseAsync(SourceFile, ctx);
		ReadDocumentInstructions(document);
		return document;
	}

	public async Task<MarkdownDocument> ParseFullAsync(Cancel ctx)
	{
		if (!_instructionsParsed)
			await MinimalParse(ctx);

		var document = await MarkdownParser.ParseAsync(SourceFile, YamlFrontMatter, ctx);
		return document;
	}

	private void ReadDocumentInstructions(MarkdownDocument document)
	{
		if (document.FirstOrDefault() is YamlFrontMatterBlock yaml)
		{
			var raw = string.Join(Environment.NewLine, yaml.Lines.Lines);
			YamlFrontMatter = FrontMatterParser.Deserialize(raw);
			Title = YamlFrontMatter.Title;
			NavigationTitle = YamlFrontMatter.NavigationTitle;
		}

		var contents = document
			.Where(block => block is HeadingBlock { Level: 2 })
			.Cast<HeadingBlock>()
			.Select(h => h.Inline?.FirstChild?.ToString())
			.Where(title => !string.IsNullOrWhiteSpace(title))
			.Select(title => new PageTocItem { Heading = title!, Slug = _slugHelper.GenerateSlug(title) })
			.ToList();
		_tableOfContent.Clear();
		foreach (var t in contents)
			_tableOfContent[t.Slug] = t;
		_instructionsParsed = true;
	}


	public string CreateHtml(MarkdownDocument document) =>
		// var writer = new StringWriter();
		// var renderer = new HtmlRenderer(writer);
		// renderer.LinkRewriter = (s => s);
		// MarkdownParser.Pipeline.Setup(renderer);
		//
		// var document = MarkdownParser.Parse(markdown, pipeline);
		// renderer.Render(document);
		// writer.Flush();
		document.ToHtml(MarkdownParser.Pipeline);
}
