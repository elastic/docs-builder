// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Net;
using System.Text;
using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Floating;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Build.Tests;

/// <summary>
/// End-to-end check that the assembler OpenAPI step generates API pages into the assembled output.
/// The default tests serve a small version index and spec from memory. The live variant fetches the real
/// index and the Elasticsearch spec from CloudFront and renders every version, so it only runs when asked for.
/// </summary>
public class AssemblerOpenApiBuildStepIntegrationTests
{
	private static void InitializeGitCheckout(IFileSystem fileSystem, string checkoutRoot)
	{
		var gitDir = fileSystem.Path.Join(checkoutRoot, ".git");
		fileSystem.Directory.CreateDirectory(gitDir);
		fileSystem.File.WriteAllText(fileSystem.Path.Join(gitDir, "HEAD"), "ref: refs/heads/main\n");
	}

	private sealed record Workspace(
		ScopedTempDirectory Scoped,
		IFileSystem FileSystem,
		DiagnosticsCollector Collector,
		AssembleContext Context,
		AssemblerDocumentationSet Set,
		AssembleSources Sources,
		string OutputDirectory
	) : IDisposable
	{
		public void Dispose() => Scoped.Dispose();
	}

	/// <summary>A narrative checkout whose docset declares the Elasticsearch API. The caller resolves the tree, as the build does.</summary>
	private static async Task<Workspace> CreateWorkspaceAsync(CancellationToken cancellationToken, bool apiExplorer = true)
	{
		var fileSystem = new FileSystem();
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var assemblyConfig = AssemblyConfiguration.Deserialize(
			$$"""
			environments:
			  staging:
			    uri: https://staging-website.elastic.co
			    path_prefix: docs
			    content_source: next
			    feature_flags:
			      ASSEMBLER_API_EXPLORER: {{(apiExplorer ? "true" : "false")}}
			narrative:
			  checkout_strategy: full
			references: {}
			"""
		);
		var scopedWorkspace = new ScopedTempDirectory(fileSystem, "assembler-openapi-integration");
		var workspaceRoot = scopedWorkspace.Directory;
		var docsetPath = fileSystem.Path.Join(workspaceRoot.FullName, "docset.yml");
		fileSystem.File.WriteAllText(
			docsetPath,
			"""
			project: test
			toc:
			  - file: index.md
			api:
			  elasticsearch:
			    - spec: elasticsearch.json
			      product: elasticsearch
			      repository: elastic/elasticsearch-specification
			"""
		);
		fileSystem.File.WriteAllText(fileSystem.Path.Join(workspaceRoot.FullName, "index.md"), "# Test\n");
		InitializeGitCheckout(fileSystem, workspaceRoot.FullName);
		var outputDirectory = fileSystem.Path.Join(workspaceRoot.FullName, "output");
		var assembleFs = new CheckoutsFileSystem(workspaceRoot, output: fileSystem.DirectoryInfo.New(outputDirectory), inner: fileSystem);
		var context = new AssembleContext(
			assemblyConfig,
			configurationContext,
			"staging",
			collector,
			assembleFs,
			workspaceRoot.FullName,
			outputDirectory
		);
		var checkout = new Checkout
		{
			Repository = new Repository { Name = "docs-content", Origin = "elastic/docs-content" },
			HeadReference = "main",
			Directory = workspaceRoot
		};
		var documentationSet = new AssemblerDocumentationSet(
			NullLoggerFactory.Instance,
			context,
			checkout,
			NoopCrossLinkResolver.Instance,
			new ReleaseNotesResolver(),
			configurationContext,
			ExportOptions.Default
		);
		var assembleSources = AssembleSources.ForTests(
			context,
			new Dictionary<string, AssemblerDocumentationSet> { [checkout.Repository.Name] = documentationSet }.ToFrozenDictionary()
		);

		return new Workspace(scopedWorkspace, fileSystem, collector, context, documentationSet, assembleSources, outputDirectory);
	}

	[Test]
	public async Task BuildAsync_GeneratesApiPagesWhenFlagEnabledAndDocsetPresent(CancellationToken cancellationToken)
	{
		using var versionIndexClient = CreateInMemoryVersionIndexClient();
		await BuildApiPagesAsync(versionIndexClient, cancellationToken);
	}

