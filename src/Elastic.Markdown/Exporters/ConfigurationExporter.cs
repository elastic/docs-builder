// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters;

public class ConfigurationExporter(
	ILoggerFactory logFactory,
	ConfigurationFileProvider configurationFileProvider,
	IDocumentationContext context
) : IMarkdownExporter
{
	private readonly ILogger<ConfigurationExporter> _logger = logFactory.CreateLogger<ConfigurationExporter>();

	/// <inheritdoc />
	public ValueTask StartAsync(CancellationToken ctx = default) => default;

	/// <inheritdoc />
	public ValueTask StopAsync(CancellationToken ctx = default) => default;

	/// <inheritdoc />
	public ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, CancellationToken ctx) => default;

	/// <inheritdoc />
	public ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, CancellationToken ctx)
	{
		var fs = context.WriteFileSystem;
		var outputDirectory = context.OutputDirectory;
		var configFolder = fs.DirectoryInfo.New(Path.Join(outputDirectory.FullName, "config"));
		if (!configFolder.Exists)
			configFolder.Create();

		_logger.LogInformation("Exporting configuration");

		var assemblerConfig = configurationFileProvider.AssemblerFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", assemblerConfig.Name, configFolder.FullName);
		Copy(fs, assemblerConfig, configFolder);

		var navigationConfig = configurationFileProvider.NavigationFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", navigationConfig.Name, configFolder.FullName);
		Copy(fs, navigationConfig, configFolder);

		var legacyUrlMappingsConfig = configurationFileProvider.LegacyUrlMappingsFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", legacyUrlMappingsConfig.Name, configFolder.FullName);
		Copy(fs, legacyUrlMappingsConfig, configFolder);

		var versionsConfig = configurationFileProvider.VersionFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", versionsConfig.Name, configFolder.FullName);
		Copy(fs, versionsConfig, configFolder);

		var productsConfig = configurationFileProvider.ProductsFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", productsConfig.Name, configFolder.FullName);
		Copy(fs, productsConfig, configFolder);

		return default;
	}

	// The file is read through the file system it came from: it lives outside the output scope the write file system is confined to.
	private static void Copy(IFileSystem writeFileSystem, IFileInfo source, IDirectoryInfo target) =>
		writeFileSystem.File.WriteAllBytes(Path.Join(target.FullName, source.Name), source.FileSystem.File.ReadAllBytes(source.FullName));
}
