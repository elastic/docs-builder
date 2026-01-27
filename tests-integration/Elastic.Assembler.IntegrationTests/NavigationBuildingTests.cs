// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AngleSharp;
using Documentation.Builder;
using Elastic.Documentation;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Documentation.ServiceDefaults;
using Elastic.Documentation.Site.Navigation;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RazorSlices;

namespace Elastic.Assembler.IntegrationTests;

public class NavigationBuildingTests(DocumentationFixture fixture, ITestOutputHelper output) : IAsyncLifetime
{
	[Fact(Skip = "Assert.SkipWhen not working on CI")]
	public async Task AssertRealNavigation()
	{
		//Skipping on CI since this relies on checking out private repositories
		Assert.SkipWhen(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI")), "Skipping in CI");
		string[] args = [];
		var builder = Host.CreateApplicationBuilder()
			.AddDocumentationServiceDefaults(ref args, (s, p) =>
			{
				_ = s.AddSingleton(AssemblyConfiguration.Create(p));
			})
			.AddDocumentationToolingDefaults();
		var host = builder.Build();

		var configurationContext = host.Services.GetRequiredService<IConfigurationContext>();

		var assemblyConfiguration = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		var collector = new TestDiagnosticsCollector(TestContext.Current.TestOutputHelper!);
		var fs = new FileSystem();
		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, "dev", collector, fs, new MockFileSystem(), null, null);
		var logFactory = new TestLoggerFactory(TestContext.Current.TestOutputHelper);
		var cloner = new AssemblerRepositorySourcer(logFactory, assembleContext);
		var checkoutResult = cloner.GetAll();
		var checkouts = checkoutResult.Checkouts.ToArray();
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		if (checkouts.Length == 0)
			throw new Exception("No checkouts found");

		var ctx = TestContext.Current.CancellationToken;
		var assembleSources = await AssembleSources.AssembleAsync(logFactory, assembleContext, checkouts, configurationContext, new HashSet<Exporter>(), ctx);

		var navigationFileInfo = configurationContext.ConfigurationFileProvider.NavigationFile;
		var siteNavigationFile = SiteNavigationFile.Deserialize(await fs.File.ReadAllTextAsync(navigationFileInfo.FullName, ctx));
		var documentationSets = assembleSources.AssembleSets.Values.Select(s => s.DocumentationSet.Navigation).ToArray();
		var navigation = new SiteNavigation(siteNavigationFile, assembleContext, documentationSets, assembleContext.Environment.PathPrefix);

		RecurseNav(navigation);

		/*
		foreach (var (source, toc) in navigation.Nodes)
		{
			var root = toc.NavigationRoot;
			root.Should().NotBeNull();
			if (root.Parent is null or not SiteNavigation)
			{
			}
			root.Parent.Should().BeOfType<SiteNavigation>();
		}*/

		var slice = _TocTree.Create(new NavigationViewModel
		{
			Title = "X",
			IsGlobalAssemblyBuild = true,
			IsPrimaryNavEnabled = true,
			Tree = navigation,
			TopLevelItems = navigation.TopLevelItems,
			TitleUrl = navigation.Index.Url,
			IsUsingNavigationDropdown = true
		});
		var html = await slice.RenderAsync(cancellationToken: ctx);
		var context = BrowsingContext.New();
		var document = await context.OpenAsync(req => req.Content(html), ctx);

		// Extract all URLs from the navigation model
		var navigationUrls = GetAllNavigationUrls(navigation).ToHashSet();
		output.WriteLine($"Navigation model URLs: {navigationUrls.Count}");

		// Extract all URLs from the rendered DOM
		var domLinks = document.QuerySelectorAll("a[href]");
		var domUrls = domLinks.Select(link => link.GetAttribute("href")!).ToHashSet();
		output.WriteLine($"DOM URLs: {domUrls.Count}");

		// Validate that all navigation URLs are present in the DOM
		navigationUrls.Should().BeSubsetOf(domUrls, "all navigation URLs should be rendered in the DOM");

		// Validate that all DOM URLs are from the navigation (no extra links)
		domUrls.Should().BeSubsetOf(navigationUrls, "DOM should not contain URLs not in the navigation model");

		// Validate exact match
		navigationUrls.Should().BeEquivalentTo(domUrls, "navigation URLs and DOM URLs should match exactly");

		// Validate we have a reasonable number of URLs (sanity check)
		navigationUrls.Should().NotBeEmpty("navigation should contain URLs");
		navigationUrls.Count.Should().BeGreaterThan(10, "navigation should have a substantial number of items");

		await collector.StopAsync(TestContext.Current.CancellationToken);


		collector.Errors.Should().Be(0);
	}


	private static void RecurseNav(INodeNavigationItem<INavigationModel, INavigationItem> navigation)
	{
		foreach (var nav in navigation.NavigationItems)
		{
			nav.NavigationRoot.Should().NotBeNull();
			if (navigation is not SiteNavigation && nav is not CrossLinkNavigationLeaf)
			{
				nav.NavigationRoot.Should().NotBeOfType<SiteNavigation>($"{nav.Url}");
				nav.NavigationRoot.Parent.Should().NotBeNull($"{nav.Url}");
				nav.NavigationRoot.Parent.Should().BeOfType<SiteNavigation>($"{nav.Url}");
			}

			if (nav is INodeNavigationItem<INavigationModel, INavigationItem> node)
				RecurseNav(node);
		}
	}

	/// <summary>
	/// Recursively extracts all URLs from the navigation tree, following the same logic as the Razor templates.
	/// Excludes hidden items and parent index items (to match _TocTreeNav.cshtml logic).
	/// </summary>
	private static IEnumerable<string> GetAllNavigationUrls(INavigationItem item)
	{
		// Skip hidden items (matches _TocTreeNav.cshtml line 9-12)
		if (item.Hidden)
			yield break;

		// Skip if this item is its parent's index (matches _TocTreeNav.cshtml line 14-16)
		if (item.Parent is not null && item.Parent.Index == item)
			yield break;

		// Yield the current item's URL
		yield return item.Url;

		// Recursively process children if this is a node
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			yield break;

		foreach (var child in node.NavigationItems)
			foreach (var url in GetAllNavigationUrls(child))
				yield return url;
	}


	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (TestContext.Current.TestState?.Result is TestResult.Passed)
			return default;
		foreach (var resource in fixture.InMemoryLogger.RecordedLogs)
			output.WriteLine(resource.Message);
		return default;
	}

	/// <inheritdoc />
	public ValueTask InitializeAsync() => default;
}