	[Test]
	[Explicit]
	public async Task BuildAsync_GeneratesApiPagesFromTheLiveVersionIndex(CancellationToken cancellationToken) =>
		await BuildApiPagesAsync(versionIndexClient: null, cancellationToken);

	/// <summary>A site navigation whose only section mounts every API, as <c>navigation.yml</c> does with <c>floating: api</c>.</summary>
	private static (AssemblerApiSections Sections, SiteNavigation Navigation) ApisSiteNavigation(Workspace workspace)
	{
		var apiSections = AssemblerApiSections.Create(workspace.Context, workspace.Sources);
		var siteNavFile = SiteNavigationFile.Deserialize(
			// language=yaml
			"""
			toc:
			  - section: APIs
			    floating: api
			"""
		);
		var navigation = new SiteNavigation(
			siteNavFile,
			workspace.Context,
			[workspace.Set.DocumentationSet.Navigation],
			"/docs",
			navigationPreviewEnabled: false,
			new FloatingSectionRegistry([apiSections])
		);
		return (apiSections, navigation);
	}

	private static VersionIndexClient CreateInMemoryVersionIndexClient(string index = InMemorySpecHandler.DefaultIndex) =>
		new(new Uri("https://specs.example.test/"), new InMemorySpecHandler(index), sleep: (_, _) => Task.CompletedTask);

	private static async Task BuildApiPagesAsync(VersionIndexClient? versionIndexClient, CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;

		using var workspace = await CreateWorkspaceAsync(cancellationToken);
		var (fileSystem, collector, context, assembleSources, outputDirectory) =
			(workspace.FileSystem, workspace.Collector, workspace.Context, workspace.Sources, workspace.OutputDirectory);
		await workspace.Set.DocumentationSet.ResolveDirectoryTree(cancellationToken);

		var stopwatch = Stopwatch.StartNew();
		await AssemblerOpenApiBuildStep.BuildAsync(
			NullLoggerFactory.Instance,
			context,
			AssemblerApiSections.Create(context, assembleSources),
			siteTopNav: null,
			cancellationToken,
			versionIndexClient
		);
		stopwatch.Stop();

		TestContext.Current?.Output.WriteLine($"OpenAPI assembler step completed in {stopwatch.ElapsedMilliseconds} ms");

		collector.Errors.Should().Be(0);

		var apiRoot = fileSystem.Path.Join(outputDirectory, "docs", "api");
		fileSystem.Directory.Exists(apiRoot).Should().BeTrue();

		var elasticsearchLanding = fileSystem.Path.Join(apiRoot, "elasticsearch", "index.html");
		fileSystem
			.File
			.Exists(elasticsearchLanding)
			.Should()
			.BeTrue("staging assembler builds should emit the unversioned elasticsearch API landing page");
		fileSystem
			.File
			.ReadAllText(elasticsearchLanding)
			.Should()
			.Contain("<meta name=\"robots\" content=\"noindex, nofollow\">", "API pages honor the environment's allow_indexing: false");

		var versionedLanding = fileSystem
			.Directory
			.EnumerateDirectories(fileSystem.Path.Join(apiRoot, "elasticsearch"))
			.FirstOrDefault(path => fileSystem.Path.GetFileName(path).StartsWith('v'));
		versionedLanding.Should().NotBeNull("versioned products should emit at least one /vN/ tree under /docs/api/elasticsearch/");
	}

