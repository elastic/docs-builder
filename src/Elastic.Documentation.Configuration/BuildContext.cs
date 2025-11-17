// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Reflection;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Synonyms;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration;

public record BuildContext : IDocumentationSetContext, IDocumentationConfigurationContext
{
	public static string Version { get; } = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyInformationalVersionAttribute>()
		.FirstOrDefault()?.InformationalVersion ?? "0.0.0";

	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }
	public IReadOnlySet<Exporter> AvailableExporters { get; }

	public IDirectoryInfo? DocumentationCheckoutDirectory { get; }
	public IDirectoryInfo DocumentationSourceDirectory { get; }
	public IDirectoryInfo OutputDirectory { get; }

	public ConfigurationFile Configuration { get; }

	public DocumentationSetFile ConfigurationYaml { get; set; }

	public VersionsConfiguration VersionsConfiguration { get; }
	public ConfigurationFileProvider ConfigurationFileProvider { get; }
	public DocumentationEndpoints Endpoints { get; }

	public ProductsConfiguration ProductsConfiguration { get; }
	public LegacyUrlMappingConfiguration LegacyUrlMappings { get; }
	public SynonymsConfiguration SynonymsConfiguration { get; }

	public IFileInfo ConfigurationPath { get; }

	public GitCheckoutInformation Git { get; }

	public IDiagnosticsCollector Collector { get; }

	public bool Force { get; init; }

	public bool AssemblerBuild { get; init; }

	// This property is used to determine if the site should be indexed by search engines
	public bool AllowIndexing { get; init; }

	public GoogleTagManagerConfiguration GoogleTagManager { get; init; }

	// This property is used for the canonical URL
	public Uri? CanonicalBaseUrl { get; init; }

	public string? UrlPathPrefix
	{
		get => string.IsNullOrWhiteSpace(field) ? "" : $"/{field.Trim('/')}";
		init;
	}

	public BuildContext(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		IConfigurationContext configurationContext
	)
		: this(collector, fileSystem, fileSystem, configurationContext, ExportOptions.Default, null, null)
	{
	}

	public BuildContext(
		IDiagnosticsCollector collector,
		IFileSystem readFileSystem,
		IFileSystem writeFileSystem,
		IConfigurationContext configurationContext,
		IReadOnlySet<Exporter> availableExporters,
		string? source = null,
		string? output = null,
		GitCheckoutInformation? gitCheckoutInformation = null
	)
	{
		Collector = collector;
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;
		AvailableExporters = availableExporters;
		SynonymsConfiguration = configurationContext.SynonymsConfiguration;
		VersionsConfiguration = configurationContext.VersionsConfiguration;
		ConfigurationFileProvider = configurationContext.ConfigurationFileProvider;
		ProductsConfiguration = configurationContext.ProductsConfiguration;
		LegacyUrlMappings = configurationContext.LegacyUrlMappings;
		Endpoints = configurationContext.Endpoints;

		var rootFolder = !string.IsNullOrWhiteSpace(source)
			? ReadFileSystem.DirectoryInfo.New(source)
			: ReadFileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName));

		(DocumentationSourceDirectory, ConfigurationPath) = Paths.FindDocsFolderFromRoot(ReadFileSystem, rootFolder);

		DocumentationCheckoutDirectory = Paths.DetermineSourceDirectoryRoot(DocumentationSourceDirectory);

		OutputDirectory = !string.IsNullOrWhiteSpace(output)
			? WriteFileSystem.DirectoryInfo.New(output)
			: WriteFileSystem.DirectoryInfo.New(Path.Combine(rootFolder.FullName, Path.Combine(".artifacts", "docs", "html")));

		if (ConfigurationPath.FullName != DocumentationSourceDirectory.FullName)
			DocumentationSourceDirectory = ConfigurationPath.Directory!;

		Git = gitCheckoutInformation ?? GitCheckoutInformation.Create(DocumentationCheckoutDirectory, ReadFileSystem);

		// Load and resolve the docset file, or create an empty one if it doesn't exist
		ConfigurationYaml = ConfigurationPath.Exists
			? DocumentationSetFile.LoadAndResolve(collector, ConfigurationPath, readFileSystem)
			: new DocumentationSetFile();

		Configuration = new ConfigurationFile(ConfigurationYaml, this, VersionsConfiguration, ProductsConfiguration);
		GoogleTagManager = new GoogleTagManagerConfiguration
		{
			Enabled = false
		};
	}

}
