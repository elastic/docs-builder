// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Structural;
using Elastic.Documentation;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class StructuralPageTests(ApiExplorerFixture fixture)
{
	[Test]
	public void CreateNavigation_IncludesAuthenticationAndServersPages()
	{
		var items = fixture.Walk().ToArray();

		var authentication = items
			.OfType<StructuralNavigationItem>()
			.Should()
			.ContainSingle(item => item.Model.Kind == ApiStructuralKind.Authentication)
			.Subject;
		var servers = items
			.OfType<StructuralNavigationItem>()
			.Should()
			.ContainSingle(item => item.Model.Kind == ApiStructuralKind.Servers)
			.Subject;

		authentication.Url.Should().Be("/api/fixture/authentication");
		authentication.Hidden.Should().BeFalse("the sidebar lists it with the guide pages");
		servers.Hidden.Should().BeTrue("the page stays for old links but is not listed in the nav");
		authentication.NavigationTitle.Should().Be("Authentication");
		servers.Url.Should().Be("/api/fixture/servers");
		servers.NavigationTitle.Should().Be("Servers");
	}

	[Test]
	public void Overview_ListsAuthenticationAsATopicAndHidesServers()
	{
		var rows = ApiOverviewBuilder.Build(fixture.Navigation, fixture.Context.VersionsConfiguration);

		rows.Select(row => row.Url).Should().NotContain("/api/fixture/servers");
		rows.Select(row => row.Url).Should().Contain("/api/fixture/authentication");
	}

	[Test]
	public void Create_EmptyDocument_OmitsAuthenticationAndServersPages()
	{
		var items = CreateItems(new OpenApiDocument { Info = new OpenApiInfo { Title = "t", Version = "1" } });

		items.Should().BeEmpty();
	}

	[Test]
	public void Create_NoSchemes_OmitsAuthenticationPage()
	{
		var document = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = "t", Version = "1" },
			Servers = [new OpenApiServer { Url = "https://example.com" }]
		};
		var items = CreateItems(document);

		items.Should().ContainSingle(item => item.Model.Kind == ApiStructuralKind.Servers);
		items.Should().NotContain(item => item.Model.Kind == ApiStructuralKind.Authentication);
	}

	[Test]
	public void Create_NoServers_OmitsServersPage()
	{
		var document = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = "t", Version = "1" },
			Components = new OpenApiComponents
			{
				SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
				{
					["apiKey"] = new OpenApiSecurityScheme
					{
						Type = SecuritySchemeType.ApiKey,
						Name = "Authorization",
						In = ParameterLocation.Header
					}
				}
			}
		};
		var items = CreateItems(document);

		items.Should().ContainSingle(item => item.Model.Kind == ApiStructuralKind.Authentication);
		items.Should().NotContain(item => item.Model.Kind == ApiStructuralKind.Servers);
	}

	private static IReadOnlyList<StructuralNavigationItem> CreateItems(OpenApiDocument document)
	{
		var root = new LandingNavigationItem("/api/fixture");
		return StructuralNavigationItem.Create(urlPathPrefix: null, "fixture", root, document);
	}

	[Test]
	public void ReadRequirements_MapsFixtureApiKey()
	{
		var requirements = StructuralViewModel.ReadRequirements(fixture.Document);

		var schemes = requirements.SelectMany(requirement => requirement.Schemes).ToArray();
		var apiKey = schemes.Should().ContainSingle(scheme => scheme.Id == "apiKey").Subject;
		apiKey.Label.Should().Be("Api key");
		apiKey.CredentialExample.Should().Be("Authorization: <value>");
		schemes.Select(scheme => scheme.Id).Should().Contain(["apiKey", "basicAuth", "bearerAuth"]);
	}

	[Test]
	public void ReadServers_MapsFixtureServer()
	{
		var servers = StructuralViewModel.ReadServers(fixture.Document);

		servers.Should().ContainSingle();
		servers[0].Url.Should().Be("https://fixture.example.com");
		servers[0].Description.Should().Be("Fixture server");
	}

	[Test]
	public async Task AuthenticationPage_WritesCommonMarkFromSchemes()
	{
		var item = fixture.Walk().OfType<StructuralNavigationItem>().Single(n => n.Model.Kind == ApiStructuralKind.Authentication);
		var markdown = await item.Model.RenderCommonMarkAsync(RenderContext(item), null, TestContext.Current!.Execution.CancellationToken);

		markdown.Should().MatchInlineSnapshot(
			"""
			# Authentication

			## Api key

			Send the encoded key with the ApiKey prefix.

			```http
			Authorization: <value>
			```

			## Basic

			```http
			Authorization: Basic <credentials>
			```

			## Bearer

			```http
			Authorization: Bearer <token>
			```
			"""
		);
	}

	[Test]
	public void AuthenticationPage_GivesEachAlternativeAHeadingWithItsBumpShAnchor()
	{
		var item = fixture.Walk().OfType<StructuralNavigationItem>().Single(n => n.Model.Kind == ApiStructuralKind.Authentication);
		var page = StructuralViewModel.Create(item.Model, RenderContext(item));

		// The page renders these fragments like its prose, so the headings are docs headings with these anchors.
		string
			.Concat(OperationCommonMark.AuthenticationFragments(page.Requirements, page.AuthenticationOptional, anchors: true))
			.Should()
			.MatchInlineSnapshot(
				"""
				## Api key [doc-authentication-api-key]

				Send the encoded key with the ApiKey prefix.

				```http
				Authorization: <value>
				```

				## Basic [doc-authentication-basic-auth]

				```http
				Authorization: Basic <credentials>
				```

				## Bearer [doc-authentication-bearer-auth]

				```http
				Authorization: Bearer <token>
				```
				"""
			);
	}

	[Test]
	public void AuthenticationPage_KeepsEachHeadingLinkableAndListsItInTheRail()
	{
		var item = fixture.Walk().OfType<StructuralNavigationItem>().Single(n => n.Model.Kind == ApiStructuralKind.Authentication);

		var page = StructuralViewModel.Create(item.Model, RenderContext(item, renderer: AnchoredHeadingRenderer.Instance));

		string
			.Join("\n", page.TocItems.Select(static t => $"{t.Level} {t.Heading} #{t.Slug}"))
			.Should()
			.MatchInlineSnapshot(
				"""
			2 Api key #doc-authentication-api-key
			2 Basic #doc-authentication-basic-auth
			2 Bearer #doc-authentication-bearer-auth
			"""
			);
		// The sanitizer keeps the id and the heading link of every listed heading, so the Bump.sh anchors land.
		string
			.Join("\n", page.BodyHtml!.Value.Split('\n').Where(static line => line.Contains("heading-wrapper", StringComparison.Ordinal)))
			.Should()
			.MatchInlineSnapshot(
				"""
				<div class="heading-wrapper" id="doc-authentication-api-key"><h2><a class="headerlink" href="#doc-authentication-api-key">Api key</a></h2>
				<div class="heading-wrapper" id="doc-authentication-basic-auth"><h2><a class="headerlink" href="#doc-authentication-basic-auth">Basic</a></h2>
				<div class="heading-wrapper" id="doc-authentication-bearer-auth"><h2><a class="headerlink" href="#doc-authentication-bearer-auth">Bearer</a></h2>
				"""
			);
	}

	[Test]
	[Arguments("apiKeyAuth", "doc-authentication-api-key-auth")]
	[Arguments("apiKey", "doc-authentication-api-key")]
	[Arguments("basicAuth", "doc-authentication-basic-auth")]
	[Arguments("es_api_key", "doc-authentication-es-api-key")]
	[Arguments("OAuth2", "doc-authentication-oauth2")]
	public void AuthenticationAnchor_SchemeId_MatchesBumpSh(string schemeId, string expected) =>
		ApiUrlBuilder.AuthenticationAnchor([schemeId]).Should().Be(expected);

	[Test]
	public async Task ServersPage_WritesCommonMarkFromServers()
	{
		var item = fixture.Walk().OfType<StructuralNavigationItem>().Single(n => n.Model.Kind == ApiStructuralKind.Servers);
		var markdown = await item.Model.RenderCommonMarkAsync(RenderContext(item), null, TestContext.Current!.Execution.CancellationToken);

		markdown.Should().MatchInlineSnapshot(
			"""
			# Servers

			- `https://fixture.example.com` (Fixture server)
			"""
		);
	}

	[Test]
	public void ReadRequirements_EmptyDocument_ReturnsNoRequirements() =>
		StructuralViewModel
			.ReadRequirements(new OpenApiDocument { Info = new OpenApiInfo { Title = "t", Version = "1" } })
			.Should()
			.BeEmpty();

	private ApiRenderContext RenderContext(
		INavigationItem? current = null,
		OpenApiDocument? document = null,
		IMarkdownStringRenderer? renderer = null
	) =>
		new(
			fixture.Context,
			document ?? fixture.Document,
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
		)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current ?? fixture.Navigation,
			MarkdownRenderer = renderer ?? PassthroughMarkdownRenderer.Instance
		};

	/// <summary>
	/// Renders <c>## Heading [anchor]</c> lines with the markup the API description pipeline gives them
	/// (<c>ApiDescriptionHeadingTests</c> in Elastic.Markdown.Tests pins it) and every other line as a paragraph.
	/// </summary>
	private sealed class AnchoredHeadingRenderer : IMarkdownStringRenderer
	{
		public static AnchoredHeadingRenderer Instance { get; } = new();

		public string Render(string markdown, IFileInfo? source) => $"<p>{markdown}</p>";

		public string RenderApiDescription(string markdown, IFileInfo? source) =>
			string.Concat(
				Lines(markdown).Select(
					line => Heading(line) is { } h
						? $"<div class=\"heading-wrapper\" id=\"{h.Slug}\"><h{h.Level}><a class=\"headerlink\" href=\"#{h.Slug}\">{h.Heading}</a></h{h.Level}>\n</div>\n"
						: $"<p>{line}</p>\n"
				)
			);

		public IReadOnlyList<TocItem> ReadApiDescriptionHeadings(string markdown, IFileInfo? source) =>
			[.. Lines(markdown).Select(Heading).OfType<TocItem>()];

		private static IEnumerable<string> Lines(string markdown) =>
			markdown.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

		private static TocItem? Heading(string line)
		{
			var level = line.TakeWhile(static c => c == '#').Count();
			var open = line.LastIndexOf('[');
			return level < 2 || open < 0 || !line.EndsWith(']') ? null : new TocItem(line[level..open].Trim(), line[(open + 1)..^1], level);
		}
	}
}