	[Test]
	public async Task DocsetApiSection_ResolvedAfterTheGenerators_IsFilledAndKeepsItsVisibility(CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;

		using var versionIndexClient = CreateInMemoryVersionIndexClient();
		using var workspace = await CreateWorkspaceAsync(cancellationToken);
		var (apiSections, navigation) = ApisSiteNavigation(workspace);

		// AssembleAsync resolves the docset before any generator exists
		await workspace.Set.DocumentationSet.ResolveDirectoryTree(cancellationToken);
		var docsetRoot = workspace
			.Set
			.DocumentationSet
			.Navigation
			.NavigationItems
			.OfType<IFloatingSectionRoot>()
			.Should()
			.ContainSingle()
			.Subject;
		docsetRoot.NavigationItems.Should().BeEmpty("no generator existed when the docset resolved");
		docsetRoot.Hidden.Should().BeTrue();

		// the build then attaches the generators and resolves the floating sections again
		_ = apiSections.EnsureGenerators(NullLoggerFactory.Instance, workspace.Context, navigation.TopNav, versionIndexClient);
		await Task.WhenAll(
			workspace.Set.DocumentationSet.Navigation.FloatingSections.Select(section => section.ResolveAsync(cancellationToken))
		);

		docsetRoot.NavigationItems.Should().NotBeEmpty("the second resolution fills the docset's API section");
		docsetRoot.Hidden.Should().BeTrue("a docset's sidebar does not change because it declares an API");
		var links = workspace.Set.DocumentationSet.CreateLinkReference();
		links.Floating.Should().ContainKey("api/elasticsearch").WhoseValue.Pages.Should().ContainKey("");
	}

	[Test]
	public async Task ApiSections_AfterTheStep_AreInTheNavigationTheSitemapAndTheLlmsListing(CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;

		using var versionIndexClient = CreateInMemoryVersionIndexClient();
		using var workspace = await CreateWorkspaceAsync(cancellationToken);
		var (apiSections, navigation) = ApisSiteNavigation(workspace);

		// the build creates the generators before the docsets resolve their trees
		_ = apiSections.EnsureGenerators(NullLoggerFactory.Instance, workspace.Context, navigation.TopNav, versionIndexClient);
		await workspace.Set.DocumentationSet.ResolveDirectoryTree(cancellationToken);

		var docsetRoot = workspace
			.Set
			.DocumentationSet
			.Navigation
			.NavigationItems
			.OfType<IFloatingSectionRoot>()
			.Should()
			.ContainSingle()
			.Subject;
		docsetRoot.Hidden.Should().BeTrue("a docset's sidebar does not change because it declares an API");
		docsetRoot.NavigationItems.Should().NotBeEmpty("the docset resolved its API section while it resolved its tree");
		var links = workspace.Set.DocumentationSet.CreateLinkReference();
		var published = links.Floating.Should().ContainKey("api/elasticsearch").WhoseValue;
		published.Versions.Should().ContainSingle(v => v.Current).Which.Name.Should().Be("main");
		published.Pages.Should().ContainKey("", "an assembled docset publishes its APIs in links.json without a toc entry");

		var hub = navigation.TopLevelItems.OfType<SectionNavigation>().Should().ContainSingle(s => s.Title == "APIs").Subject;
		var root = hub.NavigationItems.Should().ContainSingle().Subject.Should().BeAssignableTo<IFloatingSectionRoot>().Subject;
		hub.Url.Should().Be("/docs/api");
		root.NavigationItems.Should().BeEmpty("the root exists before the specs are fetched");

		await AssemblerOpenApiBuildStep.BuildAsync(
			NullLoggerFactory.Instance,
			workspace.Context,
			apiSections,
			navigation.TopNav,
			cancellationToken,
			versionIndexClient
		);

		workspace.Collector.Errors.Should().Be(0);
		var versions = root.NavigationItems.Should().AllBeOfType<LandingNavigationItem>().Subject.ToArray();
		versions.Should().HaveCountGreaterThan(1, "a versioned product mounts every released major next to main");
		versions.Where(v => !v.Hidden).Should().ContainSingle("only main is visible");

		var urls = navigation.NavigationItems.SelectMany(SitemapNavigationHelper.Flatten).Select(i => i.Url).Distinct().ToArray();
		urls.Should().Contain(
			u => u.Contains("/api/elasticsearch/operation/", StringComparison.Ordinal),
			"the sitemap walk reaches the pages of the API"
		);
		urls.Should().NotContain(u => u.Contains('#'), "a heading inside a page is not a page");
		urls.Should().Contain("/docs/api/elasticsearch/v9.4", "an older version is hidden from navigation but listed in the sitemap");

		var listing = new LlmsNavigationEnhancer().GenerateNavigationSections(navigation, new Uri(workspace.Context.Environment.Uri));
		listing.Should().Contain("## APIs").And.Contain("api/elasticsearch.md)", "the llms.txt listing comes from the navigation walk");
	}

