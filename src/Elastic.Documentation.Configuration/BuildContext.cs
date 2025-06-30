// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Reflection;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration;

public record BuildContext : IDocumentationContext
{
	public static string Version { get; } = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyInformationalVersionAttribute>()
		.FirstOrDefault()?.InformationalVersion ?? "0.0.0";

	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }

	public IDirectoryInfo? DocumentationCheckoutDirectory { get; }
	public IDirectoryInfo DocumentationSourceDirectory { get; }
	public IDirectoryInfo DocumentationOutputDirectory { get; }

	public ConfigurationFile Configuration { get; }

	public VersionsConfiguration VersionsConfig { get; init; }

	public IFileInfo ConfigurationPath { get; }

	public GitCheckoutInformation Git { get; }

	public IDiagnosticsCollector Collector { get; }

	public bool Force { get; init; }

	public bool SkipDocumentationState { get; private set; }

	public bool AssemblerBuild
	{
		get => _assemblerBuild;
		init
		{
			_assemblerBuild = value;
			SkipDocumentationState = value;
		}
	}

	// This property is used to determine if the site should be indexed by search engines
	public bool AllowIndexing { get; init; }

	public GoogleTagManagerConfiguration GoogleTagManager { get; init; }

	// This property is used for the canonical URL
	public Uri? CanonicalBaseUrl { get; init; }

	private readonly string? _urlPathPrefix;
	private readonly bool _assemblerBuild;

	public string? UrlPathPrefix
	{
		get => string.IsNullOrWhiteSpace(_urlPathPrefix) ? "" : $"/{_urlPathPrefix.Trim('/')}";
		init => _urlPathPrefix = value;
	}

	public BuildContext(IDiagnosticsCollector collector, IFileSystem fileSystem, VersionsConfiguration versionsConfig)
		: this(collector, fileSystem, fileSystem, versionsConfig, null, null)
	{
	}

	public BuildContext(
		IDiagnosticsCollector collector,
		IFileSystem readFileSystem,
		IFileSystem writeFileSystem,
		VersionsConfiguration versionsConfig,
		string? source = null,
		string? output = null,
		GitCheckoutInformation? gitCheckoutInformation = null
	)
	{
		Collector = collector;
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;
		VersionsConfig = versionsConfig;

		var rootFolder = !string.IsNullOrWhiteSpace(source)
			? ReadFileSystem.DirectoryInfo.New(source)
			: ReadFileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName));

		(DocumentationSourceDirectory, ConfigurationPath) = Paths.FindDocsFolderFromRoot(ReadFileSystem, rootFolder);

		DocumentationCheckoutDirectory = Paths.DetermineSourceDirectoryRoot(DocumentationSourceDirectory);

		DocumentationOutputDirectory = !string.IsNullOrWhiteSpace(output)
			? WriteFileSystem.DirectoryInfo.New(output)
			: WriteFileSystem.DirectoryInfo.New(Path.Combine(rootFolder.FullName, Path.Combine(".artifacts", "docs", "html")));

		if (ConfigurationPath.FullName != DocumentationSourceDirectory.FullName)
			DocumentationSourceDirectory = ConfigurationPath.Directory!;

		Git = gitCheckoutInformation ?? GitCheckoutInformation.Create(DocumentationCheckoutDirectory, ReadFileSystem);
		Configuration = new ConfigurationFile(this);
		GoogleTagManager = new GoogleTagManagerConfiguration
		{
			Enabled = false
		};
	}
}
