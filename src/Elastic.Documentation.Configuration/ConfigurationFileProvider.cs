// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration.Assembler;
using Microsoft.Extensions.DependencyInjection;
using NetEscapades.EnumGenerators;
using YamlDotNet.RepresentationModel;

namespace Elastic.Documentation.Configuration;

[EnumExtensions]
public enum ConfigurationSource
{
	Local,
	Checkout,
	Embedded
}

public partial class ConfigurationFileProvider
{
	private readonly IFileSystem _fileSystem;
	private readonly string _assemblyName;

	public ConfigurationSource ConfigurationSource { get; private set; } = ConfigurationSource.Embedded;
	public string? GitReference { get; }

	public ConfigurationFileProvider(IFileSystem fileSystem, bool skipPrivateRepositories = false)
	{
		_fileSystem = fileSystem;
		_assemblyName = typeof(ConfigurationFileProvider).Assembly.GetName().Name!;
		SkipPrivateRepositories = skipPrivateRepositories;
		TemporaryDirectory = fileSystem.Directory.CreateTempSubdirectory("docs-builder-config");

		VersionFile = CreateTemporaryConfigurationFile("versions.yml");
		AssemblerFile = CreateTemporaryConfigurationFile("assembler.yml");
		NavigationFile = CreateTemporaryConfigurationFile("navigation.yml");
		LegacyUrlMappingsFile = CreateTemporaryConfigurationFile("legacy-url-mappings.yml");
		var path = GetAppDataPath("git-ref.txt");
		if (ConfigurationSource == ConfigurationSource.Checkout && _fileSystem.File.Exists(path))
			GitReference = _fileSystem.File.ReadAllText(path);
	}

	public bool SkipPrivateRepositories { get; }

	private IDirectoryInfo TemporaryDirectory { get; }

	public IFileInfo NavigationFile { get; private set; }

	public IFileInfo VersionFile { get; }

	public IFileInfo AssemblerFile { get; }

	public IFileInfo LegacyUrlMappingsFile { get; }

	public IFileInfo CreateNavigationFile(IReadOnlyDictionary<string, Repository> privateRepositories)
	{
		if (privateRepositories.Count == 0 || !SkipPrivateRepositories)
			return NavigationFile;

		var targets = string.Join("|", privateRepositories.Keys);

		var tempFile = Path.Combine(TemporaryDirectory.FullName, "navigation.filtered.yml");
		if (_fileSystem.File.Exists(tempFile))
			return NavigationFile;

		// This routine removes `toc: `'s linking to private repositories and reindents any later lines if needed.
		// This will make any public children in the nav move up one place.
		var spacing = -1;
		var reindenting = -1;
		foreach (var l in _fileSystem.File.ReadAllLines(NavigationFile.FullName))
		{
			var line = l;
			if (spacing > -1 && !string.IsNullOrWhiteSpace(line) && !line.StartsWith(new string(' ', spacing), StringComparison.Ordinal))
			{
				spacing = -1;
				reindenting = -1;
			}

			if (spacing != -1 && Regex.IsMatch(line, $@"^\s{{{spacing}}}\S"))
			{
				spacing = -1;
				reindenting = -1;
			}

			else if (spacing != -1 && Regex.IsMatch(line, $@"^(\s{{{spacing + 3},}})\S"))
			{
				var matches = Regex.Match(line, $@"^(?<spacing>\s{{{spacing}}})(?<remainder>.+)$");
				line = $"{new string(' ', Math.Max(0, spacing - 4))}{matches.Groups["remainder"].Value}";
				reindenting = spacing;
			}

			else if (spacing == -1 && TocPrefixRegex().IsMatch(line))
			{
				var matches = Regex.Match(line, $@"^(?<spacing>\s+)-\s?toc:\s?(?:{targets})\:");
				if (matches.Success)
					spacing = matches.Groups["spacing"].Value.Length;
				if (spacing == 0)
					spacing = -1;
			}

			if (spacing == -1 || reindenting > 0)
				_fileSystem.File.AppendAllLines(tempFile, [line]);
		}
		NavigationFile = _fileSystem.FileInfo.New(tempFile);
		return NavigationFile;


	}

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
	[GeneratedRegex(@"^\s+-?\s?toc:\s?")]
	private static partial Regex TocPrefixRegex();
}

public static class ConfigurationFileProviderServiceCollectionExtensions
{
	public static IServiceCollection AddConfigurationFileProvider(
		this IServiceCollection services,
		bool skipPrivateRepositories,
		Action<IServiceCollection, ConfigurationFileProvider> configure
	)
	{
		var provider = new ConfigurationFileProvider(new FileSystem(), skipPrivateRepositories);
		_ = services.AddSingleton(provider);
		configure(services, provider);
		return services;
	}
}