	[Test]
	public async Task ApiSitemap_ListsExactlyThePagesWhosePageTypeAllowsIt_EachNamingItselfCanonical(CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;

		using var workspace = await CreateWorkspaceAsync(cancellationToken);
		var urls = SitemapUrls(await BuildNumberedMajorsAsync(workspace, failingVersion: null, cancellationToken));
		// one row per page type: the landing page of each rendered tree
		var trees = new (string Url, string PageType, bool Listed)[]
		{
			("/docs/api/elasticsearch", "versionless newest major", true),
			("/docs/api/elasticsearch/v9", "numbered copy of the newest major", false),
			("/docs/api/elasticsearch/v8", "older major", true),
			("/docs/api/elasticsearch/next", "unreleased", false)
		};
		foreach (var (url, pageType, listed) in trees)
		{
			workspace
				.FileSystem
				.File
				.Exists(Path.Join(workspace.OutputDirectory, url, "index.html"))
				.Should()
				.BeTrue($"the {pageType} tree renders");
			urls.Contains(url).Should().Be(listed, $"the sitemap {(listed ? "lists" : "leaves out")} the {pageType} tree");
		}
		urls.Should().NotContain(u => u.Contains("/v9/", StringComparison.Ordinal) || u.Contains("/next/", StringComparison.Ordinal));
		urls.Should().Contain(
			u => u.StartsWith("/docs/api/elasticsearch/v8/", StringComparison.Ordinal),
			"an older major lists its pages, not just its landing"
		);
		urls.Should().Contain(
			["/docs/api/elasticsearch/servers", "/docs/api/elasticsearch/v8/servers"],
			"a page hidden from the sidebar still renders and names itself as canonical"
		);

		ShouldEachNameItselfCanonical(workspace, urls);
	}

	[Test]
	public async Task ApiSitemap_WhenTheNewestMajorFailsToLoad_StillListsTheOlderMajor(CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;

		using var workspace = await CreateWorkspaceAsync(cancellationToken);
		var navigation = await BuildNumberedMajorsAsync(workspace, failingVersion: "9.4", cancellationToken);
		var urls = SitemapUrls(navigation);

		urls.Should().NotContain("/docs/api/elasticsearch", "nothing renders at the versionless URL");
		urls.Should().Contain("/docs/api/elasticsearch/v8", "the older major rendered");
		urls.Should().Contain("/docs/api", "the API hub links to the older major");
		urls.Should().NotContain(u => u.Contains("/next", StringComparison.Ordinal));
		ShouldEachNameItselfCanonical(workspace, urls);

		var hub = navigation.TopLevelItems.OfType<SectionNavigation>().Should().ContainSingle(s => s.Title == "APIs").Subject;
		hub
			.NavigationItems
			.OfType<IFloatingSectionRoot>()
			.Should()
			.ContainSingle()
			.Which
			.Hidden
			.Should()
			.BeTrue("the API has no versionless landing to link to");
		new LlmsNavigationEnhancer()
			.GenerateNavigationSections(navigation, new Uri(workspace.Context.Environment.Uri))
			.Should()
			.NotContain("/api/elasticsearch.md", "llms.txt does not link to the missing versionless landing");
	}

	[Test]
	public async Task ApiSitemap_WithTheApiExplorerFlagOff_IsNotWritten(CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;
		Skip.When(
			Environment.GetEnvironmentVariable("FEATURE_ASSEMBLER_API_EXPLORER") is not null,
			"FEATURE_ASSEMBLER_API_EXPLORER overrides the flag of the environment"
		);

		// the docset still declares an API, and the build runs every API step
		using var workspace = await CreateWorkspaceAsync(cancellationToken, apiExplorer: false);
		var urls = SitemapUrls(await BuildNumberedMajorsAsync(workspace, failingVersion: null, cancellationToken));
		var output = SitemapOutput.For(workspace.Context);
		_ = SitemapBuilder.Generate(urls.ToDictionary(u => u, _ => DateTimeOffset.UnixEpoch), output, workspace.Collector);

		urls.Should().NotContain(u => output.IsApiUrl(u));
		workspace.FileSystem.File.Exists(output.PathOf("sitemap-api.xml")).Should().BeFalse();
		workspace.FileSystem.File.ReadAllText(output.PathOf("sitemap.xml")).Should().NotContain("sitemap-api.xml");
	}

