// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Floating;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Testing;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Build.Tests;

/// <summary>Collects what the build reports, so a test can snapshot it.</summary>
public sealed class CapturingDiagnosticsOutput : IDiagnosticsOutput
{
	private readonly List<Diagnostic> _diagnostics = [];

	public IReadOnlyCollection<Diagnostic> Diagnostics
	{
		get
		{
			lock (_diagnostics)
				return [.. _diagnostics];
		}
	}

	public void Write(Diagnostic diagnostic)
	{
		lock (_diagnostics)
			_diagnostics.Add(diagnostic);
	}
}

/// <summary>What <see cref="InMemoryAssemblerSite.BuildAsync"/> produced.</summary>
public sealed record InMemoryAssemblerSiteResult(
	MockFileSystem FileSystem,
	string OutputRoot,
	SiteNavigation Navigation,
	IReadOnlyCollection<Diagnostic> Diagnostics,
	string[] Files
)
{
	/// <summary>The site's output folder, <c>docs/</c> below the output root as the <c>prod</c> environment's path prefix puts it.</summary>
	public string SiteRoot => $"{OutputRoot}/docs";

	public string Read(string relativePath) => FileSystem.File.ReadAllText($"{SiteRoot}/{relativePath}").Replace("\r\n", "\n");
}

/// <summary>
/// Builds an assembled site of a few fake repositories in memory, through the real <see cref="AssembleSources"/>,
/// <see cref="SiteNavigation"/>, <see cref="AssemblerBuilder"/>, sitemap and llms.txt steps of <see cref="AssemblerBuildService"/>. Only
/// the network is replaced: the cross links the link index would serve come from <see cref="FakeRepositories"/>.
/// </summary>
public static class InMemoryAssemblerSite
{
	public const string Environment = "prod";

