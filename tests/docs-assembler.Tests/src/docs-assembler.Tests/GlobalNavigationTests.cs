// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Navigation;
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
		var checkoutDirectory = FileSystem.DirectoryInfo.New(
			FileSystem.Path.Combine(Paths.GetSolutionDirectory()!.FullName, ".artifacts", "checkouts")
		);
		CheckoutDirectory = checkoutDirectory.Exists
			? checkoutDirectory.GetDirectories().FirstOrDefault(d => d.Name is "next" or "current") ?? checkoutDirectory
			: checkoutDirectory;
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
		Assert.SkipUnless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		await using var collector = new DiagnosticsCollector([]);

		var assembleContext = new AssembleContext("dev", collector, new FileSystem(), new FileSystem(), null, null);

		var pathPrefixes = GlobalNavigationFile.GetAllPathPrefixes(assembleContext);

		pathPrefixes.Should().NotBeEmpty();
		pathPrefixes.Should().Contain(new Uri("eland://reference/elasticsearch/clients/eland/"));
	}

	[Fact]
	public async Task PathProvider()
	{
		Assert.SkipUnless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		var assembleSources = await Setup();

		var navigationFile = new GlobalNavigationFile(Context, assembleSources);
		var pathProvider = new GlobalNavigationPathProvider(navigationFile, assembleSources, Context);

		assembleSources.TocTopLevelMappings.Should().NotBeEmpty().And.ContainKey(new Uri("detection-rules://"));
		pathProvider.TableOfContentsPrefixes.Should().Contain("detection-rules://");
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

		assembleSources.TocTopLevelMappings.Should().NotBeEmpty().And.ContainKey(new Uri("detection-rules://"));

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

		referenceNav.Should().NotBeNull();
		var navigationLookup = referenceNav.NavigationItems.OfType<TableOfContentsTree>().ToDictionary(i => i.Source, i => i);
		navigationLookup.Should().NotContainKey(clients);
		referenceNav.NavigationItems.OfType<TableOfContentsTree>()
			.Select(n => n.Source)
			.Should().NotContain(clients);
		referenceNav.NavigationItems.Should().HaveSameCount(navigationLookup);

		var ingestNav = navigationLookup[new Uri("docs-content://reference/ingestion-tools/")];
		ingestNav.Should().NotBeNull();
		var ingestLookup = ingestNav.NavigationItems.OfType<TableOfContentsTree>().ToDictionary(i => i.Source, i => i);
		ingestLookup.Should().NotContainKey(clients);
		ingestNav.NavigationItems.OfType<TableOfContentsTree>()
			.Select(n => n.Source)
			.Should().NotContain(clients);

		var apmNav = ingestLookup[new Uri("docs-content://reference/apm/")];
		apmNav.Should().NotBeNull();

		var apmLookup = apmNav.NavigationItems.OfType<TableOfContentsTree>().ToDictionary(i => i.Source, i => i);
		var apmAgentsNav = apmLookup[expectedParent];
		apmAgentsNav.Should().NotBeNull();

		var apmAgentLookup = apmAgentsNav.NavigationItems.OfType<TableOfContentsTree>().ToDictionary(i => i.Source, i => i);
		var dotnetAgentNav = apmAgentLookup[sut];
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


		IPositionalNavigation positionalNavigation = navigation;

		var releaseNotes = positionalNavigation.MarkdownNavigationLookup.Where(kv => kv.Key.Contains("release-notes")).ToArray();


		var addToHelm = positionalNavigation.MarkdownNavigationLookup.GetValueOrDefault("apm-k8s-attacher://reference/apm-webhook-add-helm-repo.md");
		addToHelm.Should().NotBeNull();
		var parentGroup = addToHelm!.Parent as DocumentationGroup;
		var parents = AssertHasParents(parentGroup, positionalNavigation, addToHelm);

		parents.Select(p => p.CrossLink).Should().ContainInOrder(
		[
			"apm-k8s-attacher://reference/apm-get-started-webhook.md",
			"apm-k8s-attacher://reference/index.md",
			"docs-content://reference/apm/observability/apm.md",
			"docs-content://reference/ingestion-tools/index.md",
			"docs-content://reference/index.md"
		]);

		var getStartedIntro = positionalNavigation.MarkdownNavigationLookup.GetValueOrDefault("docs-content://get-started/introduction.md");
		getStartedIntro.Should().NotBeNull();
		parentGroup = getStartedIntro!.Parent as DocumentationGroup;
		_ = AssertHasParents(parentGroup, positionalNavigation, getStartedIntro);

	}

	private static MarkdownFile[] AssertHasParents(
		DocumentationGroup? parent,
		IPositionalNavigation positionalNavigation,
		INavigationItem item
	)
	{
		parent.Should().NotBeNull();
		parent.Index.Should().NotBeNull();
		var parents2 = positionalNavigation.GetParents(item);
		var parents3 = positionalNavigation.GetParentMarkdownFiles(item);
		var markdown = (item as FileNavigationItem)?.Model!;
		var parents = positionalNavigation.GetParentMarkdownFiles(markdown);

		parents.Should().NotBeEmpty().And.HaveCount(parents2.Length).And.HaveCount(parents3.Length);
		return parents;
	}

	[Fact]
	public async Task UriResolving()
	{
		Assert.SkipUnless(HasCheckouts(), $"Requires local checkout folder: {CheckoutDirectory.FullName}");

		await using var collector = new DiagnosticsCollector([]).StartAsync(TestContext.Current.CancellationToken);

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