	private static HashSet<string> SitemapUrls(SiteNavigation navigation) =>
		navigation.NavigationItems.SelectMany(SitemapNavigationHelper.Flatten).Select(i => i.Url).ToHashSet();

	/// <summary>Builds an API with <c>main</c>, <c>9</c> and <c>8</c> and returns the site navigation the sitemap walks.</summary>
	private static async Task<SiteNavigation> BuildNumberedMajorsAsync(Workspace workspace, string? failingVersion, Cancel ctx)
	{
		using var versionIndexClient = new VersionIndexClient(
			new Uri("https://specs.example.test/"),
			new InMemorySpecHandler(
				/*lang=json,strict*/
				"""
				{
					"elastic/elasticsearch-specification": {
						"elasticsearch.json": {
							"main": { "version": "10.0" },
							"9": { "version": "9.4" },
							"8": { "version": "8.19" }
						}
					}
				}
				""",
				failingVersion
			),
			sleep: (_, _) => Task.CompletedTask
		);
		var (apiSections, navigation) = ApisSiteNavigation(workspace);
		_ = apiSections.EnsureGenerators(NullLoggerFactory.Instance, workspace.Context, navigation.TopNav, versionIndexClient);
		await workspace.Set.DocumentationSet.ResolveDirectoryTree(ctx);
		await AssemblerOpenApiBuildStep.BuildAsync(
			NullLoggerFactory.Instance,
			workspace.Context,
			apiSections,
			navigation.TopNav,
			ctx,
			versionIndexClient
		);
		workspace.Collector.Errors.Should().Be(0);
		return navigation;
	}

	private static void ShouldEachNameItselfCanonical(Workspace workspace, IEnumerable<string> urls)
	{
		foreach (var url in urls.Where(SitemapOutput.For(workspace.Context).IsApiUrl))
		{
			var page = Path.Join(workspace.OutputDirectory, url, "index.html");
			workspace.FileSystem.File.Exists(page).Should().BeTrue($"{url} is listed, so it renders");
			workspace
				.FileSystem
				.File
				.ReadAllText(page)
				.Should()
				.Contain($"<link rel=\"canonical\" href=\"https://www.elastic.co{url}\"", $"{url} names itself as canonical");
		}
	}

	/// <summary>Serves the version index and one spec for every version, except <paramref name="failingVersion"/>, which is not found.</summary>
	private sealed class InMemorySpecHandler(string index, string? failingVersion = null) : HttpMessageHandler
	{
		public const string DefaultIndex = /*lang=json,strict*/
			"""
			{
				"elastic/elasticsearch-specification": {
					"elasticsearch.json": {
						"main": { "version": "9.5" },
						"9.4": { "version": "9.4" }
					}
				}
			}
			""";

		private const string Spec = /*lang=json,strict*/
			"""
			{
				"openapi": "3.0.3",
				"info": { "title": "Elasticsearch", "version": "1.0" },
				"servers": [{ "url": "https://localhost:9200" }],
				"tags": [{ "name": "search" }],
				"paths": {
					"/_search": {
						"get": {
							"operationId": "search",
							"summary": "Run a search",
							"tags": ["search"],
							"responses": { "200": { "description": "OK" } }
						}
					}
				}
			}
			""";

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var path = request.RequestUri!.AbsolutePath;
			if (failingVersion is not null && path.Contains($"/{failingVersion}/", StringComparison.Ordinal))
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
			var body = path.EndsWith("index.json", StringComparison.Ordinal) ? index : Spec;
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json")
			});
		}
	}
}
