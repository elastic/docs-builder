// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Markdown.Page;
using RazorSlices;

namespace Elastic.Markdown.Tests;

public class RelatedLearningViewTests
{
	[Fact]
	public async Task RendersExternalLinksWithBlankTarget()
	{
		var slice = RelatedLearningView.Create(new RelatedLearningViewModel
		{
			Links =
			[
				new RelatedLearningLink
				{
					Id = "index-basics",
					Title = "Index Basics",
					Url = "https://www.elastic.co/training/index-basics",
					Pages = ["docs-content://manage-data/data-store/index-basics.md"]
				}
			]
		});

		var html = await slice.RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("href=\"https://www.elastic.co/training/index-basics\"");
		html.Should().Contain("target=\"_blank\"");
		html.Should().Contain("rel=\"noopener noreferrer\"");
		html.Should().Contain(">Index Basics</a>");
		html.Should().NotContain("<h2");
		html.Should().Contain("class=\"related-learning\"");
	}

	[Fact]
	public async Task EmptyLinks_RendersNothing()
	{
		var slice = RelatedLearningView.Create(new RelatedLearningViewModel { Links = [] });

		var html = await slice.RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().BeEmpty();
	}
}
