// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Converters;
using Elastic.Documentation.Configuration.Serialization;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Documentation.Configuration;

public class ConfigurationFileProvider
{
	private readonly IAppDataFileSystem _fileSystem;
	private readonly string _assemblyName;
	private readonly ILogger<ConfigurationFileProvider> _logger;

	public static IDeserializer Deserializer { get; } = new StaticDeserializerBuilder(new YamlStaticContext())
		.WithNamingConvention(UnderscoredNamingConvention.Instance)
		.IgnoreUnmatchedProperties()
		.WithTypeConverter(new HintTypeSetConverter())
		.WithTypeConverter(new DurationYamlConverter())
		.WithTypeConverter(new ApplicableToYamlConverter([]))
		.WithTypeConverter(new TocItemCollectionYamlConverter())
		.WithTypeConverter(new TocItemYamlConverter())
		.WithTypeConverter(new SiteTableOfContentsCollectionYamlConverter())
		.WithTypeConverter(new SiteTableOfContentsRefYamlConverter())
		.WithTypeConverter(new ApiConfigurationConverter())
		.Build();

	public ConfigurationSource ConfigurationSource { get; }

	public string? GitReference { get; }

	public ConfigurationFileProvider(
		ILoggerFactory logFactory,
		IAppDataFileSystem fileSystem,
		bool skipPrivateRepositories = false,
		ConfigurationSource? configurationSource = null
	)
	{
		_logger = logFactory.CreateLogger<ConfigurationFileProvider>();
		_fileSystem = fileSystem;
		_assemblyName = typeof(ConfigurationFileProvider).Assembly.GetName().Name!;
		SkipPrivateRepositories = skipPrivateRepositories;

		// TODO: This doesn't work as expected if a github actions consumer repo has a `config` directory.
		// ConfigurationSource = configurationSource ?? (
		// 	fileSystem.Directory.Exists(LocalConfigurationDirectory)
		// 		? ConfigurationSource.Local : ConfigurationSource.Embedded
		// 	);
		if (configurationSource is { } source)
			ConfigurationSource = source;
		else
		{
			string[] spotChecks = ["navigation.yml", "versions.yml", "products.yml", "assembler.yml", "search.yml"];
			var defaultSource = fileSystem.Directory.Exists(LocalConfigurationDirectory)
				&& spotChecks.All(f => fileSystem.File.Exists(Path.Join(LocalConfigurationDirectory, f)))
				? ConfigurationSource.Local
				: ConfigurationSource.Embedded;
			ConfigurationSource = defaultSource;
		}

		if (ConfigurationSource == ConfigurationSource.Local && !fileSystem.Directory.Exists(LocalConfigurationDirectory))
			throw new Exception(
				$"Required directory form {nameof(ConfigurationSource)}.{nameof(ConfigurationSource.Local)} directory {LocalConfigurationDirectory} does not exist."
			);

		if (ConfigurationSource == ConfigurationSource.Remote && !fileSystem.Directory.Exists(AppDataConfigurationDirectory))
			throw new Exception(
				$"Required directory form {nameof(ConfigurationSource)}.{nameof(ConfigurationSource.Remote)} directory {AppDataConfigurationDirectory} does not exist."
			);

		var path = GetAppDataPath("git-ref.txt");
		if (_fileSystem.File.Exists(path))
			GitReference = _fileSystem.File.ReadAllText(path);
		else if (ConfigurationSource == ConfigurationSource.Remote)
			throw new Exception($"Can not read git-ref.txt in directory {LocalConfigurationDirectory}");

		if (ConfigurationSource == ConfigurationSource.Remote)
		{
			_logger.LogInformation(
				"{ConfigurationSource}: git ref '{GitReference}', in {Directory}",
				$"{nameof(ConfigurationSource)}.{nameof(ConfigurationSource.Remote)}",
				GitReference,
				AppDataConfigurationDirectory
			);
		}

		if (ConfigurationSource == ConfigurationSource.Local)
		{
			_logger.LogInformation(
				"{ConfigurationSource}: located {Directory}",
				$"{nameof(ConfigurationSource)}.{nameof(ConfigurationSource.Local)}",
				LocalConfigurationDirectory
			);
		}
		if (ConfigurationSource == ConfigurationSource.Embedded)
		{
			_logger.LogInformation(
				"{ConfigurationSource} using embedded in binary configuration",
				$"{nameof(ConfigurationSource)}.{nameof(ConfigurationSource.Embedded)}"
			);
		}

		VersionFile = Locate("versions.yml");
		ProductsFile = Locate("products.yml");
		AssemblerFile = Locate("assembler.yml");
		NavigationFile = Locate("navigation.yml");
		LegacyUrlMappingsFile = Locate("legacy-url-mappings.yml");
		// reading from synonyms.yml is temporary. If you spot this again as a future reader, feel free to remove it.
		SearchFile = Locate("search.yml", "synonyms.yml");
		RelatedLearningFile = Locate("related-learning.yml");
	}

