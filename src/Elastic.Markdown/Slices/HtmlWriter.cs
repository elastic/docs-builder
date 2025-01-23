// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RazorSlices;

namespace Elastic.Markdown.Slices;

public class HtmlWriter
{
	private readonly IFileSystem _writeFileSystem;

	public HtmlWriter(DocumentationSet documentationSet, IFileSystem writeFileSystem)
	{
		_writeFileSystem = writeFileSystem;
		var services = new ServiceCollection();
		services.AddLogging();

		ServiceProvider = services.BuildServiceProvider();
		LoggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
		DocumentationSet = documentationSet;
	}

	private DocumentationSet DocumentationSet { get; }
	public ILoggerFactory LoggerFactory { get; }
	public ServiceProvider ServiceProvider { get; }

	private async Task<string> RenderNavigation(MarkdownFile markdown, Cancel ctx = default)
	{
		var slice = Layout._TocTree.Create(new NavigationViewModel
		{
			Tree = DocumentationSet.Tree,
			CurrentDocument = markdown
		});
		return await slice.RenderAsync(cancellationToken: ctx);
	}

	public async Task<string> RenderLayout(MarkdownFile markdown, Cancel ctx = default)
	{
		var document = await markdown.ParseFullAsync(ctx);
		var html = markdown.CreateHtml(document);
		await DocumentationSet.Tree.Resolve(ctx);
		var navigationHtml = await RenderNavigation(markdown, ctx);

		var previous = DocumentationSet.MarkdownFiles.GetValueOrDefault(markdown.NavigationIndex - 1);
		var next = DocumentationSet.MarkdownFiles.GetValueOrDefault(markdown.NavigationIndex + 1);

		var remote = DocumentationSet.Context.Git.RepositoryName;
		var branch = DocumentationSet.Context.Git.Branch;
		var path = Path.Combine(DocumentationSet.RelativeSourcePath, markdown.RelativePath);
		var editUrl = $"https://github.com/elastic/{remote}/edit/{branch}/{path}";

		var slice = Index.Create(new IndexViewModel
		{
			Title = markdown.Title ?? "[TITLE NOT SET]",
			TitleRaw = markdown.TitleRaw ?? "[TITLE NOT SET]",
			MarkdownHtml = html,
			PageTocItems = markdown.TableOfContents.Values.ToList(),
			Tree = DocumentationSet.Tree,
			CurrentDocument = markdown,
			PreviousDocument = previous,
			NextDocument = next,
			NavigationHtml = navigationHtml,
			UrlPathPrefix = markdown.UrlPathPrefix,
			Applies = markdown.YamlFrontMatter?.AppliesTo,
			GithubEditUrl = editUrl,
			AllowIndexing = DocumentationSet.Context.AllowIndexing
		});
		return await slice.RenderAsync(cancellationToken: ctx);
	}

	public async Task WriteAsync(IFileInfo outputFile, MarkdownFile markdown, Cancel ctx = default)
	{
		if (outputFile.Directory is { Exists: false })
			outputFile.Directory.Create();

		var rendered = await RenderLayout(markdown, ctx);
		var path = Path.ChangeExtension(outputFile.FullName, ".html");
		await _writeFileSystem.File.WriteAllTextAsync(path, rendered, ctx);
	}

}
