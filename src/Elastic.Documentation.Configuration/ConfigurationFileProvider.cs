// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Documentation.Configuration;

public class ConfigurationFileProvider
{
	private readonly IFileSystem _fileSystem;

	public ConfigurationFileProvider(IFileSystem fileSystem)
	{
		_fileSystem = fileSystem;
		TemporaryDirectory = fileSystem.Directory.CreateTempSubdirectory("docs-builder-config");

		VersionFile = CreateTemporaryConfigurationFile(EmbeddedResource.______config_versions_yml);
		AssemblerFile = CreateTemporaryConfigurationFile(EmbeddedResource.______config_assembler_yml);
		NavigationFile = CreateTemporaryConfigurationFile(EmbeddedResource.______config_navigation_yml);
		LegacyUrlMappingsFile = CreateTemporaryConfigurationFile(EmbeddedResource.______config_legacy_url_mappings_yml);
	}

	private IDirectoryInfo TemporaryDirectory { get; }

	public IFileInfo NavigationFile { get; }

	public IFileInfo VersionFile { get; }

	public IFileInfo AssemblerFile { get; }

	public IFileInfo LegacyUrlMappingsFile { get; }

	private IFileInfo CreateTemporaryConfigurationFile(EmbeddedResource resource)
	{
		var fileName = string.Join(".", resource.GetResourceName().Split('.')[^2..]);
		using var stream = GetLocalOrEmbedded(resource);
		var context = stream.ReadToEnd();
		var fi = _fileSystem.FileInfo.New(Path.Combine(TemporaryDirectory.FullName, fileName));
		_fileSystem.File.WriteAllText(fi.FullName, context);
		return fi;
	}

	private StreamReader GetLocalOrEmbedded(EmbeddedResource resource)
	{
		var fileName = string.Join(".", resource.GetResourceName().Split('.')[^2..]);
		var configPath = GetLocalPath(fileName);
		if (!_fileSystem.File.Exists(configPath))
			return GetEmbeddedStream(resource);
		var reader = _fileSystem.File.OpenText(configPath);
		return reader;
	}

	private static StreamReader GetEmbeddedStream(EmbeddedResource resource)
	{
		var name = resource.GetResourceName().Replace(".......config.", ".");
		var resourceStream = typeof(EmbeddedResource).Assembly.GetManifestResourceStream(name)!;
		var reader = new StreamReader(resourceStream, leaveOpen: false);
		return reader;
	}

	public static string LocalConfigurationDirectory => Path.Combine(Paths.WorkingDirectoryRoot.FullName, "config");

	private static string GetLocalPath(string file) => Path.Combine(LocalConfigurationDirectory, file);
}

public static class ConfigurationFileProviderServiceCollectionExtensions
{
	public static IServiceCollection AddConfigurationFileProvider(this IServiceCollection services,
		Action<IServiceCollection, ConfigurationFileProvider> configure)
	{
		var provider = new ConfigurationFileProvider(new FileSystem());
		_ = services.AddSingleton(provider);
		configure(services, provider);
		return services;
	}
}
