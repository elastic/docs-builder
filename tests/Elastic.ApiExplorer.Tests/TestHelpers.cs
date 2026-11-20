// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Search;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public static class TestHelpers
{
	/// <summary>The tags of a spec without tag groups. The sidebar lists them under the Operations label.</summary>
	public static IEnumerable<TagNavigationItem> Tags(this LandingNavigationItem navigation) =>
		navigation
			.NavigationItems
			.OfType<OperationsNavigationItem>()
			.SelectMany(static operations => operations.NavigationItems)
			.OfType<TagNavigationItem>();

	public static IConfigurationContext CreateConfigurationContext(
		IFileSystem fileSystem,
		VersionsConfiguration? versionsConfiguration = null,
		ProductsConfiguration? productsConfiguration = null
	)
	{
		versionsConfiguration ??= CreateStackVersionsConfiguration(currentMajor: 9, currentMinor: 0);
		if (productsConfiguration is null)
		{
			var products = new Dictionary<string, Product>
			{
				{
					"elasticsearch",
					new Product
					{
						Id = "elasticsearch",
						DisplayName = "Elasticsearch",
						VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
					}
				}
			};
			productsConfiguration = new ProductsConfiguration
			{
				Products = products.ToFrozenDictionary(),
				PublicReferenceProducts = products.ToFrozenDictionary(),
				ProductDisplayNames = products.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
			};
		}
		var search = new SearchTuning { Synonyms = [], Rules = [], DiminishTerms = [] };
		return new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints { Elasticsearch = ElasticsearchEndpoint.Default, },
			ConfigurationFileProvider = new ConfigurationFileProvider(NullLoggerFactory.Instance, new ConfigurationFileSystem(fileSystem)),
			VersionsConfiguration = versionsConfiguration,
			ProductsConfiguration = productsConfiguration,
			LegacyUrlMappings = new LegacyUrlMappingConfiguration { Mappings = [] },
			SearchTuning = search
		};
	}

	/// <summary>A build context rooted at the working directory with the default test configuration.</summary>
	public static BuildContext CreateBuildContext(
		DiagnosticsCollector? collector = null,
		BuildType buildType = BuildType.Isolated,
		string? urlPathPrefix = null
	) =>
		new(
			collector ?? new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			CreateConfigurationContext(new FileSystem())
		)
		{ BuildType = buildType, UrlPathPrefix = urlPathPrefix };

	/// <summary>A build context like <see cref="CreateBuildContext"/>, with the git scope and the versions and products configuration under test.</summary>
	public static BuildContext CreateScopedBuildContext(
		DiagnosticsCollector collector,
		VersionsConfiguration? versionsConfiguration = null,
		ProductsConfiguration? productsConfiguration = null,
		GitCheckoutInformation? git = null
	) =>
		new(
			collector,
			DocumentationFileSystem.Resolve(
				new FileSystem().DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName),
				new DocumentationScopeOptions { Git = git }
			),
			CreateConfigurationContext(new FileSystem(), versionsConfiguration, productsConfiguration)
		);

	/// <summary>An exporter whose Stack current version matches <c>config/versions.yml</c> (9.5.6).</summary>
	public static OpenApiDocumentExporter CurrentStackExporter() =>
		new(CreateStackVersionsConfiguration(currentMajor: 9, currentMinor: 5, patch: 6));

	/// <summary>Routes of an Elasticsearch API with <c>main</c>, <c>9</c> and <c>8</c>: 9 at the versionless URL, main at <c>next</c>.</summary>
	public static ApiVersionRoutes EsRoutes { get; } = ApiVersionRoutes.For("elasticsearch", ["main", "9", "8"]);

	/// <summary>One Elasticsearch version tree of <paramref name="spec"/>, for the exporter, routed as <see cref="EsRoutes"/>.</summary>
	public static IndexedTree EsTree(string moniker, OpenApiDocument spec) => new(EsRoutes, "elasticsearch", moniker, spec);

	/// <summary>The only tree of an API, served at <c>/api/{apiKey}</c>.</summary>
	public static IndexedTree Tree(string apiKey, string productId, string moniker, OpenApiDocument spec) =>
		new(ApiVersionRoutes.For(apiKey, [moniker]), productId, moniker, spec);

	public static VersionsConfiguration CreateStackVersionsConfiguration(int currentMajor, int currentMinor = 0, int patch = 0) =>
		new()
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack,
					new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Current = new SemVersion(currentMajor, currentMinor, patch),
						Base = new SemVersion(currentMajor, 0, 0)
					}
				}
			},
		};

	public static VersionsConfiguration CreateVersionlessConfiguration() =>
		new()
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Serverless,
					new VersioningSystem
					{
						Id = VersioningSystemId.Serverless,
						Current = new SemVersion(VersioningSystem.VersionlessSentinel, 0, 0),
						Base = new SemVersion(VersioningSystem.VersionlessSentinel, 0, 0)
					}
				}
			},
		};

	public static Product CreateProduct(string id, VersioningSystem versioningSystem, string? displayName = null) =>
		new() { Id = id, DisplayName = displayName ?? id, VersioningSystem = versioningSystem };

	/// <summary>Renders a group page the way the build does, with the group as the current navigation item.</summary>
	public static Task<string> RenderTagAsync(TagNavigationItem tag) => RenderIndexAsync(tag.Index, tag);

	/// <summary>Renders an index page (product landing or group page) to HTML.</summary>
	public static async Task<string> RenderIndexAsync(ILeafNavigationItem<IApiModel> index, INavigationItem current)
	{
		var collector = new DiagnosticsCollector([]);
		var context = CreateBuildContext(collector);
		var renderContext = new ApiRenderContext(
			context,
			new OpenApiDocument(),
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context))
		)
		{ NavigationHtml = string.Empty, CurrentNavigation = current, MarkdownRenderer = PassthroughMarkdownRenderer.Instance };

		var fs = new MockFileSystem();
		await using (var stream = fs.FileStream.New("/out.html", FileMode.Create, FileAccess.Write))
			await index.Model.RenderAsync(stream, renderContext, TestContext.Current!.Execution.CancellationToken);

		return fs.File.ReadAllText("/out.html");
	}
}

/// <summary>
/// Keeps every diagnostic in memory and writes it synchronously, so a test can snapshot what a build reported. Page trees
/// render in parallel, so writes go to a concurrent queue.
/// </summary>
public sealed class RecordingDiagnosticsCollector() : DiagnosticsCollector([])
{
	private readonly ConcurrentQueue<Diagnostic> _diagnostics = new();

	public IReadOnlyList<Diagnostic> Diagnostics => [.. _diagnostics];

	public override void Write(Diagnostic diagnostic)
	{
		IncrementSeverityCount(diagnostic);
		_diagnostics.Enqueue(diagnostic);
	}

	public override DiagnosticsCollector StartAsync(Cancel ctx) => this;

	public override Task StopAsync(Cancel cancellationToken) => Task.CompletedTask;
}
