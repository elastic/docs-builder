// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Codex.Navigation;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Navigation.Isolated.Node;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Codex;

public class CodexNavigationTests(ITestOutputHelper output) : CodexNavigationTestBase(output)
{
	[Fact]
	public void UngroupedRepos_UseStableRUrls()
	{
		CodexDocumentationSetReference[] docSets = [
			new CodexDocumentationSetReference { Name = "repo-a", Branch = "main" },
			new CodexDocumentationSetReference { Name = "repo-b", Branch = "main" }
		];
		var config = CreateCodexConfiguration("/docs");
		var docSetNavigations = CreateMockDocSetNavigations(["repo-a", "repo-b"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		codexNav.DocumentationSetInfos.Should().HaveCount(2);
		codexNav.DocumentationSetInfos.Select(d => d.Url).Should().BeEquivalentTo(["/docs/r/repo-a", "/docs/r/repo-b"]);
	}

	[Fact]
	public void GroupedRepos_UseStableRUrls_NotGroupUrls()
	{
		CodexDocumentationSetReference[] docSets = [
			new CodexDocumentationSetReference { Name = "apm-agent", Branch = "main", Group = "observability" },
			new CodexDocumentationSetReference { Name = "uptime", Branch = "main", Group = "observability" }
		];
		var config = CreateCodexConfiguration("/docs");
		var docSetNavigations = CreateMockDocSetNavigations(["apm-agent", "uptime"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		// Repos still use /r/ prefix even when in a category
		codexNav.DocumentationSetInfos.Select(d => d.Url).Should().BeEquivalentTo(["/docs/r/apm-agent", "/docs/r/uptime"]);

		// Group landing pages use /g/ prefix
		codexNav.GroupNavigations.Should().HaveCount(1);
		codexNav.GroupNavigations.First().Url.Should().Be("/docs/g/observability");
	}

	[Fact]
	public void GroupNavigation_ContainsAllGroupMembers()
	{
		CodexDocumentationSetReference[] docSets = [
			new CodexDocumentationSetReference { Name = "apm-agent", Branch = "main", Group = "observability" },
			new CodexDocumentationSetReference { Name = "uptime", Branch = "main", Group = "observability" },
			new CodexDocumentationSetReference { Name = "standalone", Branch = "main" }
		];
		var config = CreateCodexConfiguration("/docs");
		var docSetNavigations = CreateMockDocSetNavigations(["apm-agent", "uptime", "standalone"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		var observabilityGroup = codexNav.GroupNavigations.First();
		observabilityGroup.NavigationItems.Should().HaveCount(2);
		observabilityGroup.DocumentationSetInfos.Should().HaveCount(2);
		observabilityGroup.DocumentationSetInfos.Select(d => d.Name).Should().BeEquivalentTo(["apm-agent", "uptime"]);
	}

	[Fact]
	public void GroupedRepos_HaveGroupNavigationAsRoot()
	{
		CodexDocumentationSetReference[] docSets = [new CodexDocumentationSetReference { Name = "apm-agent", Branch = "main", Group = "observability" }];
		var config = CreateCodexConfiguration("/docs");
		var docSetNavigations = CreateMockDocSetNavigations(["apm-agent"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		var groupNav = codexNav.GroupNavigations.First();
		var docSetNavItem = groupNav.NavigationItems.First();

		// The doc set's parent should be the group navigation
		docSetNavItem.Parent.Should().BeSameAs(groupNav);

		// Verify the HomeProvider was set (check via the navigation interfaces)
		if (docSetNavigations["apm-agent"] is INavigationHomeAccessor homeAccessor)
		{
			homeAccessor.HomeProvider.Should().NotBeNull();
			homeAccessor.HomeProvider!.NavigationRoot.Should().BeSameAs(groupNav);
		}
	}

	[Fact]
	public void UngroupedRepos_HaveOwnNavigationAsRoot()
	{
		CodexDocumentationSetReference[] docSets = [new CodexDocumentationSetReference { Name = "standalone", Branch = "main" }];
		var config = CreateCodexConfiguration("/docs");
		var docSetNavigations = CreateMockDocSetNavigations(["standalone"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		// Ungrouped repos are NOT parented to codex (so breadcrumbs start at the docset root)
		var standaloneNavItem = codexNav.NavigationItems.First();
		standaloneNavItem.Parent.Should().BeNull();

		// Verify the HomeProvider points to itself (for isolated sidebar)
		if (docSetNavigations["standalone"] is INavigationHomeAccessor homeAccessor)
		{
			homeAccessor.HomeProvider.Should().NotBeNull();
			homeAccessor.HomeProvider!.NavigationRoot.Should().BeSameAs(standaloneNavItem);
		}
	}

	[Fact]
	public void DisplayName_OverridesNavigationTitle()
	{
		CodexDocumentationSetReference[] docSets = [
			new CodexDocumentationSetReference
			{
				Name = "apm-agent",
				Branch = "main",
				DisplayName = "APM Agent Documentation"
			}
		];
		var config = CreateCodexConfiguration("/docs");
		var docSetNavigations = CreateMockDocSetNavigations(["apm-agent"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		// The DocumentationSetInfo should use the display name
		codexNav.DocumentationSetInfos.First().Title.Should().Be("APM Agent Documentation");

		// The navigation title override should be set
		docSetNavigations["apm-agent"].NavigationTitleOverride.Should().Be("APM Agent Documentation");
	}

	[Fact]
	public void EmptySitePrefix_GeneratesRootUrls()
	{
		CodexDocumentationSetReference[] docSets = [
			new CodexDocumentationSetReference { Name = "repo-a", Branch = "main" },
			new CodexDocumentationSetReference { Name = "repo-b", Branch = "main", Group = "tools" }
		];
		var config = CreateCodexConfiguration("");
		var docSetNavigations = CreateMockDocSetNavigations(["repo-a", "repo-b"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		codexNav.Url.Should().BeEmpty();
		codexNav.DocumentationSetInfos.Select(d => d.Url).Should().Contain("/r/repo-a");
		codexNav.DocumentationSetInfos.Select(d => d.Url).Should().Contain("/r/repo-b");
		codexNav.GroupNavigations.First().Url.Should().Be("/g/tools");
	}

	[Fact]
	public void SlashSitePrefix_TreatedAsRoot()
	{
		// When loading from YAML, "/" gets normalized to empty by CodexConfiguration.Deserialize
		// language=yaml
		var yaml = """
		           title: "Test Codex"
		           site_prefix: /
		           """;
		var config = CodexConfiguration.Deserialize(yaml);
		CodexDocumentationSetReference[] docSets = [new CodexDocumentationSetReference { Name = "repo-a", Branch = "main" }];
		var docSetNavigations = CreateMockDocSetNavigations(["repo-a"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		// "/" should be normalized to empty string
		codexNav.Url.Should().BeEmpty();
		codexNav.DocumentationSetInfos.First().Url.Should().Be("/r/repo-a");
	}

	[Fact]
	public void MultipleCategories_CreateSeparateGroupNavigations()
	{
		CodexDocumentationSetReference[] docSets = [
			new CodexDocumentationSetReference { Name = "apm", Branch = "main", Group = "observability" },
			new CodexDocumentationSetReference { Name = "uptime", Branch = "main", Group = "observability" },
			new CodexDocumentationSetReference { Name = "siem", Branch = "main", Group = "security" },
			new CodexDocumentationSetReference { Name = "endpoint", Branch = "main", Group = "security" }
		];
		var config = CreateCodexConfiguration("/docs");
		var docSetNavigations = CreateMockDocSetNavigations(["apm", "uptime", "siem", "endpoint"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		codexNav.GroupNavigations.Should().HaveCount(2);

		var observability = codexNav.GroupNavigations.First(g => g.GroupSlug == "observability");
		observability.NavigationItems.Should().HaveCount(2);
		observability.DocumentationSetInfos.Select(d => d.Name).Should().BeEquivalentTo(["apm", "uptime"]);

		var security = codexNav.GroupNavigations.First(g => g.GroupSlug == "security");
		security.NavigationItems.Should().HaveCount(2);
		security.DocumentationSetInfos.Select(d => d.Name).Should().BeEquivalentTo(["siem", "endpoint"]);
	}

	[Fact]
	public void GroupTitle_FormatsSlugToTitleCase()
	{
		CodexDocumentationSetReference[] docSets = [new CodexDocumentationSetReference { Name = "tool", Branch = "main", Group = "developer-tools" }];
		var config = CreateCodexConfiguration("/docs");
		var docSetNavigations = CreateMockDocSetNavigations(["tool"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		var group = codexNav.GroupNavigations.First();
		group.DisplayTitle.Should().Be("Developer Tools");
	}

	[Fact]
	public void CodexNavigationItems_ContainGroupLinksAndUngroupedRepos()
	{
		CodexDocumentationSetReference[] docSets = [
			new CodexDocumentationSetReference { Name = "grouped1", Branch = "main", Group = "tools" },
			new CodexDocumentationSetReference { Name = "grouped2", Branch = "main", Group = "tools" },
			new CodexDocumentationSetReference { Name = "ungrouped", Branch = "main" }
		];
		var config = CreateCodexConfiguration("/docs");
		var docSetNavigations = CreateMockDocSetNavigations(["grouped1", "grouped2", "ungrouped"]);
		var codexNav = new CodexNavigation(config, docSets, CreateContext(), docSetNavigations);

		// Codex navigation items should contain:
		// 1. A GroupLinkLeaf pointing to /g/tools
		// 2. The ungrouped repo directly
		codexNav.NavigationItems.Should().HaveCount(2);

		var groupLink = codexNav.NavigationItems.OfType<GroupLinkLeaf>().Should().ContainSingle().Subject;
		groupLink.Url.Should().Be("/docs/g/tools");

		var ungroupedNav = codexNav.NavigationItems.OfType<IRootNavigationItem<IDocumentationFile, INavigationItem>>().Should().ContainSingle().Subject;
		ungroupedNav.Url.Should().Be("/docs/r/ungrouped");
	}
}
