// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Legacy;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Renderers;
using Elastic.Markdown.Myst.Renderers.LlmMarkdown;
using Elastic.Markdown.Page;
using Markdig.Syntax;
using RazorSlices;
using IFileInfo = System.IO.Abstractions.IFileInfo;

namespace Elastic.Markdown;

public class HtmlWriter(
	DocumentationSet documentationSet,
	IFileSystem writeFileSystem,
	IDescriptionGenerator descriptionGenerator,
	INavigationHtmlWriter? navigationHtmlWriter = null,
	ILegacyUrlMapper? legacyUrlMapper = null,
	IPositionalNavigation? positionalNavigation = null
)
	: IMarkdownStringRenderer
{
	private DocumentationSet DocumentationSet { get; } = documentationSet;

	private INavigationHtmlWriter NavigationHtmlWriter { get; } =
		navigationHtmlWriter ?? new IsolatedBuildNavigationHtmlWriter(documentationSet.Context, documentationSet.Tree);

	private StaticFileContentHashProvider StaticFileContentHashProvider { get; } = new(new EmbeddedOrPhysicalFileProvider(documentationSet.Context));
	private ILegacyUrlMapper LegacyUrlMapper { get; } = legacyUrlMapper ?? new NoopLegacyUrlMapper();
	private IPositionalNavigation PositionalNavigation { get; } = positionalNavigation ?? documentationSet;

	/// <inheritdoc />
	public string Render(string markdown, IFileInfo? source)
	{
		source ??= DocumentationSet.Context.ConfigurationPath;
		var parsed = DocumentationSet.MarkdownParser.ParseStringAsync(markdown, source, null);
		return MarkdownFile.CreateHtml(parsed);
	}

	public async Task<RenderResult> RenderLayout(MarkdownFile markdown, Cancel ctx = default)
	{
		var document = await markdown.ParseFullAsync(ctx);
		return await RenderLayout(markdown, document, ctx);
	}

	private async Task<RenderResult> RenderLayout(MarkdownFile markdown, MarkdownDocument document, Cancel ctx = default)
	{
		var html = MarkdownFile.CreateHtml(document);
		await DocumentationSet.Tree.Resolve(ctx);

		var fullNavigationRenderResult = await NavigationHtmlWriter.RenderNavigation(markdown.NavigationRoot, INavigationHtmlWriter.AllLevels, ctx);
		var miniNavigationRenderResult = await NavigationHtmlWriter.RenderNavigation(markdown.NavigationRoot, 1, ctx);

		var navigationHtmlRenderResult = DocumentationSet.Context.Configuration.Features.LazyLoadNavigation
			? miniNavigationRenderResult
			: fullNavigationRenderResult;

		var current = PositionalNavigation.GetCurrent(markdown);
		var previous = PositionalNavigation.GetPrevious(markdown);
		var next = PositionalNavigation.GetNext(markdown);
		var parents = PositionalNavigation.GetParentsOfMarkdownFile(markdown);

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

		var siteName = DocumentationSet.Tree.Index.Title ?? "Elastic Documentation";

		var legacyPages = LegacyUrlMapper.MapLegacyUrl(markdown.YamlFrontMatter?.MappedPages);

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

		string? allVersionsUrl = null;

		if (PositionalNavigation.MarkdownNavigationLookup.TryGetValue("docs-content://versions.md", out var item))
			allVersionsUrl = item.Url;


		var navigationFileName = $"{fullNavigationRenderResult.Id}.nav.html";

		_ = DocumentationSet.NavigationRenderResults.TryAdd(
			fullNavigationRenderResult.Id,
			fullNavigationRenderResult
		);


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
			Tree = DocumentationSet.Tree,
			CurrentDocument = markdown,
			CurrentNavigationItem = current,
			PreviousDocument = previous,
			NextDocument = next,
			Parents = parents,
			NavigationHtml = navigationHtmlRenderResult.Html,
			NavigationFileName = navigationFileName,
			UrlPathPrefix = markdown.UrlPathPrefix,
			AppliesTo = markdown.YamlFrontMatter?.AppliesTo,
			GithubEditUrl = editUrl,
			AllowIndexing = DocumentationSet.Context.AllowIndexing && (markdown.CrossLink.Equals("docs-content://index.md", StringComparison.OrdinalIgnoreCase) || markdown is DetectionRuleFile || !current.Hidden),
			CanonicalBaseUrl = DocumentationSet.Context.CanonicalBaseUrl,
			GoogleTagManager = DocumentationSet.Context.GoogleTagManager,
			Features = DocumentationSet.Configuration.Features,
			StaticFileContentHashProvider = StaticFileContentHashProvider,
			ReportIssueUrl = reportUrl,
			CurrentVersion = legacyPages?.Count > 0 ? legacyPages.ElementAt(0).Version : "9.0+",
			AllVersionsUrl = allVersionsUrl,
			LegacyPages = legacyPages?.Skip(1).ToArray(),
			VersionDropdownItems = VersionDrownDownItemViewModel.FromLegacyPageMappings(legacyPages?.Skip(1).ToArray()),
			Products = allProducts,
			VersionsConfig = DocumentationSet.Context.VersionsConfig
		});

		return new RenderResult
		{
			Html = await slice.RenderAsync(cancellationToken: ctx),
			FullNavigationPartialHtml = fullNavigationRenderResult.Html,
			NavigationFileName = navigationFileName
		};

	}

	public async Task<MarkdownDocument> WriteAsync(IDirectoryInfo outBaseDir, IFileInfo outputFile, MarkdownFile markdown, IConversionCollector? collector, Cancel ctx = default)
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
		collector?.Collect(markdown, document, rendered.Html);
		await writeFileSystem.File.WriteAllTextAsync(path, rendered.Html, ctx);

		if (!DocumentationSet.Configuration.Features.LazyLoadNavigation)
			return document;

		var navFilePath = Path.Combine(outBaseDir.FullName, rendered.NavigationFileName);
		if (!writeFileSystem.File.Exists(navFilePath))
			await writeFileSystem.File.WriteAllTextAsync(navFilePath, rendered.FullNavigationPartialHtml, ctx);
		return document;
	}

}

public record RenderResult
{
	public required string Html { get; init; }
	public required string FullNavigationPartialHtml { get; init; }
	public required string NavigationFileName { get; init; }

}