	private static ConfigurationContext CreateConfigurationContext(MockFileSystem fileSystem, FakeSiteContent content)
	{
		foreach (var (name, yaml) in content.ConfigFiles)
			fileSystem.AddFile(Path.Join(ConfigurationFileProvider.AppDataConfigurationDirectory, name), new MockFileData(yaml));
		foreach (var name in new[]
		{
			"versions.yml",
			"products.yml",
			"legacy-url-mappings.yml",
			"search.yml",
			"related-learning.yml",
			"navigation_preview.yml"
		})
			fileSystem.AddFile(Path.Join(ConfigurationFileProvider.AppDataConfigurationDirectory, name), new MockFileData(string.Empty));
		fileSystem.AddFile(
			Path.Join(ConfigurationFileProvider.AppDataConfigurationDirectory, "related-learning.yml"),
			new MockFileData("links: {}\n")
		);
		fileSystem.AddFile(Path.Join(ConfigurationFileProvider.AppDataConfigurationDirectory, "git-ref.txt"), new MockFileData("fake"));
		var provider = new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(fileSystem),
			skipPrivateRepositories: false,
			ConfigurationSource.Remote
		);
		var versions = new VersioningSystemCatalog
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				[VersioningSystemId.Stack] = new()
				{
					Id = VersioningSystemId.Stack,
					Current = new SemVersion(9, 0, 0),
					Base = new SemVersion(9, 0, 0)
				},
				[VersioningSystemId.Serverless] = new()
				{
					Id = VersioningSystemId.Serverless,
					Current = new SemVersion(1, 0, 0),
					Base = new SemVersion(1, 0, 0)
				}
			}
		};
		var basis = TestHelpers.CreateConfigurationContext(fileSystem, versions);
		return new ConfigurationContext
		{
			Endpoints = basis.Endpoints,
			ConfigurationFileProvider = provider,
			Catalog = new GlobalCatalog(versions, basis.Catalog.Products, basis.Catalog.LegacyUrlMappings),
			SearchTuning = basis.SearchTuning
		};
	}

	public static async Task<InMemoryAssemblerSiteResult> BuildAsync(Cancel ctx, FakeSiteContent? content = null)
	{
		content ??= FakeRepositories.Default;
		var fileSystem = new MockFileSystem();
		content.WriteTo(fileSystem);

		var output = new CapturingDiagnosticsOutput();
		var collector = new DiagnosticsCollector([output]);
		_ = collector.StartAsync(ctx);

		var configurationContext = CreateConfigurationContext(fileSystem, content);
		var assemblyConfiguration = AssemblyLoader.Create(configurationContext.ConfigurationFileProvider);
		var checkoutRoot = fileSystem.DirectoryInfo.New("/checkouts");
		var outputRoot = fileSystem.DirectoryInfo.New("/output");
		var checkoutsFileSystem = new CheckoutsFileSystem(checkoutRoot, output: outputRoot, inner: fileSystem);
		var context = new AssembleContext(
			assemblyConfiguration,
			configurationContext,
			Environment,
			collector,
			checkoutsFileSystem,
			"/checkouts",
			"/output"
		);

		var checkouts = content
			.Repositories
			.Select(
				r => new Checkout
				{
					Repository = r == NarrativeRepository.RepositoryName
						? assemblyConfiguration.Narrative
						: assemblyConfiguration.ReferenceRepositories[r],
					HeadReference = "0000000000000000000000000000000000000000",
					Directory = fileSystem.DirectoryInfo.New($"/checkouts/{r}")
				}
			)
			.ToArray();
		var exporters = new HashSet<Exporter>
		{
			Exporter.Html,
			Exporter.LLMText,
			Exporter.Configuration,
			Exporter.LinkMetadata,
			Exporter.Redirects
		};

		var sources = await AssembleSources.AssembleOfflineAsync(
			NullLoggerFactory.Instance,
			context,
			checkouts,
			configurationContext,
			exporters,
			content.CrossLinks(),
			ctx
		);

		var navigationFileInfo = configurationContext.ConfigurationFileProvider.NavigationFile;
		var siteNavigationFile = SiteNavigationLoader.Deserialize(
			await fileSystem.File.ReadAllTextAsync(navigationFileInfo.FullName, ctx),
			collector,
			navigationFileInfo.FullName
		);
		var navigation = new SiteNavigation(
			siteNavigationFile,
			context,
			sources.AssembleSets.Values.Select(s => s.DocumentationSet.Navigation).ToArray(),
			context.Environment.PathPrefix,
			navigationPreviewEnabled: false,
			FloatingSectionRegistry.Empty
		);
		_ = SiteNavigationLoader.ValidatePathPrefixes(collector, siteNavigationFile, navigationFileInfo);

		var pathProvider = new GlobalNavigationPathProvider(navigation, sources, context);
		var htmlWriter = new GlobalNavigationHtmlWriter(NullLoggerFactory.Instance, navigation, collector);
		var builder = new AssemblerBuilder(
			NullLoggerFactory.Instance,
			context,
			navigation,
			htmlWriter,
			pathProvider,
			legacyUrlMapper: null
		);
		await builder.BuildAllAsync(sources.AssembleSets, exporters, ctx);

		// the same steps as the end of AssemblerBuildService.BuildAll
		var urls = navigation.NavigationItems.SelectMany(SitemapNavigationHelper.Flatten).Select(n => n.Url).Distinct();
		var now = DateTimeOffset.UtcNow;
		_ = SitemapBuilder.Generate(urls.ToDictionary(u => u, _ => now), context.WriteFileSystem, context.OutputWithPathPrefixDirectory);
		var llmsPath = $"{context.OutputWithPathPrefixDirectory.FullName}/llms.txt";
		if (fileSystem.File.Exists(llmsPath))
		{
			var navigationSections = new LlmsNavigationEnhancer().GenerateNavigationSections(navigation, new Uri(context.Environment.Uri));
			await fileSystem.File.WriteAllTextAsync(
				llmsPath,
				fileSystem.File.ReadAllText(llmsPath) + System.Environment.NewLine + navigationSections,
				Encoding.UTF8,
				ctx
			);
		}

		await collector.StopAsync(ctx);
		var files = fileSystem
			.Directory
			.EnumerateFiles("/output", "*", SearchOption.AllDirectories)
			.Select(f => fileSystem.Path.GetRelativePath("/output", f).Replace('\\', '/'))
			.Order(StringComparer.Ordinal)
			.ToArray();
		return new InMemoryAssemblerSiteResult(fileSystem, "/output", navigation, output.Diagnostics, files);
	}
}
