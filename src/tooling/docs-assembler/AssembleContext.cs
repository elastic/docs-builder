// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Reflection;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;

namespace Documentation.Assembler;

public class AssembleContext : IDocumentationConfigurationContext
{
	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }

	public IDiagnosticsCollector Collector { get; }

	public AssemblyConfiguration Configuration { get; }

	/// <inheritdoc />
	public VersionsConfiguration VersionsConfiguration { get; }

	public ConfigurationFileProvider ConfigurationFileProvider { get; }

	/// <inheritdoc />
	public DocumentationEndpoints Endpoints { get; }

	public IDirectoryInfo CheckoutDirectory { get; }

	public IDirectoryInfo OutputDirectory { get; }

	public bool Force { get; init; }

	/// This property is used to determine if the site should be indexed by search engines
	public bool AllowIndexing { get; init; }

	public PublishEnvironment Environment { get; }

	public AssembleContext(
		AssemblyConfiguration configuration,
		IConfigurationContext configurationContext,
		string environment,
		DiagnosticsCollector collector,
		IFileSystem readFileSystem,
		IFileSystem writeFileSystem,
		string? checkoutDirectory,
		string? output
	)
	{
		Collector = collector;
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;

		Configuration = configuration;
		ConfigurationFileProvider = configurationContext.ConfigurationFileProvider;
		VersionsConfiguration = configurationContext.VersionsConfiguration;
		Endpoints = configurationContext.Endpoints;

		if (!Configuration.Environments.TryGetValue(environment, out var env))
			throw new Exception($"Could not find environment {environment}");
		Environment = env;

		var contentSource = Environment.ContentSource.ToStringFast(true);
		var defaultCheckoutDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "checkouts", contentSource);
		CheckoutDirectory = ReadFileSystem.DirectoryInfo.New(checkoutDirectory ?? defaultCheckoutDirectory);
		var defaultOutputDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly");
		OutputDirectory = ReadFileSystem.DirectoryInfo.New(output ?? defaultOutputDirectory);
	}
}
