// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Net;
using System.Text;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links.CrossLinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Build.Tests;

/// <summary>
/// End-to-end check that the assembler OpenAPI step generates API pages into the assembled output.
/// The default test serves a small version index and spec from memory. The live variant fetches the real
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

	[Test]
	public async Task BuildAsync_GeneratesApiPagesWhenFlagEnabledAndDocsetPresent(CancellationToken cancellationToken)
	{
		using var versionIndexClient = new VersionIndexClient(
			new Uri("https://specs.example.test/"),
			new InMemorySpecHandler(),
			sleep: (_, _) => Task.CompletedTask
		);
		await BuildApiPagesAsync(versionIndexClient, cancellationToken);
	}

	[Test]
	[Explicit]
	public async Task BuildAsync_GeneratesApiPagesFromTheLiveVersionIndex(CancellationToken cancellationToken) =>
		await BuildApiPagesAsync(versionIndexClient: null, cancellationToken);

	private static async Task BuildApiPagesAsync(VersionIndexClient? versionIndexClient, CancellationToken cancellationToken)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;

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
		using var scopedWorkspace = new ScopedTempDirectory(fileSystem, "assembler-openapi-integration");
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

		await documentationSet.DocumentationSet.ResolveDirectoryTree(cancellationToken);

		var stopwatch = Stopwatch.StartNew();
		await AssemblerOpenApiBuildStep.BuildAsync(
			NullLoggerFactory.Instance,
			context,
			assembleSources,
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
