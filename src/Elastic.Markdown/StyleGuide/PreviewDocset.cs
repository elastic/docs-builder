// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.IO;
using Elastic.Markdown.Page;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Markdown.StyleGuide;

/// <summary>How a preview documentation set is built.</summary>
/// <param name="Branded">Adds a <c>branding</c> section, so the footer carries its copyright line.</param>
/// <param name="BuildType">The kind of build the pages are written by.</param>
/// <param name="PageViewFactory">Chooses the page view, as Codex does. The default is the isolated and assembler view.</param>
/// <param name="NavigationHtml">Replaces the tree beside the page, to show a navigation the sample set cannot produce on its own.</param>
public sealed record PreviewDocsetOptions
{
	public bool Branded { get; init; }
	public BuildType BuildType { get; init; } = BuildType.Isolated;
	public IPageViewFactory? PageViewFactory { get; init; }
	public string? NavigationHtml { get; init; }
	public string RepositoryName { get; init; } = "lorem-ipsum";

	/// <summary>The set's title and description, as its index page gives them.</summary>
	public string? Title { get; init; }

	public string? Description { get; init; }
}

/// <summary>
/// A small documentation set written to a temporary folder and built by the real pipeline, with the new design on,
/// so the style guide's previews are pages the renderer wrote, not look-alikes.
/// </summary>
public sealed class PreviewDocset : IAsyncDisposable
{
	private readonly IDirectoryInfo _root;
	private readonly DocumentationGenerator _generator;

	private PreviewDocset(IDirectoryInfo root, DocumentationGenerator generator)
	{
		_root = root;
		_generator = generator;
	}

	/// <param name="template">The context of the running site, lending its configuration.</param>
	public static async Task<PreviewDocset> CreateAsync(BuildContext template, PreviewDocsetOptions options, Cancel ctx)
	{
		var fileSystem = new FileSystem();
		var root = fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), $"style-guide-{Guid.NewGuid():N}"));
		root.Create();
		// A bare .git folder anchors the checkout, and the git information is made up so the header has a deployment to show.
		_ = fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(root.FullName, ".git"));
		var docs = fileSystem.Path.Combine(root.FullName, "docs");
		Write(fileSystem, docs, "docset.yml", PreviewSamples.DocSet(options.Branded));
		Write(fileSystem, docs, "index.md", PreviewSamples.IndexPage(options.Title, options.Description));
		Write(fileSystem, docs, "kitchen-sink.md", PreviewSamples.KitchenSink);
		Write(fileSystem, docs, "product.md", PreviewSamples.Product);
		Write(fileSystem, docs, "landing.md", PreviewSamples.Landing);
		Write(fileSystem, docs, "archive.md", PreviewSamples.Archive);
		Write(fileSystem, docs, "not-found.md", PreviewSamples.NotFound);
		Write(fileSystem, docs, "guides/index.md", PreviewSamples.GuideIndex);
		Write(fileSystem, docs, "guides/install.md", PreviewSamples.GuideInstall);
		Write(fileSystem, docs, "guides/configure.md", PreviewSamples.GuideConfigure);

		var configuration = new ConfigurationContext
		{
			VersionsConfiguration = template.VersionsConfiguration,
			ConfigurationFileProvider = template.ConfigurationFileProvider,
			Endpoints = template.Endpoints,
			ProductsConfiguration = template.ProductsConfiguration,
			LegacyUrlMappings = template.LegacyUrlMappings,
			SearchConfiguration = template.SearchConfiguration
		};
		var docFs = DocumentationFileSystem.Resolve(
			fileSystem.DirectoryInfo.New(docs),
			new DocumentationScopeOptions
			{
				Inner = fileSystem,
				Git = new GitCheckoutInformation
				{
					Branch = "main",
					Remote = "https://github.com/acme/lorem-ipsum.git",
					Ref = "45954b6e0b1d4b0d8f5c6c0c3f1b4a5d9a7c2e11",
					RepositoryName = options.RepositoryName,
					GitHubRef = "refs/pull/42/merge"
				}
			}
		);
		var context = new BuildContext(new DiagnosticsCollector([]), docFs, configuration) { BuildType = options.BuildType };
		context.Configuration.Features.NavigationPreviewEnabled = true;

		var set = new DocumentationSet(context, NullLoggerFactory.Instance, NoopCrossLinkResolver.Instance);
		var generator = new DocumentationGenerator(
			set,
			NullLoggerFactory.Instance,
			navigationHtmlWriter: options.NavigationHtml is { } html ? new FixedNavigationWriter(html) : null,
			pageViewFactory: options.PageViewFactory
		);
		await generator.ResolveDirectoryTree(ctx);
		return new PreviewDocset(root, generator);
	}

	private static void Write(IFileSystem fileSystem, string docs, string relative, string content)
	{
		var path = fileSystem.Path.Combine(docs, relative);
		_ = fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
		fileSystem.File.WriteAllText(path, content);
	}

	private MarkdownFile File(string relativePath) =>
		_generator.DocumentationSet.TryFindDocumentByRelativePath(relativePath) as MarkdownFile
			?? throw new InvalidOperationException($"The sample page {relativePath} was not found.");

	/// <summary>The whole page as the isolated build writes it.</summary>
	/// <summary>The set's own navigation, for building a Codex out of several sets.</summary>
	public Elastic.Documentation.Navigation.Isolated.Node.IDocumentationSetNavigation Navigation => _generator.DocumentationSet.Navigation;

	/// <summary>The context the set was built in.</summary>
	public BuildContext Context => _generator.DocumentationSet.Context;

	public async Task<string> RenderPageAsync(string relativePath, Cancel ctx) =>
		(await _generator.RenderLayout(File(relativePath), ctx)).Html;

	/// <summary>Only the article, as the Markdown renderer writes it, for dropping into another build's layout.</summary>
	public async Task<string> RenderArticleAsync(string relativePath, Cancel ctx)
	{
		var markdown = File(relativePath);
		var document = await markdown.ParseFullAsync(_generator.DocumentationSet.TryFindDocumentByRelativePath, ctx);
		return MarkdownFile.CreateHtml(document);
	}

	public ValueTask DisposeAsync()
	{
		try
		{
			_root.Delete(recursive: true);
		}
		catch (IOException)
		{
			// A temporary folder that cannot be removed is left for the system to clean up.
		}
		return ValueTask.CompletedTask;
	}
}

/// <summary>A navigation writer that always writes the same tree.</summary>
internal sealed class FixedNavigationWriter(string html) : INavigationHtmlWriter
{
	public Task<NavigationRenderResult> RenderNavigation(
		Elastic.Documentation.Navigation.IRootNavigationItem<Elastic.Documentation.Navigation.INavigationModel, Elastic.Documentation.Navigation.INavigationItem> currentRootNavigation,
		Elastic.Documentation.Navigation.INavigationItem currentNavigationItem,
		Cancel ctx = default
	) => Task.FromResult(new NavigationRenderResult { Html = html, Id = "style-guide-fixed" });
}
