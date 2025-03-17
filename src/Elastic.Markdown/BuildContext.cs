// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Extensions;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Discovery;

namespace Elastic.Markdown;

public record BuildContext
{
	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }

	public IDirectoryInfo DocumentationSourceDirectory { get; }
	public IDirectoryInfo DocumentationOutputDirectory { get; }

	public ConfigurationFile Configuration { get; set; }

	public IFileInfo ConfigurationPath { get; }

	public GitCheckoutInformation Git { get; }

	public DiagnosticsCollector Collector { get; }

	public bool Force { get; init; }

	// This property is used to determine if the site should be indexed by search engines
	public bool AllowIndexing { get; init; }

	private readonly string? _urlPathPrefix;
	public string? UrlPathPrefix
	{
		get => string.IsNullOrWhiteSpace(_urlPathPrefix) ? "" : $"/{_urlPathPrefix.Trim('/')}";
		init => _urlPathPrefix = value;
	}

	private readonly string? _staticUrlPathPrefix;
	public string? StaticUrlPathPrefix
	{
		get => !string.IsNullOrWhiteSpace(_staticUrlPathPrefix)
			? $"/{_staticUrlPathPrefix.Trim('/')}"
			: UrlPathPrefix;
		init => _staticUrlPathPrefix = value;
	}

	public BuildContext(IFileSystem fileSystem)
		: this(new DiagnosticsCollector([]), fileSystem, fileSystem, null, null) { }

	public BuildContext(DiagnosticsCollector collector, IFileSystem fileSystem)
		: this(collector, fileSystem, fileSystem, null, null) { }

	public BuildContext(DiagnosticsCollector collector, IFileSystem readFileSystem, IFileSystem writeFileSystem)
		: this(collector, readFileSystem, writeFileSystem, null, null) { }

	public BuildContext(DiagnosticsCollector collector, IFileSystem readFileSystem, IFileSystem writeFileSystem, string? source, string? output)
	{
		Collector = collector;
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;

		var rootFolder = !string.IsNullOrWhiteSpace(source)
			? ReadFileSystem.DirectoryInfo.New(source)
			: ReadFileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName));

		(DocumentationSourceDirectory, ConfigurationPath) = FindDocsFolderFromRoot(rootFolder);

		DocumentationOutputDirectory = !string.IsNullOrWhiteSpace(output)
			? WriteFileSystem.DirectoryInfo.New(output)
			: WriteFileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName, Path.Combine(".artifacts", "docs", "html")));

		if (ConfigurationPath.FullName != DocumentationSourceDirectory.FullName)
			DocumentationSourceDirectory = ConfigurationPath.Directory!;


		Git = GitCheckoutInformation.Create(DocumentationSourceDirectory, ReadFileSystem);
		Configuration = new ConfigurationFile(this);
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
