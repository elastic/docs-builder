// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Text;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Floating;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

/// <summary>What <see cref="BaselineSite.Build"/> assembles.</summary>
internal sealed record BaselineOptions
{
	/// <summary>Mirrors <c>--skip-private-repositories</c>: the private repositories leave the config, the filtered navigation file and the checkouts.</summary>
	public bool SkipPrivateRepositories { get; init; }

	/// <summary>Reads <c>navigation_preview.yml</c> and builds the top nav, as an environment with the NAVIGATION_PREVIEW flag does.</summary>
	public bool NavigationPreview { get; init; }

	/// <summary>The files each stub docset lists, besides its <c>index.md</c>.</summary>
	public IReadOnlyList<string> StubFiles { get; init; } = ["overview.md"];

	public FloatingSectionRegistry? FloatingSections { get; init; }

	/// <summary>Repositories that are checked out but not part of the site, as a repository dropped from the checkouts would be.</summary>
	public IReadOnlySet<string> MissingCheckouts { get; init; } = ImmutableHashSet<string>.Empty;

	/// <summary>Replaces the text of the navigation file after the private repositories were filtered out.</summary>
	public Func<string, string>? RewriteNavigation { get; init; }
}

internal sealed record BaselineSiteResult(
	SiteNavigation Navigation,
	SiteNavigationFile NavigationFile,
	AssemblyConfiguration Configuration,
	TestDocumentationSetContext SiteContext,
	string NavigationText,
	IReadOnlyCollection<string> Repositories
);

/// <summary>
/// The site navigation of the frozen copies of <c>assembler.yml</c>, <c>navigation.yml</c> and <c>navigation_preview.yml</c>
/// in <c>Baseline/</c>, over one stub docset per repository. The copies are frozen on purpose: the shipped files change with
/// every onboarded repository, and these tests lock what the navigation code does with a fixed input.
/// </summary>
internal static class BaselineSite
{
	public const string Environment = "prod";

	public static string ReadBaseline(string name)
	{
		var assembly = typeof(BaselineSite).Assembly;
		var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith($".Baseline.{name}", StringComparison.Ordinal));
		using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
		return reader.ReadToEnd();
	}

	private static (MockFileSystem FileSystem, ConfigurationFileProvider Provider) CreateProvider(
		bool skipPrivateRepositories,
		bool navigationPreview
	)
	{
		var configFileSystem = new MockFileSystem();
		foreach (var file in new[] { "assembler.yml", "navigation.yml", "navigation_preview.yml" })
		{
			configFileSystem.AddFile(
				Path.Join(ConfigurationFileProvider.AppDataConfigurationDirectory, file),
				new MockFileData(ReadBaseline(file))
			);
		}
		// the config directory of a remote source is the only one the provider's scoped file system lets a mock file system serve
		configFileSystem.AddFile(
			Path.Join(ConfigurationFileProvider.AppDataConfigurationDirectory, "git-ref.txt"),
			new MockFileData("baseline")
		);
		// read and copied, never parsed here
		foreach (var file in new[] { "versions.yml", "products.yml", "legacy-url-mappings.yml", "search.yml", "related-learning.yml" })
		{
			configFileSystem.AddFile(
				Path.Join(ConfigurationFileProvider.AppDataConfigurationDirectory, file),
				new MockFileData(string.Empty)
			);
		}

		var provider = new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(configFileSystem),
			skipPrivateRepositories,
			ConfigurationSource.Remote
		);
		if (navigationPreview)
			_ = provider.UseNavigationPreview();
		return (configFileSystem, provider);
	}

	/// <summary>
	/// The cross-link mappings the assembler derives from the navigation file (<c>AssembleSources.GetTocMappings</c>), read through an
	/// <see cref="AssembleContext"/> of <paramref name="environment"/>. The <c>demo</c> environment turns the navigation preview on.
	/// </summary>
	public static FrozenDictionary<Uri, NavigationTocMapping> TocMappings(string environment, bool skipPrivateRepositories)
	{
		var (configFileSystem, provider) = CreateProvider(skipPrivateRepositories, navigationPreview: false);
		using var _ = provider;
		var configuration = AssemblyConfiguration.Create(provider);
		var configurationContext = new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints { Elasticsearch = new ElasticsearchEndpoint { Uri = new Uri("http://localhost:9200") } },
			ConfigurationFileProvider = provider,
			VersionsConfiguration = new VersionsConfiguration
			{
				VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>()
			},
			ProductsConfiguration = new ProductsConfiguration
			{
				Products = FrozenDictionary<string, Product>.Empty,
				PublicReferenceProducts = FrozenDictionary<string, Product>.Empty,
				ProductDisplayNames = FrozenDictionary<string, string>.Empty
			},
			SearchTuning = new SearchTuning { Synonyms = [], Rules = [], DiminishTerms = [] },
			LegacyUrlMappings = new LegacyUrlMappingConfiguration { Mappings = [] }
		};
		var fileSystem = new MockFileSystem();
		var collector = new TestDiagnosticsCollector();
		var checkouts = new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem);
		var context = new AssembleContext(configuration, configurationContext, environment, collector, checkouts, "/checkouts", "/output");
		return AssembleSources.GetTocMappings(context);
	}

	public static BaselineSiteResult Build(BaselineOptions? options = null)
	{
		options ??= new BaselineOptions();
		var (configFileSystem, provider) = CreateProvider(options.SkipPrivateRepositories, options.NavigationPreview);
		using var _ = provider;
		var configuration = AssemblyConfiguration.Create(provider);
		var navigationFileInfo = provider.CreateNavigationFile(configuration);
		var navigationText = configFileSystem.File.ReadAllText(navigationFileInfo.FullName).Replace("\r\n", "\n");
		if (options.RewriteNavigation is { } rewrite)
			navigationText = rewrite(navigationText);

		var fileSystem = new MockFileSystem();
		var siteContext = new TestDocumentationSetContext(
			fileSystem,
			fileSystem.DirectoryInfo.New("/docs"),
			fileSystem.DirectoryInfo.New("/output"),
			fileSystem.FileInfo.New("/docs/navigation.yml")
		)
		{ BuildType = BuildType.Assembler };
		var navigationFile = SiteNavigationFile.Deserialize(navigationText, siteContext.Collector, "/docs/navigation.yml");

		var stubRepositories = StubDocsets.Write(fileSystem, navigationFile, configuration, options);
		var documentationSets = new List<IDocumentationSetNavigation>();
		foreach (var repository in stubRepositories)
		{
			var context = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, $"/checkouts/current/{repository}");
			var docset = DocumentationSetFile.LoadAndResolve(
				context.Collector,
				fileSystem.FileInfo.New($"/checkouts/current/{repository}/docs/docset.yml"),
				new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
			);
			documentationSets.Add(
				new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance)
			);
		}

		var navigation = new SiteNavigation(
			navigationFile,
			siteContext,
			documentationSets,
			configuration.Environments[Environment].PathPrefix,
			options.NavigationPreview,
			options.FloatingSections
		);
		return new BaselineSiteResult(navigation, navigationFile, configuration, siteContext, navigationText, stubRepositories);
	}
}

