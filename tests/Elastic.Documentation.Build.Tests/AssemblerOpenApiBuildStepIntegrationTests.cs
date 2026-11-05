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
		using var versionIndexClient = CreateInMemoryVersionIndexClient();
		await BuildApiPagesAsync(versionIndexClient, cancellationToken);
	}

	[Test]
	[Explicit]
	public async Task BuildAsync_GeneratesApiPagesFromTheLiveVersionIndex(CancellationToken cancellationToken) =>
		await BuildApiPagesAsync(versionIndexClient: null, cancellationToken);

	private static VersionIndexClient CreateInMemoryVersionIndexClient() =>
		new(new Uri("https://specs.example.test/"), new InMemorySpecHandler(), sleep: (_, _) => Task.CompletedTask);

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
	public async Task ApiSections_AfterTheStep_AreInTheNavigationTheSitemapAndTheLlmsListing(CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;

		using var versionIndexClient = CreateInMemoryVersionIndexClient();
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
			u => u.Contains("/api/doc/elasticsearch/operation/", StringComparison.Ordinal),
			"the sitemap walk reaches the pages of the API"
		);
		urls.Should().NotContain(u => u.Contains('#'), "a heading inside a page is not a page");
		urls.Should().NotContain(u => u.Contains("/api/doc/elasticsearch/v", StringComparison.Ordinal), "older versions are hidden");

		var listing = new LlmsNavigationEnhancer().GenerateNavigationSections(navigation, new Uri(workspace.Context.Environment.Uri));
		listing.Should().Contain("## APIs").And.Contain("api/doc/elasticsearch.md)", "the llms.txt listing comes from the navigation walk");
	}

	private sealed class InMemorySpecHandler : HttpMessageHandler
	{
		private const string Index = /*lang=json,strict*/
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
			var body = request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal) ? Index : Spec;
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json")
			});
		}
	}
}
