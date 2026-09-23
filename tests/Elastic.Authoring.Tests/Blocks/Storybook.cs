// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks.Storybook;

public class StorybookMissingReference : MarkdownTest
{
	protected override string Markdown => """
		:::{storybook}
		:::
		""";

	[Fact(DisplayName = "has error")]
	public async Task HasError() => await Docs.HasError("requires :id: or :project:");
}

public class StorybookMissingRegistry : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{storybook}
		:id: kibana:shared_ux:components-button--regular
		:::
		""";

	[Fact(DisplayName = "has error")]
	public async Task HasError() => await Docs.HasError("requires docset.yml storybook.registry");
}
