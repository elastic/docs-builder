// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Codex.Group;
using Elastic.Codex.Landing;
using Elastic.Codex.Navigation;
using Elastic.Codex.Page;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.StyleGuide;
using Elastic.Markdown.StyleGuide;
using RazorSlices;

namespace Elastic.Codex.StyleGuide;

/// <summary>
/// The Codex pages of the style guide's previews. A few small sample documentation sets in two groups go through the real Codex
/// navigation and views, so the landing, a group and a set's pages are what a Codex build writes, around placeholder content.
/// </summary>
public static class CodexStyleGuidePreviews
{
	public static IReadOnlyList<string> Ids { get; } = ["codex-landing", "codex-group", "codex-docs-group", "codex-docs-ungrouped"];

	private const string Lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt.";

	private const string ConfigurationYaml =
		"""
		title: Lorem Ipsum Docs
		groups:
		  - id: search
		    name: Search
		    description: Lorem ipsum dolor sit amet, consectetur adipiscing elit.
		    icon: search
		  - id: platform
		    name: Platform
		    description: Sed do eiusmod tempor incididunt ut labore.
		    icon: gear
		""";

	private sealed record SampleSet(string Name, string Title, string Icon, string? Group);

	private static readonly SampleSet[] Sets =
	[
		new("lorem-ipsum", "Lorem Ipsum", "search", "search"),
		new("dolor-sit", "Dolor Sit", "code", "search"),
		new("amet-consectetur", "Amet Consectetur", "database", "platform"),
		new("adipiscing-elit", "Adipiscing Elit", "cloud", "platform"),
		new("quis-nostrud", "Quis Nostrud", "question", null)
	];

	private sealed class PreviewCodexContext(BuildContext inner) : ICodexDocumentationContext
	{
		public IDiagnosticsCollector Collector => inner.Collector;
		public DocumentationWriteFileSystem WriteFileSystem => inner.WriteFileSystem;
		public IDirectoryInfo OutputDirectory => inner.OutputDirectory;
		public IFileInfo ConfigurationPath => inner.ConfigurationPath;
		public BuildType BuildType => BuildType.Codex;

		public void EmitError(string message) { }
	}

	/// <summary>Renders a preview as a complete HTML document, or <c>null</c> when the id is unknown.</summary>
	/// <param name="template">The context of the running site, lending its configuration to the sample documentation sets.</param>
	public static async Task<string?> RenderAsync(
		string id,
		BuildContext template,
		EmbeddedOrPhysicalFileProvider staticFiles,
		Cancel ctx = default
	)
	{
		if (!Ids.Contains(id))
			return null;

		if (id is "codex-docs-group" or "codex-docs-ungrouped")
			return await RenderDocsPageAsync(template, inGroup: id == "codex-docs-group", ctx);

		var docsets = new List<PreviewDocset>();
		try
		{
			foreach (var set in Sets)
			{
				docsets.Add(
					await PreviewDocset.CreateAsync(
						template,
						new PreviewDocsetOptions
						{
							BuildType = BuildType.Codex,
							RepositoryName = set.Name,
							Title = set.Title,
							Description = Lorem
						},
						ctx
					)
				);
			}

			var context = docsets[0].Context;
			var references = Sets.Select(
				s => new CodexDocumentationSetReference { Name = s.Name, Branch = "main", Group = s.Group, Icon = s.Icon }
			).ToList();
			var navigations = Sets.Zip(docsets, (set, docset) => (set.Name, docset.Navigation)).ToDictionary(
				pair => pair.Name,
				pair => pair.Navigation
			);
			var navigation = new CodexNavigation(
				CodexConfiguration.Deserialize(ConfigurationYaml),
				references,
				new PreviewCodexContext(context),
				navigations
			);

			var hashes = new StaticFileContentHashProvider(staticFiles);
			var writer = new CodexNavigationHtmlWriter(context, navigation);
			if (id == "codex-landing")
			{
				var rendered = await writer.RenderNavigation(navigation, navigation.Index, ctx);
				var viewModel = new LandingViewModel(RenderContext(context, navigation, hashes, navigation.Index, rendered.Html))
				{
					IndexPage = (CodexIndexPage)navigation.Index.Model
				};
				return await DesignSystemLandingView.Create(viewModel).RenderAsync(cancellationToken: ctx);
			}

			var group = navigation.GroupNavigations.First(g => g.GroupSlug == "search");
			var groupRendered = await writer.RenderNavigation(group, group.Index, ctx);
			var groupModel = new GroupLandingViewModel(RenderContext(context, navigation, hashes, group.Index, groupRendered.Html))
			{
				Group = group
			};
			return await DesignSystemGroupLandingView.Create(groupModel).RenderAsync(cancellationToken: ctx);
		}
		finally
		{
			foreach (var docset in docsets)
				await docset.DisposeAsync();
		}
	}

	private static CodexRenderContext RenderContext(
		BuildContext context,
		CodexNavigation navigation,
		StaticFileContentHashProvider hashes,
		Elastic.Documentation.Navigation.INavigationItem current,
		string navigationHtml
	) => new(context, navigation, hashes) { NavigationHtml = navigationHtml, CurrentNavigation = current };

	// A set of a Codex site is built by the real generator with the Codex page view. A set in a group has the group in its
	// trail and beside its sibling set in the tree.
	private static async Task<string> RenderDocsPageAsync(BuildContext template, bool inGroup, Cancel ctx)
	{
		var trail = inGroup
			? new[] { new CodexBreadcrumb("Home", "/"), new CodexBreadcrumb("Search", "#"), new CodexBreadcrumb("Lorem Ipsum", "#") }
			: [new CodexBreadcrumb("Home", "/"), new CodexBreadcrumb("Lorem Ipsum", "#")];
		var options = new PreviewDocsetOptions
		{
			BuildType = BuildType.Codex,
			PageViewFactory = new CodexPageViewFactory("Lorem Ipsum Docs", trail),
			NavigationHtml = inGroup ? await StyleGuidePreviews.NavHtmlAsync(PreviewNavigation.CodexDocs(true), ctx) : null
		};
		await using var docset = await PreviewDocset.CreateAsync(template, options, ctx);
		return await docset.RenderPageAsync("index.md", ctx);
	}
}
