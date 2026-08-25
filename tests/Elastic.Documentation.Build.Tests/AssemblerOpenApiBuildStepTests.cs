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

public class AssemblerOpenApiBuildStepTests : IDisposable
{
	private readonly List<ScopedTempDirectory> _tempDirectories = [];

	private static readonly string MinimalAssemblerYaml = """
		environments:
		  prod:
		    uri: https://www.elastic.co
		    path_prefix: docs
		    content_source: current
		  staging:
		    uri: https://staging-website.elastic.co
		    path_prefix: docs
		    content_source: next
		    feature_flags:
		      ASSEMBLER_API_EXPLORER: true
		narrative:
		  checkout_strategy: full
		references: {}
		""";

	[Fact]
	public async Task BuildAsync_SkipsWhenFeatureFlagDisabled()
	{
		var fileSystem = new FileSystem();
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var assemblyConfig = AssemblyConfiguration.Deserialize(MinimalAssemblerYaml);
		var tempDirectory = CreateTempDirectory(fileSystem);
		var outputDirectory = fileSystem.Path.Join(tempDirectory.FullName, "output");
		var assembleFs = new CheckoutsFileSystem(tempDirectory, output: fileSystem.DirectoryInfo.New(outputDirectory), inner: fileSystem);
		var context = new AssembleContext(
			assemblyConfig,
			configurationContext,
			"prod",
			collector,
			assembleFs,
			tempDirectory.FullName,
			outputDirectory);
		var assembleSources = AssembleSources.ForTests(context, FrozenDictionary<string, AssemblerDocumentationSet>.Empty);

		var urls = await AssemblerOpenApiBuildStep.BuildAsync(
			NullLoggerFactory.Instance,
			context,
			assembleSources,
			TestContext.Current.CancellationToken);

		urls.Should().BeEmpty();
		fileSystem.Directory.Exists(fileSystem.Path.Join(outputDirectory, "docs", "api"))
			.Should().BeFalse("OpenAPI generation must not run when the feature flag is disabled");
	}

	[Fact]
	public async Task BuildAsync_SkipsWhenNoApiDeclarationsAndFlagEnabled()
	{
		var fileSystem = new FileSystem();
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var assemblyConfig = AssemblyConfiguration.Deserialize(MinimalAssemblerYaml);
		var tempDirectory = CreateTempDirectory(fileSystem);
		var outputDirectory = fileSystem.Path.Join(tempDirectory.FullName, "output");
		var assembleFs = new CheckoutsFileSystem(tempDirectory, output: fileSystem.DirectoryInfo.New(outputDirectory), inner: fileSystem);
		var context = new AssembleContext(
			assemblyConfig,
			configurationContext,
			"staging",
			collector,
			assembleFs,
			tempDirectory.FullName,
			outputDirectory);
		var assembleSources = AssembleSources.ForTests(context, FrozenDictionary<string, AssemblerDocumentationSet>.Empty);

		var urls = await AssemblerOpenApiBuildStep.BuildAsync(
			NullLoggerFactory.Instance,
			context,
			assembleSources,
			TestContext.Current.CancellationToken);

		urls.Should().BeEmpty();
		fileSystem.Directory.Exists(fileSystem.Path.Join(outputDirectory, "docs", "api"))
			.Should().BeFalse("OpenAPI generation must not run without API declarations");
	}

	[Fact]
	public void DiscoverExportSources_MapsApiKeysToExportSources()
	{
		var collector = new DiagnosticsCollector([]);
		var withApi = CreateDocumentationSet("docs-content", "elasticsearch", collector);
		var assembleSets = new Dictionary<string, AssemblerDocumentationSet>
		{
			[withApi.Checkout.Repository.Name] = withApi
		}.ToFrozenDictionary();

		var sources = AssemblerOpenApiBuildStep.DiscoverExportSources(assembleSets, collector);

		sources.Should().ContainSingle();
		sources[0].ApiKey.Should().Be("elasticsearch");
		sources[0].Git.Should().Be(withApi.BuildContext.Git);
	}

