// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.IO;
using RazorSlices;

namespace Elastic.Markdown.Slices;

public class HtmlWriter(DocumentationSet documentationSet, IFileSystem writeFileSystem)
{
	private DocumentationSet DocumentationSet { get; } = documentationSet;

	private async Task<string> RenderNavigation(MarkdownFile markdown, Cancel ctx = default)
	{
		var slice = Layout._TocTree.Create(new NavigationViewModel
		{
			Tree = DocumentationSet.Tree,
			CurrentDocument = markdown
		});
		return await slice.RenderAsync(cancellationToken: ctx);
	}

	private string? _renderedNavigation;

	public static async Task<string> RenderPageHtml(MarkdownFile markdown, Cancel ctx = default)
	{
		var document = await markdown.ParseFullAsync(ctx);
		var html = MarkdownFile.CreateHtml(document);
		return html;
	}

	public async Task<string> RenderLayout(MarkdownFile markdown, string markdownHtml, Cancel ctx = default)
	{
		await DocumentationSet.Tree.Resolve(ctx);
		_renderedNavigation ??= await RenderNavigation(markdown, ctx);

		var previous = DocumentationSet.GetPrevious(markdown);
		var next = DocumentationSet.GetNext(markdown);

		var remote = DocumentationSet.Context.Git.RepositoryName;
		var branch = DocumentationSet.Context.Git.Branch;
		var path = Path.Combine(DocumentationSet.RelativeSourcePath, markdown.RelativePath);
		var editUrl = $"https://github.com/elastic/{remote}/edit/{branch}/{path}";

		var slice = Index.Create(new IndexViewModel
		{
			Title = markdown.Title ?? "[TITLE NOT SET]",
			TitleRaw = markdown.TitleRaw ?? "[TITLE NOT SET]",
			MarkdownHtml = markdownHtml,
			PageTocItems = [.. markdown.TableOfContents.Values],
			CurrentDocument = markdown,
			PreviousDocument = previous,
			NextDocument = next,
			NavigationHtml = _renderedNavigation,
			UrlPathPrefix = markdown.UrlPathPrefix,
			Applies = markdown.YamlFrontMatter?.AppliesTo,
			GithubEditUrl = editUrl,
			AllowIndexing = DocumentationSet.Context.AllowIndexing && !markdown.Hidden
		});
		return await slice.RenderAsync(cancellationToken: ctx);
	}


	public async Task<string> RenderPage(MarkdownFile markdown, string markdownHtml, Cancel ctx = default)
	{
		var previous = DocumentationSet.GetPrevious(markdown);
		var next = DocumentationSet.GetNext(markdown);

		var remote = DocumentationSet.Context.Git.RepositoryName;
		var branch = DocumentationSet.Context.Git.Branch;
		var path = Path.Combine(DocumentationSet.RelativeSourcePath, markdown.RelativePath);
		var editUrl = $"https://github.com/elastic/{remote}/edit/{branch}/{path}";

		var slice = Page.Create(new IndexViewModel
		{
			Title = markdown.Title ?? "[TITLE NOT SET]",
			TitleRaw = markdown.TitleRaw ?? "[TITLE NOT SET]",
			MarkdownHtml = markdownHtml,
			PageTocItems = [.. markdown.TableOfContents.Values],
			CurrentDocument = markdown,
			PreviousDocument = previous,
			NavigationHtml = string.Empty,
			NextDocument = next,
			UrlPathPrefix = markdown.UrlPathPrefix,
			Applies = markdown.YamlFrontMatter?.AppliesTo,
			GithubEditUrl = editUrl,
			AllowIndexing = DocumentationSet.Context.AllowIndexing && !markdown.Hidden
		});
		return await slice.RenderAsync(cancellationToken: ctx);
	}

	public async Task WriteAsync(IFileInfo outputFile, MarkdownFile markdown, Cancel ctx = default)
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
		var mainPath = Path.ChangeExtension(path, ".main.html");

		var pageHtml = await RenderPageHtml(markdown, ctx);

		var rendered = await RenderLayout(markdown, pageHtml, ctx);
		await writeFileSystem.File.WriteAllTextAsync(path, rendered, ctx);

		var renderedPage = await RenderPage(markdown, pageHtml, ctx);
		await writeFileSystem.File.WriteAllTextAsync(mainPath, renderedPage, ctx);
	}

}
