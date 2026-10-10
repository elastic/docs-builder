// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Site.Components.DiagnosticsPanel;
using Elastic.Documentation.Site.Components.NavSide;
using Elastic.Documentation.Site.Navigation;
using RazorSlices;

namespace Elastic.Markdown.StyleGuide;

/// <summary>
/// Whole pages of the isolated and assembler builds, written by the real generator from a small sample documentation set
/// with the new design on, so the style guide shows what a build writes without a clone of any repository.
/// </summary>
public static class StyleGuidePreviews
{
	public static IReadOnlyList<string> Ids { get; } =
	[
		"isolated-article",
		"isolated-directives",
		"isolated-branded",
		"isolated-nested",
		"assembler-article",
		"assembler-nested",
		"assembler-article-island",
		"assembler-product",
		"assembler-landing",
		"assembler-archive",
		"assembler-not-found"
	];

	/// <summary>Renders a preview as a complete HTML document, or <c>null</c> when the id is unknown.</summary>
	/// <param name="template">The context of the running site, lending its configuration to the sample documentation set.</param>
	public static async Task<string?> RenderAsync(string id, BuildContext template, Cancel ctx = default)
	{
		if (!Ids.Contains(id))
			return null;

		var assembler = id.StartsWith("assembler", StringComparison.Ordinal);
		var options = new PreviewDocsetOptions
		{
			Branded = id == "isolated-branded",
			BuildType = assembler ? BuildType.Assembler : BuildType.Isolated,
			// An island's tree has back links to the sections above it, which a single sample set cannot supply.
			NavigationHtml = id == "assembler-article-island" ? await NavHtmlAsync(NavSideExamples.Navigation, ctx) : null
		};
		await using var docset = await PreviewDocset.CreateAsync(template, options, ctx);
		var page = await docset.RenderPageAsync(PageFor(id), ctx);

		// The console of docs-builder serve, with made-up messages of each severity, as it sits under a page being written.
		return id == "isolated-directives"
			? page.Replace(
				"</body>",
				await _DiagnosticsPanel.Create(new DiagnosticsPanelModel { Samples = DiagnosticsPanelExamples.Samples }).RenderAsync(
					cancellationToken: ctx
				) + "</body>",
				StringComparison.Ordinal
			)
			: page;
	}

	private static string PageFor(string id) => id switch
	{
		"isolated-directives" => "kitchen-sink.md",
		"assembler-product" => "product.md",
		"isolated-nested" or "assembler-nested" => "guides/deploy/cloud/aws/networking/private-link.md",
		"assembler-landing" => "landing.md",
		"assembler-archive" => "archive.md",
		"assembler-not-found" => "not-found.md",
		_ => "index.md"
	};

	/// <summary>The tree beside a page, drawn by the sidebar component, for builds that cannot be sampled with a single set.</summary>
	public static async Task<string> NavHtmlAsync(NavigationRenderModel navigation, Cancel ctx) =>
		await _NavSide.Create(new NavSideModel { Navigation = navigation }).RenderAsync(cancellationToken: ctx);
}
