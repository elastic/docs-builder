// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;
using Markdig.Syntax;
using RazorSlices;
using IFileInfo = System.IO.Abstractions.IFileInfo;

namespace Elastic.Markdown.Slices;

public class HtmlWriter(DocumentationSet documentationSet, IFileSystem writeFileSystem)
{
	private DocumentationSet DocumentationSet { get; } = documentationSet;
	private StaticFileContentHashProvider StaticFileContentHashProvider { get; } = new(new EmbeddedOrPhysicalFileProvider(documentationSet.Build));

	private async Task<string> RenderNavigation(string topLevelGroupId, MarkdownFile markdown, Cancel ctx = default)
	{
		var group = DocumentationSet.Tree.NavigationItems
			.OfType<GroupNavigation>()
			.FirstOrDefault(i => i.Group.Id == topLevelGroupId)?.Group;

		var slice = Layout._TocTree.Create(new NavigationViewModel
		{
			Title = group?.Index?.NavigationTitle ?? DocumentationSet.Tree.Index?.NavigationTitle ?? "Docs",
			TitleUrl = group?.Index?.Url ?? DocumentationSet.Tree.Index?.Url ?? DocumentationSet.Build.UrlPathPrefix ?? "/",
			Tree = group ?? DocumentationSet.Tree,
			CurrentDocument = markdown,
			IsRoot = topLevelGroupId == DocumentationSet.Tree.Id,
			Features = DocumentationSet.Configuration.Features,
			TopLevelItems = DocumentationSet.Tree.NavigationItems.OfType<GroupNavigation>().ToList()
		});
		return await slice.RenderAsync(cancellationToken: ctx);
	}

	private readonly ConcurrentDictionary<string, string> _renderedNavigationCache = [];

	public async Task<string> RenderLayout(MarkdownFile markdown, Cancel ctx = default)
	{
		var document = await markdown.ParseFullAsync(ctx);
		return await RenderLayout(markdown, document, ctx);
	}

	private static string GetTopLevelGroupId(MarkdownFile markdown) =>
		markdown.YieldParentGroups().Length > 1
			? markdown.YieldParentGroups()[^2]
			: markdown.YieldParentGroups()[0];

	public async Task<string> RenderLayout(MarkdownFile markdown, MarkdownDocument document, Cancel ctx = default)
	{
		var html = markdown.CreateHtml(document);
		await DocumentationSet.Tree.Resolve(ctx);

		var topLevelNavigationItems = DocumentationSet.Tree.NavigationItems
			.OfType<GroupNavigation>()
			.Select(i => i.Group);

		string? navigationHtml;

		if (DocumentationSet.Configuration.Features.IsPrimaryNavEnabled)
		{
			var topLevelGroupId = GetTopLevelGroupId(markdown);
			if (!_renderedNavigationCache.TryGetValue(topLevelGroupId, out var value))
			{
				value = await RenderNavigation(topLevelGroupId, markdown, ctx);
				_renderedNavigationCache[topLevelGroupId] = value;
			}
			navigationHtml = value;
		}
		else
		{
			if (!_renderedNavigationCache.TryGetValue("root", out var value))
			{
				value = await RenderNavigation(DocumentationSet.Tree.Id, markdown, ctx);
				_renderedNavigationCache["root"] = value;
			}
			navigationHtml = value;
		}

		var previous = DocumentationSet.GetPrevious(markdown);
		var next = DocumentationSet.GetNext(markdown);

		var remote = DocumentationSet.Build.Git.RepositoryName;
		var branch = DocumentationSet.Build.Git.Branch;
		var path = Path.Combine(DocumentationSet.RelativeSourcePath, markdown.RelativePath);
		var editUrl = $"https://github.com/elastic/{remote}/edit/{branch}/{path}";
		var slice = Index.Create(new IndexViewModel
		{
			Title = markdown.Title ?? "[TITLE NOT SET]",
			TitleRaw = markdown.TitleRaw ?? "[TITLE NOT SET]",
			MarkdownHtml = html,
			PageTocItems = [.. markdown.PageTableOfContent.Values],
			Tree = DocumentationSet.Tree,
			CurrentDocument = markdown,
			PreviousDocument = previous,
			NextDocument = next,
			TopLevelNavigationItems = [.. topLevelNavigationItems],
			NavigationHtml = navigationHtml,
			UrlPathPrefix = markdown.UrlPathPrefix,
			Applies = markdown.YamlFrontMatter?.AppliesTo,
			GithubEditUrl = editUrl,
			AllowIndexing = DocumentationSet.Build.AllowIndexing && !markdown.Hidden,
			CanonicalBaseUrl = DocumentationSet.Build.CanonicalBaseUrl,
			Features = DocumentationSet.Configuration.Features,
			StaticFileContentHashProvider = StaticFileContentHashProvider
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
