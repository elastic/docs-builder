// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Myst.FrontMatter;
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
		FilePath = sourceFile.FullName;
		UrlPathPrefix = context.UrlPathPrefix;
		MarkdownParser = parser;
		Collector = context.Collector;
	}

	private DiagnosticsCollector Collector { get; }

	public DocumentationGroup? Parent
	{
		get => FileName == "index.md" ? _parent?.Parent : _parent;
		set => _parent = value;
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

	private readonly HashSet<string> _additionalLabels = new();
	public IReadOnlySet<string> AdditionalLabels => _additionalLabels;

	public string FilePath { get; }
	public string FileName { get; }
	public string Url => $"{UrlPathPrefix}/{RelativePath.Replace(".md", ".html")}";

	private bool _instructionsParsed;
	private DocumentationGroup? _parent;

	public MarkdownFile[] YieldParents()
	{
		var parents = new List<MarkdownFile>();
		var parent = Parent;
		do
		{
			if (parent is { Index: not null } && parent.Index != this)
				parents.Add(parent.Index);
			parent = parent?.Parent;
		} while (parent != null);
		return parents.ToArray();
	}

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
		if (Title == RelativePath)
			Collector.EmitWarning(SourceFile.FullName, "Missing yaml front-matter block defining a title or a level 1 header");
		return document;
	}

	private void ReadDocumentInstructions(MarkdownDocument document)
	{
		if (document.FirstOrDefault() is YamlFrontMatterBlock yaml)
		{
			var raw = string.Join(Environment.NewLine, yaml.Lines.Lines);
			YamlFrontMatter = ReadYamlFrontMatter(document, raw);
			Title = YamlFrontMatter.Title;
			NavigationTitle = YamlFrontMatter.NavigationTitle;
		}
		else
		{
			Title = RelativePath;
			NavigationTitle = RelativePath;
		}

		var contents = document
			.Where(block => block is HeadingBlock { Level: >= 2 })
			.Cast<HeadingBlock>()
			.Select(h => (h.GetData("header") as string, h.GetData("anchor") as string))
			.Select(h => new PageTocItem
			{
				Heading = h.Item1!.Replace("`", "").Replace("*", ""),
				Slug = _slugHelper.GenerateSlug(h.Item2 ?? h.Item1)
			})
			.ToList();
		_tableOfContent.Clear();
		foreach (var t in contents)
			_tableOfContent[t.Slug] = t;

		var labels = document.Descendants<DirectiveBlock>()
			.Select(b => b.CrossReferenceName)
			.Where(l => !string.IsNullOrWhiteSpace(l))
			.Select(_slugHelper.GenerateSlug)
			.ToArray();
		foreach (var label in labels)
		{
			if (!string.IsNullOrEmpty(label))
				_additionalLabels.Add(label);
		}

		_instructionsParsed = true;
	}

	private YamlFrontMatter ReadYamlFrontMatter(MarkdownDocument document, string raw)
	{
		try
		{
			return YamlSerialization.Deserialize<YamlFrontMatter>(raw);
		}
		catch (Exception e)
		{
			Collector.EmitError(FilePath, "Failed to parse yaml front matter block.", e);
			return new YamlFrontMatter();
		}
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
