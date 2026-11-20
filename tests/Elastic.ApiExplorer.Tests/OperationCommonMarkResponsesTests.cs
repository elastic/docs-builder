// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Navigation;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class OperationCommonMarkResponsesTests(ApiExplorerFixture fixture)
{
	[Test]
	public void Write_ResponsesSection_IsTitledResponsesWhateverTheCount()
	{
		var lines = new List<string>();
		foreach (var nav in fixture
			.Walk()
			.OfType<OperationNavigationItem>()
			.OrderBy(static n => n.Model.Operation.OperationId, StringComparer.Ordinal))
		{
			var context = RenderContext(nav);
			var page = OperationPageModel.Create(nav.Model, context);
			var markdown = OperationCommonMark.Write(nav.Model, page, context).ReplaceLineEndings("\n");
			var heading = markdown.Split('\n').FirstOrDefault(static line => line is "## Response" or "## Responses");
			lines.Add($"{nav.Model.Operation.OperationId}: {page.Responses.Count} response(s), heading {heading ?? "none"}");
		}

		_ = string
			.Join('\n', lines)
			.Should()
			.MatchInlineSnapshot(
				"""
			: 1 response(s), heading ## Responses
			: 1 response(s), heading ## Responses
			docs-get: 1 response(s), heading ## Responses
			docs-get-source: 1 response(s), heading ## Responses
			search: 2 response(s), heading ## Responses
			"""
			);
	}

	private ApiRenderContext RenderContext(INavigationItem current) =>
		new(fixture.Context, fixture.Document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};
}
