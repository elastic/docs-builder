// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.StyleGuide;
using Elastic.Documentation.Site.FileProviders;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ApiStyleGuidePreviewTests(ApiExplorerFixture fixture)
{
	private async Task<string> RenderAsync(string id)
	{
		using var provider = new EmbeddedOrPhysicalFileProvider(fixture.Context);
		return await ApiStyleGuidePreviews.RenderAsync(id, fixture.Context, provider, TestContext.Current!.Execution.CancellationToken)
			?? throw new InvalidOperationException($"No preview {id}");
	}

	[Test]
	[Arguments("api-catalog", "Elastic APIs")]
	[Arguments("api-product", "Lorem API")]
	[Arguments("api-operation", "Run a search")]
	[Arguments("api-operation-plain", "Get a document")]
	public async Task Preview_RendersTheRealPageWithTheNewDesign(string id, string expected)
	{
		var html = await RenderAsync(id);

		html.Should().Contain("<!DOCTYPE html>").And.Contain(expected);
		html.Should().Contain("ds.css").And.NotContain("styles.css");
	}

	[Test]
	[Arguments("api-catalog")]
	[Arguments("api-product")]
	[Arguments("api-operation")]
	[Arguments("api-operation-plain")]
	public async Task Preview_IsWrittenWithTheAssemblerChrome(string id)
	{
		var html = await RenderAsync(id);

		html.Should().Contain("id=\"elastic-nav\"").And.Contain("id=\"docs-bar\"");
	}

	[Test]
	public async Task Operation_ShowsExamplesOnlyWhenTheSpecHasThem()
	{
		var withExamples = await RenderAsync("api-operation");
		var plain = await RenderAsync("api-operation-plain");

		withExamples.Should().Contain("Lorem ipsum");
		plain.Should().NotContain("Lorem ipsum");
	}

	[Test]
	public async Task UnknownPreview_IsNull()
	{
		using var provider = new EmbeddedOrPhysicalFileProvider(fixture.Context);

		(await ApiStyleGuidePreviews.RenderAsync(
			"nope",
			fixture.Context,
			provider,
			TestContext.Current!.Execution.CancellationToken
		)).Should().BeNull();
	}
}
