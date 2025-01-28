// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Discovery;

namespace Elastic.Markdown;

public record BuildContext
{
	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }

	public IDirectoryInfo SourcePath { get; }
	public IDirectoryInfo OutputPath { get; }

	public IFileInfo ConfigurationPath { get; }

	public GitCheckoutInformation Git { get; }

	public required DiagnosticsCollector Collector { get; init; }

	public bool Force { get; init; }

	public string? UrlPathPrefix
	{
		get => string.IsNullOrWhiteSpace(_urlPathPrefix) ? "" : $"/{_urlPathPrefix.Trim('/')}";
		init => _urlPathPrefix = value;
	}

	// This property is used to determine if the site should be indexed by search engines
	public bool AllowIndexing { get; init; }

	private readonly string? _urlPathPrefix;

	public BuildContext(IFileSystem fileSystem)
		: this(fileSystem, fileSystem, null, null) { }

	public BuildContext(IFileSystem readFileSystem, IFileSystem writeFileSystem)
		: this(readFileSystem, writeFileSystem, null, null) { }

	public BuildContext(IFileSystem readFileSystem, IFileSystem writeFileSystem, string? source, string? output)
	{
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;

		var rootFolder = !string.IsNullOrWhiteSpace(source)
			? ReadFileSystem.DirectoryInfo.New(source)
			: ReadFileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName));

		(SourcePath, ConfigurationPath) = FindDocsFolderFromRoot(rootFolder);

		OutputPath = !string.IsNullOrWhiteSpace(output)
			? WriteFileSystem.DirectoryInfo.New(output)
			: WriteFileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName, ".artifacts/docs/html"));

		if (ConfigurationPath.FullName != SourcePath.FullName)
			SourcePath = ConfigurationPath.Directory!;

		Git = GitCheckoutInformation.Create(ReadFileSystem);
	}

	private (IDirectoryInfo, IFileInfo) FindDocsFolderFromRoot(IDirectoryInfo rootPath)
	{
		var configurationPath = ReadFileSystem.FileInfo.New(Path.Combine(rootPath.FullName, "docset.yml"));
		if (rootPath.Exists &&
			ReadFileSystem.File.Exists(Path.Combine(rootPath.FullName, "docset.yml")))
			return (rootPath, configurationPath);

		configurationPath = ReadFileSystem.FileInfo.New(Path.Combine(rootPath.FullName, "_docset.yml"));
		if (rootPath.Exists &&
			ReadFileSystem.File.Exists(Path.Combine(rootPath.FullName, "_docset.yml")))
			return (rootPath, configurationPath);

		configurationPath = rootPath
			.EnumerateFiles("docset.yml", SearchOption.AllDirectories)
			.OrderByDescending(f => f.Name switch { "docset.yml" => 2, "_docset.yml" => 1, _ => 0 })
			.FirstOrDefault()
			?? throw new Exception($"Can not locate docset.yml file in '{rootPath}'");

		var docsFolder = configurationPath.Directory
			?? throw new Exception($"Can not locate docset.yml file in '{rootPath}'");

		return (docsFolder, configurationPath);
	}

}
