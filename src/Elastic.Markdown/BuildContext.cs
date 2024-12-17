// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;

namespace Elastic.Markdown;

public record BuildContext
{
	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }

	public IDirectoryInfo SourcePath { get; }
	public IDirectoryInfo OutputPath { get; }

	public IFileInfo ConfigurationPath { get; }

	public GitConfiguration Git { get; }

	public required DiagnosticsCollector Collector { get; init; }

	public bool Force { get; init; }

	public string? UrlPathPrefix
	{
		get => string.IsNullOrWhiteSpace(_urlPathPrefix) ? "" : $"/{_urlPathPrefix.Trim('/')}";
		init => _urlPathPrefix = value;
	}

	private readonly string? _urlPathPrefix;

	public BuildContext(IFileSystem fileSystem)
		: this(fileSystem, fileSystem, null, null) { }

	public BuildContext(IFileSystem readFileSystem, IFileSystem writeFileSystem)
		: this(readFileSystem, writeFileSystem, null, null) { }

	public BuildContext(IFileSystem readFileSystem, IFileSystem writeFileSystem, string? source, string? output)
	{
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;

		var docsFolder = FindDocsFolderFromRoot();

		SourcePath = !string.IsNullOrWhiteSpace(source) ? ReadFileSystem.DirectoryInfo.New(source) : docsFolder;

		OutputPath = !string.IsNullOrWhiteSpace(output)
			? WriteFileSystem.DirectoryInfo.New(output)
			: WriteFileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName, ".artifacts/docs/html"));

		ConfigurationPath =
		    ReadFileSystem.FileInfo.New(Path.Combine(SourcePath.FullName, "docset.yml"));

		if (ConfigurationPath.FullName != SourcePath.FullName)
			SourcePath = ConfigurationPath.Directory!;

		Git = GitConfiguration.Create(ReadFileSystem);
	}

	private IDirectoryInfo FindDocsFolderFromRoot()
	{
		var defaultDocsFolder = ReadFileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName, "docs"));
		var root = ReadFileSystem.DirectoryInfo.New(Paths.Root.FullName);
		var docsFolder = root.EnumerateFiles("docset.yml", SearchOption.AllDirectories).FirstOrDefault();
		return docsFolder?.Directory ?? defaultDocsFolder;
	}

}
