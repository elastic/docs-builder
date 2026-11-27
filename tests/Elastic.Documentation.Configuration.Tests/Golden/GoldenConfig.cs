// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Testing.Linked;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// Loads the global configuration files the way the CLI does, through a <see cref="ConfigurationFileProvider"/>, from the
/// frozen copies under <c>Golden/Baseline</c> and with one file at a time swapped for a test's own text.
/// </summary>
internal static partial class GoldenConfig
{
	public const string Assembler = "assembler.yml";
	public const string Navigation = "navigation.yml";
	public const string NavigationPreview = "navigation_preview.yml";
	public const string Products = "products.yml";
	public const string Versions = "versions.yml";
	public const string LegacyUrlMappings = "legacy-url-mappings.yml";
	public const string RelatedLearning = "related-learning.yml";
	public const string Search = "search.yml";

	private static readonly string[] AllFiles =
	[
		Assembler,
		Navigation,
		NavigationPreview,
		Products,
		Versions,
		LegacyUrlMappings,
		RelatedLearning,
		Search
	];

	public static string BaselineText(string fileName) =>
		File.ReadAllText(Path.Join(AppContext.BaseDirectory, "Golden", "Baseline", fileName));

	/// <summary>A provider over a mock file system that holds the baseline, with <paramref name="replace"/> swapped in.</summary>
	public static ConfigurationFileProvider CreateProvider(
		string? replaceFile = null,
		string? replaceWith = null,
		bool skipPrivateRepositories = false,
		string? replaceStoredAs = null
	)
	{
		var fileSystem = new MockFileSystem();
		foreach (var file in AllFiles)
		{
			var text = file == replaceFile ? replaceWith : BaselineText(file);
			if (text is null)
				continue;
			fileSystem.AddFile(
				Path.Join(ConfigurationFileProvider.AppDataConfigurationDirectory, file == replaceFile ? replaceStoredAs ?? file : file),
				new MockFileData(text)
			);
		}

		// the Local source reads <cwd>/config, which the scoped file system does not cover under a test host; Remote reads app data
		fileSystem.AddFile(Path.Join(ConfigurationFileProvider.AppDataConfigurationDirectory, "git-ref.txt"), new MockFileData("golden"));
		return new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(fileSystem),
			skipPrivateRepositories,
			ConfigurationSource.Remote
		);
	}

	public static AssemblyConfiguration LoadAssembler(bool skipPrivateRepositories = false)
	{
		using var provider = CreateProvider(skipPrivateRepositories: skipPrivateRepositories);
		var config = AssemblyConfiguration.Create(provider);
		RegisterPrivateRepositories(config);
		return config;
	}

	/// <summary>
	/// Whatever the loaded assembler configuration marks <c>private: true</c> is written as <c>private-repo-N</c> in this
	/// test's snapshots. The baseline already uses those names; this keeps a name of the input from reaching a snapshot.
	/// </summary>
	public static void RegisterPrivateRepositories(object? model)
	{
		if (model is AssemblyConfiguration config)
			SnapshotScrubbing.AddPrivateRepositories(config.AllPrivateRepositoryNames);
	}

	public static VersioningSystemCatalog LoadVersions()
	{
		using var provider = CreateProvider();
		return provider.CreateVersionConfiguration();
	}

	public static ProductCatalog LoadProducts()
	{
		using var provider = CreateProvider();
		return provider.CreateProducts(provider.CreateVersionConfiguration());
	}

	/// <summary>
	/// What loading <paramref name="load"/> does with <paramref name="yaml"/> in place of <paramref name="file"/>: the
	/// model dump, or <c>throws</c> and the exception chain (type and message, no stack trace).
	/// </summary>
	public static string Outcome(
		string file,
		string? yaml,
		Func<ConfigurationFileProvider, object?> load,
		params string[] skipProperties
	) => Outcome(file, yaml, load, null, false, skipProperties);

	/// <summary>The same, with a model type that <paramref name="summarize"/> names written as that one line.</summary>
	public static string Outcome(
		string file,
		string? yaml,
		Func<ConfigurationFileProvider, object?> load,
		Func<object, string?> summarize
	) => Outcome(file, yaml, load, summarize, false);

	public static string Outcome(
		string file,
		string? yaml,
		Func<ConfigurationFileProvider, object?> load,
		Func<object, string?>? summarize,
		bool skipPrivateRepositories,
		params string[] skipProperties
	)
	{
		try
		{
			using var provider = CreateProvider(file, yaml, skipPrivateRepositories);
			var model = load(provider);
			RegisterPrivateRepositories(model);
			return ModelDump.ToText(model, summarize, skipProperties);
		}
		catch (Exception e)
		{
			return Throws(e);
		}
	}

	private static string Throws(Exception e)
	{
		var sb = new StringBuilder("throws\n");
		for (var current = e; current is not null; current = current.InnerException)
		{
			var message = ScrubPaths(current.Message);
			_ = sb.Append(current == e ? "" : "inner ").Append(current.GetType().Name).Append(": ").Append(message.TrimEnd()).Append('\n');
		}
		return sb.ToString();
	}

	// the per-provider runtime folder (a fresh Guid under the app-data folder) and the folder a missing file was looked for in
	private static string ScrubPaths(string message) =>
		InDirectory()
			.Replace(RuntimeDirectory().Replace(message.Replace('\\', '/'), "{config-runtime}"), "in directory {config-dir}")
			.TrimEnd();

	[GeneratedRegex("(?:[A-Za-z]:)?/[^\\n]*?config-runtime/[0-9a-f]{32}", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex RuntimeDirectory();

	[GeneratedRegex("in directory [^\\n]+", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex InDirectory();
}
