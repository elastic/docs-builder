// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Codex.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Markdown.StyleGuide;
using RazorSlices;

namespace Elastic.Codex.StyleGuide;

/// <summary>The Codex pages of the style guide's previews, rendered with the real views around placeholder documentation sets.</summary>
public static class CodexStyleGuidePreviews
{
	public static IReadOnlyList<string> Ids { get; } = ["codex-landing", "codex-group"];

	private const string Lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt.";

	private static CodexCardModel Card(string name, string title, int pages, string icon, string? description = Lorem) =>
		new()
		{
			Url = "#",
			Title = title,
			Description = description,
			Icon = icon,
			PageCount = pages,
			RepoPath = $"/r/{name}"
		};

	private static readonly CodexCardModel[] SearchCards =
	[
		Card("lorem-ipsum", "Lorem Ipsum", 1284, "search"),
		Card("dolor-sit", "Dolor Sit", 540, "code"),
		Card("amet-consectetur", "Amet Consectetur", 942, "database"),
		Card("adipiscing-elit", "Adipiscing Elit", 310, "pulse")
	];

	private static readonly CodexCardModel[] PlatformCards =
	[
		Card("sed-do", "Sed Do Eiusmod", 186, "gear"),
		Card("tempor-incididunt", "Tempor Incididunt", 702, "cloud"),
		Card("ut-labore", "Ut Labore", 64, "documentation", null)
	];

	/// <summary>Renders a preview as a complete HTML document, or <c>null</c> when the id is unknown.</summary>
	public static async Task<string?> RenderAsync(string id, EmbeddedOrPhysicalFileProvider staticFiles, Cancel ctx = default)
	{
		var layout = Layout(new StaticFileContentHashProvider(staticFiles), id == "codex-group");
		var model = id switch
		{
			"codex-landing" =>
				new CodexPreviewModel(
					layout,
					Landing: new CodexLandingContent(
						"Lorem Ipsum Docs",
						"https://codex.example.dev/mcp",
						2,
						7,
						4_028,
						[
							new CodexSection("Search", "#", Lorem, SearchCards.Length, SearchCards),
							new CodexSection("Platform", "#", null, PlatformCards.Length, PlatformCards),
							new CodexSection(
								"Documentation sets",
								null,
								null,
								2,
								[Card("quis-nostrud", "Quis Nostrud", 83, "question"), Card("exercitation", "Exercitation", 36, "pencil")],
								"codex-grid"
							)
						]
					)
				),
			"codex-group" => new CodexPreviewModel(layout, Group: new CodexGroupContent("Search", Lorem, SearchCards)),
			_ => null
		};
		return model is null ? null : await CodexPreviewPage.Create(model).RenderAsync(cancellationToken: ctx);
	}

	private static GlobalLayoutViewModel Layout(StaticFileContentHashProvider hashes, bool group) =>
		new()
		{
			DocSetName = "Lorem Ipsum Docs",
			Description = Lorem,
			CurrentNavigationItem = PreviewNavigation.Page("Lorem Ipsum Docs", "/", false),
			Previous = null,
			Next = null,
			NavigationHtml = string.Empty,
			UrlPathPrefix = null,
			AllowIndexing = false,
			CanonicalBaseUrl = null,
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			Features = new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }),
			StaticFileContentHashProvider = hashes,
			BuildType = BuildType.Codex,
			HeaderTitle = "Lorem Ipsum Docs",
			CodexBreadcrumbs = group ? [new CodexBreadcrumb("Home", "/"), new CodexBreadcrumb("Search", "#")] : null
		};
}
