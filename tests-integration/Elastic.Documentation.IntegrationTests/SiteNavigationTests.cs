// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging.Abstractions;
using TUnit.Core.Interfaces;

namespace Elastic.Documentation.IntegrationTests;

[ClassDataSource<DocumentationFixture>(Shared = SharedType.PerAssembly)]
public class SiteNavigationTests(DocumentationFixture fixture) : IAsyncInitializer, IAsyncDisposable
{
	private DiagnosticsCollector Collector { get; } = new DiagnosticsCollector([]);
	private AssembleContext Context { get; set; } = null!;
	private FileSystem FileSystem { get; } = new FileSystem();
	private IDirectoryInfo CheckoutDirectory { get; set; } = null!;

	private bool HasCheckouts() => CheckoutDirectory.Exists;

	public Task InitializeAsync()
	{
		var checkoutDirectory = FileSystem.DirectoryInfo.New(
			FileSystem.Path.Join(Paths.GetSolutionDirectory()!.FullName, ".artifacts", "checkouts")
		);
		CheckoutDirectory = checkoutDirectory.Exists
			? checkoutDirectory.GetDirectories().FirstOrDefault(d => d.Name is "next" or "current") ?? checkoutDirectory
			: checkoutDirectory;
		var configurationContext = TestHelpers.CreateConfigurationContext(FileSystem);
		var config = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		var assembleFs = CheckoutsFileSystem.FromWorkingDirectory(FileSystem);
		Context = new AssembleContext(config, configurationContext, "dev", Collector, assembleFs, CheckoutDirectory.FullName, null);
		return Task.CompletedTask;
	}

	private Checkout CreateCheckout(IFileSystem fs, Repository repository)
	{
		var name = repository.Name;
		var path = repository.Path is { } p
			? fs.DirectoryInfo.New(p)
			: fs.DirectoryInfo.New(fs.Path.Join(CheckoutDirectory.FullName, name));
		return new Checkout
		{
			Repository = new Repository { Name = name, Origin = $"elastic/{name}" },
			HeadReference = Guid.NewGuid().ToString(),
			Directory = path
		};
	}

	private async Task<(AssembleSources Sources, SiteNavigation Navigation)> Setup()
	{
		_ = Collector.StartAsync(TestContext.Current!.Execution.CancellationToken);

		var repos = Context.Configuration.AvailableRepositories.Where(kv => !kv.Value.Skip).Select(kv => kv.Value).ToArray();
		var checkouts = repos.Select(r => CreateCheckout(FileSystem, r)).ToArray();
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var assembleSources = await AssembleSources.AssembleAsync(
			NullLoggerFactory.Instance,
			Context,
			checkouts,
			configurationContext,
			ExportOptions.Default,
			TestContext.Current!.Execution.CancellationToken
		);

		var navigationFileInfo = configurationContext.ConfigurationFileProvider.NavigationFile;
		var siteNavigationFile = SiteNavigationFile.Deserialize(
			await FileSystem.File.ReadAllTextAsync(navigationFileInfo.FullName, TestContext.Current!.Execution.CancellationToken)
		);
		var documentationSets = assembleSources.AssembleSets.Values.Select(s => s.DocumentationSet.Navigation).ToArray();
		var navigation = new SiteNavigation(siteNavigationFile, Context, documentationSets, Context.Environment.PathPrefix);

		return (assembleSources, navigation);
	}

	[Test]
	public async Task ReadAllPathPrefixes()
	{
		Skip.Unless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		await using var collector = new DiagnosticsCollector([]);

		var fileSystem = new FileSystem();
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var config = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		var assembleFs2 = CheckoutsFileSystem.FromWorkingDirectory(fileSystem);
		var context = new AssembleContext(config, configurationContext, "dev", collector, assembleFs2);

		var navigationFileInfo = configurationContext.ConfigurationFileProvider.NavigationFile;
		var siteNavigationFile = SiteNavigationFile.Deserialize(
			await FileSystem.File.ReadAllTextAsync(navigationFileInfo.FullName, TestContext.Current!.Execution.CancellationToken)
		);

		var declaredSources = SiteNavigationFile.GetAllDeclaredSources(siteNavigationFile);
		declaredSources.Should().NotBeEmpty();
		declaredSources.Should().Contain(new Uri("eland://reference"));
	}

	[Test]
	public async Task SiteNavigationNodesContainAllDocumentationSets()
	{
		Skip.Unless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		var (assembleSources, navigation) = await Setup();

		navigation.Nodes.Should().NotBeEmpty();
		navigation.Nodes.Should().ContainKey(new Uri("detection-rules://"));
	}

