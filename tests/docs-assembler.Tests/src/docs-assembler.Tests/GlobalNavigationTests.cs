// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Documentation.Assembler.Configuration;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;
using FluentAssertions;

namespace Documentation.Assembler.Tests;

public class GlobalNavigationPathProviderTests
{
	private DiagnosticsCollector Collector { get; }
	private AssembleContext Context { get; }
	private FileSystem FileSystem { get; }
	private IDirectoryInfo CheckoutDirectory { get; set; }

	private bool HasCheckouts() => CheckoutDirectory.Exists;

	public GlobalNavigationPathProviderTests()
	{
		FileSystem = new FileSystem();
		CheckoutDirectory = FileSystem.DirectoryInfo.New(
			FileSystem.Path.Combine(Paths.GetSolutionDirectory()!.FullName, ".artifacts", "checkouts")
		);
		Collector = new DiagnosticsCollector([]);
		Context = new AssembleContext("dev", Collector, FileSystem, FileSystem, CheckoutDirectory.FullName, null);
	}

	private Checkout CreateCheckout(IFileSystem fs, string name) =>
		new()
		{
			Repository = new Repository
			{
				Name = name,
				Origin = $"elastic/{name}"
			},
			HeadReference = Guid.NewGuid().ToString(),
			Directory = fs.DirectoryInfo.New(fs.Path.Combine(Path.Combine(CheckoutDirectory.FullName, name)))
		};

	private async Task<AssembleSources> Setup()
	{
		_ = Collector.StartAsync(TestContext.Current.CancellationToken);

		string[] nar = [NarrativeRepository.RepositoryName];
		var repos = nar.Concat(Context.Configuration.ReferenceRepositories
				.Where(kv => !kv.Value.Skip)
				.Select(kv => kv.Value.Name)
			)
			.ToArray();
		var checkouts = repos.Select(r => CreateCheckout(FileSystem, r)).ToArray();

		var assembleSources = await AssembleSources.AssembleAsync(Context, checkouts, TestContext.Current.CancellationToken);
		return assembleSources;
	}

	[Fact]
	public async Task ReadAllPathPrefixes()
	{
		await using var collector = new DiagnosticsCollector([]);

		var assembleContext = new AssembleContext("dev", collector, new FileSystem(), new FileSystem(), null, null);

		var pathPrefixes = GlobalNavigationFile.GetAllPathPrefixes(assembleContext);

		pathPrefixes.Should().NotBeEmpty();
		pathPrefixes.Should().Contain(new Uri("eland://reference/elasticsearch/clients/eland/"));
	}

	[Fact]
	public async Task ParsesReferences()
	{
		Assert.SkipUnless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		var expectedRoot = new Uri("docs-content://reference/");
		var expectedParent = new Uri("docs-content://reference/apm-agents/");
		var sut = new Uri("apm-agent-dotnet://reference/");
		var clients = new Uri("docs-content://reference/elasticsearch-clients/");
		var assembleSources = await Setup();

		assembleSources.TocTopLevelMappings.Should().NotBeEmpty().And.ContainKey(sut);
		assembleSources.TocTopLevelMappings[sut].TopLevelSource.Should().Be(expectedRoot);
		assembleSources.TocTopLevelMappings.Should().NotBeEmpty().And.ContainKey(expectedRoot);
		assembleSources.TocTopLevelMappings[sut].ParentSource.Should().Be(expectedParent);

		var navigationFile = new GlobalNavigationFile(Context, assembleSources);
		var referenceToc = navigationFile.TableOfContents.FirstOrDefault(t => t.Source == expectedRoot);
		referenceToc.Should().NotBeNull();
		referenceToc!.TocReferences.Should().NotContainKey(clients);

		var ingestTools = referenceToc.TocReferences[new Uri("docs-content://reference/ingestion-tools/")];
		ingestTools.Should().NotBeNull();

		var apmReference = ingestTools.TocReferences[new Uri("docs-content://reference/apm/")];
		apmReference.Should().NotBeNull();

		var agentsRef = apmReference.TocReferences[expectedParent];
		apmReference.Should().NotBeNull();

		var agentsRefTocReference = agentsRef.TocReferences[sut];
		agentsRefTocReference.Should().NotBeNull();

		var navigation = new GlobalNavigation(assembleSources, navigationFile);
		var referenceNav = navigation.NavigationLookup[expectedRoot];
		navigation.NavigationItems.Should().HaveSameCount(navigation.NavigationLookup);

		var referenceOrder = referenceNav.Group.NavigationItems.OfType<TocNavigationItem>()
			.Last().Source.Should().Be(new Uri("docs-content://reference/glossary/"));

		referenceNav.Should().NotBeNull();
		referenceNav.NavigationLookup.Should().NotContainKey(clients);
		referenceNav.Group.NavigationItems.OfType<TocNavigationItem>()
			.Select(n => n.Source)
			.Should().NotContain(clients);
		referenceNav.Group.NavigationItems.Should().HaveSameCount(referenceNav.NavigationLookup);

		var ingestNav = referenceNav.NavigationLookup[new Uri("docs-content://reference/ingestion-tools/")];
		ingestNav.Should().NotBeNull();
		ingestNav.NavigationLookup.Should().NotContainKey(clients);
		ingestNav.Group.NavigationItems.OfType<TocNavigationItem>()
			.Select(n => n.Source)
			.Should().NotContain(clients);

		var apmNav = ingestNav.NavigationLookup[new Uri("docs-content://reference/apm/")];
		apmNav.Should().NotBeNull();

		var apmAgentsNav = apmNav.NavigationLookup[expectedParent];
		apmAgentsNav.Should().NotBeNull();

		var dotnetAgentNav = apmAgentsNav.NavigationLookup[sut];
		dotnetAgentNav.Should().NotBeNull();

		var resolved = navigation.NavigationItems;
		resolved.Should().NotBeNull();

	}



