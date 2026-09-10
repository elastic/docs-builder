// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Structural;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class StructuralPageTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
{
	[Fact]
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

		authentication.Url.Should().Be("/api/doc/fixture/authentication");
		authentication.NavigationTitle.Should().Be("Authentication");
		servers.Url.Should().Be("/api/doc/fixture/servers");
		servers.NavigationTitle.Should().Be("Servers");
	}

	[Fact]
	public void ReadSchemes_MapsFixtureApiKey()
	{
		var schemes = StructuralViewModel.ReadSchemes(RenderContext());

		var apiKey = schemes.Should().ContainSingle(scheme => scheme.Id == "apiKey").Subject;
		apiKey.Heading.Should().Be("Api key (apiKey)");
		apiKey.CredentialExample.Should().Be("Authorization: <value>");
		schemes.Select(scheme => scheme.Id).Should().Contain(["apiKey", "basicAuth", "bearerAuth"]);
	}

	[Fact]
	public void ReadServers_MapsFixtureServer()
	{
		var servers = StructuralViewModel.ReadServers(fixture.Document);

		servers.Should().ContainSingle();
		servers[0].Url.Should().Be("https://fixture.example.com");
		servers[0].Description.Should().Be("Fixture server");
	}

	[Fact]
	public async Task AuthenticationPage_WritesCommonMarkFromSchemes()
	{
		var item = fixture.Walk().OfType<StructuralNavigationItem>().Single(n => n.Model.Kind == ApiStructuralKind.Authentication);
		var markdown = await item.Model.RenderCommonMarkAsync(RenderContext(item), TestContext.Current.CancellationToken);

		markdown.Should().Contain("# Authentication");
		markdown.Should().Contain("## Api key (apiKey)");
		markdown.Should().Contain("Authorization: <value>");
	}

	[Fact]
	public async Task ServersPage_WritesCommonMarkFromServers()
	{
		var item = fixture.Walk().OfType<StructuralNavigationItem>().Single(n => n.Model.Kind == ApiStructuralKind.Servers);
		var markdown = await item.Model.RenderCommonMarkAsync(RenderContext(item), TestContext.Current.CancellationToken);

		markdown.Should().Contain("# Servers");
		markdown.Should().Contain("`https://fixture.example.com` (Fixture server)");
	}

	[Fact]
	public void ReadSchemes_EmptyDocument_ReturnsNoSchemes()
	{
		var context = RenderContext(document: new OpenApiDocument { Info = new OpenApiInfo { Title = "t", Version = "1" } });

		StructuralViewModel.ReadSchemes(context).Should().BeEmpty();
	}

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
