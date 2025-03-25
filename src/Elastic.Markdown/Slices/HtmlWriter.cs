// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Discovery;
using Elastic.Markdown.IO.Navigation;
using Markdig.Syntax;
using RazorSlices;
using IFileInfo = System.IO.Abstractions.IFileInfo;

namespace Elastic.Markdown.Slices;

public interface INavigationHtmlWriter
{
	Task<string> RenderNavigation(INavigation currentRootNavigation, Cancel ctx = default);

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

	public async Task<string> RenderNavigation(INavigation currentRootNavigation, Cancel ctx = default)
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

	private NavigationViewModel CreateNavigationModel(INavigation navigation)
	{
		if (navigation is not DocumentationGroup tree)
			throw new InvalidOperationException("Expected a documentation group");

		return new NavigationViewModel
		{
			Title = tree.Index?.NavigationTitle ?? "Docs",
			TitleUrl = tree.Index?.Url ?? Set.Build.UrlPathPrefix ?? "/",
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
	INavigationHtmlWriter? navigationHtmlWriter = null)
{
	private DocumentationSet DocumentationSet { get; } = documentationSet;
	public INavigationHtmlWriter NavigationHtmlWriter { get; } = navigationHtmlWriter ?? new IsolatedBuildNavigationHtmlWriter(documentationSet);
	private StaticFileContentHashProvider StaticFileContentHashProvider { get; } = new(new EmbeddedOrPhysicalFileProvider(documentationSet.Build));


	public async Task<string> RenderLayout(MarkdownFile markdown, Cancel ctx = default)
	{
		var document = await markdown.ParseFullAsync(ctx);
		return await RenderLayout(markdown, document, ctx);
	}

	private async Task<string> RenderLayout(MarkdownFile markdown, MarkdownDocument document, Cancel ctx = default)
	{
		var html = markdown.CreateHtml(document);
		await DocumentationSet.Tree.Resolve(ctx);

		var navigationHtml = await NavigationHtmlWriter.RenderNavigation(markdown.NavigationRoot, ctx);

		var previous = DocumentationSet.GetPrevious(markdown);
		var next = DocumentationSet.GetNext(markdown);

		var remote = DocumentationSet.Build.Git.RepositoryName;
		var branch = DocumentationSet.Build.Git.Branch;
		string? editUrl = null;
		if (DocumentationSet.Build.Git != GitCheckoutInformation.Unavailable && DocumentationSet.Build.DocumentationCheckoutDirectory is { } checkoutDirectory)
		{
			var relativeSourcePath = Path.GetRelativePath(checkoutDirectory.FullName, DocumentationSet.Build.DocumentationSourceDirectory.FullName);
			var path = Path.Combine(relativeSourcePath, markdown.RelativePath);
			editUrl = $"https://github.com/elastic/{remote}/edit/{branch}/{path}";
		}

		Uri? reportLinkParameter = null;
		if (DocumentationSet.Build.CanonicalBaseUrl is not null)
			reportLinkParameter = new Uri(DocumentationSet.Build.CanonicalBaseUrl, Path.Combine(DocumentationSet.Build.UrlPathPrefix ?? string.Empty, markdown.Url));
		var reportUrl = $"https://github.com/elastic/docs-content/issues/new?template=issue_report.yaml&link={reportLinkParameter}&labels=source:web";

		var slice = Index.Create(new IndexViewModel
		{
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
			NavigationHtml = navigationHtml,
			UrlPathPrefix = markdown.UrlPathPrefix,
			Applies = markdown.YamlFrontMatter?.AppliesTo,
			GithubEditUrl = editUrl,
			AllowIndexing = DocumentationSet.Build.AllowIndexing && !markdown.Hidden,
			CanonicalBaseUrl = DocumentationSet.Build.CanonicalBaseUrl,
			EnableGoogleTagManager = DocumentationSet.Build.EnableGoogleTagManager,
			Features = DocumentationSet.Configuration.Features,
			StaticFileContentHashProvider = StaticFileContentHashProvider,
			ReportIssueUrl = reportUrl
		});
		return await slice.RenderAsync(cancellationToken: ctx);
	}

	public async Task WriteAsync(IFileInfo outputFile, MarkdownFile markdown, IConversionCollector? collector, Cancel ctx = default)
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
	}
}
