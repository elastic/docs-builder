// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Search;
using Elastic.Documentation.Tests.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;
using YamlDotNet.Core;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>How a corpus is bent out of shape: the mutations the loaders must survive.</summary>
internal static class Mutations
{
	public static string Apply(string text, int kind, double at, int n)
	{
		var lines = text.Split('\n');
		var line = Math.Min((int)(at * lines.Length), lines.Length - 1);

		string Replace(Func<string, string> edit) => string.Join('\n', lines.Select((l, i) => i == line ? edit(l) : l));

		return kind switch
		{
			// drop a line
			0 => string.Join('\n', lines.Where((_, i) => i != line)),
			// truncate
			1 => text[..Math.Min((int)(at * text.Length), text.Length)],
			// swap two neighbouring lines
			2 =>
				string.Join(
					'\n',
					lines.Select((l, i) => i == line ? lines[(line + 1) % lines.Length] : i == (line + 1) % lines.Length ? lines[line] : l)
				),
			// an unknown key at the top
			3 => $"zz_unknown_{n}: {n}\n{text}",
			// repeat a line
			4 => Replace(l => l + "\n" + l),
			// lose the indentation of a line
			5 => Replace(l => l.TrimStart()),
			// a line of junk
			6 => Replace(_ => n % 2 == 0 ? "- [: {" : "key: *missing"),
			// a tab where the indentation was
			7 => Replace(l => "\t" + l),
			// swap the first colon of a line for a semicolon
			8 => Replace(l => l.Contains(':', StringComparison.Ordinal) ? l.Replace(':', ';') : l),
			// a value that is not what it was: a list for a scalar, an empty value
			_ =>
				Replace(
					l => l.Contains(':', StringComparison.Ordinal)
						? l[..(l.IndexOf(':', StringComparison.Ordinal) + 1)] + (n % 2 == 0 ? " [1, 2]" : " ~")
						: l
				)
		};
	}

	/// <summary>The text itself and eight positions of each of ten mutations: the same eighty-one texts on every run, so a failure is a text, not a seed.</summary>
	public static IEnumerable<string> Corpus(string text) =>
		new[] { text }.Concat(
			Enumerable.Range(0, 10).SelectMany(kind => Enumerable.Range(0, 8).Select(p => Apply(text, kind, (p + 0.5) / 8, p)))
		);

	/// <summary>Runs <paramref name="check"/> over <see cref="Corpus"/>; a failure names the mutation and carries the text.</summary>
	public static void Each(string text, Action<string> check)
	{
		var i = 0;
		foreach (var variant in Corpus(text))
		{
			try
			{
				check(variant);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(
					$"mutation {i} (kind {(i - 1) / 8}, position {(i - 1) % 8}) of the corpus:\n{variant}",
					e
				);
			}
			i++;
		}
	}

	public static Gen<string> Of(string text) =>
		Gen.Select(Gen.Int[0, 9], Gen.Double[0, 1], Gen.Int[0, 1000], (kind, at, n) => Apply(text, kind, at, n));
}

/// <summary>
/// Loader laws: a loader is a function from text to a <see cref="Result{T}"/>, so loading the same text twice is the same result, what a writer writes
/// loads back as itself, and no text, however bent, makes a loader fail in a way its caller does not expect. The corpus is the shipped baseline
/// configuration; the variants are the same text with a line dropped, swapped, repeated, truncated, a key added or a value replaced. Fixed seeds.
/// </summary>
// These laws run in parallel: loaders are thread-safe (ConcurrentLoadTests proves it). The NullReferenceException and the 100% CPU hang seen while they were
// first written were texts, not races (LoaderTerminationTests holds them): a non-mapping entry in navigation.yml that no loop consumed, and empty values.
public class LoaderProperties
{
	internal static readonly Dictionary<string, string> BaselineConfig =
		Directory.EnumerateFiles(BaselineFile("config")).ToDictionary(Path.GetFileName, f => File.ReadAllText(f))!;

