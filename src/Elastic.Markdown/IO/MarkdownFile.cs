// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Myst.FrontMatter;
using Elastic.Markdown.Slices;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;

namespace Elastic.Markdown.IO;


public record MarkdownFile : DocumentationFile
{
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

	public int NavigationIndex { get; internal set; } = -1;

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
		return document;
	}

	private void ReadDocumentInstructions(MarkdownDocument document)
	{

		Title = document
			.FirstOrDefault(block => block is HeadingBlock { Level: 1 })?
			.GetData("header") as string;

		if (document.FirstOrDefault() is YamlFrontMatterBlock yaml)
		{
			var raw = string.Join(Environment.NewLine, yaml.Lines.Lines);
			YamlFrontMatter = ReadYamlFrontMatter(raw);

			// TODO remove when migration tool and our demo content sets are updated
			var deprecatedTitle = YamlFrontMatter.Title;
			if (!string.IsNullOrEmpty(deprecatedTitle))
			{
				Collector.EmitWarning(FilePath, "'title' is no longer supported in yaml frontmatter please use a level 1 header instead.");
				// TODO remove fallback once migration is over and we fully deprecate front matter titles
				if (string.IsNullOrEmpty(Title))
					Title = deprecatedTitle;
			}


			// set title on yaml front matter manually.
			// frontmatter gets passed around as page information throughout
			YamlFrontMatter.Title = Title;

			NavigationTitle = YamlFrontMatter.NavigationTitle;
			if (!string.IsNullOrEmpty(NavigationTitle))
			{
				var props = MarkdownParser.Configuration.Substitutions;
				var properties = YamlFrontMatter.Properties;
				if (properties is { Count: >= 0 } local)
				{
					var allProperties = new Dictionary<string, string>(local);
					foreach (var (key, value) in props)
						allProperties[key] = value;
					if (NavigationTitle.AsSpan().ReplaceSubstitutions(allProperties, out var replacement))
						NavigationTitle = replacement;
				}
				else
				{
					if (NavigationTitle.AsSpan().ReplaceSubstitutions(properties, out var replacement))
						NavigationTitle = replacement;
				}
			}
		}
		else
			YamlFrontMatter = new YamlFrontMatter { Title = Title };

		if (string.IsNullOrEmpty(Title))
		{
			Title = RelativePath;
			Collector.EmitWarning(FilePath, "Document has no title, using file name as title.");
		}



		var contents = document
			.Where(block => block is HeadingBlock { Level: >= 2 })
			.Cast<HeadingBlock>()
			.Select(h => (h.GetData("header") as string, h.GetData("anchor") as string))
			.Select(h => new PageTocItem
			{
				Heading = h.Item1!.Replace("`", "").Replace("*", ""),
				Slug = (h.Item2 ?? h.Item1).Slugify()
			})
			.ToList();
		_tableOfContent.Clear();
		foreach (var t in contents)
			_tableOfContent[t.Slug] = t;

		var labels = document.Descendants<DirectiveBlock>()
			.Select(b => b.CrossReferenceName)
			.Where(l => !string.IsNullOrWhiteSpace(l))
			.Select(s => s.Slugify())
			.ToArray();
		foreach (var label in labels)
		{
			if (!string.IsNullOrEmpty(label))
				_additionalLabels.Add(label);
		}

		_instructionsParsed = true;
	}

	private YamlFrontMatter ReadYamlFrontMatter(string raw)
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


	public string CreateHtml(MarkdownDocument document)
	{
		//we manually render title and optionally append an applies block embedded in yaml front matter.
		var h1 = document.Descendants<HeadingBlock>().FirstOrDefault(h => h.Level == 1);
		if (h1 is not null)
			document.Remove(h1);
		return document.ToHtml(MarkdownParser.Pipeline);
	}
}
