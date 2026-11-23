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
		apiKey.CredentialExample.Should().Be("Authorization: ApiKey <api-key>");
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
			Authorization: ApiKey <api-key>
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
	public void AuthenticationPage_ListsEachAlternativeInTheRailByItsBumpShAnchor()
	{
		var item = fixture.Walk().OfType<StructuralNavigationItem>().Single(n => n.Model.Kind == ApiStructuralKind.Authentication);

		var page = StructuralViewModel.Create(item.Model, RenderContext(item));

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
	}

	[Test]
	public async Task AuthenticationFragments_DescriptionWithAnUnclosedFence_StaysInItsOwnFragment()
	{
		// Each fragment renders on its own, so the fence cannot swallow the credential or the next alternative's heading.
		var document = await TestSpecs.LoadValidSpecAsync(
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "security": [{ "apiKeyAuth": [] }, { "basicAuth": [] }],
			  "components": { "securitySchemes": {
			    "apiKeyAuth": { "type": "apiKey", "in": "header", "name": "Authorization", "description": "Send the key:\n\n```\ncurl -H ..." },
			    "basicAuth": { "type": "http", "scheme": "basic" }
			  } }
			}
			"""
		);

		string
			.Join("<<< next fragment >>>\n", OperationCommonMark.AuthenticationFragments(document, anchors: true))
			.Should()
			.MatchInlineSnapshot(
				"""
			## Api key [doc-authentication-api-key-auth]

			<<< next fragment >>>
			Send the key:

			```
			curl -H ...

			<<< next fragment >>>
			```http
			Authorization: ApiKey <api-key>
			```

			## Basic [doc-authentication-basic-auth]

			```http
			Authorization: Basic <credentials>
			```
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

	private ApiRenderContext RenderContext(INavigationItem? current = null, OpenApiDocument? document = null) =>
		new(
			fixture.Context,
			document ?? fixture.Document,
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
		)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current ?? fixture.Navigation,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};
}
