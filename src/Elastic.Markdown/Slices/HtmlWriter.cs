// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Legacy;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Myst.FrontMatter;
using Markdig.Syntax;
using RazorSlices;
using IFileInfo = System.IO.Abstractions.IFileInfo;

namespace Elastic.Markdown.Slices;

public interface INavigationHtmlWriter
{
	Task<string> RenderNavigation(INavigationGroup currentRootNavigation, Uri navigationSource, Cancel ctx = default);

	async Task<string> Render(NavigationViewModel model, Cancel ctx)
	{
		var slice = Layout._TocTree.Create(model);
		return await slice.RenderAsync(cancellationToken: ctx);
	}
}

public class IsolatedBuildNavigationHtmlWriter(DocumentationSet set) : INavigationHtmlWriter
{
	private DocumentationSet Set { get; } = set;

	private readonly ConcurrentDictionary<string, string> _renderedNavigationCache = [];

	public async Task<string> RenderNavigation(INavigationGroup currentRootNavigation, Uri navigationSource, Cancel ctx = default)
	{
		var navigation = Set.Configuration.Features.IsPrimaryNavEnabled
			? currentRootNavigation
			: Set.Tree;

		if (_renderedNavigationCache.TryGetValue(navigation.Id, out var value))
			return value;

		var model = CreateNavigationModel(navigation);
		value = await ((INavigationHtmlWriter)this).Render(model, ctx);
		_renderedNavigationCache[navigation.Id] = value;
		return value;
	}

	private NavigationViewModel CreateNavigationModel(INavigationGroup navigation)
	{
		if (navigation is not DocumentationGroup tree)
			throw new InvalidOperationException("Expected a documentation group");

		return new NavigationViewModel
		{
			Title = tree.Index?.NavigationTitle ?? "Docs",
			TitleUrl = tree.Index?.Url ?? Set.Context.UrlPathPrefix ?? "/",
			Tree = tree,
			IsPrimaryNavEnabled = Set.Configuration.Features.IsPrimaryNavEnabled,
			IsGlobalAssemblyBuild = false,
			TopLevelItems = Set.Tree.NavigationItems.OfType<GroupNavigationItem>().ToList()
		};
	}
}