	public bool SkipPrivateRepositories { get; }

	// Embedded configuration has no file of its own; it is written once to a directory unique to this instance so
	// parallel processes and tests sharing ApplicationData never collide.
	[field: AllowNull, MaybeNull]
	private IDirectoryInfo EmbeddedDirectory
	{
		get
		{
			if (field is not null)
				return field;
			field = _fileSystem.DirectoryInfo.New(
				Path.Join(Paths.ApplicationData.FullName, "config-runtime", Guid.NewGuid().ToString("N"))
			);
			field.Create();
			return field;
		}
	}

	public IFileInfo NavigationFile { get; }

	/// <summary>
	/// <c>navigation_preview.yml</c>, for environments with the <c>navigation-preview</c> flag; <c>navigation.yml</c>
	/// when the configuration has no preview file (older remote refs).
	/// </summary>
	[field: AllowNull, MaybeNull]
	public IFileInfo NavigationPreviewFile => field ??= Locate("navigation_preview.yml", "navigation.yml");

	public IFileInfo VersionFile { get; }

	public IFileInfo ProductsFile { get; }

	public IFileInfo AssemblerFile { get; }

	public IFileInfo LegacyUrlMappingsFile { get; }

	public IFileInfo SearchFile { get; }

	public IFileInfo RelatedLearningFile { get; }

	/// <summary>
	/// The file <paramref name="fileName"/> (or <paramref name="fallback"/>) is read from: in place for local and remote
	/// configuration, written out once for embedded configuration.
	/// </summary>
	private IFileInfo Locate(string fileName, string? fallback = null)
	{
		if (ConfigurationSource is ConfigurationSource.Local or ConfigurationSource.Remote)
		{
			var (directory, candidate, candidateFallback) = ConfigurationSource == ConfigurationSource.Local
				? (LocalConfigurationDirectory, GetLocalPath(fileName), fallback is null ? null : GetLocalPath(fallback))
				: (AppDataConfigurationDirectory, GetAppDataPath(fileName), fallback is null ? null : GetAppDataPath(fallback));
			if (_fileSystem.File.Exists(candidate))
				return _fileSystem.FileInfo.New(candidate);
			if (candidateFallback is not null && _fileSystem.File.Exists(candidateFallback))
				return _fileSystem.FileInfo.New(candidateFallback);
			throw new Exception($"Can not read {fileName} in directory {directory}");
		}

		using var stream = GetEmbeddedStream(fileName, fallback);
		var fi = _fileSystem.FileInfo.New(Path.Join(EmbeddedDirectory.FullName, fileName));
		_fileSystem.File.WriteAllText(fi.FullName, stream.ReadToEnd());
		// The IFileInfo was created before the file was written; refresh so Exists and Length reflect the written file.
		fi.Refresh();
		return fi;
	}

	private StreamReader GetEmbeddedStream(string fileName, string? fallback = null)
	{
		var assembly = typeof(ConfigurationFileProvider).Assembly;
		var resourceStream = assembly.GetManifestResourceStream($"{_assemblyName}.{fileName}")
			?? (fallback is not null ? assembly.GetManifestResourceStream($"{_assemblyName}.{fallback}") : null)
			?? throw new Exception($"Can not read embedded resource {fileName}");
		return new StreamReader(resourceStream, leaveOpen: false);
	}

	public static string AppDataConfigurationDirectory { get; } = Path.Join(Paths.ApplicationData.FullName, "config-clone", "config");
	public static string LocalConfigurationDirectory { get; } = Path.Join(Directory.GetCurrentDirectory(), "config");

	private static string GetLocalPath(string file) => Path.Join(LocalConfigurationDirectory, file);
	private static string GetAppDataPath(string file) => Path.Join(AppDataConfigurationDirectory, file);
}

public static class ConfigurationFileProviderServiceCollectionExtensions
{
	public static IServiceCollection AddConfigurationFileProvider(
		this IServiceCollection services,
		bool skipPrivateRepositories,
		ConfigurationSource? configurationSource,
		Action<IServiceCollection, ConfigurationFileProvider> configure
	)
	{
		using var sp = services.BuildServiceProvider();
		var logFactory = sp.GetRequiredService<ILoggerFactory>();
		var provider = new ConfigurationFileProvider(
			logFactory,
			new ConfigurationFileSystem(),
			skipPrivateRepositories,
			configurationSource
		);
		_ = services.AddSingleton(provider);
		configure(services, provider);
		return services;
	}
}