	internal static ConfigurationFileProvider ProviderWith(string file, string text)
	{
		var fs = new MockFileSystem();
		var directory = ConfigurationFileProvider.AppDataConfigurationDirectory;
		foreach (var (name, content) in BaselineConfig)
			fs.AddFile(Path.Join(directory, name), new MockFileData(name == file ? text : content));
		fs.AddFile(Path.Join(directory, "git-ref.txt"), new MockFileData("golden"));
		return new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(fs),
			false,
			ConfigurationSource.Remote
		);
	}

	internal sealed class NoEnvironment : IEnvironmentVariables
	{
		public string? GetEnvironmentVariable(string name) => null;

		public bool IsRunningOnCI => false;
	}

	/// <summary>One public YAML entry point: the file it reads and a way to run it over that file's text, giving the record (or null) and what it reported.</summary>
	internal sealed record YamlEntry(string Name, string File, Func<string, (object? Value, IReadOnlyList<Diagnostic> Diagnostics)> Load);

	private static (object? Value, IReadOnlyList<Diagnostic> Diagnostics) Of<T>(Result<T> result) where T : class =>
		(result.Value, result.Diagnostics);

	internal static IEnumerable<YamlEntry> YamlEntries()
	{
		yield return new YamlEntry(
			"AssemblyLoader.Load",
			"assembler.yml",
			text => Of(AssemblyLoader.Load(ProviderWith("assembler.yml", text), new NoEnvironment()))
		);
		yield return new YamlEntry(
			"CatalogLoader (products)",
			"products.yml",
			text => Of(CatalogLoader.Load(ProviderWith("products.yml", text)))
		);
		yield return new YamlEntry(
			"CatalogLoader (versions)",
			"versions.yml",
			text => Of(CatalogLoader.Load(ProviderWith("versions.yml", text)))
		);
		yield return new YamlEntry(
			"CatalogLoader (legacy url mappings)",
			"legacy-url-mappings.yml",
			text => Of(CatalogLoader.Load(ProviderWith("legacy-url-mappings.yml", text)))
		);
		yield return new YamlEntry(
			"SearchTuningLoader.Load",
			"search.yml",
			text => Of(SearchTuningLoader.Load(ProviderWith("search.yml", text)))
		);
		yield return new YamlEntry(
			"RelatedLearningLoader.Load",
			"related-learning.yml",
			text => Of(RelatedLearningLoader.Load(ProviderWith("related-learning.yml", text)))
		);
		yield return new YamlEntry(
			"GlobalConfigurationLoader (navigation)",
			"navigation.yml",
			text => Of(GlobalConfigurationLoader.LoadOnce(ProviderWith("navigation.yml", text)))
		);
		yield return new YamlEntry("SiteNavigationLoader.Load", "navigation.yml", text =>
		{
			var provider = ProviderWith("navigation.yml", text);
			var assembly = AssemblyLoader.Create(BaselineProvider());
			return Of(SiteNavigationLoader.Load(text, provider.NavigationFile, assembly, skipPrivateRepositories: false));
		});
		yield return new YamlEntry("CodexSiteLoader.Load", "codex.example.yml", text =>
		{
			var fs = new MockFileSystem(new Dictionary<string, MockFileData> { ["/codex.yml"] = new(text) });
			return Of(CodexSiteLoader.Load(fs.FileInfo.New("/codex.yml"), "/codex.yml", forPublish: false));
		});
	}

	public static IEnumerable<string> Entries() => YamlEntries().Select(e => e.Name);

	/// <summary>Exceptions a loader is documented to let out, by entry point; any other exception is a loader that fails where it should report.</summary>
	private static readonly Dictionary<string, Type[]> Documented = new()
	{
		// "A document the parser rejects still throws (its message is what the caller reports)"
		["SiteNavigationLoader.Load"] = [typeof(YamlException)]
	};

	private static void AssertOnlyDocumentedExceptions(YamlEntry entry, string text)
	{
		try
		{
			_ = entry.Load(text);
		}
		catch (Exception e)
		{
			(Documented.GetValueOrDefault(entry.Name) ?? [])
				.Any(t => t.IsInstanceOfType(e))
				.Should()
				.BeTrue($"{entry.Name} may only throw what it documents, but it threw {e.GetType().FullName}: {e.Message}");
		}
	}

	/// <summary>
	/// Law: loading the same text twice gives equal records and the same diagnostics, for the shipped file and for every bent variant of it.
	/// Consequence if broken: a loader depends on something that is not its input (a clock, a shared cache, an enumeration order), so a build is not
	/// reproducible and a golden snapshot is not a snapshot.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Entries))]
	public void LoadingTheSameTextTwice_GivesEqualResults(string entryName)
	{
		var entry = YamlEntries().Single(e => e.Name == entryName);
		Mutations.Each(BaselineConfig[entry.File], text =>
		{
			(object? Value, IReadOnlyList<Diagnostic> Diagnostics) first, second;
			try
			{
				first = entry.Load(text);
				second = entry.Load(text);
			}
			catch (Exception)
			{
				// a documented throw is the same throw twice; what may be thrown is the next law's business
				return;
			}
			second.Value.Should().Be(first.Value);
			second.Diagnostics.Should().Equal(first.Diagnostics);
		});
	}

	/// <summary>
	/// Law: no text, however bent, makes a loader throw anything but what it documents; the result is a record with its findings or a failure with
	/// its diagnostics.
	/// Consequence if broken: a typo in a shipped configuration file ends a build or a Lambda with a stack trace instead of a message with a file and a line.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Entries))]
	public void ALoaderNeverThrowsOnBentText(string entryName)
	{
		var entry = YamlEntries().Single(e => e.Name == entryName);
		Mutations.Each(BaselineConfig[entry.File], text => AssertOnlyDocumentedExceptions(entry, text));
	}

	/// <summary>The docset: the baseline tree with <c>_docset.yml</c> replaced; the metadata read and the full load.</summary>
	[Test]
	public void TheDocumentationSetLoader_NeverThrowsOnBentText_AndLoadsTwiceEqual()
	{
		var provider = BaselineProvider();
		var catalog = CatalogLoader.Load(provider).OrThrow();
		var fs = DocumentationSetGoldenTests.CreateDocsTree();
		var original = fs.File.ReadAllText("/docs/_docset.yml");
		var file = fs.FileInfo.New("/docs/_docset.yml");
		var scoped = new ScopedFileSystem(fs, "/docs");

		Result<LoadedDocumentationSet> Load() =>
			DocumentationSetLoader.Load(file, file.Directory!, fs, scoped, catalog.VersioningSystems, catalog.Products, new NoEnvironment());

		Mutations.Each(original, text =>
		{
			fs.File.WriteAllText("/docs/_docset.yml", text);
			var metadata = DocumentationSetLoader.LoadMetadata(file);
			(metadata.Value is not null || metadata.HasErrors).Should().BeTrue();

			var first = Load();
			var second = Load();
			second.Value.Should().Be(first.Value);
			second.Diagnostics.Should().Equal(first.Diagnostics);
		});
	}

	/// <summary>
	/// Law (regression cases found by random mutation): a key a file leaves empty, or a key cut off mid-word, is reported as a finding and never reaches a
	/// mapping or a rule as a null collection.
	/// Consequence if broken: a half-saved assembler.yml or docset.yml ends the build with a NullReferenceException and no file or line.
	/// </summary>
	[Test]
	public void AKeyTheFileLeavesEmpty_IsReportedNotThrown()
	{
		var assembler = BaselineConfig["assembler.yml"];
		var cut = assembler[..(assembler.IndexOf("search-team:", StringComparison.Ordinal) + "search-team:\n    privat".Length)];
		AssertOnlyDocumentedExceptions(YamlEntries().Single(e => e.Name == "AssemblyLoader.Load"), cut);

		var provider = BaselineProvider();
		var catalog = CatalogLoader.Load(provider).OrThrow();
		var fs = DocumentationSetGoldenTests.CreateDocsTree();
		var file = fs.FileInfo.New("/docs/_docset.yml");
		var scoped = new ScopedFileSystem(fs, "/docs");
		foreach (var key in new[] { "exclude", "cross_links", "release_notes", "subs", "extensions", "products", "toc", "api", "cta" })
		{
			fs.File.WriteAllText("/docs/_docset.yml", $"project: test\n{key}:\n");
			var act =
				() => DocumentationSetLoader.Load(
					file,
					file.Directory!,
					fs,
					scoped,
					catalog.VersioningSystems,
					catalog.Products,
					new NoEnvironment()
				);
			act.Should().NotThrow($"`{key}:` with nothing under it is an empty value, not a crash");
		}
	}

	/// <summary>The changelog configuration: loaded from text with the baseline catalog, twice, and never with an exception.</summary>
	[Test]
	[Arguments("kibana.changelog.yml")]
	[Arguments("elasticsearch.changelog.yml")]
	public async Task TheChangelogConfigurationLoader_NeverThrowsOnBentText_AndLoadsTwiceEqual(string name)
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
		const string configPath = "/repo/docs/changelog.yml";
		var original = await File.ReadAllTextAsync(BaselineFile("changelog", name));

		async Task<(object? Value, IReadOnlyList<Diagnostic> Diagnostics)> Load(string text)
		{
			var fs = new MockFileSystem(new Dictionary<string, MockFileData> { [configPath] = new(text) }, "/repo");
			var result = await new ChangelogConfigurationLoader(NullLoggerFactory.Instance, context, fs).Load(
				configPath,
				CancellationToken.None
			);
			return (result.Value, result.Diagnostics);
		}

		foreach (var text in Mutations.Corpus(original))
		{
			var first = await Load(text);
			var second = await Load(text);
			second.Value.Should().Be(first.Value, "loading the same text twice is the same record");
			second.Diagnostics.Should().Equal(first.Diagnostics);
		}
	}

	// ── record -> text -> record ──────────────────────────────────────────────

	private static IReadOnlyList<T>? NoneIfEmpty<T>(IReadOnlyList<T>? list) => list is { Count: 0 } ? null : list;

	/// <summary>What the file says of an entry: an empty list is written as no list, and the PR number is the file name's, not a field of the file.</summary>
	private static ChangelogEntry AsWritten(ChangelogEntry e) =>
