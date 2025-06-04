// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Reflection;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Site;
using Elastic.Markdown.IO;

namespace Documentation.Assembler;

public class AssembleContext
{
	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }

	public DiagnosticsCollector Collector { get; }

	public AssemblyConfiguration Configuration { get; set; }

	public IFileInfo ConfigurationPath { get; }

	public IFileInfo NavigationPath { get; }

	public IFileInfo HistoryMappingPath { get; }

	public IDirectoryInfo CheckoutDirectory { get; set; }

	public IDirectoryInfo OutputDirectory { get; set; }

	public bool Force { get; init; }

	/// This property is used to determine if the site should be indexed by search engines
	public bool AllowIndexing { get; init; }

	public PublishEnvironment Environment { get; set; }

	public AssembleContext(
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

		var configPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "src", "tooling", "docs-assembler", "assembler.yml");
		// temporarily fallback to embedded assembler.yml
		// This will live in docs-content soon
		if (!ReadFileSystem.File.Exists(configPath))
			ExtractAssemblerConfiguration(configPath, "assembler.yml");
		ConfigurationPath = ReadFileSystem.FileInfo.New(configPath);
		Configuration = AssemblyConfiguration.Deserialize(ReadFileSystem.File.ReadAllText(ConfigurationPath.FullName));

		var navigationPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "src", "tooling", "docs-assembler", "navigation.yml");
		if (!ReadFileSystem.File.Exists(navigationPath))
			ExtractAssemblerConfiguration(navigationPath, "navigation.yml");
		NavigationPath = ReadFileSystem.FileInfo.New(navigationPath);

		var historyMappingPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "src", "tooling", "docs-assembler", "legacy-url-mappings.yml");
		if (!ReadFileSystem.File.Exists(historyMappingPath))
			ExtractAssemblerConfiguration(historyMappingPath, "legacy-url-mappings.yml");
		HistoryMappingPath = ReadFileSystem.FileInfo.New(historyMappingPath);

		if (!Configuration.Environments.TryGetValue(environment, out var env))
			throw new Exception($"Could not find environment {environment}");
		Environment = env;

		var contentSource = Environment.ContentSource.ToStringFast(true);
		var defaultCheckoutDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "checkouts", contentSource);
		CheckoutDirectory = ReadFileSystem.DirectoryInfo.New(checkoutDirectory ?? defaultCheckoutDirectory);
		var defaultOutputDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly");
		OutputDirectory = ReadFileSystem.DirectoryInfo.New(output ?? defaultOutputDirectory);


	}

	private void ExtractAssemblerConfiguration(string configPath, string file)
	{
		var embeddedStaticFiles = Assembly.GetExecutingAssembly()
			.GetManifestResourceNames()
			.ToList();
		var configFile = embeddedStaticFiles.First(f => f.EndsWith(file));
		using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(configFile);
		if (resourceStream == null)
			return;

		var outputFile = WriteFileSystem.FileInfo.New(configPath);
		if (outputFile.Directory is { Exists: false })
			outputFile.Directory.Create();
		using var stream = outputFile.OpenWrite();
		resourceStream.CopyTo(stream);
	}
}
