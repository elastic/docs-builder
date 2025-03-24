// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Documentation.Assembler.Building;
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
	[Fact]
	public async Task ParsesGlobalNavigation()
	{
		var expectedRoot = new Uri("docs-content://extend");
		var kibanaExtendMoniker = new Uri("kibana://extend/");
		var fs = new FileSystem();

		await using var collector = new DiagnosticsCollector([]);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		var assembleContext = new AssembleContext("dev", collector, fs, fs, null, null);

		string[] nar = [NarrativeRepository.RepositoryName];
		var repos = nar.Concat(assembleContext.Configuration.ReferenceRepositories
				.Where(kv => !kv.Value.Skip)
				.Select(kv => kv.Value.Name)
			)
			.ToArray();
		var checkouts = repos.Select(r => CreateCheckout(fs, r)).ToArray();

		var assembleSources = await AssembleSources.AssembleAsync(assembleContext, checkouts, TestContext.Current.CancellationToken);
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

		var navigationFile = new GlobalNavigationFile(assembleContext, assembleSources);
		navigationFile.TableOfContents.Should().NotBeNull().And.NotBeEmpty();
		navigationFile.TableOfContents.Count.Should().BeLessThan(20);

		var navigation = new GlobalNavigation(assembleSources, navigationFile);
		navigation.TopLevelItems.Count.Should().BeLessThan(20);
		var resolved = navigation.NavigationItems;
	}

	public static Checkout CreateCheckout(IFileSystem fs, string name) =>
		new()
		{
			Repository = new Repository
			{
				Name = name,
				Origin = $"elastic/{name}"
			},
			HeadReference = Guid.NewGuid().ToString(),
			Directory = fs.DirectoryInfo.New(fs.Path.Combine(Paths.GetSolutionDirectory()!.FullName, ".artifacts", "checkouts", name))
		};

	[Fact]
	public async Task UriResolving()
	{
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
