// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Structural;
using Elastic.ApiExplorer.Topics;
using Elastic.Documentation.Site.FileProviders;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<DocumentSecurityFixture>(Shared = SharedType.PerClass)]
public class DocumentSecurityRenderingTests(DocumentSecurityFixture fixture)
{
	private const string AuthenticationSection = "data-section=\"authentication\"";

	[Test]
	public async Task Render_InheritedSecurity_ShowsABadgeLinkingToThePageAndNoSection()
	{
		var html = await RenderOperationAsync("inherits");

		html.Should().Contain(
			"<a class=\"applicable-info\" href=\"/api/doc/fixture/authentication\"><span class=\"applicable-name\">Auth</span></a>"
		);
		html.Should().NotContain(AuthenticationSection);
	}

	[Test]
	public async Task Render_SameAlternativesInAnotherOrder_IsTreatedAsInherited()
	{
		var html = await RenderOperationAsync("repeats");

		html.Should().Contain("href=\"/api/doc/fixture/authentication\"><span class=\"applicable-name\">Auth</span></a>");
		html.Should().NotContain(AuthenticationSection);
	}

	[Test]
	public async Task Render_NarrowedSecurity_ShowsItsOwnSectionAndABadgeLinkingToIt()
	{
		var html = await RenderOperationAsync("narrows");

		html.Should().Contain("<a class=\"applicable-info\" href=\"#authentication\"><span class=\"applicable-name\">Auth</span></a>");
		html.Should().Contain(AuthenticationSection).And.Contain("Authorization: Bearer &lt;token&gt;");
	}

	[Test]
	public async Task Render_AnonymousAlternative_SaysAuthOptional()
	{
		var html = await RenderOperationAsync("optional");

		html.Should().Contain("href=\"#authentication\"><span class=\"applicable-name\">Auth optional</span></a>");
		html.Should().Contain("Authentication is optional.");
	}

	[Test]
	public async Task Render_DocumentAlternativesPlusAnonymous_IsAnOverrideNotInherited()
	{
		var html = await RenderOperationAsync("relaxes");

		html.Should().Contain("href=\"#authentication\"><span class=\"applicable-name\">Auth optional</span></a>");
		html.Should().Contain(AuthenticationSection).And.Contain("Authentication is optional.");
	}

	[Test]
	public void CreateNavigation_TagGroups_RootsOperationsAtAClassificationThatStillFindsTheAuthenticationPage()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First();

		nav.NavigationRoot.Should().BeOfType<ClassificationNavigationItem>();
		LandingNavigationItem.Of(nav.NavigationRoot)!.AuthenticationUrl.Should().Be("/api/doc/fixture/authentication");
	}

	[Test]
	public async Task Render_EmptySecurity_SaysNoAuthWithoutALinkOrSection()
	{
		var html = await RenderOperationAsync("anonymous");

		html.Should().Contain("<span class=\"applicable-info\"><span class=\"applicable-name\">No auth</span></span>");
		html.Should().NotContain(AuthenticationSection);
	}

	[Test]
	public void CreateNavigation_DocumentSecurity_ListsTheAuthenticationPageUnderTopicsAndPointsTheBadgeAtIt()
	{
		var topics = fixture.Walk().OfType<TopicsNavigationItem>().Single();

		topics.NavigationItems.Should().ContainSingle().Which.Should().BeOfType<StructuralNavigationItem>().Which.Hidden.Should().BeFalse();
		fixture.Navigation.AuthenticationUrl.Should().Be("/api/doc/fixture/authentication");
	}

	[Test]
	public async Task AuthenticationPage_ListsTheDocumentAlternativesOnly()
	{
		var item = fixture.Walk().OfType<StructuralNavigationItem>().Single(n => n.Model.Kind == ApiStructuralKind.Authentication);

		var markdown = await item.Model.RenderCommonMarkAsync(RenderContext(item), null, TestContext.Current!.Execution.CancellationToken);

		markdown.Should().Contain("- Api key: `Authorization: <value>`").And.Contain("- Basic: `Authorization: Basic <credentials>`");
		markdown.Should().NotContain("Bearer");
	}

	private async Task<string> RenderOperationAsync(string operationId)
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().Single(n => n.Model.Operation.OperationId == operationId);
		var renderContext = RenderContext(nav);

		var fs = new MockFileSystem();
		await using (var stream = fs.FileStream.New("/out.html", FileMode.Create, FileAccess.Write))
			await nav.Model.RenderAsync(stream, renderContext, null, TestContext.Current!.Execution.CancellationToken);

		return fs.File.ReadAllText("/out.html");
	}

	private ApiRenderContext RenderContext(Elastic.Documentation.Navigation.INavigationItem current) =>
		new(fixture.Context, fixture.Document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};
}
