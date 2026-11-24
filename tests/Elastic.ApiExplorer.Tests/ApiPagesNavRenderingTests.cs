// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Components.Navigation.NavTree;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public partial class ApiPagesNavRenderingTests
{
	[Test]
	public async Task Render_Isolated_HostsProductAndVersionAheadOfTheTree()
	{
		var model = CreateLayoutModel(
			"/api/elasticsearch/v9/",
			"/api/elasticsearch/v9.md",
			apiContext: CreateApiContext(versions: [
				new("main", "/api/elasticsearch/", Selected: false),
				new("v9", "/api/elasticsearch/v9/", Selected: true),
				new("v8", "/api/elasticsearch/v8/", Selected: false),
			])
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		// One popover, placed by api-menu.ts, opened by one button: API and version are a single choice,
		// and only the selected product and the selected version are marked current.
		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_Isolated_MarksOnlyCurrentProductSelected()
	{
		var model = CreateLayoutModel("/api/elasticsearch/", "/api/elasticsearch.md", apiContext: CreateApiContext());

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_Assembler_ShowsTheApiIdentityAboveTheFilter()
	{
		var model = CreateLayoutModel(
			"/api/elasticsearch/",
			"/api/elasticsearch.md",
			apiContext: CreateApiContext(versions: [
				new("main", "/api/elasticsearch/", Selected: true),
				new("v8", "/api/elasticsearch/v8/", Selected: false),
			]),
			buildType: BuildType.Assembler
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_PreservesTheNavAcrossHtmxSwapsWhenPreviewEnabled()
	{
		var model = CreateLayoutModel(
			"/api/elasticsearch/",
			"/api/elasticsearch.md",
			features: new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true })
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Layout_PutsTheNavigationAfterTheArticleWhenPreviewEnabled()
	{
		var model = CreateLayoutModel(
			"/api/elasticsearch/",
			"/api/elasticsearch.md",
			features: new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true })
		);

		var html = await _Layout.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		var articleClose = html.IndexOf("</article>", StringComparison.Ordinal);
		var nav = html.IndexOf("id=\"pages-nav\"", StringComparison.Ordinal);
		articleClose.Should().BeGreaterThan(-1);
		nav.Should().BeGreaterThan(articleClose);
	}

	[Test]
	public async Task Render_ShowsTheEndpointFilterOnAssemblerBuilds()
	{
		var model = CreateLayoutModel("/api/elasticsearch/", "/api/elasticsearch.md", buildType: BuildType.Assembler);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_ShowsTheEndpointFilterWhenNavigationPreviewIsOn()
	{
		var model = CreateLayoutModel(
			"/api/elasticsearch/",
			"/api/elasticsearch.md",
			features: new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }),
			buildType: BuildType.Assembler
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_OmitsTheEndpointFilterOnIsolatedBuilds()
	{
		var model = CreateLayoutModel("/api/elasticsearch/", "/api/elasticsearch.md", buildType: BuildType.Isolated);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public void DirectOperationUrl_ExpandsEveryAncestorAndMarksTheOperationCurrent()
	{
		const string operationUrl = "/api/elasticsearch/operation/operation-synonyms-put-synonym";
		const string html =
			"""
			<ul id="nav-tree-es">
				<li class="nav-folder">
					<div class="peer nav-folder-peer">
						<input id="other-group" type="checkbox" class="hidden">
						<a href="/api/elasticsearch/group/other" class="sidebar-link nav-folder-link nav-v2-link"><span class="nav-v2-nav-text">Other</span></a>
					</div>
					<div class="nav-subtree-clip">
						<ul class="nav-subtree">
							<li class="flex group/li">
								<a href="/api/elasticsearch/operation/operation-other" class="sidebar-link nav-link nav-v2-link"><span class="nav-v2-nav-text">Other op</span></a>
							</li>
						</ul>
					</div>
				</li>
				<li class="nav-folder">
					<div class="peer nav-folder-peer">
						<input id="search-apis" type="checkbox" class="hidden">
						<span class="sidebar-link nav-folder-link nav-v2-link"><span class="nav-v2-nav-text">Search</span></span>
					</div>
					<div class="nav-subtree-clip">
						<ul class="nav-subtree">
							<li class="nav-folder">
								<div class="peer nav-folder-peer">
									<input id="synonyms" type="checkbox" class="hidden">
									<a href="/api/elasticsearch/group/synonyms" class="sidebar-link nav-folder-link nav-v2-link"><span class="nav-v2-nav-text">Synonyms</span></a>
								</div>
								<div class="nav-subtree-clip">
									<ul class="nav-subtree">
										<li class="flex group/li">
											<a href="/api/elasticsearch/operation/operation-synonyms-put-synonym" class="sidebar-link nav-link nav-v2-link"><span class="nav-v2-nav-text">Create or update a synonym set</span></a>
										</li>
									</ul>
								</div>
							</li>
						</ul>
					</div>
				</li>
			</ul>
			""";

		var marked = NavigationCurrentMarker.Apply(html, operationUrl);

		marked.Should().Contain(
			"href=\"/api/elasticsearch/operation/operation-synonyms-put-synonym\" class=\"sidebar-link nav-link nav-v2-link current\""
		);
		marked.Should().Contain("id=\"search-apis\" type=\"checkbox\" class=\"hidden\" checked");
		marked.Should().Contain("id=\"synonyms\" type=\"checkbox\" class=\"hidden\" checked");
		marked.Should().Contain("id=\"other-group\" type=\"checkbox\" class=\"hidden\">");
		marked.Should().NotContain("id=\"other-group\" type=\"checkbox\" class=\"hidden\" checked");
		CountOccurrences(marked, "nav-subtree-clip--open").Should().Be(2);
	}

	[Test]
	public async Task Render_OmitsTheEndpointFilterWhenAirGapped()
	{
		var model = CreateLayoutModel(
			"/api/elasticsearch/",
			"/api/elasticsearch.md",
			features: new FeatureFlags(new Dictionary<string, bool> { ["air-gapped"] = true }),
			buildType: BuildType.Assembler
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().NotContain("navigation-search");
	}

	[Test]
	[Arguments("get", "GET", "Get a document source")]
	[Arguments("post", "POST", "Index a document")]
	[Arguments("put", "PUT", "Create or update a document")]
	[Arguments("patch", "PATCH", "Update a document")]
	[Arguments("delete", "DELETE", "Delete a document")]
	public async Task Render_OperationRow_ShowsVerbTextAfterOperationName(string method, string verb, string title)
	{
		var html = await _TocTreeNav.Create([
			new NavigationRenderNode
			{
				Kind = NavigationRenderNodeKind.Leaf,
				IsTopLevel = false,
				NavigationTitle = title,
				Url = "/api/elasticsearch/operation/example",
				HttpMethods = [method]
			}
		]).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		var link = OperationLink(html);
		link.Should().Contain($">{verb}<");
		link.Should().Contain(title);
		link.IndexOf(title, StringComparison.Ordinal).Should().BeLessThan(link.IndexOf($">{verb}<", StringComparison.Ordinal));
		link.Should().NotContain("aria-label");
		link.Should().NotContain("icon-api-arrow");
		link.Should().NotContain("icon-api-x");
		link.Should().NotContain("deprecated");
	}

	[Test]
	public async Task Render_DeprecatedOperationRow_LooksCurrentAndNamesItForScreenReaders()
	{
		NavigationRenderNode[] nodes =
		[
			new()
			{
				Kind = NavigationRenderNodeKind.Leaf,
				IsTopLevel = false,
				NavigationTitle = "Get a document source",
				Url = "/api/elasticsearch/operation/operation-get-source",
				HttpMethods = ["get"],
				Deprecated = true
			}
		];
		var ct = TestContext.Current!.Execution.CancellationToken;

		string[] chips =
		[
			OperationLink(await _TocTreeNav.Create(nodes).RenderAsync(cancellationToken: ct)),
			OperationLink(await _NavTreeItems.Create(nodes).RenderAsync(cancellationToken: ct))
		];
		var legacy = OperationLink(await _TocTreeNavLegacy.Create(nodes).RenderAsync(cancellationToken: ct));

		foreach (var link in chips.Append(legacy))
		{
			link[..link.IndexOf('>')].Should().NotContain("deprecated");
			var hidden = link.IndexOf("<span class=\"sr-only\">Deprecated</span>", StringComparison.Ordinal);
			hidden.Should().BeGreaterThan(link.IndexOf("Get a document source", StringComparison.Ordinal));
			link.Should().NotContain("aria-label");
		}

		foreach (var link in chips)
			link
				.IndexOf("<span class=\"sr-only\">Deprecated</span>", StringComparison.Ordinal)
				.Should()
				.BeLessThan(link.IndexOf(">GET<", StringComparison.Ordinal));
		// The legacy chip is the link's ::after, which is read after every child, so after "Deprecated" too.
		legacy.Should().Contain("data-method=\"GET\"");
	}

	[Test]
	public async Task RenderLegacy_SingleMethodLeaf_CarriesTheMethodAsAnAttribute()
	{
		NavigationRenderNode[] nodes =
		[
			new()
			{
				Kind = NavigationRenderNodeKind.Leaf,
				IsTopLevel = false,
				NavigationTitle = "Reindex documents",
				Url = "/api/doc/elasticsearch/operation/operation-reindex",
				HttpMethods = ["post"]
			},
			new()
			{
				Kind = NavigationRenderNodeKind.Leaf,
				IsTopLevel = false,
				NavigationTitle = "Run a search",
				Url = "/api/doc/elasticsearch/operation/operation-search",
				HttpMethods = ["post", "get"]
			}
		];

		var html = await _TocTreeNavLegacy.Create(nodes).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchInlineSnapshot(
			"""
					<li>
						<a href="/api/doc/elasticsearch/operation/operation-reindex" class="sidebar-link" data-method="POST">
							<span>Reindex documents</span>
						</a>
					</li>
					<li>
						<a href="/api/doc/elasticsearch/operation/operation-search" class="sidebar-link">
							<span>Run a search</span>
			<span class="nav-v2-api-method-group"><span class="nav-v2-api-method nav-v2-api-method-post">POST</span><span class="nav-v2-api-method nav-v2-api-method-get">GET</span></span>
						</a>
					</li>
			"""
		);
	}

	[Test]
	public async Task RenderLegacy_ExtensionMethodWithMarkup_IsEncodedInTheAttribute()
	{
		NavigationRenderNode[] nodes =
		[
			new()
			{
				Kind = NavigationRenderNodeKind.Leaf,
				IsTopLevel = false,
				NavigationTitle = "Custom verb",
				Url = "/api/test/operation/operation-custom",
				HttpMethods = ["x\"&lt;"]
			}
		];

		var html = await _TocTreeNavLegacy.Create(nodes).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("data-method=\"X&quot;&amp;LT;\"");
	}

	[Test]
	[Arguments("/api/kibana/group/endpoint-dashboards#get-started", true)]
	[Arguments("/api/kibana/group/endpoint-dashboards", false)]
	public async Task Render_InPageLeaf_OptsOutOfHtmxBoost(string url, bool optsOut)
	{
		NavigationRenderNode[] nodes =
		[
			new() { Kind = NavigationRenderNodeKind.Leaf, IsTopLevel = false, NavigationTitle = "Get started", Url = url }
		];
		var ct = TestContext.Current!.Execution.CancellationToken;

		foreach (var html in new[]
		{
			await _TocTreeNav.Create(nodes).RenderAsync(cancellationToken: ct),
			await _TocTreeNavLegacy.Create(nodes).RenderAsync(cancellationToken: ct)
		})
		{
			var link = OperationLink(html);
			link.Should().NotContain("&quot;");
			if (optsOut)
				link.Should().Contain(" hx-boost=\"false\">");
			else
				link.Should().NotContain("hx-boost");
		}
	}

	private static string OperationLink(string html)
	{
		var start = html.IndexOf("<a ", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0);
		var end = html.IndexOf("</a>", start, StringComparison.Ordinal);
		end.Should().BeGreaterThan(start);
		return html[start..(end + 4)];
	}

	internal static ApiLayoutViewModel CreateLayoutModel(
		string navigationUrl,
		string markdownUrl,
		ApiIdentity? apiContext = null,
		FeatureFlags? features = null,
		BuildType buildType = BuildType.Isolated
	)
	{
		var fs = new FileSystem();
		var context = TestHelpers.CreateBuildContext();
		return new()
		{
			DocSetName = "Api Explorer",
			Description = string.Empty,
			CurrentNavigationItem = new LandingNavigationItem(navigationUrl).Index,
			Previous = null,
			Next = null,
			NavigationHtml = "<nav>tree</nav>",
			BetaBanner = ApiBetaBanner.For(null, navigationUrl),
			UrlPathPrefix = string.Empty,
			AllowIndexing = false,
			CanonicalBaseUrl = null,
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			Features = features ?? new FeatureFlags([]),
			BuildType = buildType,
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			TocItems = [],
			MarkdownUrl = markdownUrl,
			Breadcrumbs = [],
			ApiContext = apiContext,
		};
	}

	private static ApiIdentity CreateApiContext(IReadOnlyList<NavigationSelectOption>? versions = null) =>
		new(
			"Elasticsearch API",
			IconSvg: null,
			[
				new(
					Title: null,
					[
						new("Elasticsearch", "/api/elasticsearch/", Selected: true, IconSvg: null),
						new("Kibana", "/api/kibana/", Selected: false, IconSvg: null)
					]
				)
			],
			HubUrl: "/api/",
			versions ?? [],
			new ApiSpecDownload("/api/elasticsearch.json", "/api/elasticsearch.yaml")
		);

	private static int CountOccurrences(string html, string value)
	{
		var count = 0;
		var start = 0;
		while ((start = html.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
		{
			count++;
			start += value.Length;
		}

		return count;
	}

	[Test]
	public async Task Render_MultiMethodOperationRow_JoinsEveryMethodIntoOneChipDominantFirst()
	{
		var html = await _TocTreeNav.Create([
			new NavigationRenderNode
			{
				Kind = NavigationRenderNodeKind.Leaf,
				IsTopLevel = false,
				NavigationTitle = "Search",
				Url = "/api/elasticsearch/operation/search",
				HttpMethods = ["post", "get"]
			}
		]).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		OperationLink(html)
			.Should()
			.MatchInlineSnapshot(
				"""
			<a href="/api/elasticsearch/operation/search" class="sidebar-link nav-link nav-v2-link">
							<span class="nav-v2-nav-text">Search</span>
							<span class="nav-v2-api-method-group"><span class="nav-v2-api-method nav-v2-api-method-post">POST</span><span class="nav-v2-api-method nav-v2-api-method-get">GET</span></span>

						</a>
			"""
			);
	}
}