public class HtmlWriter(
	DocumentationSet documentationSet,
	IFileSystem writeFileSystem,
	IDescriptionGenerator descriptionGenerator,
	INavigationHtmlWriter? navigationHtmlWriter = null,
	ILegacyUrlMapper? legacyUrlMapper = null,
	IPositionalNavigation? positionalNavigation = null
)
{
	private DocumentationSet DocumentationSet { get; } = documentationSet;
	public INavigationHtmlWriter NavigationHtmlWriter { get; } = navigationHtmlWriter ?? new IsolatedBuildNavigationHtmlWriter(documentationSet);
	private StaticFileContentHashProvider StaticFileContentHashProvider { get; } = new(new EmbeddedOrPhysicalFileProvider(documentationSet.Context));
	private ILegacyUrlMapper LegacyUrlMapper { get; } = legacyUrlMapper ?? new NoopLegacyUrlMapper();
	private IPositionalNavigation PositionalNavigation { get; } = positionalNavigation ?? documentationSet;

	public async Task<string> RenderLayout(MarkdownFile markdown, Cancel ctx = default)
	{
		var document = await markdown.ParseFullAsync(ctx);
		return await RenderLayout(markdown, document, ctx);
	}

	private async Task<string> RenderLayout(MarkdownFile markdown, MarkdownDocument document, Cancel ctx = default)
	{
		var html = MarkdownFile.CreateHtml(document);
		await DocumentationSet.Tree.Resolve(ctx);

		var navigationHtml = await NavigationHtmlWriter.RenderNavigation(markdown.NavigationRoot, markdown.NavigationSource, ctx);

		var previous = PositionalNavigation.GetPrevious(markdown);
		var next = PositionalNavigation.GetNext(markdown);
		var parents = PositionalNavigation.GetParentMarkdownFiles(markdown);

		var remote = DocumentationSet.Context.Git.RepositoryName;
		var branch = DocumentationSet.Context.Git.Branch;
		string? editUrl = null;
		if (DocumentationSet.Context.Git != GitCheckoutInformation.Unavailable && DocumentationSet.Context.DocumentationCheckoutDirectory is { } checkoutDirectory)
		{
			var relativeSourcePath = Path.GetRelativePath(checkoutDirectory.FullName, DocumentationSet.Context.DocumentationSourceDirectory.FullName);
			var path = Path.Combine(relativeSourcePath, markdown.RelativePath);
			editUrl = $"https://github.com/elastic/{remote}/edit/{branch}/{path}";
		}

		Uri? reportLinkParameter = null;
		if (DocumentationSet.Context.CanonicalBaseUrl is not null)
			reportLinkParameter = new Uri(DocumentationSet.Context.CanonicalBaseUrl, Path.Combine(DocumentationSet.Context.UrlPathPrefix ?? string.Empty, markdown.Url));
		var reportUrl = $"https://github.com/elastic/docs-content/issues/new?template=issue-report.yaml&link={reportLinkParameter}&labels=source:web";

		var siteName = DocumentationSet.Tree.Index?.Title ?? "Elastic Documentation";

		var legacyPage = LegacyUrlMapper.MapLegacyUrl(markdown.YamlFrontMatter?.MappedPages);

		var configProducts = DocumentationSet.Configuration.Products.Select(p =>
		{
			if (Products.AllById.TryGetValue(p, out var product))
				return product;
			throw new ArgumentException($"Invalid product id: {p}");
		});

		var frontMatterProducts = markdown.YamlFrontMatter?.Products ?? [];

		var allProducts = frontMatterProducts
			.Union(configProducts)
			.Distinct()
			.ToHashSet();

		var slice = Index.Create(new IndexViewModel
		{
			SiteName = siteName,
			DocSetName = DocumentationSet.Name,
			Title = markdown.Title ?? "[TITLE NOT SET]",
			Description = markdown.YamlFrontMatter?.Description ?? descriptionGenerator.GenerateDescription(document),
			TitleRaw = markdown.TitleRaw ?? "[TITLE NOT SET]",
			MarkdownHtml = html,
			PageTocItems = [.. markdown.PageTableOfContent.Values],
			Tree = DocumentationSet.Tree,
			CurrentDocument = markdown,
			PreviousDocument = previous,
			NextDocument = next,
			Parents = parents,
			NavigationHtml = navigationHtml,
			UrlPathPrefix = markdown.UrlPathPrefix,
			AppliesTo = markdown.YamlFrontMatter?.AppliesTo,
			GithubEditUrl = editUrl,
			AllowIndexing = DocumentationSet.Context.AllowIndexing && (markdown is DetectionRuleFile || !markdown.Hidden),
			CanonicalBaseUrl = DocumentationSet.Context.CanonicalBaseUrl,
			GoogleTagManager = DocumentationSet.Context.GoogleTagManager,
			Features = DocumentationSet.Configuration.Features,
			StaticFileContentHashProvider = StaticFileContentHashProvider,
			ReportIssueUrl = reportUrl,
			LegacyPage = legacyPage,
			Products = allProducts
		});
		return await slice.RenderAsync(cancellationToken: ctx);
	}

	public async Task<MarkdownDocument> WriteAsync(IFileInfo outputFile, MarkdownFile markdown, IConversionCollector? collector, Cancel ctx = default)
	{
		if (outputFile.Directory is { Exists: false })
			outputFile.Directory.Create();

		string path;
		if (outputFile.Name == "index.md")
			path = Path.ChangeExtension(outputFile.FullName, ".html");
		else
		{
			var dir = outputFile.Directory is null
				? null
				: Path.Combine(outputFile.Directory.FullName, Path.GetFileNameWithoutExtension(outputFile.Name));

			if (dir is not null && !writeFileSystem.Directory.Exists(dir))
				_ = writeFileSystem.Directory.CreateDirectory(dir);

			path = dir is null
				? Path.GetFileNameWithoutExtension(outputFile.Name) + ".html"
				: Path.Combine(dir, "index.html");
		}

		var document = await markdown.ParseFullAsync(ctx);

		var rendered = await RenderLayout(markdown, document, ctx);
		collector?.Collect(markdown, document, rendered);
		await writeFileSystem.File.WriteAllTextAsync(path, rendered, ctx);
		return document;
	}
}
