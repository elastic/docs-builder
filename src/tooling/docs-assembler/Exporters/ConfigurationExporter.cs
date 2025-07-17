// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Exporters;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Exporters;

public class ConfigurationExporter(ILoggerFactory logFactory, AssembleContext context) : IMarkdownExporter
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
		var configFolder = fs.DirectoryInfo.New(Path.Combine(context.OutputDirectory.FullName, "config"));
		if (!configFolder.Exists)
			configFolder.Create();

		_logger.LogInformation("Exporting configuration");

		var assemblerConfig = context.ConfigurationFileProvider.AssemblerFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", assemblerConfig.Name, configFolder.FullName);
		context.WriteFileSystem.File.Copy(assemblerConfig.FullName, Path.Combine(configFolder.FullName, assemblerConfig.Name), true);

		var navigationConfig = context.ConfigurationFileProvider.NavigationFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", navigationConfig.Name, configFolder.FullName);
		context.WriteFileSystem.File.Copy(navigationConfig.FullName, Path.Combine(configFolder.FullName, navigationConfig.Name), true);

		var legacyUrlMappingsConfig = context.ConfigurationFileProvider.LegacyUrlMappingsFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", legacyUrlMappingsConfig.Name, configFolder.FullName);
		context.WriteFileSystem.File.Copy(legacyUrlMappingsConfig.FullName, Path.Combine(configFolder.FullName, legacyUrlMappingsConfig.Name), true);

		var versionsConfig = context.ConfigurationFileProvider.VersionFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", versionsConfig.Name, configFolder.FullName);
		context.WriteFileSystem.File.Copy(versionsConfig.FullName, Path.Combine(configFolder.FullName, versionsConfig.Name), true);

		return default;
	}
}
