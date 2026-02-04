// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Codex.Navigation;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Navigation.Isolated.Node;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Codex;

/// <summary>
/// Tests that verify navigation is correctly structured for grouped and ungrouped repos.
/// Even though URLs are stable (/r/repo-name), the sidebar navigation should differ:
/// - Grouped repos: sidebar shows the group landing + all group members as top-level items
/// - Ungrouped repos: sidebar shows only that repo's internal navigation
/// </summary>
public class CodexNavigationRenderingTests(ITestOutputHelper output) : CodexNavigationTestBase(output)
{
	[Fact]
	public void GroupNavigation_TopLevelItems_ContainsAllGroupMembers()
	{
		// Arrange: Create a codex with grouped repos
		var config = CreateCodexConfiguration(
			sitePrefix: "/docs",
			docSets: [
				new CodexDocumentationSetReference { Name = "apm-agent", Branch = "main", Category = "observability", DisplayName = "APM Agent" },
				new CodexDocumentationSetReference { Name = "uptime", Branch = "main", Category = "observability", DisplayName = "Uptime" },
				new CodexDocumentationSetReference { Name = "logs", Branch = "main", Category = "observability", DisplayName = "Logs" }
			]);

		var docSetNavigations = CreateMockDocSetNavigations(["apm-agent", "uptime", "logs"]);
		var codexNav = new CodexNavigation(config, CreateContext(), docSetNavigations);

		// Get the group navigation
		var groupNav = codexNav.GroupNavigations.First();

		// Assert: Group navigation's top-level items should contain all 3 repos
		groupNav.NavigationItems.Should().HaveCount(3);
		groupNav.NavigationItems.Select(i => i.Url).Should().BeEquivalentTo([
			"/docs/r/apm-agent",
			"/docs/r/uptime",
			"/docs/r/logs"
		]);
	}

	[Fact]
	public void GroupNavigation_TopLevelItems_UseDisplayNames()
	{
		// Arrange
		var config = CreateCodexConfiguration(
			sitePrefix: "/docs",
			docSets: [
				new CodexDocumentationSetReference { Name = "apm-agent", Branch = "main", Category = "observability", DisplayName = "APM Agent Docs" },
				new CodexDocumentationSetReference { Name = "uptime", Branch = "main", Category = "observability", DisplayName = "Uptime Monitoring" }
			]);

		var docSetNavigations = CreateMockDocSetNavigations(["apm-agent", "uptime"]);
		var codexNav = new CodexNavigation(config, CreateContext(), docSetNavigations);

		var groupNav = codexNav.GroupNavigations.First();

		// Assert: Navigation titles should use the display names from config
		groupNav.NavigationItems.Select(i => i.NavigationTitle).Should().BeEquivalentTo([
			"APM Agent Docs",
			"Uptime Monitoring"
		]);
	}

	[Fact]
	public void UngroupedRepo_NavigationRoot_IsItself()
	{
		// Arrange
		var config = CreateCodexConfiguration(
			sitePrefix: "/docs",
			docSets: [
				new CodexDocumentationSetReference { Name = "standalone", Branch = "main" }
			]);

		var docSetNavigations = CreateMockDocSetNavigations(["standalone"]);
		var codexNav = new CodexNavigation(config, CreateContext(), docSetNavigations);

		// Get the standalone repo's navigation
		var standaloneNav = docSetNavigations["standalone"] as INavigationHomeAccessor;
		standaloneNav.Should().NotBeNull();

		// Assert: The HomeProvider's NavigationRoot should be the repo itself
		standaloneNav!.HomeProvider.Should().NotBeNull();

		// The navigation root for an ungrouped repo should be itself (DocumentationSetNavigation)
		// This means when rendering, it will show only its own navigation tree
		var navRoot = standaloneNav.HomeProvider!.NavigationRoot;
		navRoot.Should().BeAssignableTo<IDocumentationSetNavigation>();
		navRoot.Url.Should().Be("/docs/r/standalone");
	}

	[Fact]
	public void GroupedRepo_NavigationRoot_IsGroupNavigation()
	{
		// Arrange
		var config = CreateCodexConfiguration(
			sitePrefix: "/docs",
			docSets: [
				new CodexDocumentationSetReference { Name = "apm", Branch = "main", Category = "observability" },
				new CodexDocumentationSetReference { Name = "uptime", Branch = "main", Category = "observability" }
			]);

		var docSetNavigations = CreateMockDocSetNavigations(["apm", "uptime"]);
		var codexNav = new CodexNavigation(config, CreateContext(), docSetNavigations);

		// Get the grouped repo's navigation
		var apmNav = docSetNavigations["apm"] as INavigationHomeAccessor;
		apmNav.Should().NotBeNull();

		// Assert: The HomeProvider's NavigationRoot should be the GroupNavigation
		apmNav!.HomeProvider.Should().NotBeNull();

		var navRoot = apmNav.HomeProvider!.NavigationRoot;
		navRoot.Should().BeOfType<GroupNavigation>();
		navRoot.Url.Should().Be("/docs/g/observability");

		// The group navigation should contain both repos
		var groupNav = (GroupNavigation)navRoot;
		groupNav.NavigationItems.Should().HaveCount(2);
	}

