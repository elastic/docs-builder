// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Assembler;

public class AssembleContext : IDocumentationConfigurationContext
{
	public ScopedFileSystem ReadFileSystem { get; }
	public ScopedFileSystem WriteFileSystem { get; }

	public IDiagnosticsCollector Collector { get; }

	public AssemblyConfiguration Configuration { get; }

	/// <inheritdoc />
	public VersionsConfiguration VersionsConfiguration { get; }

	public ConfigurationFileProvider ConfigurationFileProvider { get; }

	/// <inheritdoc />
	public DocumentationEndpoints Endpoints { get; }

	public ProductsConfiguration ProductsConfiguration { get; }
	public LegacyUrlMappingConfiguration LegacyUrlMappings { get; }
	public SearchConfiguration SearchConfiguration { get; }

	// Always use the production URL. In case a page is leaked to a search engine, it should point to the production site.
	/// <inheritdoc />
	public Uri? CanonicalBaseUrl { get; } = new("https://www.elastic.co");

	public IDirectoryInfo CheckoutDirectory { get; }

	public IDirectoryInfo OutputDirectory { get; }

	/// <summary>
	/// The output directory with the path prefix applied.
	/// This is where assembled content (sitemap.xml, llms.txt, link-index.snapshot.json, etc.) should be written.
	/// </summary>
	public IDirectoryInfo OutputWithPathPrefixDirectory { get; }

	/// <inheritdoc />
	public IFileInfo ConfigurationPath { get; }

	/// <inheritdoc />
	public BuildType BuildType => BuildType.Assembler;

	public PublishEnvironment Environment { get; }

	public AssembleContext(
		AssemblyConfiguration configuration,
		IConfigurationContext configurationContext,
		string environment,
		IDiagnosticsCollector collector,
		ScopedFileSystem readFileSystem,
		ScopedFileSystem writeFileSystem,
		string? checkoutDirectory,
		string? output
	)
	{
		Collector = collector;
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;

		Configuration = configuration;
		ConfigurationFileProvider = configurationContext.ConfigurationFileProvider;
		ConfigurationPath = ConfigurationFileProvider.AssemblerFile;
		VersionsConfiguration = configurationContext.VersionsConfiguration;
		SearchConfiguration = configurationContext.SearchConfiguration;
		Endpoints = configurationContext.Endpoints;
		Endpoints.BuildType = "assembler";
		ProductsConfiguration = configurationContext.ProductsConfiguration;
		LegacyUrlMappings = configurationContext.LegacyUrlMappings;

		if (!Configuration.Environments.TryGetValue(environment, out var env))
			throw new Exception($"Could not find environment {environment}");
		Environment = env;

		Endpoints.Environment = environment;

		var contentSource = Environment.ContentSource.ToStringFast(true);
		var defaultCheckoutDirectory = Path.Join(Paths.ApplicationData.FullName, "checkouts", contentSource);
		CheckoutDirectory = checkoutDirectory is null
			? FileSystemFactory.AppData.DirectoryInfo.New(defaultCheckoutDirectory)
			: ReadFileSystem.DirectoryInfo.New(checkoutDirectory);
		var defaultOutputDirectory = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly");
		OutputDirectory = WriteFileSystem.DirectoryInfo.New(output ?? defaultOutputDirectory);

		// Calculate the output directory with path prefix once
		var pathPrefix = Environment.PathPrefix;
		OutputWithPathPrefixDirectory = string.IsNullOrEmpty(pathPrefix)
			? OutputDirectory
			: WriteFileSystem.DirectoryInfo.New(WriteFileSystem.Path.Join(OutputDirectory.FullName, pathPrefix));
	}
}
