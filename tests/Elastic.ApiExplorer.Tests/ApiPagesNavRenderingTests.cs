// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public partial class ApiPagesNavRenderingTests
{
	[Test]
	public async Task Render_HostsTheVersionSwitcherAheadOfTheTree()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/v9/",
			"/api/doc/elasticsearch/v9.md",
			versionSwitcherItems: [
				new("latest", "/api/doc/elasticsearch/", Selected: false),
				new("v9", "/api/doc/elasticsearch/v9/", Selected: true),
				new("v8", "/api/doc/elasticsearch/v8/", Selected: false),
			]
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("id=\"api-version-switcher\"");
		html.Should().Contain("aria-selected=\"true\"");
		html.Should().Contain("aria-selected=\"false\"");
		html.Should().NotContain("selected=\"False\"");
		html.Should().NotContain("selected=\"True\"");
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
		html
			.IndexOf("id=\"api-version-switcher\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("<nav>tree</nav>", StringComparison.Ordinal));
	}

	[Test]
	public async Task Render_MarksOnlyCurrentHubProductSelected()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			hubSwitcherItems: [
				new("Back to hub", "/api/", Selected: false),
				new("Elasticsearch", "/api/doc/elasticsearch/", Selected: true),
				new("Kibana", "/api/doc/kibana/", Selected: false),
			]
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("id=\"api-hub-switcher\"");
		html.Should().NotContain("api-version-switcher");
		html.Should().NotContain("selected=\"False\"");
		html.Should().NotContain("selected=\"True\"");
		html.Should().Contain("<option value=\"/api/\">Back to hub</option>");
		html.Should().Contain("<option value=\"/api/doc/elasticsearch/\" selected>Elasticsearch</option>");
		html.Should().Contain("<option value=\"/api/doc/kibana/\">Kibana</option>");
		CountSelectedOptions(html).Should().Be(1);
	}

	[Test]
	public async Task Render_Assembler_OmitsTheHubSwitcher()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			hubSwitcherItems: [
				new("Back to hub", "/api/", Selected: false),
				new("Elasticsearch", "/api/doc/elasticsearch/", Selected: true),
			],
			buildType: BuildType.Assembler
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().NotContain("api-hub-switcher");
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
	}

	[Test]
	public async Task Render_PreservesTheNavAcrossHtmxSwapsWhenPreviewEnabled()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			features: new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true })
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("id=\"pages-nav\"");
		html.Should().Contain("hx-preserve");
	}

	[Test]
	public async Task Render_ShowsJumpToPageOnAssemblerBuilds()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			versionSwitcherItems: [
				new("latest", "/api/doc/elasticsearch/", Selected: true),
				new("v8", "/api/doc/elasticsearch/v8/", Selected: false),
			],
			buildType: BuildType.Assembler
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("<navigation-search type=\"api\" placeholder=\"Jump to API\"></navigation-search>");
		html
			.IndexOf("id=\"api-version-switcher\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("navigation-search", StringComparison.Ordinal));
	}

	[Test]
	public async Task Render_ShowsJumpToPageWhenNavigationPreviewIsOn()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			features: new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }),
			buildType: BuildType.Assembler
		);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("<navigation-search type=\"api\" placeholder=\"Jump to API\"></navigation-search>");
		html.Should().Contain("hx-preserve");
	}

	[Test]
	public async Task Render_OmitsJumpToPageOnIsolatedBuilds()
	{
		var model = CreateLayoutModel("/api/doc/elasticsearch/", "/api/doc/elasticsearch.md", buildType: BuildType.Isolated);

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().NotContain("navigation-search");
	}

	[Test]
	public void DirectOperationUrl_ExpandsEveryAncestorAndMarksTheOperationCurrent()
	{
		const string operationUrl = "/api/doc/elasticsearch/operation/operation-synonyms-put-synonym";
		const string html =
			"""
			<ul id="nav-tree-es">
				<li class="nav-folder">
					<div class="peer nav-folder-peer">
						<input id="other-group" type="checkbox" class="hidden">
						<a href="/api/doc/elasticsearch/group/other" class="sidebar-link nav-folder-link nav-v2-link"><span class="nav-v2-nav-text">Other</span></a>
					</div>
					<div class="nav-subtree-clip">
						<ul class="nav-subtree">
							<li class="flex group/li">
								<a href="/api/doc/elasticsearch/operation/operation-other" class="sidebar-link nav-link nav-v2-link"><span class="nav-v2-nav-text">Other op</span></a>
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
									<a href="/api/doc/elasticsearch/group/synonyms" class="sidebar-link nav-folder-link nav-v2-link"><span class="nav-v2-nav-text">Synonyms</span></a>
								</div>
								<div class="nav-subtree-clip">
									<ul class="nav-subtree">
										<li class="flex group/li">
											<a href="/api/doc/elasticsearch/operation/operation-synonyms-put-synonym" class="sidebar-link nav-link nav-v2-link"><span class="nav-v2-nav-text">Create or update a synonym set</span></a>
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
			"href=\"/api/doc/elasticsearch/operation/operation-synonyms-put-synonym\" class=\"sidebar-link nav-link nav-v2-link current\""
		);
		marked.Should().Contain("id=\"search-apis\" type=\"checkbox\" class=\"hidden\" checked");
		marked.Should().Contain("id=\"synonyms\" type=\"checkbox\" class=\"hidden\" checked");
		marked.Should().Contain("id=\"other-group\" type=\"checkbox\" class=\"hidden\">");
		marked.Should().NotContain("id=\"other-group\" type=\"checkbox\" class=\"hidden\" checked");
		CountOccurrences(marked, "nav-subtree-clip--open").Should().Be(2);
	}

	[Test]
	public async Task Render_OmitsJumpToPageWhenAirGapped()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
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
				Url = "/api/doc/elasticsearch/operation/example",
				HttpMethod = method
			}
		]).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		var link = OperationLink(html);
		link.Should().Contain($">{verb}<");
		link.Should().Contain(title);
		link.IndexOf(title, StringComparison.Ordinal).Should().BeLessThan(link.IndexOf($">{verb}<", StringComparison.Ordinal));
		link.Should().NotContain("aria-label");
		link.Should().NotContain("icon-api-arrow");
		link.Should().NotContain("icon-api-x");
	}

	private static string OperationLink(string html)
	{
		var start = html.IndexOf("<a ", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0);
		var end = html.IndexOf("</a>", start, StringComparison.Ordinal);
		end.Should().BeGreaterThan(start);
		return html[start..(end + 4)];
	}

	private static ApiLayoutViewModel CreateLayoutModel(
		string navigationUrl,
		string markdownUrl,
		IReadOnlyList<ApiVersionSwitcherItem>? versionSwitcherItems = null,
		IReadOnlyList<ApiVersionSwitcherItem>? hubSwitcherItems = null,
		FeatureFlags? features = null,
		BuildType buildType = BuildType.Isolated
	)
	{
		var fs = new FileSystem();
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(fs)
		);
		return new()
		{
			DocSetName = "Api Explorer",
			Description = string.Empty,
			CurrentNavigationItem = new LandingNavigationItem(navigationUrl).Index,
			Previous = null,
			Next = null,
			NavigationHtml = "<nav>tree</nav>",
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
			VersionSwitcherItems = versionSwitcherItems ?? [],
			HubSwitcherItems = hubSwitcherItems ?? [],
		};
	}

	private static int CountSelectedOptions(string html) =>
		OptionTag().Matches(html).Count(m => m.Value.Contains(" selected", StringComparison.Ordinal));

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

	[GeneratedRegex("<option[^>]*>", RegexOptions.IgnoreCase)]
	private static partial Regex OptionTag();
}
