// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using System.IO.Abstractions;
using AwesomeAssertions;
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

	[Fact]
	public async Task BuildAsync_GeneratesApiPagesWhenFlagEnabledAndDocsetPresent()
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;

		var fileSystem = new FileSystem();
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var assemblyConfig = AssemblyConfiguration.Deserialize("""
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
			""");
		using var scopedWorkspace = new ScopedTempDirectory(fileSystem, "assembler-openapi-integration");
		var workspaceRoot = scopedWorkspace.Directory;
		var docsetPath = fileSystem.Path.Join(workspaceRoot.FullName, "docset.yml");
		fileSystem.File.WriteAllText(docsetPath, """
			project: test
			toc:
			  - file: index.md
			api:
			  elasticsearch:
			    - spec: elasticsearch.json
			      product: elasticsearch
			      repository: elastic/elasticsearch-specification
			""");
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
			outputDirectory);
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
			ExportOptions.Default);
		var assembleSources = AssembleSources.ForTests(
			context,
			new Dictionary<string, AssemblerDocumentationSet> { [checkout.Repository.Name] = documentationSet }
				.ToFrozenDictionary());

		await documentationSet.DocumentationSet.ResolveDirectoryTree(TestContext.Current.CancellationToken);

		var stopwatch = Stopwatch.StartNew();
		await AssemblerOpenApiBuildStep.BuildAsync(
			NullLoggerFactory.Instance,
			context,
			assembleSources,
			TestContext.Current.CancellationToken);
		stopwatch.Stop();

		TestContext.Current.TestOutputHelper?.WriteLine(
			$"OpenAPI assembler step completed in {stopwatch.ElapsedMilliseconds} ms");

		collector.Errors.Should().Be(0);

		var apiRoot = fileSystem.Path.Join(outputDirectory, "docs", "api");
		fileSystem.Directory.Exists(apiRoot).Should().BeTrue();

		var elasticsearchLanding = fileSystem.Path.Join(apiRoot, "doc", "elasticsearch", "index.html");
		fileSystem.File.Exists(elasticsearchLanding).Should().BeTrue(
			"staging assembler builds should emit the unversioned elasticsearch API landing page");

		var versionedLanding = fileSystem.Directory
			.EnumerateDirectories(fileSystem.Path.Join(apiRoot, "doc", "elasticsearch"))
			.FirstOrDefault(path => fileSystem.Path.GetFileName(path).StartsWith('v'));
		versionedLanding.Should().NotBeNull(
			"versioned products should emit at least one /vN/ tree under /docs/api/doc/elasticsearch/");
	}
}
