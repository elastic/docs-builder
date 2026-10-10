// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Codex.StyleGuide;
using Elastic.Documentation.Navigation.Tests.Isolation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.StyleGuide;
using Elastic.Markdown.StyleGuide;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

public class StyleGuidePreviewTests() : DocumentationSetNavigationTestBase()
{
	private async Task<string?> RenderAsync(string id)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		using var provider = new EmbeddedOrPhysicalFileProvider(CreateContext(fileSystem));
		var ct = TestContext.Current!.Execution.CancellationToken;
		return await StyleGuidePreviews.RenderAsync(id, provider, ct) ?? await CodexStyleGuidePreviews.RenderAsync(id, provider, ct);
	}

	[Test]
	public void Catalog_ListsOnlyPreviewsThatCanBeRendered() =>
		StyleGuidePreviewCatalog
			.Entries
			.Select(e => e.Id)
			.Should()
			.BeEquivalentTo([.. StyleGuidePreviews.Ids, .. CodexStyleGuidePreviews.Ids]);

	[Test]
	[Arguments("isolated-article", "docs-shell")]
	[Arguments("isolated-article", "deployment")]
	[Arguments("isolated-branded", "Lorem Ipsum Inc.")]
	[Arguments("assembler-article", "id=\"docs-bar\"")]
	[Arguments("assembler-landing", "class=\"landing\"")]
	[Arguments("assembler-archive", "Archive")]
	[Arguments("assembler-not-found", "Page not found")]
	[Arguments("codex-landing", "codex-hero")]
	[Arguments("codex-landing", "Lorem Ipsum")]
	[Arguments("codex-group", "codex-group__title")]
	[Arguments("codex-landing", "codex-hero")]
	[Arguments("codex-landing", "Lorem Ipsum")]
	[Arguments("codex-group", "codex-group__title")]
	public async Task Preview_RendersTheRealLayout(string id, string expected)
	{
		var html = await RenderAsync(id);

		html.Should().NotBeNull().And.Contain("<!DOCTYPE html>").And.Contain(expected);
		html.Should().Contain("ds.css").And.NotContain("styles.css");
	}

	[Test]
	public async Task UnknownPreview_IsNull() => (await RenderAsync("nope")).Should().BeNull();
}
