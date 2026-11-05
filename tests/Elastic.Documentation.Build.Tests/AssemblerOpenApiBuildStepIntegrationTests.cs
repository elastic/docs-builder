// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
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
/// End-to-end check that the assembler OpenAPI step can generate HTML from the live version index.
/// Requires network access to CloudFront.
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
	private static async Task<Workspace> CreateWorkspaceAsync(CancellationToken cancellationToken)
	{
		var fileSystem = new FileSystem();
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var assemblyConfig = AssemblyConfiguration.Deserialize(
			"""
			environments:
			  staging:
			    uri: https://staging-website.elastic.co
			    path_prefix: docs
			    content_source: next
			    feature_flags:
			      ASSEMBLER_API_EXPLORER: true
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
			cancellationToken
		);
		stopwatch.Stop();

		TestContext.Current?.Output.WriteLine($"OpenAPI assembler step completed in {stopwatch.ElapsedMilliseconds} ms");

		collector.Errors.Should().Be(0);

		var apiRoot = fileSystem.Path.Join(outputDirectory, "docs", "api");
		fileSystem.Directory.Exists(apiRoot).Should().BeTrue();

		var elasticsearchLanding = fileSystem.Path.Join(apiRoot, "doc", "elasticsearch", "index.html");
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
			.EnumerateDirectories(fileSystem.Path.Join(apiRoot, "doc", "elasticsearch"))
			.FirstOrDefault(path => fileSystem.Path.GetFileName(path).StartsWith('v'));
		versionedLanding.Should().NotBeNull("versioned products should emit at least one /vN/ tree under /docs/api/doc/elasticsearch/");
	}

	[Test]
	public async Task DocsetApiSection_ResolvedAfterTheGenerators_IsFilledAndKeepsItsVisibility(CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;

		using var workspace = await CreateWorkspaceAsync(cancellationToken);
		var apiSections = AssemblerApiSections.Create(workspace.Context, workspace.Sources);
		var navigation = new SiteNavigation(
			SiteNavigationFile.Deserialize(
				// language=yaml
				"""
				toc:
				  - section: APIs
				    floating: api
				"""
			),
			workspace.Context,
			[workspace.Set.DocumentationSet.Navigation],
			"/docs",
			navigationPreviewEnabled: false,
			new FloatingSectionRegistry([apiSections])
		);

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
		_ = apiSections.EnsureGenerators(NullLoggerFactory.Instance, workspace.Context, navigation.TopNav);
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

		using var workspace = await CreateWorkspaceAsync(cancellationToken);
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

		// the build creates the generators before the docsets resolve their trees
		_ = apiSections.EnsureGenerators(NullLoggerFactory.Instance, workspace.Context, navigation.TopNav);
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
			cancellationToken
		);

		workspace.Collector.Errors.Should().Be(0);
		var versions = root.NavigationItems.Should().AllBeOfType<LandingNavigationItem>().Subject.ToArray();
		versions.Should().HaveCountGreaterThan(1, "a versioned product mounts every released major next to main");
		versions.Where(v => !v.Hidden).Should().ContainSingle("only main is visible");

		var urls = navigation.NavigationItems.SelectMany(SitemapNavigationHelper.Flatten).Select(i => i.Url).Distinct().ToArray();
		urls.Should().Contain(
			u => u.Contains("/api/doc/elasticsearch/operation/", StringComparison.Ordinal),
			"the sitemap walk reaches the pages of the API"
		);
		urls.Should().NotContain(u => u.Contains('#'), "a heading inside a page is not a page");
		urls.Should().NotContain(u => u.Contains("/api/doc/elasticsearch/v", StringComparison.Ordinal), "older versions are hidden");

		var listing = new LlmsNavigationEnhancer().GenerateNavigationSections(navigation, new Uri(workspace.Context.Environment.Uri));
		listing.Should().Contain("## APIs").And.Contain("api/doc/elasticsearch.md)", "the llms.txt listing comes from the navigation walk");
	}
}
