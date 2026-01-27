// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO;
using Elastic.Markdown.Page;
using Markdig.Syntax;
using RazorSlices;
using IFileInfo = System.IO.Abstractions.IFileInfo;

namespace Elastic.Markdown;

public class HtmlWriter(
	DocumentationSet documentationSet,
	IFileSystem writeFileSystem,
	IDescriptionGenerator descriptionGenerator,
	INavigationTraversable? positionalNavigation = null,
	INavigationHtmlWriter? navigationHtmlWriter = null,
	ILegacyUrlMapper? legacyUrlMapper = null,
	IDocumentInferrerService? documentInferrerService = null
)
	: IMarkdownStringRenderer
{
	private DocumentationSet DocumentationSet { get; } = documentationSet;

	private INavigationHtmlWriter NavigationHtmlWriter { get; } =
		navigationHtmlWriter ?? new IsolatedBuildNavigationHtmlWriter(documentationSet.Context, documentationSet.Navigation);

	private StaticFileContentHashProvider StaticFileContentHashProvider { get; } = new(new EmbeddedOrPhysicalFileProvider(documentationSet.Context));
	private ILegacyUrlMapper LegacyUrlMapper { get; } = legacyUrlMapper ?? new NoopLegacyUrlMapper();
	private INavigationTraversable NavigationTraversable { get; } = positionalNavigation ?? documentationSet;

	private IDocumentInferrerService DocumentInferrerService { get; } = documentInferrerService ?? new NoopDocumentInferrer();

	/// <inheritdoc />
	public string Render(string markdown, IFileInfo? source)
	{
		source ??= DocumentationSet.Context.ConfigurationPath;
		var parsed = DocumentationSet.MarkdownParser.ParseStringAsync(markdown, source, null);
		return MarkdownFile.CreateHtml(parsed);
	}

	public async Task<RenderResult> RenderLayout(MarkdownFile markdown, Cancel ctx = default)
	{
		var document = await markdown.ParseFullAsync(DocumentationSet.TryFindDocumentByRelativePath, ctx);
		return await RenderLayout(markdown, document, ctx);
	}

	private async Task<RenderResult> RenderLayout(MarkdownFile markdown, MarkdownDocument document, Cancel ctx = default)
	{
		var html = MarkdownFile.CreateHtml(document);
		await DocumentationSet.ResolveDirectoryTree(ctx);
		var navigationItem = NavigationTraversable.GetNavigationFor(markdown);

		var root = navigationItem.NavigationRoot;

		var navigationHtmlRenderResult = await NavigationHtmlWriter.RenderNavigation(root, navigationItem, ctx);

		var current = NavigationTraversable.GetNavigationFor(markdown);
		var previous = NavigationTraversable.GetPrevious(markdown);
		var next = NavigationTraversable.GetNext(markdown);
		var parents = NavigationTraversable.GetParentsOfMarkdownFile(markdown);

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
			reportLinkParameter = new Uri(DocumentationSet.Context.CanonicalBaseUrl, Path.Combine(DocumentationSet.Context.UrlPathPrefix ?? string.Empty, current.Url));
		var reportUrl = $"https://github.com/elastic/docs-content/issues/new?template=issue-report.yaml&link={reportLinkParameter}&labels=source:web";

		var siteName = DocumentationSet.Navigation.NavigationTitle;
		var legacyPages = LegacyUrlMapper.MapLegacyUrl(markdown.YamlFrontMatter?.MappedPages);

		// Use DocumentInferrerService to get merged products and versioning info
		var inference = DocumentInferrerService.InferForMarkdown(
			DocumentationSet.Context.Git.RepositoryName,
			markdown.YamlFrontMatter?.MappedPages,
			DocumentationSet.Configuration.Products,
			markdown.YamlFrontMatter?.Products,
			markdown.YamlFrontMatter?.AppliesTo
		);
		var pageProducts = inference.RelatedProducts.ToHashSet();

		string? allVersionsUrl = null;

		// TODO exposese allversions again
		//if (PositionalNavigation.MarkdownNavigationLookup.TryGetValue("docs-content://versions.md", out var item))
		//	allVersionsUrl = item.Url;

		// Get versioning from inference result's product
		var pageVersioning = inference.Product?.VersioningSystem
			?? DocumentationSet.Context.VersionsConfiguration?.GetVersioningSystem(VersioningSystemId.Stack)
			?? throw new InvalidOperationException($"No versioning system available for page '{markdown.RelativePath}'. " +
				"Ensure VersionsConfiguration contains a Stack versioning system or the inferred product has a VersioningSystem defined.");

		var currentBaseVersion = $"{pageVersioning.Base.Major}.{pageVersioning.Base.Minor}+";

		//TODO should we even distinctby
		var breadcrumbs = parents.Reverse().DistinctBy(p => p.Url).ToArray();
		var breadcrumbsList = CreateStructuredBreadcrumbsData(markdown, breadcrumbs);
		var structuredBreadcrumbsJsonString = JsonSerializer.Serialize(breadcrumbsList, BreadcrumbsContext.Default.BreadcrumbsList);


		// Git info for isolated header
		var gitRepo = DocumentationSet.Context.Git.RepositoryName;
		var gitBranch = DocumentationSet.Context.Git.Branch;
		var gitRef = DocumentationSet.Context.Git.Ref;
		string? gitHubDocsUrl = null;
		if (!string.IsNullOrEmpty(gitRepo) && gitRepo != "unavailable" && !string.IsNullOrEmpty(gitBranch) && gitBranch != "unavailable")
			gitHubDocsUrl = $"https://github.com/elastic/{gitRepo}/tree/{gitBranch}/docs";

		var slice = Page.Index.Create(new IndexViewModel
		{
			IsAssemblerBuild = DocumentationSet.Context.AssemblerBuild,
			SiteName = siteName,
			DocSetName = DocumentationSet.Name,
			Title = markdown.Title ?? "[TITLE NOT SET]",
			Description = markdown.YamlFrontMatter?.Description ?? descriptionGenerator.GenerateDescription(document),
			TitleRaw = markdown.TitleRaw ?? "[TITLE NOT SET]",
			MarkdownHtml = html,
			PageTocItems = [.. markdown.PageTableOfContent.Values],
			CurrentDocument = markdown,
			CurrentNavigationItem = current,
			PreviousDocument = previous,
			NextDocument = next,
			Breadcrumbs = breadcrumbs,
			NavigationHtml = navigationHtmlRenderResult.Html,
			UrlPathPrefix = markdown.UrlPathPrefix,
			AppliesTo = markdown.YamlFrontMatter?.AppliesTo,
			GithubEditUrl = editUrl,
			MarkdownUrl = current.Url.TrimEnd('/') + ".md",
			AllowIndexing = DocumentationSet.Context.AllowIndexing && markdown.YamlFrontMatter?.NoIndex != true && (markdown.CrossLink.Equals("docs-content://index.md", StringComparison.OrdinalIgnoreCase) || markdown is DetectionRuleFile || !current.Hidden),
			CanonicalBaseUrl = DocumentationSet.Context.CanonicalBaseUrl,
			GoogleTagManager = DocumentationSet.Context.GoogleTagManager,
			Features = DocumentationSet.Configuration.Features,
			StaticFileContentHashProvider = StaticFileContentHashProvider,
			ReportIssueUrl = reportUrl,
			CurrentVersion = currentBaseVersion,
			AllVersionsUrl = allVersionsUrl,
			LegacyPages = legacyPages?.ToArray(),
			VersionDropdownItems = VersionDropDownItemViewModel.FromLegacyPageMappings(legacyPages?.ToArray()),
			Products = pageProducts,
			VersioningSystem = pageVersioning,
			VersionsConfig = DocumentationSet.Context.VersionsConfiguration!,
			StructuredBreadcrumbsJson = structuredBreadcrumbsJsonString,
			// Git info for isolated header
			GitBranch = gitBranch != "unavailable" ? gitBranch : null,
			GitCommitShort = gitRef is { Length: >= 7 } r && r != "unavailable" ? r[..7] : null,
			GitRepository = gitRepo != "unavailable" ? gitRepo : null,
			GitHubDocsUrl = gitHubDocsUrl
		});

		return new RenderResult
		{
			Html = await slice.RenderAsync(cancellationToken: ctx)
		};

	}

	private BreadcrumbsList CreateStructuredBreadcrumbsData(MarkdownFile markdown, INavigationItem[] crumbs)
	{
		List<BreadcrumbListItem> breadcrumbItems = [];
		var position = 1;
		// Add parents
		breadcrumbItems.AddRange(crumbs.Select(parent => new BreadcrumbListItem
		{
			Position = position++,
			Name = parent.NavigationTitle,
			Item = new Uri(DocumentationSet.Context.CanonicalBaseUrl ?? new Uri("http://localhost"), Path.Combine(DocumentationSet.Context.UrlPathPrefix ?? string.Empty, parent.Url)).ToString()
		}));
		// Add current page
		breadcrumbItems.Add(new BreadcrumbListItem
		{
			Position = position,
			Name = markdown.Title ?? markdown.NavigationTitle,
			Item = null,
		});
		var breadcrumbsList = new BreadcrumbsList
		{
			ItemListElement = breadcrumbItems
		};
		return breadcrumbsList;
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

		var document = await markdown.ParseFullAsync(DocumentationSet.TryFindDocumentByRelativePath, ctx);

		var rendered = await RenderLayout(markdown, document, ctx);
		collector?.Collect(markdown, document, rendered.Html);
		await writeFileSystem.File.WriteAllTextAsync(path, rendered.Html, ctx);

		return document;
	}


}

public record RenderResult
{
	public required string Html { get; init; }
}