	[Fact]
	public async Task ParsesGlobalNavigation()
	{
		Assert.SkipUnless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		var expectedRoot = new Uri("docs-content://extend");
		var kibanaExtendMoniker = new Uri("kibana://extend/");

		var assembleSources = await Setup();
		assembleSources.TocTopLevelMappings.Should().NotBeEmpty().And.ContainKey(kibanaExtendMoniker);
		assembleSources.TocTopLevelMappings[kibanaExtendMoniker].TopLevelSource.Should().Be(expectedRoot);
		assembleSources.TocTopLevelMappings.Should().NotBeEmpty().And.ContainKey(new Uri("docs-content://reference/apm/"));

		var uri = new Uri("integration-docs://reference/");
		assembleSources.TreeCollector.Should().NotBeNull();
		_ = assembleSources.TreeCollector.TryGetTableOfContentsTree(uri, out var tree);
		tree.Should().NotBeNull();

		_ = assembleSources.TreeCollector.TryGetTableOfContentsTree(new Uri("docs-content://reference/"), out tree);
		tree.Should().NotBeNull();

		assembleSources.AssembleSets.Should().NotBeEmpty();

		assembleSources.TocConfigurationMapping.Should().NotBeEmpty().And.ContainKey(kibanaExtendMoniker);
		var kibanaConfigMapping = assembleSources.TocConfigurationMapping[kibanaExtendMoniker];
		kibanaConfigMapping.Should().NotBeNull();
		kibanaConfigMapping.TableOfContentsConfiguration.Should().NotBeNull();
		assembleSources.TocConfigurationMapping[kibanaExtendMoniker].Should().NotBeNull();

		var navigationFile = new GlobalNavigationFile(Context, assembleSources);
		navigationFile.TableOfContents.Should().NotBeNull().And.NotBeEmpty();
		navigationFile.TableOfContents.Count.Should().BeLessThan(20);

		var navigation = new GlobalNavigation(assembleSources, navigationFile);
		navigation.TopLevelItems.Count.Should().BeLessThan(20);
		var resolved = navigation.NavigationItems;
		resolved.Should().NotBeNull();
	}

	[Fact]
	public async Task UriResolving()
	{
		Assert.SkipUnless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		await using var collector = new DiagnosticsCollector([]);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		var fs = new FileSystem();
		var assembleContext = new AssembleContext("prod", collector, fs, fs, null, null);
		var repos = assembleContext.Configuration.ReferenceRepositories
			.Where(kv => !kv.Value.Skip)
			.Select(kv => kv.Value.Name)
			.Concat([NarrativeRepository.RepositoryName])
			.ToArray();
		var checkouts = repos.Select(r => CreateCheckout(fs, r)).ToArray();

		var assembleSources = await AssembleSources.AssembleAsync(assembleContext, checkouts, TestContext.Current.CancellationToken);
		var globalNavigationFile = new GlobalNavigationFile(assembleContext, assembleSources);

		globalNavigationFile.TableOfContents.Should().NotBeNull().And.NotBeEmpty();

		var uriResolver = assembleSources.UriResolver;

		// docs-content://reference/apm/something.md - url hasn't changed
		var resolvedUri = uriResolver.Resolve(new Uri("docs-content://reference/apm/something.md"), "/reference/apm/something");
		resolvedUri.Should().Be("https://www.elastic.co/docs/reference/apm/something");

		resolvedUri = uriResolver.Resolve(new Uri("apm-agent-ios://reference/instrumentation.md"), "/reference/instrumentation");
		resolvedUri.Should().Be("https://www.elastic.co/docs/reference/apm/agents/ios/instrumentation");

		resolvedUri = uriResolver.Resolve(new Uri("apm-agent-android://reference/a/file.md"), "/reference/a/file");
		resolvedUri.Should().Be("https://www.elastic.co/docs/reference/apm/agents/android/a/file");

		resolvedUri = uriResolver.Resolve(new Uri("elasticsearch-net://reference/b/file.md"), "/reference/b/file");
		resolvedUri.Should().Be("https://www.elastic.co/docs/reference/elasticsearch/clients/dotnet/b/file");

		resolvedUri = uriResolver.Resolve(new Uri("elasticsearch://extend/c/file.md"), "/extend/c/file");
		resolvedUri.Should().Be("https://www.elastic.co/docs/extend/elasticsearch/c/file");
	}
}
