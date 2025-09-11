// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration.Assembler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Configuration;

public partial class ConfigurationFileProvider
{
	private readonly IFileSystem _fileSystem;
	private readonly string _assemblyName;
	private readonly ILogger<ConfigurationFileProvider> _logger;

	public ConfigurationSource ConfigurationSource { get; }
	public string? GitReference { get; }

	public ConfigurationFileProvider(
		ILoggerFactory logFactory,
		IFileSystem fileSystem,
		bool skipPrivateRepositories = false,
		ConfigurationSource? configurationSource = null
	)
	{
		_logger = logFactory.CreateLogger<ConfigurationFileProvider>();
		_fileSystem = fileSystem;
		_assemblyName = typeof(ConfigurationFileProvider).Assembly.GetName().Name!;
		SkipPrivateRepositories = skipPrivateRepositories;
		TemporaryDirectory = fileSystem.Directory.CreateTempSubdirectory("docs-builder-config");

		ConfigurationSource = configurationSource ?? (
			fileSystem.Directory.Exists(LocalConfigurationDirectory)
				? ConfigurationSource.Local
				: fileSystem.Directory.Exists(LocalConfigurationDirectory)
					? ConfigurationSource.Init
					: ConfigurationSource.Embedded
			);

		if (ConfigurationSource == ConfigurationSource.Local && !fileSystem.Directory.Exists(LocalConfigurationDirectory))
			throw new Exception($"Required directory form {nameof(ConfigurationSource)}.{nameof(ConfigurationSource.Local)} directory {LocalConfigurationDirectory} does not exist.");

		if (ConfigurationSource == ConfigurationSource.Init && !fileSystem.Directory.Exists(AppDataConfigurationDirectory))
			throw new Exception($"Required directory form {nameof(ConfigurationSource)}.{nameof(ConfigurationSource.Init)} directory {AppDataConfigurationDirectory} does not exist.");

		var path = GetAppDataPath("git-ref.txt");
		if (_fileSystem.File.Exists(path))
			GitReference = _fileSystem.File.ReadAllText(path);
		else if (ConfigurationSource == ConfigurationSource.Init)
			throw new Exception($"Can not read git-ref.txt in directory {LocalConfigurationDirectory}");

		if (ConfigurationSource == ConfigurationSource.Init)
		{
			_logger.LogInformation("{ConfigurationSource}: git ref '{GitReference}', in {Directory}",
				$"{nameof(ConfigurationSource)}.{nameof(ConfigurationSource.Init)}", GitReference, AppDataConfigurationDirectory);
		}

		if (ConfigurationSource == ConfigurationSource.Local)
		{
			_logger.LogInformation("{ConfigurationSource}: located {Directory}",
				$"{nameof(ConfigurationSource)}.{nameof(ConfigurationSource.Local)}", AppDataConfigurationDirectory);
		}
		if (ConfigurationSource == ConfigurationSource.Embedded)
		{
			_logger.LogInformation("{ConfigurationSource} using embedded in binary configuration",
				$"{nameof(ConfigurationSource)}.{nameof(ConfigurationSource.Embedded)}");
		}

		VersionFile = CreateTemporaryConfigurationFile("versions.yml");
		AssemblerFile = CreateTemporaryConfigurationFile("assembler.yml");
		NavigationFile = CreateTemporaryConfigurationFile("navigation.yml");
		LegacyUrlMappingsFile = CreateTemporaryConfigurationFile("legacy-url-mappings.yml");
	}

	public bool SkipPrivateRepositories { get; }

	private IDirectoryInfo TemporaryDirectory { get; }

	public IFileInfo NavigationFile { get; private set; }

	public IFileInfo VersionFile { get; }

	public IFileInfo AssemblerFile { get; }

	public IFileInfo LegacyUrlMappingsFile { get; }

	public IFileInfo CreateNavigationFile(AssemblyConfiguration configuration)
	{
		var privateRepositories = configuration.PrivateRepositories;
		if (privateRepositories.Count == 0 || !SkipPrivateRepositories)
			return NavigationFile;

		var targets = string.Join("|", privateRepositories.Keys);

		var tempFile = Path.Combine(TemporaryDirectory.FullName, "navigation.filtered.yml");
		if (_fileSystem.File.Exists(tempFile))
			return NavigationFile;

		_logger.LogInformation("Filtering navigation file to remove private repositories");

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

		if (configuration.AvailableRepositories.TryGetValue("docs-builder", out var docsBuildRepository) && docsBuildRepository is { Skip: false, Path: not null })
		{
			// language=yaml
			_fileSystem.File.AppendAllText(tempFile,
				"""
				      - toc: docs-builder://
				        path_prefix: reference/docs-builder
				        children:
				          - toc: docs-builder://development
				            path_prefix: reference/docs-builder/dev
				            children:
				              - toc: docs-builder://development/link-validation
				                path_prefix: reference/docs-builder/dev/link-val

				""");
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
		if (ConfigurationSource == ConfigurationSource.Local && _fileSystem.File.Exists(localPath))
		{
			var reader = _fileSystem.File.OpenText(localPath);
			return reader;
		}
		if (ConfigurationSource == ConfigurationSource.Local)
			throw new Exception($"Can not read {fileName} in directory {LocalConfigurationDirectory}");

		var appDataPath = GetAppDataPath(fileName);
		if (ConfigurationSource == ConfigurationSource.Init && _fileSystem.File.Exists(appDataPath))
		{
			var reader = _fileSystem.File.OpenText(appDataPath);
			return reader;
		}
		if (ConfigurationSource == ConfigurationSource.Init)
			throw new Exception($"Can not read {fileName} in directory {AppDataConfigurationDirectory}");
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
	public static IServiceCollection AddConfigurationFileProvider(this IServiceCollection services,
		bool skipPrivateRepositories,
		Documentation.ConfigurationSource? configurationSource,
		Action<IServiceCollection, ConfigurationFileProvider> configure)
	{
		using var sp = services.BuildServiceProvider();
		var logFactory = sp.GetRequiredService<ILoggerFactory>();
		var provider = new ConfigurationFileProvider(logFactory, new FileSystem(), skipPrivateRepositories, configurationSource);
		_ = services.AddSingleton(provider);
		configure(services, provider);
		return services;
	}
}