	[Fact]
	public void DiscoverApiOwners_EmitsErrorWhenDuplicateKeysDeclared()
	{
		var collector = new DiagnosticsCollector([]);
		var first = CreateDocumentationSet("docs-content", "elasticsearch", collector);
		var second = CreateDocumentationSet("docs-builder", "elasticsearch", collector);
		var assembleSets = new Dictionary<string, AssemblerDocumentationSet>
		{
			[first.Checkout.Repository.Name] = first,
			[second.Checkout.Repository.Name] = second
		}.ToFrozenDictionary();

		_ = AssemblerOpenApiBuildStep.DiscoverApiOwners(assembleSets, collector);

		collector.Errors.Should().Be(1);
	}

	[Fact]
	public void DiscoverApiOwners_ReturnsOwnersForSetsWithApiDeclarations()
	{
		var collector = new DiagnosticsCollector([]);
		var withApi = CreateDocumentationSet("docs-content", "elasticsearch", collector);
		var withoutApi = CreateDocumentationSet("kibana", apiKey: null, collector);
		var assembleSets = new Dictionary<string, AssemblerDocumentationSet>
		{
			[withApi.Checkout.Repository.Name] = withApi,
			[withoutApi.Checkout.Repository.Name] = withoutApi
		}.ToFrozenDictionary();

		var owners = AssemblerOpenApiBuildStep.DiscoverApiOwners(assembleSets, collector);

		owners.Should().ContainSingle().Which.Set.Checkout.Repository.Name.Should().Be("docs-content");
	}

	public void Dispose()
	{
		foreach (var directory in _tempDirectories)
			directory.Dispose();
		GC.SuppressFinalize(this);
	}


	private static void InitializeGitCheckout(IFileSystem fileSystem, string checkoutRoot)
	{
		var gitDir = fileSystem.Path.Join(checkoutRoot, ".git");
		fileSystem.Directory.CreateDirectory(gitDir);
		fileSystem.File.WriteAllText(fileSystem.Path.Join(gitDir, "HEAD"), "ref: refs/heads/main\n");
	}

	private IDirectoryInfo CreateTempDirectory(IFileSystem fileSystem)
	{
		var tempDirectory = new ScopedTempDirectory(fileSystem, "assembler-openapi-test");
		_tempDirectories.Add(tempDirectory);
		return tempDirectory.Directory;
	}

	private AssemblerDocumentationSet CreateDocumentationSet(
		string repositoryName,
		string? apiKey,
		DiagnosticsCollector collector)
	{
		var fileSystem = new FileSystem();
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var assemblyConfig = AssemblyConfiguration.Deserialize(MinimalAssemblerYaml);
		var scopedCheckoutRoot = new ScopedTempDirectory(fileSystem, "assembler-openapi-owner");
		_tempDirectories.Add(scopedCheckoutRoot);
		var checkoutRoot = scopedCheckoutRoot.Directory;
		var docsetPath = fileSystem.Path.Join(checkoutRoot.FullName, "docset.yml");
		var docsetYaml = apiKey is null
			? """
				project: test
				toc:
				  - file: index.md
				"""
			: $$"""
				project: test
				toc:
				  - file: index.md
				api:
				  {{apiKey}}:
				    - spec: elasticsearch-openapi.json
				      product: elasticsearch
				      repository: elastic/elasticsearch-specification
				""";
		fileSystem.File.WriteAllText(docsetPath, docsetYaml);
		fileSystem.File.WriteAllText(fileSystem.Path.Join(checkoutRoot.FullName, "index.md"), "# Test\n");
		InitializeGitCheckout(fileSystem, checkoutRoot.FullName);

		var outputDirectory = fileSystem.Path.Join(checkoutRoot.FullName, "output");
		var assembleFs = new CheckoutsFileSystem(checkoutRoot, output: fileSystem.DirectoryInfo.New(outputDirectory), inner: fileSystem);
		var context = new AssembleContext(
			assemblyConfig,
			configurationContext,
			"staging",
			collector,
			assembleFs,
			checkoutRoot.FullName,
			outputDirectory);
		var checkout = new Checkout
		{
			Repository = new Repository { Name = repositoryName, Origin = $"elastic/{repositoryName}" },
			HeadReference = "main",
			Directory = checkoutRoot
		};
		return new AssemblerDocumentationSet(
			NullLoggerFactory.Instance,
			context,
			checkout,
			NoopCrossLinkResolver.Instance,
			new ReleaseNotesResolver(),
			configurationContext,
			ExportOptions.Default);
	}
}
