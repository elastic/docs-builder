// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Exporters;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Tooling.Exporters;

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
		var configFolder = fs.DirectoryInfo.New(Path.Combine(outputDirectory.FullName, "config"));
		if (!configFolder.Exists)
			configFolder.Create();

		_logger.LogInformation("Exporting configuration");

		var assemblerConfig = configurationFileProvider.AssemblerFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", assemblerConfig.Name, configFolder.FullName);
		fs.File.Copy(assemblerConfig.FullName, Path.Combine(configFolder.FullName, assemblerConfig.Name), true);

		var navigationConfig = configurationFileProvider.NavigationFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", navigationConfig.Name, configFolder.FullName);
		fs.File.Copy(navigationConfig.FullName, Path.Combine(configFolder.FullName, navigationConfig.Name), true);

		var legacyUrlMappingsConfig = configurationFileProvider.LegacyUrlMappingsFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", legacyUrlMappingsConfig.Name, configFolder.FullName);
		fs.File.Copy(legacyUrlMappingsConfig.FullName, Path.Combine(configFolder.FullName, legacyUrlMappingsConfig.Name), true);

		var versionsConfig = configurationFileProvider.VersionFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", versionsConfig.Name, configFolder.FullName);
		fs.File.Copy(versionsConfig.FullName, Path.Combine(configFolder.FullName, versionsConfig.Name), true);

		var productsConfig = configurationFileProvider.ProductsFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", productsConfig.Name, configFolder.FullName);
		fs.File.Copy(productsConfig.FullName, Path.Combine(configFolder.FullName, productsConfig.Name), true);

		return default;
	}
}
