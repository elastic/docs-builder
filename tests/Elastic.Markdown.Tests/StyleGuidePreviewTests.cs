// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.StyleGuide;
using Elastic.Codex.StyleGuide;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.StyleGuide;
using Elastic.Markdown.StyleGuide;

namespace Elastic.Markdown.Tests;

public class StyleGuidePreviewTests
{
	private static BuildContext Template()
	{
		var realFs = new FileSystem();
		var invocation = realFs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		return new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(invocation),
			TestHelpers.CreateConfigurationContext(realFs)
		);
	}

	private static async Task<string?> RenderAsync(string id)
	{
		var template = Template();
		using var provider = new EmbeddedOrPhysicalFileProvider(template);
		var ct = TestContext.Current!.Execution.CancellationToken;
		return await StyleGuidePreviews.RenderAsync(id, template, ct)
			?? await CodexStyleGuidePreviews.RenderAsync(id, template, provider, ct)
			?? await ApiStyleGuidePreviews.RenderAsync(id, template, provider, ct);
	}

	[Test]
	public void Catalog_ListsOnlyPreviewsThatCanBeRendered() =>
		StyleGuidePreviewCatalog
			.Entries
			.Select(e => e.Id)
			.Should()
			.BeEquivalentTo([.. StyleGuidePreviews.Ids, .. CodexStyleGuidePreviews.Ids, .. ApiStyleGuidePreviews.Ids]);

	[Test]
	[Arguments("isolated-article", "layout layout--three")]
	[Arguments("isolated-article", "deployment")]
	[Arguments("isolated-directives", "Every directive and role")]
	[Arguments("isolated-directives", "admonition")]
	[Arguments("isolated-directives", "diagnostics__row")]
	[Arguments("api-catalog", "Elastic APIs")]
	[Arguments("api-product", "Lorem API")]
	[Arguments("api-operation", "Run a search")]
	[Arguments("api-operation-plain", "Get a document")]
	[Arguments("isolated-branded", "Lorem Ipsum Inc.")]
	[Arguments("assembler-article", "id=\"docs-bar\"")]
	[Arguments("assembler-article-island", "nav-side__back")]
	[Arguments("assembler-product", "hub-hero")]
	[Arguments("assembler-landing", "class=\"landing\"")]
	[Arguments("assembler-archive", "Archive")]
	[Arguments("assembler-not-found", "Page not found")]
	[Arguments("codex-landing", "codex-hero")]
	[Arguments("codex-landing", "Lorem Ipsum")]
	[Arguments("codex-group", "codex-group__title")]
	[Arguments("codex-docs-group", "layout layout--three")]
	[Arguments("codex-docs-ungrouped", "layout layout--three")]
	public async Task Preview_RendersTheRealLayout(string id, string expected)
	{
		var html = await RenderAsync(id);

		html.Should().NotBeNull().And.Contain("<!DOCTYPE html>").And.Contain(expected);
		html.Should().Contain("ds.css").And.NotContain("styles.css");
	}

	[Test]
	public async Task AssemblerPage_HasTheSiteSectionsInItsDocsBar()
	{
		var html = await RenderAsync("assembler-article");

		html.Should().Contain("Guides").And.Contain("APIs").And.Contain("Products");
	}

	[Test]
	public async Task Directives_CarriesTheDiagramInsideThePage()
	{
		var html = await RenderAsync("isolated-directives");

		html.Should().Contain("src=\"data:image/svg+xml;base64,");
	}

	[Test]
	public async Task TopLevelPage_IsNeverAnIsland()
	{
		var top = await RenderAsync("assembler-article");
		var island = await RenderAsync("assembler-article-island");

		top.Should().NotContain("nav-side__back");
		island.Should().Contain("nav-side__back").And.Contain("nav-side__heading--island");
	}

	[Test]
	public async Task CodexDocsPage_PutsTheGroupInTheTrailOnlyWhenThereIsOne()
	{
		var grouped = await RenderAsync("codex-docs-group");
		var ungrouped = await RenderAsync("codex-docs-ungrouped");

		ungrouped.Should().Contain("codex-trail");
		Crumbs(grouped).Should().Be(Crumbs(ungrouped) + 1, "the group adds one step to the trail");
	}

	private static int Crumbs(string? html) => System.Text.RegularExpressions.Regex.Count(html ?? string.Empty, "breadcrumb__item");

	[Test]
	public async Task UnknownPreview_IsNull() => (await RenderAsync("nope")).Should().BeNull();
}
