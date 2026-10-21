// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Every public way to load a record from a file runs its constraint set exactly once and returns a <see cref="Result{T}"/>
/// (or reports to a collector). The raw parse helpers are internal, so there is no public path that skips the check.
/// </summary>
public class LoaderEntryPointTests
{
	private sealed class NoEnvironment : IEnvironmentVariables
	{
		public string? GetEnvironmentVariable(string name) => null;

		public bool IsRunningOnCI => false;
	}

	/// <summary>A provider over the baseline config with <paramref name="file"/> replaced by <paramref name="content"/>.</summary>
	private static ConfigurationFileProvider ProviderWith(string file, string content)
	{
		var directory = Directory.CreateTempSubdirectory("loader-entry-point").FullName;
		foreach (var source in Directory.EnumerateFiles(BaselineFile("config")))
			File.Copy(source, Path.Join(directory, Path.GetFileName(source)));
		File.WriteAllText(Path.Join(directory, file), content);
		return ProviderFor(directory);
	}

	[Test]
	public void CatalogLoader_ChecksEachPartOnce()
	{
		var result = CatalogLoader.Load(BaselineProvider());
		var catalog = result.OrThrow();

		var expected = ProductCatalogConstraints.All.Report(catalog.Products).ToDiagnostics("products.yml");
		foreach (var diagnostic in expected)
			result.Diagnostics.Count(d => d.RuleId == diagnostic.RuleId && d.Message == diagnostic.Message).Should().Be(1);
	}

