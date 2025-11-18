// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AngleSharp;
using Documentation.Builder;
using Elastic.Documentation;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Documentation.ServiceDefaults;
using Elastic.Documentation.Site.Navigation;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RazorSlices;

namespace Elastic.Assembler.IntegrationTests;

public class NavigationRootTests(DocumentationFixture fixture, ITestOutputHelper output) : IAsyncLifetime
{
	[Fact]
	public async Task AssertRealNavigation()
	{
		//Skipping on CI since this relies on checking out private repositories
		Assert.SkipWhen(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI")), "Skipping in CI");
		string[] args = [];
		var builder = Host.CreateApplicationBuilder()
			.AddDocumentationServiceDefaults(ref args, (s, p) =>
			{
				_ = s.AddSingleton(AssemblyConfiguration.Create(p));
			})
			.AddDocumentationToolingDefaults();
		var host = builder.Build();

		var configurationContext = host.Services.GetRequiredService<IConfigurationContext>();

		var assemblyConfiguration = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		var collector = new TestDiagnosticsCollector(TestContext.Current.TestOutputHelper);
		var fs = new FileSystem();
		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, "dev", collector, fs, new MockFileSystem(), null, null);
		var logFactory = new TestLoggerFactory(TestContext.Current.TestOutputHelper);
		var cloner = new AssemblerRepositorySourcer(logFactory, assembleContext);
		var checkoutResult = cloner.GetAll();
		var checkouts = checkoutResult.Checkouts.ToArray();
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		if (checkouts.Length == 0)
			throw new Exception("No checkouts found");

		var ctx = TestContext.Current.CancellationToken;
		var assembleSources = await AssembleSources.AssembleAsync(logFactory, assembleContext, checkouts, configurationContext, new HashSet<Exporter>(), ctx);

		var navigationFileInfo = configurationContext.ConfigurationFileProvider.NavigationFile;
		var siteNavigationFile = SiteNavigationFile.Deserialize(await fs.File.ReadAllTextAsync(navigationFileInfo.FullName, ctx));
		var documentationSets = assembleSources.AssembleSets.Values.Select(s => s.DocumentationSet.Navigation).ToArray();
		var navigation = new SiteNavigation(siteNavigationFile, assembleContext, documentationSets, assembleContext.Environment.PathPrefix);

		var allowedRoots = navigation.TopLevelItems.Concat([navigation]).ToHashSet();
		foreach (var item in ((INavigationTraversable)navigation).YieldAll())
			item.NavigationRoot.Should().BeOneOf(allowedRoots, "Navigation for '{0}' has bad root '{1}'", item.Url, item.NavigationRoot.Identifier);

		foreach (var item in ((INavigationTraversable)navigation).NavigationIndexedByOrder.Values)
			item.NavigationRoot.Should().BeOneOf(allowedRoots, "Navigation for '{0}' has bad root '{1}' indexed by order {2}", item.Url, item.NavigationRoot.Identifier, item.NavigationIndex);

		collector.Errors.Should().Be(0);
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (TestContext.Current.TestState?.Result is TestResult.Passed)
			return default;
		foreach (var resource in fixture.InMemoryLogger.RecordedLogs)
			output.WriteLine(resource.Message);
		return default;
	}

	/// <inheritdoc />
	public ValueTask InitializeAsync() => default;
}
