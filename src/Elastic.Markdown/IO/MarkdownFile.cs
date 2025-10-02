// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Myst.Directives.Admonition;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Myst.Directives.Stepper;
using Elastic.Markdown.Myst.FrontMatter;
using Elastic.Markdown.Myst.InlineParsers;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;

namespace Elastic.Markdown.IO;

public record MarkdownFile : DocumentationFile, ITableOfContentsScope, INavigationModel
{
	private string? _navigationTitle;

	private readonly DocumentationSet _set;

	private readonly IFileInfo _configurationFile;

	private readonly IReadOnlyDictionary<string, string> _globalSubstitutions;

	public MarkdownFile(
		IFileInfo sourceFile,
		IDirectoryInfo rootPath,
		MarkdownParser parser,
		BuildContext build,
		DocumentationSet set
	)
		: base(sourceFile, rootPath, build.Git.RepositoryName)
	{
		FileName = sourceFile.Name;
		FilePath = sourceFile.FullName;

		UrlPathPrefix = build.UrlPathPrefix;
		MarkdownParser = parser;
		Collector = build.Collector;
		_configurationFile = build.Configuration.SourceFile;
		_globalSubstitutions = build.Configuration.Substitutions;
		_set = set;
		//may be updated by DocumentationGroup.ProcessTocItems
		//todo refactor mutability of MarkdownFile as a whole
		ScopeDirectory = build.Configuration.ScopeDirectory;

		NavigationRoot = set.Tree;
	}

	public bool PartOfNavigation { get; set; }

