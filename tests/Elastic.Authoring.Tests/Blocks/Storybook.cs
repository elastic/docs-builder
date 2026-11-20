// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks;

public class StorybookMissingReference : MarkdownTest
{
	protected override string Markdown => """
		:::{storybook}
		:::
		""";

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics("error {repo}/docs/index.md:2 storybook directive requires :id: or :project:.");
}

public class StorybookMissingRegistry : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{storybook}
		:id: kibana:shared_ux:components-button--regular
		:::
		""";

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics("error {repo}/docs/index.md:2 storybook directive requires docset.yml storybook.registry.");
}
