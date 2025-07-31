// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.ObjectModel;
using System.IO.Abstractions;
using Aspire.Hosting.Testing;
using Documentation.Assembler;
using Documentation.Assembler.Building;
using Documentation.Assembler.Legacy;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Documentation.Builder.Http;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.LegacyDocs;
using Elastic.Documentation.Tooling;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Xunit;

[assembly: CaptureConsole, AssemblyFixture(typeof(Elastic.Assembler.IntegrationTests.AssembleFixture))]

namespace Elastic.Assembler.IntegrationTests;


public class AssembleFixture : IAsyncLifetime
{
	private static IFileSystem FileSystem { get; } = new FileSystem();
	public ConfigurationFileProvider ConfigurationFileProvider { get; } = new(FileSystem);
	public StaticWebHost WebsiteHost { get; private set; } = null!;
	public Task WebsiteRunning { get; private set; } = null!;

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		await WebsiteHost.StopAsync(TestContext.Current.CancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask InitializeAsync()
	{
		//var builder = await DistributedApplicationTestingBuilder.CreateAsync([]);
		var ctx = TestContext.Current.CancellationToken;
		var logFactory = LoggerFactory.Create(builder =>
		{
			_ = builder.AddConsole();
		});
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, null).StartAsync(ctx);

		var assemblyConfiguration = AssemblyConfiguration.Create(ConfigurationFileProvider);
		var versionsConfig = ConfigurationFileProvider.CreateVersionConfiguration();

		var assembleContext = new AssembleContext(assemblyConfiguration, ConfigurationFileProvider, "dev", collector, FileSystem, FileSystem, null, null);
		var cloner = new AssemblerRepositorySourcer(logFactory, assembleContext);

		_ = await cloner.CloneAll(false, ctx);

		_ = GlobalNavigationFile.ValidatePathPrefixes(assembleContext);

		var checkoutResult = cloner.GetAll();
		var checkouts = checkoutResult.Checkouts.ToArray();

		if (checkouts.Length == 0)
			throw new Exception("No checkouts found");

		var assembleSources = await AssembleSources.AssembleAsync(logFactory, assembleContext, checkouts, versionsConfig, ctx);
		var navigationFile = new GlobalNavigationFile(assembleContext, assembleSources);

		var navigation = new GlobalNavigation(assembleSources, navigationFile);

		var pathProvider = new GlobalNavigationPathProvider(navigationFile, assembleSources, assembleContext);
		var htmlWriter = new GlobalNavigationHtmlWriter(logFactory, navigation, collector);
		var legacyPageChecker = new LegacyPageChecker();
		var historyMapper = new PageLegacyUrlMapper(legacyPageChecker, assembleSources.HistoryMappings);

		var exporters = new HashSet<ExportOption>([ExportOption.Html, ExportOption.Configuration]);
		var builder = new AssemblerBuilder(logFactory, assembleContext, navigation, htmlWriter, pathProvider, historyMapper);
		await builder.BuildAllAsync(assembleSources.AssembleSets, exporters, ctx);

		await cloner.WriteLinkRegistrySnapshot(checkoutResult.LinkRegistrySnapshot, ctx);

		var redirectsPath = Path.Combine(assembleContext.OutputDirectory.FullName, "redirects.json");

		var sitemapBuilder = new SitemapBuilder(navigation.NavigationItems, assembleContext.WriteFileSystem, assembleContext.OutputDirectory);
		sitemapBuilder.Generate();

		await collector.StopAsync(ctx);

		WebsiteHost = new StaticWebHost(4001);
		WebsiteRunning = WebsiteHost.RunAsync(ctx);
		_ = await WebsiteHost.WaitForAppStartup(ctx);
	}

}

public class DatabaseTestClass1(AssembleFixture fixture)
{
	[Fact]
	public async Task X()
	{
		using var client = new HttpClient { BaseAddress = new Uri("http://localhost:4001") };
		var root = await client.GetStringAsync("/", TestContext.Current.CancellationToken);
		Console.WriteLine(root);

		fixture.Should().NotBeNull();
	}


	// ...
}
