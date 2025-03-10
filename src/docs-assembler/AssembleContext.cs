// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Documentation.Assembler.Configuration;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;

namespace Documentation.Assembler;

public class AssembleContext
{
	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }

	public DiagnosticsCollector Collector { get; }

	public AssemblyConfiguration Configuration { get; set; }

	public IFileInfo ConfigurationPath { get; }

	public IDirectoryInfo OutputDirectory { get; set; }

	public AssembleContext(DiagnosticsCollector collector, IFileSystem readFileSystem, IFileSystem writeFileSystem, string? output)
	{
		Collector = collector;
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;

		var configPath = Path.Combine(Paths.Root.FullName, "src/docs-assembler/assembler.yml");
		ConfigurationPath = ReadFileSystem.FileInfo.New(configPath);
		Configuration = AssemblyConfiguration.Deserialize(ReadFileSystem.File.ReadAllText(ConfigurationPath.FullName));
		OutputDirectory = ReadFileSystem.DirectoryInfo.New(output ?? ".artifacts/assembly");
	}
}
