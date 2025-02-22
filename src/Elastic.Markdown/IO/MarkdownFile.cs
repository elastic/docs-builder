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
using Elastic.Markdown.Myst.InlineParsers;
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
		_configurationFile = context.Configuration.SourceFile;
	}

	public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

	private DiagnosticsCollector Collector { get; }

	public DocumentationGroup? Parent
	{
		get => FileName == "index.md" ? _parent?.Parent : _parent;
		set => _parent = value;
	}

	public bool Hidden { get; internal set; }
	public string? UrlPathPrefix { get; }
	private MarkdownParser MarkdownParser { get; }
	public YamlFrontMatter? YamlFrontMatter { get; private set; }
	public string? TitleRaw { get; private set; }

	public string? Title
	{
		get => _title;
		private set
		{
			_title = value?.StripMarkdown();
			TitleRaw = value;
		}
	}

	public string? NavigationTitle
	{
		get => !string.IsNullOrEmpty(_navigationTitle) ? _navigationTitle : Title;
		private set => _navigationTitle = value?.StripMarkdown();
	}

	//indexed by slug
	private readonly Dictionary<string, PageTocItem> _tableOfContent = new(StringComparer.OrdinalIgnoreCase);
	public IReadOnlyDictionary<string, PageTocItem> TableOfContents => _tableOfContent;

	private readonly HashSet<string> _anchors = new(StringComparer.OrdinalIgnoreCase);
	public IReadOnlySet<string> Anchors => _anchors;

	public string FilePath { get; }
	public string FileName { get; }

	private string? _url;
	public string Url
	{
		get
		{
			if (_url is not null)
				return _url;
			_url = Path.GetFileName(RelativePath) == "index.md"
				? $"{UrlPathPrefix}/{RelativePath.Remove(RelativePath.LastIndexOf("index.md", StringComparison.Ordinal), "index.md".Length)}"
				: $"{UrlPathPrefix}/{RelativePath.Remove(RelativePath.LastIndexOf(".md", StringComparison.Ordinal), 3)}";
			return _url;
		}
	}

	private string? _urlFetch;
	public string UrlFetch
	{
		get
		{
			if (_urlFetch is not null)
				return _urlFetch;
			_urlFetch = Url.TrimEnd('/') + "/index.page.html";
			return _urlFetch;
		}
	}

	public int NavigationIndex { get; internal set; } = -1;

	public string? GroupId { get; set; }

	private bool _instructionsParsed;
	private DocumentationGroup? _parent;
	private string? _title;
	private readonly IFileInfo _configurationFile;

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

		return [.. parents];
	}

	public string[] YieldParentGroups()
	{
		var parents = new List<string>();
		if (GroupId is not null)
			parents.Add(GroupId);
		var parent = Parent;
		do
		{
			if (parent is not null)
				parents.Add(parent.Id);
			parent = parent?.Parent;
		} while (parent != null);

		return [.. parents];
	}

	/// this get set by documentationset when validating redirects
	/// because we need to minimally parse to see the anchors anchor validation is deferred.
	public IReadOnlyDictionary<string, string?>? AnchorRemapping { get; set; }

	private void ValidateAnchorRemapping()
	{
		if (AnchorRemapping is null)
			return;
		foreach (var (_, v) in AnchorRemapping)
		{
			if (v is null or "" or "!")
				continue;
			if (Anchors.Contains(v))
				continue;

			Collector.EmitError(_configurationFile.FullName, $"Bad anchor remap '{v}' does not exist in {RelativePath}");
		}
	}

	public async Task<MarkdownDocument> MinimalParseAsync(Cancel ctx)
	{
		var document = await MarkdownParser.MinimalParseAsync(SourceFile, ctx);
		ReadDocumentInstructions(document);
		ValidateAnchorRemapping();
		return document;
	}

	public async Task<MarkdownDocument> ParseFullAsync(Cancel ctx)
	{
		if (!_instructionsParsed)
			_ = await MinimalParseAsync(ctx);

		var document = await MarkdownParser.ParseAsync(SourceFile, YamlFrontMatter, ctx);
		return document;
	}

	private IReadOnlyDictionary<string, string> GetSubstitutions()
	{
		var globalSubstitutions = MarkdownParser.Configuration.Substitutions;
		var fileSubstitutions = YamlFrontMatter?.Properties;
		if (fileSubstitutions is not { Count: >= 0 })
			return globalSubstitutions;

		var allProperties = new Dictionary<string, string>(fileSubstitutions);
		foreach (var (key, value) in globalSubstitutions)
			allProperties[key] = value;
		return allProperties;
	}

	private void ReadDocumentInstructions(MarkdownDocument document)
	{
		Title = document
			.FirstOrDefault(block => block is HeadingBlock { Level: 1 })?
			.GetData("header") as string;

		YamlFrontMatter = ProcessYamlFrontMatter(document);
		NavigationTitle = YamlFrontMatter.NavigationTitle;

		var subs = GetSubstitutions();

		if (!string.IsNullOrEmpty(NavigationTitle))
		{
			if (NavigationTitle.AsSpan().ReplaceSubstitutions(subs, out var replacement))
				NavigationTitle = replacement;
		}

		if (string.IsNullOrEmpty(Title))
		{
			Title = RelativePath;
			Collector.EmitWarning(FilePath, "Document has no title, using file name as title.");
		}
		else if (Title.AsSpan().ReplaceSubstitutions(subs, out var replacement))
			Title = replacement;

		var contents = document
			.Descendants<HeadingBlock>()
			.Where(block => block is { Level: >= 2 })
			.Select(h => (h.GetData("header") as string, h.GetData("anchor") as string, h.Level))
			.Select(h =>
			{
				var header = h.Item1!.StripMarkdown();
				if (header.AsSpan().ReplaceSubstitutions(subs, out var replacement))
					header = replacement;
				return new PageTocItem { Heading = header, Slug = (h.Item2 ?? header).Slugify(), Level = h.Level };
			})
			.ToList();

		_tableOfContent.Clear();
		foreach (var t in contents)
			_tableOfContent[t.Slug] = t;

		var anchors = document.Descendants<DirectiveBlock>()
			.Select(b => b.CrossReferenceName)
			.Where(l => !string.IsNullOrWhiteSpace(l))
			.Select(s => s.Slugify())
			.Concat(document.Descendants<InlineAnchor>().Select(a => a.Anchor))
			.Concat(_tableOfContent.Values.Select(t => t.Slug))
			.Where(anchor => !string.IsNullOrEmpty(anchor))
			.ToArray();

		foreach (var label in anchors)
			_ = _anchors.Add(label);

		_instructionsParsed = true;
	}

	private YamlFrontMatter ProcessYamlFrontMatter(MarkdownDocument document)
	{
		if (document.FirstOrDefault() is not YamlFrontMatterBlock yaml)
			return new YamlFrontMatter { Title = Title };

		var raw = string.Join(Environment.NewLine, yaml.Lines.Lines);
		var fm = ReadYamlFrontMatter(raw);

		// TODO remove when migration tool and our demo content sets are updated
		var deprecatedTitle = fm.Title;
		if (!string.IsNullOrEmpty(deprecatedTitle))
		{
			Collector.EmitWarning(FilePath, "'title' is no longer supported in yaml frontmatter please use a level 1 header instead.");
			// TODO remove fallback once migration is over and we fully deprecate front matter titles
			if (string.IsNullOrEmpty(Title))
				Title = deprecatedTitle;
		}

		// set title on yaml front matter manually.
		// frontmatter gets passed around as page information throughout
		fm.Title = Title;
		return fm;
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


	public static string CreateHtml(MarkdownDocument document)
	{
		//we manually render title and optionally append an applies block embedded in yaml front matter.
		var h1 = document.Descendants<HeadingBlock>().FirstOrDefault(h => h.Level == 1);
		if (h1 is not null)
			_ = document.Remove(h1);
		return document.ToHtml(MarkdownParser.Pipeline);
	}
}