/// <summary>
/// One stub docset per repository the navigation file mounts or the assembler config names: a docset with an index page and
/// <see cref="BaselineOptions.StubFiles"/>, and a nested <c>toc.yml</c> with the same pages for every table of contents the
/// navigation file declares below the repository root, so <c>repo://a/b</c> exists as a toc inside the toc <c>a</c>.
/// </summary>
internal static class StubDocsets
{
	public static IReadOnlyCollection<string> Write(
		MockFileSystem fileSystem,
		SiteNavigationFile navigationFile,
		AssemblyConfiguration configuration,
		BaselineOptions options
	)
	{
		var tocsByRepository = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
		foreach (var source in SiteNavigationFile.GetAllDeclaredSources(navigationFile).Concat(
			SiteNavigationFile.GetPhantomPrefixes(navigationFile)
		))
			Add(tocsByRepository, source.Scheme, SourcePath(source));
		foreach (var (name, repository) in configuration.AvailableRepositories)
		{
			if (!repository.Skip)
				Add(tocsByRepository, name, string.Empty);
		}

		foreach (var (repository, tocs) in tocsByRepository)
		{
			if (options.MissingCheckouts.Contains(repository))
				continue;
			WriteRepository(fileSystem, repository, tocs, options.StubFiles);
		}
		return [.. tocsByRepository.Keys.Where(r => !options.MissingCheckouts.Contains(r))];
	}

	private static string SourcePath(Uri source) => (source.Host + source.AbsolutePath).Trim('/');

	private static void Add(SortedDictionary<string, SortedSet<string>> map, string repository, string path)
	{
		if (!map.TryGetValue(repository, out var set))
			map[repository] = set = [];
		_ = set.Add(path);
	}

	private static void WriteRepository(MockFileSystem fileSystem, string repository, SortedSet<string> tocs, IReadOnlyList<string> files)
	{
		var baseDir = $"/checkouts/current/{repository}";
		fileSystem.AddDirectory($"{baseDir}/.git");
		var allTocs = new SortedSet<string>(tocs.Where(t => t.Length > 0), StringComparer.Ordinal);
		foreach (var toc in tocs.Where(t => t.Length > 0))
		{
			var segments = toc.Split('/');
			for (var i = 1; i < segments.Length; i++)
				_ = allTocs.Add(string.Join('/', segments.Take(i)));
		}

		fileSystem.AddFile(
			$"{baseDir}/docs/docset.yml",
			new MockFileData(TocYaml(string.Empty, allTocs, files, "project: " + repository + "\n"))
		);
		WritePages(fileSystem, $"{baseDir}/docs", $"{repository}", files);
		foreach (var toc in allTocs)
		{
			var dir = $"{baseDir}/docs/{toc}";
			fileSystem.AddFile($"{dir}/toc.yml", new MockFileData(TocYaml(toc, allTocs, files, string.Empty)));
			WritePages(fileSystem, dir, $"{repository} {toc}", files);
		}
	}

	private static string TocYaml(string toc, SortedSet<string> allTocs, IReadOnlyList<string> files, string header)
	{
		var sb = new StringBuilder(header).Append("toc:\n  - file: index.md\n");
		foreach (var file in files)
			_ = sb.Append("  - file: ").Append(file).Append('\n');
		var prefix = toc.Length == 0 ? string.Empty : toc + "/";
		foreach (var child in allTocs.Where(
			t => t.StartsWith(prefix, StringComparison.Ordinal) && t.Length > prefix.Length && !t[prefix.Length..].Contains('/')
		))
			_ = sb.Append("  - toc: ").Append(child[prefix.Length..]).Append('\n');
		return sb.ToString();
	}

	private static void WritePages(MockFileSystem fileSystem, string dir, string title, IReadOnlyList<string> files)
	{
		fileSystem.AddFile($"{dir}/index.md", new MockFileData($"# {title}"));
		foreach (var file in files)
			fileSystem.AddFile($"{dir}/{file}", new MockFileData($"# {title} {Path.GetFileNameWithoutExtension(file)}"));
	}
}
