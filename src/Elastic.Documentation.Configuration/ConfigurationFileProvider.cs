// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation.Configuration;

[EnumExtensions]
public enum ConfigurationSource
{
	Local,
	Checkout,
	Embedded
}

public class ConfigurationFileProvider
{
	private readonly IFileSystem _fileSystem;
	private readonly string _assemblyName;

	public ConfigurationSource ConfigurationSource { get; private set; } = ConfigurationSource.Embedded;
	public string? GitReference { get; }

	public ConfigurationFileProvider(IFileSystem fileSystem)
	{
		_fileSystem = fileSystem;
		_assemblyName = typeof(ConfigurationFileProvider).Assembly.GetName().Name!;
		TemporaryDirectory = fileSystem.Directory.CreateTempSubdirectory("docs-builder-config");

		VersionFile = CreateTemporaryConfigurationFile("versions.yml");
		AssemblerFile = CreateTemporaryConfigurationFile("assembler.yml");
		NavigationFile = CreateTemporaryConfigurationFile("navigation.yml");
		LegacyUrlMappingsFile = CreateTemporaryConfigurationFile("legacy-url-mappings.yml");
		var path = GetAppDataPath("git-ref.txt");
		if (ConfigurationSource == ConfigurationSource.Checkout && _fileSystem.File.Exists(path))
			GitReference = _fileSystem.File.ReadAllText(path);
	}

	private IDirectoryInfo TemporaryDirectory { get; }

	public IFileInfo NavigationFile { get; }

	public IFileInfo VersionFile { get; }

	public IFileInfo AssemblerFile { get; }

	public IFileInfo LegacyUrlMappingsFile { get; }

	private IFileInfo CreateTemporaryConfigurationFile(string fileName)
	{
		using var stream = GetLocalOrEmbedded(fileName);
		var context = stream.ReadToEnd();
		var fi = _fileSystem.FileInfo.New(Path.Combine(TemporaryDirectory.FullName, fileName));
		_fileSystem.File.WriteAllText(fi.FullName, context);
		return fi;
	}

	private StreamReader GetLocalOrEmbedded(string fileName)
	{
		var localPath = GetLocalPath(fileName);
		var appDataPath = GetAppDataPath(fileName);
		if (_fileSystem.File.Exists(localPath))
		{
			ConfigurationSource = ConfigurationSource.Local;
			var reader = _fileSystem.File.OpenText(localPath);
			return reader;
		}
		if (_fileSystem.File.Exists(appDataPath))
		{
			ConfigurationSource = ConfigurationSource.Checkout;
			var reader = _fileSystem.File.OpenText(appDataPath);
			return reader;
		}
		return GetEmbeddedStream(fileName);
	}

	private StreamReader GetEmbeddedStream(string fileName)
	{
		var resourceName = $"{_assemblyName}.{fileName}";
		var resourceStream = typeof(ConfigurationFileProvider).Assembly.GetManifestResourceStream(resourceName)!;
		var reader = new StreamReader(resourceStream, leaveOpen: false);
		return reader;
	}

	private static string AppDataConfigurationDirectory { get; } = Path.Combine(Paths.ApplicationData.FullName, "config-clone", "config");
	private static string LocalConfigurationDirectory { get; } = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "config");

	private static string GetLocalPath(string file) => Path.Combine(LocalConfigurationDirectory, file);
	private static string GetAppDataPath(string file) => Path.Combine(AppDataConfigurationDirectory, file);
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
