// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Reflection;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration;

public record BuildContext : IDocumentationSetContext, IDocumentationConfigurationContext
{
	public static string Version { get; } = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyInformationalVersionAttribute>()
		.FirstOrDefault()?.InformationalVersion ?? "0.0.0";

	/// <summary>The resolved documentation filesystem. All other path/scope properties are computed from this.</summary>
	public DocumentationFileSystem FileSystem { get; }

	/// <summary>
	/// Read scope. Satisfies <see cref="IDocumentationSetContext"/>.
	/// Use <see cref="FileSystem"/> directly when the richer type is needed.
	/// </summary>
	public IDocumentationFileSystem ReadFileSystem => FileSystem.Read;

	/// <summary>Write scope. Does not permit <c>.git</c> writes.</summary>
	public DocumentationWriteFileSystem WriteFileSystem => FileSystem.Write;

	public IReadOnlySet<Exporter> AvailableExporters { get; init; }

	public IDirectoryInfo DocumentationCheckoutDirectory => FileSystem.Paths.CheckoutDirectory;
	public IDirectoryInfo DocumentationSourceDirectory => FileSystem.Paths.SourceDirectory;
	public IDirectoryInfo OutputDirectory => FileSystem.Paths.OutputDirectory;
	public IFileInfo ConfigurationPath => FileSystem.Paths.ConfigurationPath;
	public GitCheckoutInformation Git => FileSystem.Paths.Git;

	public ConfigurationFile Configuration { get; private set; }
	public DocumentationSetFile ConfigurationYaml { get; set; }

	public VersionsConfiguration VersionsConfiguration { get; }
	public ConfigurationFileProvider ConfigurationFileProvider { get; }
	public DocumentationEndpoints Endpoints { get; }
	public ProductsConfiguration ProductsConfiguration { get; }
	public RelatedLearningConfiguration RelatedLearningConfiguration { get; }
	public LegacyUrlMappingConfiguration LegacyUrlMappings { get; }
	public SearchConfiguration SearchConfiguration { get; }
	public IEnvironmentVariables Environment { get; }
	public IDiagnosticsCollector Collector { get; }
	public bool Force { get; init; }
	public BuildType BuildType { get; init; } = BuildType.Isolated;

	/// <summary>
	/// The content source this build publishes (assembler builds only): <see cref="Assembler.ContentSource.Current"/>
	/// for production, <see cref="Assembler.ContentSource.Next"/> for staging. Null for isolated/local
	/// builds, which have no publish target and render everything.
	/// </summary>
	public ContentSource? ContentSource { get; init; }

	// This property is used to determine if the site should be indexed by search engines
	public bool AllowIndexing { get; init; }
	public GoogleTagManagerConfiguration GoogleTagManager { get; init; }
	public OptimizelyConfiguration Optimizely { get; init; }
	public Uri? CanonicalBaseUrl { get; init; }

	public string? UrlPathPrefix
	{
		get => string.IsNullOrWhiteSpace(field) ? "" : $"/{field.Trim('/')}";
		init;
	}

	/// <summary>Site root path for HTMX (e.g. codex root). When set, overrides derivation from UrlPathPrefix.</summary>
	public string? SiteRootPath { get; init; }

	/// <summary>
	/// Primary constructor. Pass a resolved <see cref="DocumentationFileSystem"/> from
	/// <see cref="DocumentationFileSystem.Resolve(IDirectoryInfo?, DocumentationScopeOptions?)"/>.
	/// </summary>
	public BuildContext(
		IDiagnosticsCollector collector,
		DocumentationFileSystem fileSystem,
		IConfigurationContext configurationContext,
		IEnvironmentVariables? environment = null
	)
	{
		Collector = collector;
		FileSystem = fileSystem;
		AvailableExporters = ExportOptions.Default;
		Environment = environment ?? SystemEnvironmentVariables.Instance;
		SearchConfiguration = configurationContext.SearchConfiguration;
		VersionsConfiguration = configurationContext.VersionsConfiguration;
		ConfigurationFileProvider = configurationContext.ConfigurationFileProvider;
		ProductsConfiguration = configurationContext.ProductsConfiguration;
		RelatedLearningConfiguration = configurationContext.ConfigurationFileProvider
			.CreateRelatedLearningConfiguration();
		LegacyUrlMappings = configurationContext.LegacyUrlMappings;
		Endpoints = configurationContext.Endpoints;

		GoogleTagManager = new GoogleTagManagerConfiguration { Enabled = false };
		Optimizely = new OptimizelyConfiguration { Enabled = false };


		ConfigurationYaml = ConfigurationPath.Exists
			? DocumentationSetFile.LoadAndResolve(collector, ConfigurationPath, fileSystem.Read)
			: new DocumentationSetFile();

		Configuration = new ConfigurationFile(ConfigurationYaml, this, VersionsConfiguration, ProductsConfiguration);
	}

	/// <summary>Re-reads docset.yml from disk and rebuilds the configuration. Used by the serve command on file changes.</summary>
	public void ReloadConfiguration()
	{
		var previousFeatures = Configuration.Features;
		ConfigurationYaml = ConfigurationPath.Exists
			? DocumentationSetFile.LoadAndResolve(Collector, ConfigurationPath, ReadFileSystem as ScopedFileSystem)
			: new DocumentationSetFile();
		Configuration = new ConfigurationFile(ConfigurationYaml, this, VersionsConfiguration, ProductsConfiguration);
		Configuration.Features.DiagnosticsPanelEnabled = previousFeatures.DiagnosticsPanelEnabled;
	}
}