	[Test]
	public void CatalogLoader_AParserFailureIsAnErrorDiagnostic()
	{
		var result = CatalogLoader.Load(ProviderWith("legacy-url-mappings.yml", "mappings:\n  /a:\n    product: not-a-product\n"));

		result.Value.Should().BeNull();
		result.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.File == "legacy-url-mappings.yml");
	}

	[Test]
	public void SearchTuningLoader_ReportsAViolationOnce()
	{
		var result = SearchTuningLoader.Load(ProviderWith("search.yml", "synonyms:\n  - [elastic search, es]\n"));

		result.Diagnostics.Count(d => d.RuleId == SearchTuningConstraints.SynonymTermsSingleWord.Name).Should().Be(1);
	}

	[Test]
	public void RelatedLearningLoader_ReportsAViolationOnceAndFailsOnError()
	{
		var result = RelatedLearningLoader.Load(
			ProviderWith("related-learning.yml", "links:\n  sample:\n    title: A title\n    url: not-a-url\n")
		);

		result.Value.Should().BeNull();
		result.Diagnostics.Count(d => d.RuleId == RelatedLearningConstraints.LinkUrlAbsoluteHttp.Name).Should().Be(1);
	}

	[Test]
	public void AssemblyLoader_LoadYamlChecksTheText()
	{
		var result = AssemblyLoader.LoadYaml(
			"environments:\n  prod:\n    uri: https://example.test\n",
			"assembler.yml",
			environment: new NoEnvironment()
		);

		// the entry the lambda uses: the loader's own finding and the rules' violations ride in one Result, each once
		result
			.Diagnostics
			.Count(d => d.Severity == Severity.Warning && d.Message.Contains("does not declare content_source"))
			.Should()
			.Be(1);
		result.Diagnostics.Select(d => (d.RuleId, d.Message)).Should().OnlyHaveUniqueItems();
	}

	[Test]
	public void CodexSiteLoader_PublishingAddsItsRuleOnce_ReadingDoesNot()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData> { ["/codex.yml"] = new("site_prefix: /\ngroups: []") });
		var file = fs.FileInfo.New("/codex.yml");

		var read = CodexSiteLoader.Load(file, "/codex.yml", forPublish: false);
		var publish = CodexSiteLoader.Load(file, "/codex.yml", forPublish: true);

		read.HasErrors.Should().BeFalse();
		publish.Value.Should().BeNull();
		publish.Diagnostics.Count(d => d.RuleId == "environment-required").Should().Be(1);
	}

	[Test]
	public void CodexSiteLoader_AMissingFileIsAnErrorDiagnostic()
	{
		var fs = new MockFileSystem();

		var result = CodexSiteLoader.Load(fs.FileInfo.New("/nope.yml"), "/nope.yml", forPublish: false);

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error && d.Message.Contains("not found"));
	}

	private static (MockFileSystem Fs, ScopedFileSystem Scoped) DocsetTree(string docset)
	{
		var fs = new MockFileSystem();
		fs.AddFile("/docs/docset.yml", new MockFileData(docset));
		fs.AddFile("/docs/index.md", new MockFileData("# Hello"));
		return (fs, new ScopedFileSystem(fs, "/docs"));
	}

	private const string DocsetWithUnknownReleaseNotesProduct =
		"project: test\nrelease_notes:\n  - product: not-a-product\ntoc:\n  - file: index.md\n";

	[Test]
	public void DocumentationSetLoader_LoadChecksTheDocsetOnce()
	{
		var catalog = CatalogLoader.Load(BaselineProvider()).OrThrow();
		var (fs, scoped) = DocsetTree(DocsetWithUnknownReleaseNotesProduct);

		var result = DocumentationSetLoader.Load(
			fs.FileInfo.New("/docs/docset.yml"),
			fs.DirectoryInfo.New("/docs"),
			fs,
			scoped,
			catalog.VersioningSystems,
			catalog.Products,
			new NoEnvironment()
		);

		// a docset with errors still loads (the build reports and continues), and each violation is reported exactly once
		result.Value.Should().NotBeNull();
		result.Diagnostics.Count(d => d.RuleId == DocumentationSetConstraints.ReleaseNotesProductKnown.Name).Should().Be(1);
		result.Value!.Resolved.ReleaseNotesProducts.Should().BeEmpty();
	}

	[Test]
	public void DocumentationSetLoader_LoadMetadataIsDeliberatelyUnchecked()
	{
		var (fs, _) = DocsetTree(
			"project: test\nregistry: internal\n" + DocsetWithUnknownReleaseNotesProduct.Replace("project: test\n", "")
		);

		var result = DocumentationSetLoader.LoadMetadata(fs.FileInfo.New("/docs/docset.yml"));

		// the registry is readable, the table of contents is not resolved, and no rule ran
		result.Value!.Registry.Should().Be("internal");
		result.Value.TableOfContents.Should().BeEmpty();
		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public void DocumentationSetLoader_LoadMetadataReportsUnreadableYamlAsAnError()
	{
		var (fs, _) = DocsetTree("project: [unclosed");

		var result = DocumentationSetLoader.LoadMetadata(fs.FileInfo.New("/docs/docset.yml"));

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error);
	}

	[Test]
	public async Task ChangelogConfigurationLoader_TheDefaultsPassThroughTheSetAndBreakNoRule()
	{
		var provider = BaselineProvider();
		var catalog = CatalogLoader.Load(provider).OrThrow();
		var context = new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints { Elasticsearch = ElasticsearchEndpoint.Default },
			ConfigurationFileProvider = provider,
			Catalog = new GlobalCatalog(catalog.VersioningSystems, catalog.Products, LegacyUrlMappingCatalog.None),
			SearchTuning = SearchTuning.None
		};
		var collector = new CapturingDiagnosticsCollector();
		var loader = new ChangelogConfigurationLoader(NullLoggerFactory.Instance, context, new MockFileSystem());

		// no file: the built-in defaults are returned, after the same check a file gets, and nothing is reported
		var configuration = (await loader.Load("/repo/docs/changelog.yml", CancellationToken.None)).EmitTo(collector);

		configuration.Should().BeSameAs(ChangelogConfiguration.Default);
		collector.Diagnostics.Should().BeEmpty();
	}

	/// <summary>
	/// The boundary rule, enforced: a public method of a loader never hands a domain record to its caller from text or a file without a
	/// check: it returns a <see cref="Result{T}"/> (the changelog configuration loader included; no loader is exempt for taking a collector).
	/// </summary>
	[Test]
	public void NoPublicLoaderMethodReturnsAnUncheckedDomainRecord()
	{
		var domain = typeof(Result<>).Assembly;
		var offenders = typeof(AssemblyLoader)
			.Assembly
			.GetExportedTypes()
			.Where(t => t.Name.EndsWith("Loader", StringComparison.Ordinal))
			.SelectMany(
				t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly),
				(t, m) => (Type: t, Method: m)
			)
			.Where(x => !x.Method.IsSpecialName)
			.Where(x => IsDomainRecord(Unwrap(x.Method.ReturnType), domain))
			// a derivation from a record that already exists (SiteNavigationLoader.ForAssembly) is not a load; Phase 2b makes those checked domain operations
			.Where(x => !x.Method.GetParameters().Any(p => IsDomainRecord(p.ParameterType, domain)))
			.Select(x => $"{x.Type.Name}.{x.Method.Name} returns {x.Method.ReturnType.Name}")
			.ToList();

		offenders.Should().BeEmpty("a loader's public surface returns Result<T> (checked); raw parse helpers are internal");
	}

	/// <summary>
	/// The release-notes boundary, enforced: the file's shape (the DTOs, the raw deserializer) is internal, so the only way outside the assembly to get a
	/// changelog entry or bundle from text is <see cref="ReleaseNotesLoader"/> / <see cref="BundleLoader"/>, which return a checked <see cref="Result{T}"/>,
	/// including for the collections of records they hand out.
	/// </summary>
	[Test]
	public void ReleaseNotesDtosAreInternal_AndTheirLoadersReturnCheckedResults()
	{
		var configuration = typeof(ReleaseNotesLoader).Assembly;
		configuration
			.GetExportedTypes()
			.Where(
				t => t.Name.EndsWith("Dto", StringComparison.Ordinal) && t.Namespace?.EndsWith("ReleaseNotes", StringComparison.Ordinal) ==
					true
			)
			.Should()
			.BeEmpty("a release-notes DTO is the file's shape, not the model");
		configuration
			.GetExportedTypes()
			.Should()
			.NotContain(t => t.Name == "ReleaseNotesSerialization", "the raw deserializer is the loader's");

		var domain = typeof(Result<>).Assembly;
		var offenders = new[] { typeof(ReleaseNotesLoader), typeof(BundleLoader) }
			.SelectMany(
				t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly),
				(t, m) => (Type: t, Method: m)
			)
			.Where(x => !x.Method.IsSpecialName)
			.Where(x => IsDomainRecord(UnwrapElement(x.Method.ReturnType), domain))
			// a derivation from records that already exist (resolving a bundle's entries, merging loaded bundles) is not a load
			.Where(x => !x.Method.GetParameters().Any(p => IsDomainRecord(UnwrapElement(p.ParameterType), domain)))
			.Select(x => $"{x.Type.Name}.{x.Method.Name} returns {x.Method.ReturnType.Name}")
			.ToList();

		offenders.Should().BeEmpty("the release-notes loaders hand out records only inside a Result<T>");
	}

	// the element of a collection of records: List<ChangelogEntry>, IReadOnlyList<LoadedBundle>
	private static Type UnwrapElement(Type type)
	{
		type = Unwrap(type);
		return type.IsGenericType && type.GetGenericArguments().Length == 1 && typeof(System.Collections.IEnumerable).IsAssignableFrom(type)
			? type.GetGenericArguments()[0]
			: type;
	}

	private static Type Unwrap(Type type)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
			return Unwrap(type.GetGenericArguments()[0]);
		return Nullable.GetUnderlyingType(type) ?? type;
	}

	// Result<T> is the checked form; anything else declared in the domain assembly is a record (or a value of one).
	private static bool IsDomainRecord(Type type, Assembly domain) =>
		type.Assembly == domain && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>)) && type != typeof(Result<>);
}
