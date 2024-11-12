// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Slices;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Helpers;
using Markdig.Syntax;
using Slugify;

namespace Elastic.Markdown.IO;

public class MarkdownFile : DocumentationFile
{
	private readonly SlugHelper _slugHelper = new();
	private string? _tocTitle;

	public MarkdownFile(IFileInfo sourceFile, IDirectoryInfo rootPath, MarkdownParser parser, BuildContext context)
		: base(sourceFile, rootPath)
	{
		ParentFolders = RelativePath.Split(Path.DirectorySeparatorChar).SkipLast(1).ToArray();
		FileName = sourceFile.Name;
		UrlPathPrefix = context.UrlPathPrefix;
		MarkdownParser = parser;
	}

	public string? UrlPathPrefix { get; }
	private MarkdownParser MarkdownParser { get; }
	private FrontMatterParser FrontMatterParser { get; } = new();
	public YamlFrontMatter? YamlFrontMatter { get; private set; }
	public string? Title { get; private set; }
	public string? TocTitle
	{
		get => !string.IsNullOrEmpty(_tocTitle) ? _tocTitle : Title;
		set => _tocTitle = value;
	}

	public List<PageTocItem> TableOfContents { get; } = new();
	public IReadOnlyList<string> ParentFolders { get; }
	public string FileName { get; }
	public string Url => $"{UrlPathPrefix}/{RelativePath.Replace(".md", ".html")}";

	public async Task ParseAsync(Cancel ctx) => await ParseFullAsync(ctx);

	public async Task<MarkdownDocument> ParseFullAsync(Cancel ctx)
	{
		var document = await MarkdownParser.QuickParseAsync(SourceFile, ctx);
		if (document.FirstOrDefault() is YamlFrontMatterBlock yaml)
		{
			var raw = string.Join(Environment.NewLine, yaml.Lines.Lines);
			YamlFrontMatter = FrontMatterParser.Deserialize(raw);
			Title = YamlFrontMatter.Title;
		}

		var contents = document
			.Where(block => block is HeadingBlock { Level: 2 })
			.Cast<HeadingBlock>()
			.Select(h => h.Inline?.FirstChild?.ToString())
			.Where(title => !string.IsNullOrWhiteSpace(title))
			.Select(title => new PageTocItem { Heading = title!, Slug = _slugHelper.GenerateSlug(title) })
			.ToList();
		TableOfContents.Clear();
		TableOfContents.AddRange(contents);
		return document;
	}

	public async Task<string> CreateHtmlAsync(YamlFrontMatter? matter, Cancel ctx)
	{
		var document = await MarkdownParser.ParseAsync(SourceFile, matter, ctx);
		return document.ToHtml(MarkdownParser.Pipeline);
	}
}