	[Test]
	public async Task ParsesReferences()
	{
		Skip.Unless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		var expectedRoot = new Uri("docs-content://");
		var dotnetAgentSource = new Uri("apm-agent-dotnet://");
		var detectionRulesSource = new Uri("detection-rules://");
		var (assembleSources, navigation) = await Setup();

		// Verify that the navigation nodes contain the expected sources
		navigation.Nodes.Should().NotBeEmpty().And.ContainKey(dotnetAgentSource);
		navigation.Nodes.Should().ContainKey(expectedRoot);
		navigation.Nodes.Should().ContainKey(detectionRulesSource);

		// Verify the navigation structure is built correctly
		navigation.NavigationItems.Should().NotBeEmpty();

		// Find a specific navigation item in the tree (apm-agent-dotnet)
		var dotnetAgentNode = navigation.Nodes[dotnetAgentSource];
		dotnetAgentNode.Should().NotBeNull();
		dotnetAgentNode.Identifier.Should().Be(dotnetAgentSource);

		// Verify that the resolved navigation has the expected items
		navigation.NavigationItems.Should().NotBeNull();
	}

	[Test]
	public async Task ParsesSiteNavigation()
	{
		Skip.Unless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		var kibanaSource = new Uri("kibana://");
		var integrationsRepoName = "integrations";
		var docsContentRepoName = "docs-content";

		var (assembleSources, navigation) = await Setup();

		// Verify kibana is in the navigation nodes
		navigation.Nodes.Should().NotBeEmpty().And.ContainKey(kibanaSource);

		// Verify integrations and docs-content sources exist in assembleSources
		assembleSources.AssembleSets.Should().NotBeEmpty();
		assembleSources.AssembleSets.Should().ContainKey(integrationsRepoName);
		assembleSources.AssembleSets.Should().ContainKey(docsContentRepoName);

		// Verify the SiteNavigation is constructed properly
		navigation.NavigationItems.Should().NotBeNull().And.NotBeEmpty();
		navigation.TopLevelItems.Should().NotBeEmpty();
		navigation.TopLevelItems.Count.Should().BeLessThan(20);

		// Verify parent-child relationships
		var firstTopLevelItem = navigation.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().First();
		firstTopLevelItem.Should().NotBeNull();
		firstTopLevelItem.Parent.Should().Be(navigation);
		firstTopLevelItem.NavigationRoot.Should().Be(firstTopLevelItem);
	}

	[Test]
	public async Task UriResolving()
	{
		Skip.Unless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		await using var collector = new DiagnosticsCollector([]).StartAsync(TestContext.Current!.Execution.CancellationToken);

		var fs = new FileSystem();
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var config = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		var assembleFs3 = CheckoutsFileSystem.FromWorkingDirectory(fs);
		var assembleContext = new AssembleContext(config, configurationContext, "prod", collector, assembleFs3);
		var repos = assembleContext.Configuration.AvailableRepositories.Where(kv => !kv.Value.Skip).Select(kv => kv.Value).ToArray();
		var checkouts = repos.Select(r => CreateCheckout(fs, r)).ToArray();
		var assembleSources = await AssembleSources.AssembleAsync(
			NullLoggerFactory.Instance,
			assembleContext,
			checkouts,
			configurationContext,
			ExportOptions.Default,
			TestContext.Current!.Execution.CancellationToken
		);

		var uriResolver = assembleSources.UriResolver;

		// docs-content://reference/apm/something.md - url hasn't changed
		var resolvedUri = uriResolver.Resolve(new Uri("docs-content://reference/apm/something.md"), "reference/apm/something");
		resolvedUri.Should().Be("https://www.elastic.co/docs/reference/apm/something");

		resolvedUri = uriResolver.Resolve(new Uri("apm-agent-nodejs://reference/instrumentation.md"), "reference/instrumentation");
		resolvedUri.Should().Be("https://www.elastic.co/docs/reference/apm/agents/nodejs/instrumentation");

		resolvedUri = uriResolver.Resolve(new Uri("apm-agent-dotnet://reference/a/file.md"), "reference/a/file");
		resolvedUri.Should().Be("https://www.elastic.co/docs/reference/apm/agents/dotnet/a/file");

		resolvedUri = uriResolver.Resolve(new Uri("elasticsearch-net://reference/b/file.md"), "reference/b/file");
		resolvedUri.Should().Be("https://www.elastic.co/docs/reference/elasticsearch/clients/dotnet/b/file");

		resolvedUri = uriResolver.Resolve(new Uri("elasticsearch://extend/c/file.md"), "extend/c/file");
		resolvedUri.Should().Be("https://www.elastic.co/docs/extend/elasticsearch/c/file");
	}

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (TestContext.Current!.Execution.Result?.State is not TestState.Failed)
			return default;
		foreach (var resource in fixture.InMemoryLogger.RecordedLogs.ToList())
			TestContext.Current?.Output.WriteLine(resource.Message);
		return default;
	}
}
