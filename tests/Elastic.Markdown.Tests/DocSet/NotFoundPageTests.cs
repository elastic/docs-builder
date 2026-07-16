// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Markdown.Tests.DocSet;

public class NotFoundPageTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public async Task RenderLayout_NotFoundPage_IncludesRecoveryOptions()
	{
		var notFound = Set.MarkdownFiles.Single(file => file.RelativePath.EndsWith("404.md", StringComparison.Ordinal));
		Configuration!.Features.RelatedPagesEnabled = true;

		var rendered = await Generator.RenderLayout(notFound, TestContext.Current.CancellationToken);

		rendered.Html.Should().Contain("Page not found");
		rendered.Html.Should().NotContain("<navigation-search");
		rendered.Html.Should().Contain("<related-pages>");
		rendered.Html.Should().Contain("Open full search");
	}

	[Fact]
	public async Task RenderLayout_RelatedPagesDisabled_OmitsRelatedPagesComponent()
	{
		var notFound = Set.MarkdownFiles.Single(file => file.RelativePath.EndsWith("404.md", StringComparison.Ordinal));

		var rendered = await Generator.RenderLayout(notFound, TestContext.Current.CancellationToken);

		rendered.Html.Should().NotContain("<related-pages>");
	}
}