	public IDirectoryInfo ScopeDirectory { get; set; }

	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; set; }

	private IDiagnosticsCollector Collector { get; }

	public string? UrlPathPrefix { get; }
	protected MarkdownParser MarkdownParser { get; }
	public YamlFrontMatter? YamlFrontMatter { get; private set; }
	public string? TitleRaw { get; protected set; }

	public string? Title
	{
		get => _title;
		protected set
		{
			_title = value?.StripMarkdown();
			TitleRaw = value;
		}
	}

	public string NavigationTitle
	{
		get => !string.IsNullOrEmpty(_navigationTitle) ? _navigationTitle : Title ?? string.Empty;
		private set => _navigationTitle = value.StripMarkdown();
	}


	//indexed by slug
	private readonly Dictionary<string, PageTocItem> _pageTableOfContent = new(StringComparer.OrdinalIgnoreCase);
	public IReadOnlyDictionary<string, PageTocItem> PageTableOfContent => _pageTableOfContent;

	private readonly HashSet<string> _anchors = new(StringComparer.OrdinalIgnoreCase);
	public IReadOnlySet<string> Anchors => _anchors;

	public string FilePath { get; }
	public string FileName { get; }

	protected virtual string RelativePathUrl => RelativePath;

	private string DefaultUrlPathSuffix
	{
		get
		{
			var relativePath = RelativePathUrl;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				relativePath = relativePath.Replace('\\', '/');
			return Path.GetFileName(relativePath) == "index.md"
				? $"/{relativePath.Remove(relativePath.LastIndexOf("index.md", StringComparison.Ordinal), "index.md".Length)}"
				: $"/{relativePath.Remove(relativePath.LastIndexOf(SourceFile.Extension, StringComparison.Ordinal), SourceFile.Extension.Length)}";
		}
	}

	private string DefaultUrlPath => $"{UrlPathPrefix}{DefaultUrlPathSuffix}";

	private string? _url;
	public string Url
	{
		get
		{
			if (_url is not null)
				return _url;
			if (_set.CrossLinkResolver.UriResolver is IsolatedBuildEnvironmentUriResolver)
			{
				_url = DefaultUrlPath;
				return _url;
			}
			var crossLink = new Uri(CrossLink);
			var uri = _set.CrossLinkResolver.UriResolver.Resolve(crossLink, DefaultUrlPathSuffix);
			_url = uri.AbsolutePath;
			return _url;

		}
	}

	//public int NavigationIndex { get; set; } = -1;

	private bool _instructionsParsed;
	private string? _title;

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

	protected virtual async Task<MarkdownDocument> GetMinimalParseDocumentAsync(Cancel ctx) =>
		await MarkdownParser.MinimalParseAsync(SourceFile, ctx);

	protected virtual async Task<MarkdownDocument> GetParseDocumentAsync(Cancel ctx) =>
		await MarkdownParser.ParseAsync(SourceFile, YamlFrontMatter, ctx);

	public async Task<MarkdownDocument> MinimalParseAsync(Cancel ctx)
	{
		var document = await GetMinimalParseDocumentAsync(ctx);
		ReadDocumentInstructions(document);
		ValidateAnchorRemapping();
		return document;
	}

	public async Task<MarkdownDocument> ParseFullAsync(Cancel ctx)
	{
		if (!_instructionsParsed)
			_ = await MinimalParseAsync(ctx);

		var document = await GetParseDocumentAsync(ctx);
		ValidateDuplicateAnchors(document);
		return document;
	}

	private IReadOnlyDictionary<string, string> GetSubstitutions()
	{
		var globalSubstitutions = _globalSubstitutions;
		var fileSubstitutions = YamlFrontMatter?.Properties;
		if (fileSubstitutions is not { Count: >= 0 })
			return globalSubstitutions;

		var allProperties = new Dictionary<string, string>(fileSubstitutions);
		foreach (var (key, value) in globalSubstitutions)
			allProperties[key] = value;
		return allProperties;
	}

	private void ValidateDuplicateAnchors(MarkdownDocument document)
	{
		// Collect all anchors with their source blocks
		var anchorSources = new List<(string Anchor, int Line, int Column, int Length, string Type)>();

		// Collect dropdown anchors
		foreach (var dropdown in document.Descendants<DropdownBlock>())
		{
			if (!string.IsNullOrEmpty(dropdown.CrossReferenceName))
			{
				anchorSources.Add((
					dropdown.CrossReferenceName,
					dropdown.Line + 1,
					dropdown.Column,
					dropdown.OpeningLength,
					"dropdown"
				));
			}
		}

		// Collect heading anchors
		foreach (var heading in document.Descendants<HeadingBlock>())
		{
			var header = heading.GetData("header") as string;
			var anchor = heading.GetData("anchor") as string;
			var slugTarget = (anchor ?? header) ?? string.Empty;
			if (!string.IsNullOrEmpty(slugTarget))
			{
				var slug = slugTarget.Slugify();
				anchorSources.Add((
					slug,
					heading.Line + 1,
					heading.Column,
					1, // heading length
					"heading"
				));
			}
		}

		// Group by anchor and find duplicates
		var duplicateGroups = anchorSources
			.GroupBy(a => a.Anchor, StringComparer.OrdinalIgnoreCase)
			.Where(g => g.Count() > 1);

		foreach (var group in duplicateGroups)
		{
			var anchor = group.Key;
			foreach (var (_, line, column, length, type) in group)
			{
				Collector.Write(new Diagnostic
				{
					Severity = Severity.Hint,
					File = SourceFile.FullName,
					Line = line,
					Column = column,
					Length = length,
					Message = $"Duplicate anchor '{anchor}' found in {type}. Multiple elements with the same anchor may cause linking issues."
				});
			}
		}
	}

	protected void ReadDocumentInstructions(MarkdownDocument document)
	{
		Title ??= document
			.FirstOrDefault(block => block is HeadingBlock { Level: 1 })?
			.GetData("header") as string;

		var yamlFrontMatter = ProcessYamlFrontMatter(document);
		YamlFrontMatter = yamlFrontMatter;
		if (yamlFrontMatter.NavigationTitle is not null)
			NavigationTitle = yamlFrontMatter.NavigationTitle;

		var subs = GetSubstitutions();

		if (!string.IsNullOrEmpty(NavigationTitle))
		{
			if (NavigationTitle.AsSpan().ReplaceSubstitutions(subs, Collector, out var replacement))
				NavigationTitle = replacement;
		}

		if (string.IsNullOrEmpty(Title))
		{
			Title = RelativePath;
			Collector.EmitWarning(FilePath, "Document has no title, using file name as title.");
		}
		else if (Title.AsSpan().ReplaceSubstitutions(subs, Collector, out var replacement))
			Title = replacement;

		var toc = GetAnchors(_set, MarkdownParser, YamlFrontMatter, document, subs, out var anchors);

		_pageTableOfContent.Clear();
		foreach (var t in toc)
			_pageTableOfContent[t.Slug] = t;


		foreach (var label in anchors)
			_ = _anchors.Add(label);

		_instructionsParsed = true;
	}

	public static List<PageTocItem> GetAnchors(
		DocumentationSet set,
		MarkdownParser parser,
		YamlFrontMatter? frontMatter,
		MarkdownDocument document,
		IReadOnlyDictionary<string, string> subs,
		out string[] anchors)
	{
		var includeBlocks = document.Descendants<IncludeBlock>().ToArray();
		var includes = includeBlocks
			.Where(i => i.Found)
			.Select(i =>
			{
				var relativePath = i.IncludePathRelativeToSource;
				if (relativePath is null
					|| !set.FlatMappedFiles.TryGetValue(relativePath, out var file)
					|| file is not SnippetFile snippet)
					return null;

				var anchors = snippet.GetAnchors(set, parser, frontMatter);
				return new { Block = i, Anchors = anchors };
			})
			.Where(i => i is not null)
			.ToArray();

		var includedTocs = includes
			.SelectMany(i => i!.Anchors!.TableOfContentItems
				.Select(item => new { TocItem = item, i.Block.Line }))
			.ToArray();

		// Collect headings from standard markdown
		var headingTocs = document
			.Descendants<HeadingBlock>()
			.Where(block => block is { Level: >= 2 })
			.Select(h => (h.GetData("header") as string, h.GetData("anchor") as string, h.Level, h.Line))
			.Where(h => h.Item1 is not null)
			.Select(h =>
			{
				var header = h.Item1!.StripMarkdown();
				return new
				{
					TocItem = new PageTocItem
					{
						Heading = header,
						Slug = (h.Item2 ?? h.Item1).Slugify(),
						Level = h.Level
					},
					h.Line
				};
			});

		// Collect headings from Stepper steps
		var stepperTocs = document
			.Descendants<DirectiveBlock>()
			.OfType<StepBlock>()
			.Where(step => !string.IsNullOrEmpty(step.Title))
			.Where(step => !IsNestedInOtherDirective(step))
			.Select(step =>
			{
				var processedTitle = step.Title;
				// Apply substitutions to step titles
				if (subs.Count > 0 && processedTitle.AsSpan().ReplaceSubstitutions(subs, set.Context.Collector, out var replacement))
					processedTitle = replacement;

				return new
				{
					TocItem = new PageTocItem
					{
						Heading = processedTitle,
						Slug = step.Anchor,
						Level = step.HeadingLevel // Use dynamic heading level
					},
					step.Line
				};
			});

		var toc = headingTocs
			.Concat(stepperTocs)
			.Concat(includedTocs)
			.OrderBy(item => item.Line)
			.Select(item => item.TocItem)
			.Select(toc => subs.Count == 0
				? toc
				: toc.Heading.AsSpan().ReplaceSubstitutions(subs, set.Context.Collector, out var r)
					? toc with { Heading = r }
					: toc)
			.ToList();

		var includedAnchors = includes.SelectMany(i => i!.Anchors!.Anchors).ToArray();
		anchors =
		[
			..document.Descendants<DirectiveBlock>()
				.Select(b => b.CrossReferenceName)
				.Where(l => !string.IsNullOrWhiteSpace(l))
				.Select(s => s.Slugify())
				.Concat(document.Descendants<InlineAnchor>().Select(a => a.Anchor))
				.Concat(toc.Select(t => t.Slug))
				.Where(anchor => !string.IsNullOrEmpty(anchor))
				.Concat(includedAnchors)
		];
		return toc;
	}

	private static bool IsNestedInOtherDirective(DirectiveBlock block)
	{
		var parent = block.Parent;
		while (parent is not null)
		{
			if (parent is DirectiveBlock { } otherDirective && otherDirective != block && otherDirective is not StepperBlock)
				return true;
			parent = parent.Parent;
		}
		return false;
	}

	private YamlFrontMatter ProcessYamlFrontMatter(MarkdownDocument document)
	{
		if (document.FirstOrDefault() is not YamlFrontMatterBlock yaml)
			return new YamlFrontMatter { Title = Title };

		var raw = string.Join(Environment.NewLine, yaml.Lines.Lines);
		var fm = ReadYamlFrontMatter(raw);

		if (fm.AppliesTo?.Diagnostics is not null)
		{
			foreach (var (severity, message) in fm.AppliesTo.Diagnostics)
				Collector.Emit(severity, FilePath, message);
		}

		// Validate mapped_pages URLs
		if (fm.MappedPages is not null)
		{
			foreach (var url in fm.MappedPages)
			{
				if (!string.IsNullOrEmpty(url) && (!url.StartsWith("https://www.elastic.co/guide", StringComparison.OrdinalIgnoreCase) || !Uri.IsWellFormedUriString(url, UriKind.Absolute)))
				{
					Collector.EmitError(FilePath, $"Invalid mapped_pages URL: \"{url}\". All mapped_pages URLs must start with \"https://www.elastic.co/guide\". Please update the URL to reference content under the Elastic documentation guide.");
				}
			}
		}

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
			return YamlSerialization.Deserialize<YamlFrontMatter>(raw, _set.Context.ProductsConfiguration);
		}
		catch (InvalidProductException e)
		{
			Collector.EmitError(FilePath, "Invalid product in yaml front matter.", e);
			return new YamlFrontMatter();
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
