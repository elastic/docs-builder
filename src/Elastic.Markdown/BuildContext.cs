// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;

namespace Elastic.Markdown;

public record BuildContext : IDocumentationContext
{
	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }

	public IDirectoryInfo? DocumentationCheckoutDirectory { get; }
	public IDirectoryInfo DocumentationSourceDirectory { get; }
	public IDirectoryInfo DocumentationOutputDirectory { get; }

	public ConfigurationFile Configuration { get; }

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

	public BuildContext(IDiagnosticsCollector collector, IFileSystem fileSystem)
		: this(collector, fileSystem, fileSystem, null, null) { }

	public BuildContext(
		IDiagnosticsCollector collector,
		IFileSystem readFileSystem,
		IFileSystem writeFileSystem,
		string? source = null,
		string? output = null,
		GitCheckoutInformation? gitCheckoutInformation = null
	)
	{
		Collector = collector;
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;

		var rootFolder = !string.IsNullOrWhiteSpace(source)
			? ReadFileSystem.DirectoryInfo.New(source)
			: ReadFileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName));

		(DocumentationSourceDirectory, ConfigurationPath) = FindDocsFolderFromRoot(rootFolder);

		DocumentationCheckoutDirectory = Paths.DetermineSourceDirectoryRoot(DocumentationSourceDirectory);

		DocumentationOutputDirectory = !string.IsNullOrWhiteSpace(output)
			? WriteFileSystem.DirectoryInfo.New(output)
			: WriteFileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, Path.Combine(".artifacts", "docs", "html")));

		if (ConfigurationPath.FullName != DocumentationSourceDirectory.FullName)
			DocumentationSourceDirectory = ConfigurationPath.Directory!;

		Git = gitCheckoutInformation ?? GitCheckoutInformation.Create(DocumentationCheckoutDirectory, ReadFileSystem);
		Configuration = new ConfigurationFile(this);
		GoogleTagManager = new GoogleTagManagerConfiguration
		{
			Enabled = false
		};
	}

	private (IDirectoryInfo, IFileInfo) FindDocsFolderFromRoot(IDirectoryInfo rootPath)
	{
		string[] files = ["docset.yml", "_docset.yml"];
		string[] knownFolders = [rootPath.FullName, Path.Combine(rootPath.FullName, "docs")];
		var mostLikelyTargets =
			from file in files
			from folder in knownFolders
			select Path.Combine(folder, file);

		var knownConfigPath = mostLikelyTargets.FirstOrDefault(ReadFileSystem.File.Exists);
		var configurationPath = knownConfigPath is null ? null : ReadFileSystem.FileInfo.New(knownConfigPath);
		if (configurationPath is not null)
			return (configurationPath.Directory!, configurationPath);

		configurationPath = rootPath
			.EnumerateFiles("*docset.yml", SearchOption.AllDirectories)
			.FirstOrDefault()
			?? throw new Exception($"Can not locate docset.yml file in '{rootPath}'");

		var docsFolder = configurationPath.Directory
			?? throw new Exception($"Can not locate docset.yml file in '{rootPath}'");

		return (docsFolder, configurationPath);
	}

}