#pragma warning disable CS0618 // the obsolete target is read from old files and never written
		e with
		{
			Prs = NoneIfEmpty(e.Prs),
			Issues = NoneIfEmpty(e.Issues),
			Areas = NoneIfEmpty(e.Areas),
			Pr = null,
			Products = e.Products?.Select(p => p with { Target = null }).ToList()
		};
#pragma warning restore CS0618

	private static BundledEntry AsWritten(BundledEntry e) =>
		e with
		{
			Prs = NoneIfEmpty(e.Prs),
			Issues = NoneIfEmpty(e.Issues),
			Areas = NoneIfEmpty(e.Areas),
			Products = NoneIfEmpty(e.Products)
		};

	private static Bundle AsWritten(Bundle b) =>
		b with { Entries = [.. b.Entries.Select(AsWritten)], ExcludeEntries = [.. b.ExcludeEntries.Select(AsWritten)] };

	/// <summary>
	/// Law: a changelog entry written as YAML and loaded again, under the file name it carries, is the entry that was written, with nothing reported.
	/// Consequence if broken: what the CLI writes is not what the build reads, and a note changes between the author's checkout and the release page.
	/// </summary>
	[Test]
	public void AChangelogEntry_RoundTripsThroughItsYaml() =>
		ChangelogEntryExamples.Generated<ChangelogEntry>().Check(
			entry =>
			{
				var text = ReleaseNotesLoader.Serialize(entry);

				var loaded = ReleaseNotesLoader.LoadEntry(text, entry.FileName!);

				loaded.Value.Should().Be(AsWritten(entry));
				loaded.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
			},
			iter: 200
		);

	/// <summary>
	/// Law: a bundle written as YAML and loaded again is the bundle that was written, and a bundle that satisfied the rules still does.
	/// Consequence if broken: a published bundle is not the bundle that was assembled.
	/// </summary>
	[Test]
	public void ABundle_RoundTripsThroughItsYaml() =>
		BundleExamples.Generated<Bundle>().Check(
			bundle =>
			{
				var text = ReleaseNotesLoader.Serialize(bundle);

				var loaded = ReleaseNotesLoader.LoadBundle(text, "bundle.yaml");

				loaded.Value.Should().Be(AsWritten(bundle));
				loaded.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
			},
			iter: 200
		);

	/// <summary>
	/// Law: a links.json written and loaded again is the manifest that was written.
	/// Consequence if broken: the links a build publishes are not the links another build resolves against.
	/// </summary>
	[Test]
	public void ALinkManifest_RoundTripsThroughItsJson() =>
		DomainGen.Of<LinkManifest>(o => o.Normalize<LinkManifest>(LinkExamples.Manifest)).Check(
			manifest =>
			{
				var loaded = LinkLoader.LoadManifest(LinkLoader.Serialize(manifest), "links.json");

				loaded.Value.Should().Be(manifest);
				loaded.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
			},
			iter: 200
		);

	/// <summary>
	/// Law: a link-index.json written and loaded again is the registry that was written (its ETag is the object's, not the file's).
	/// Consequence if broken: the registry a publisher uploads is not the one the next publisher adds to.
	/// </summary>
	[Test]
	public void ALinkRegistry_RoundTripsThroughItsJson() =>
		DomainGen.Of<LinkRegistry>(o => o.Normalize<LinkRegistry>(r => LinkExamples.Registry(r))).Check(
			registry =>
			{
				var loaded = LinkLoader.LoadRegistry(LinkLoader.Serialize(registry), "link-index.json", registry.ETag);

				loaded.Value.Should().Be(registry);
				loaded.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
			},
			iter: 200
		);

	// ── arbitrary text ────────────────────────────────────────────────────────

	/// <summary>
	/// Law: the release-notes and link loaders return a Result for any text: a bent entry, bundle, manifest or registry, or plain garbage, is a failure
	/// with diagnostics or a record with findings, never an exception.
	/// Consequence if broken: one corrupt object in the pool or the bucket stops a whole render, scrub or cross-link check.
	/// </summary>
	[Test]
	public void ReleaseNotesAndLinkLoaders_NeverThrowOnBentText() =>
		Gen.Select(
			ChangelogEntryExamples.Generated<ChangelogEntry>(),
			BundleExamples.Generated<Bundle>(),
			DomainGen.Of<LinkManifest>(o => o.Normalize<LinkManifest>(LinkExamples.Manifest)),
			DomainGen.Of<LinkRegistry>(o => o.Normalize<LinkRegistry>(r => LinkExamples.Registry(r))),
			Gen.Select(Gen.Int[0, 9], Gen.Double[0, 1], Gen.Int[0, 1000], Gen.String)
		).Check(
			x =>
			{
				var (entry, bundle, manifest, registry, (kind, at, n, garbage)) = x;

				string Bend(string text) => kind == 9 ? garbage : Mutations.Apply(text, kind, at, n);

				var results = new Action[]
				{
					() => _ = ReleaseNotesLoader.LoadEntry(Bend(ReleaseNotesLoader.Serialize(entry)), "1-a.yaml"),
					() => _ = ReleaseNotesLoader.LoadBundle(Bend(ReleaseNotesLoader.Serialize(bundle)), "bundle.yaml"),
					() => _ = new BundleLoader(new MockFileSystem()).LoadBundlesFromContent([
						("9.0.0.yaml", Bend(ReleaseNotesLoader.Serialize(bundle)))
					]),
					() => _ = LinkLoader.LoadManifest(Bend(LinkLoader.Serialize(manifest)), "links.json"),
					() => _ = LinkLoader.LoadRegistry(Bend(LinkLoader.Serialize(registry)), "link-index.json")
				};
				foreach (var run in results)
					run.Should().NotThrow();
			},
			iter: 150
		);
}
