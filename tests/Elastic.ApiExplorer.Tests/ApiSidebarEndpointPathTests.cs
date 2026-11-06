// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.Navigation;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ApiSidebarEndpointPathTests(ApiExplorerFixture fixture)
{
	[Test]
	public void Create_OperationLeaves_CarryMethodAndPathTemplate()
	{
		var leaves = Flatten(RenderModel().Tree)
			.Where(static n => n.HttpPath is not null)
			.Select(static n => $"{n.HttpMethod!.ToUpperInvariant()} {n.HttpPath}  {n.NavigationTitle}");

		string
			.Join('\n', leaves)
			.Should()
			.MatchInlineSnapshot(
				"""
			POST /{index}/_search  Search
			GET /{index}/_doc/{id}  docs.get
			GET /{index}/_source/{id}  docs.get_source
			POST /_fixture/anonymous  /_fixture/anonymous:POST
			DELETE /_fixture/cleanup  Clean up fixture data
			"""
			);
	}

	[Test]
	public async Task Render_OperationLeaf_ShowsThePathUnderTheTitle()
	{
		var leaf = Flatten(RenderModel().Tree).First(static n => n.HttpPath == "/{index}/_doc/{id}");

		var html = await _TocTreeNav.Create([leaf]).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<li class="flex group/li">
				<a href="/api/doc/fixture/operation/operation-docs-get" class="sidebar-link nav-link nav-v2-link">
					<span class="nav-v2-nav-text">docs.get
						<code class="nav-api-path">/{index}/_doc/{id}</code>
					</span>
					<span class="nav-v2-api-method nav-v2-api-method-get">GET</span>
				</a>
			</li>
			"""
		);
	}

	private NavigationRenderModel RenderModel() =>
		NavigationRenderModel.Create(
			tree: fixture.Navigation,
			topLevelItems: [],
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false
		);

	private static IEnumerable<NavigationRenderNode> Flatten(IEnumerable<NavigationRenderNode> nodes) =>
		nodes.SelectMany(static n => (IEnumerable<NavigationRenderNode>)[n, .. Flatten(n.NavigationItems)]);
}