	[Fact]
	public void CodexNavigation_TopLevelItems_ShowsGroupLinksAndUngroupedRepos()
	{
		// Arrange: Mix of grouped and ungrouped repos
		var config = CreateCodexConfiguration(
			sitePrefix: "/docs",
			docSets: [
				new CodexDocumentationSetReference { Name = "apm", Branch = "main", Category = "observability" },
				new CodexDocumentationSetReference { Name = "uptime", Branch = "main", Category = "observability" },
				new CodexDocumentationSetReference { Name = "standalone1", Branch = "main" },
				new CodexDocumentationSetReference { Name = "standalone2", Branch = "main" }
			]);

		var docSetNavigations = CreateMockDocSetNavigations(["apm", "uptime", "standalone1", "standalone2"]);
		var codexNav = new CodexNavigation(config, CreateContext(), docSetNavigations);

		// Assert: Codex nav should have 3 top-level items:
		// 1 group link (/g/observability) + 2 ungrouped repos
		codexNav.NavigationItems.Should().HaveCount(3);

		// One should be a group link
		var groupLinks = codexNav.NavigationItems.OfType<GroupLinkLeaf>().ToList();
		groupLinks.Should().HaveCount(1);
		groupLinks[0].Url.Should().Be("/docs/g/observability");

		// Two should be doc set navigations (ungrouped repos)
		var docSetNavs = codexNav.NavigationItems.OfType<IRootNavigationItem<IDocumentationFile, INavigationItem>>().ToList();
		docSetNavs.Should().HaveCount(2);
		docSetNavs.Select(n => n.Url).Should().BeEquivalentTo(["/docs/r/standalone1", "/docs/r/standalone2"]);
	}

	[Fact]
	public void AllGroupMembers_ShareSameNavigationRoot()
	{
		// Arrange
		var config = CreateCodexConfiguration(
			sitePrefix: "/docs",
			docSets: [
				new CodexDocumentationSetReference { Name = "repo1", Branch = "main", Category = "group1" },
				new CodexDocumentationSetReference { Name = "repo2", Branch = "main", Category = "group1" },
				new CodexDocumentationSetReference { Name = "repo3", Branch = "main", Category = "group1" }
			]);

		var docSetNavigations = CreateMockDocSetNavigations(["repo1", "repo2", "repo3"]);
		var codexNav = new CodexNavigation(config, CreateContext(), docSetNavigations);

		// Get each repo's navigation root
		var repo1Root = ((INavigationHomeAccessor)docSetNavigations["repo1"]).HomeProvider?.NavigationRoot;
		var repo2Root = ((INavigationHomeAccessor)docSetNavigations["repo2"]).HomeProvider?.NavigationRoot;
		var repo3Root = ((INavigationHomeAccessor)docSetNavigations["repo3"]).HomeProvider?.NavigationRoot;

		// Assert: All 3 should share the same GroupNavigation instance
		repo1Root.Should().BeSameAs(repo2Root);
		repo2Root.Should().BeSameAs(repo3Root);
		repo1Root.Should().BeOfType<GroupNavigation>();
	}

	[Fact]
	public void DifferentGroups_HaveDifferentNavigationRoots()
	{
		// Arrange
		var config = CreateCodexConfiguration(
			sitePrefix: "/docs",
			docSets: [
				new CodexDocumentationSetReference { Name = "obs-repo", Branch = "main", Category = "observability" },
				new CodexDocumentationSetReference { Name = "sec-repo", Branch = "main", Category = "security" }
			]);

		var docSetNavigations = CreateMockDocSetNavigations(["obs-repo", "sec-repo"]);
		var codexNav = new CodexNavigation(config, CreateContext(), docSetNavigations);

		// Get each repo's navigation root
		var obsRoot = ((INavigationHomeAccessor)docSetNavigations["obs-repo"]).HomeProvider?.NavigationRoot;
		var secRoot = ((INavigationHomeAccessor)docSetNavigations["sec-repo"]).HomeProvider?.NavigationRoot;

		// Assert: Different groups should have different navigation roots
		obsRoot.Should().NotBeSameAs(secRoot);
		((GroupNavigation)obsRoot!).GroupSlug.Should().Be("observability");
		((GroupNavigation)secRoot!).GroupSlug.Should().Be("security");
	}

	[Fact]
	public void GroupLandingPage_HasAllMembersAsNavigationItems()
	{
		// Arrange
		var config = CreateCodexConfiguration(
			sitePrefix: "",
			docSets: [
				new CodexDocumentationSetReference { Name = "a", Branch = "main", Category = "tools" },
				new CodexDocumentationSetReference { Name = "b", Branch = "main", Category = "tools" },
				new CodexDocumentationSetReference { Name = "c", Branch = "main", Category = "tools" }
			]);

		var docSetNavigations = CreateMockDocSetNavigations(["a", "b", "c"]);
		var codexNav = new CodexNavigation(config, CreateContext(), docSetNavigations);

		var groupNav = codexNav.GroupNavigations.First();

		// Assert: Group navigation should have all members
		groupNav.NavigationItems.Should().HaveCount(3);

		// And the index page should be the group landing
		groupNav.Index.Url.Should().Be("/g/tools");
		groupNav.Index.NavigationTitle.Should().Be("Tools");
	}
}
