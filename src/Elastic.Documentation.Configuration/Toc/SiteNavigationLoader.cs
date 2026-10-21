// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Converters;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Serialization;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

// SiteNavigationDto and PhantomRegistrationDto keep their original names: the type's name is part of how an unknown key is reported.
internal sealed class SiteNavigationDto
{
	[YamlPropertyName("phantoms")]
	public List<PhantomRegistrationDto> Phantoms { get; set; } = [];

	[YamlPropertyName("toc")]
	public SiteTableOfContentsDto TableOfContents { get; set; } = [];
}

internal sealed class PhantomRegistrationDto
{
	[YamlPropertyName("toc")]
	public string Source { get; set; } = null!;
}

/// <summary>The top-level <c>toc:</c> entries of <c>navigation.yml</c>, bound by <see cref="SiteTableOfContentsCollectionYamlConverter"/>.</summary>
internal sealed class SiteTableOfContentsDto : List<SiteNavigationEntry>;

/// <summary>Loads <c>navigation.yml</c> into a <see cref="SiteNavigationDefinition"/> and validates it.</summary>
public static class SiteNavigationLoader
{
	/// <summary>Parses <paramref name="yaml"/>, unchecked. Entries without a <c>toc:</c> key, or with an unparsable source, throw. Callers outside this assembly use <see cref="Load"/>.</summary>
	internal static SiteNavigationDefinition Deserialize(string yaml, string? sourceName = null) =>
		FromDto(ConfigurationDocument.Read<SiteNavigationDto>(yaml, sourceName));

	/// <summary>Parses <paramref name="yaml"/>, unchecked, reporting unknown keys to <paramref name="collector"/> instead of failing the parse.</summary>
	internal static SiteNavigationDefinition Deserialize(string yaml, IDiagnosticsCollector collector, string sourceName) =>
		FromDto(ConfigurationDocument.Read<SiteNavigationDto>(yaml, collector, sourceName));

	private static SiteNavigationDefinition FromDto(SiteNavigationDto dto) =>
		new()
		{
			// `phantoms:` with nothing under it, an entry with nothing in it, or a `toc:` with no value is an empty one, not a null
			Phantoms =
			[
				.. (dto.Phantoms ?? []).Where(p => p is not null).Select(p => new PhantomRegistration { Source = p.Source ?? string.Empty })
			],
			TableOfContents = dto.TableOfContents ?? []
		};

	// language=yaml
	private const string LocalDocsBuilderToc =
		"""
		toc:
		  - toc: docs-builder://
		    path_prefix: reference/docs-builder
		    children:
		      - toc: docs-builder://development
		        path_prefix: reference/docs-builder/dev
		        children:
		          - toc: docs-builder://development/link-validation
		            path_prefix: reference/docs-builder/dev/link-val
		""";

	/// <summary>
	/// The navigation an assembler build uses: when private repositories are skipped, their mounts and phantoms are
	/// removed, and a local docs-builder checkout is mounted so docset changes can be tried in an assembled build. The result is
	/// checked against <see cref="SiteNavigationConstraints"/> once, by <see cref="SiteNavigationDefinition.WithoutRepositories"/>.
	/// </summary>
	public static Result<SiteNavigationDefinition> ForAssembly(
		SiteNavigationDefinition navigation,
		AssemblyConfiguration assembly,
		bool skipPrivateRepositories,
		string file = ""
	)
	{
		if (assembly.PrivateRepositories.Count == 0 || !skipPrivateRepositories)
			return navigation.WithoutRepositories(new HashSet<string>(), file: file);

		var mountLocalDocsBuilder = assembly.AvailableRepositories.TryGetValue("docs-builder", out var docsBuilder)
			&& docsBuilder is { Skip: false, Path: not null };
		return navigation.WithoutRepositories(
			new HashSet<string>(assembly.PrivateRepositories.Keys),
			mountLocalDocsBuilder ? Deserialize(LocalDocsBuilderToc).TableOfContents : null,
			file
		);
	}

	/// <summary>
	/// Where each mounted table of contents is published: its source (normalized to end in <c>/</c>) and path prefix.
	/// The assembler resolves cross-links through these. A bare <c>toc: name</c> without <c>path_prefix</c> is
	/// published under its name; any other mount without one is reported and falls back to its source path.
	/// </summary>
	public static FrozenDictionary<Uri, NavigationTocMapping> GetTocMappings(
		SiteNavigationDefinition navigation,
		IFileInfo navigationFile,
		IDiagnosticsCollector collector
	)
	{
		var mappings = new Dictionary<Uri, NavigationTocMapping>();
		foreach (var entry in navigation.TableOfContents)
		{
			if (entry is SiteTableOfContentsRef toc)
				AddTocMappings(toc, navigationFile, collector, mappings);
			else
				foreach (var child in entry.Children)
					AddTocMappings(child, navigationFile, collector, mappings);
		}
		return mappings.ToFrozenDictionary();
	}

	private static void AddTocMappings(
		SiteTableOfContentsRef toc,
		IFileInfo navigationFile,
		IDiagnosticsCollector collector,
		Dictionary<Uri, NavigationTocMapping> mappings
	)
	{
		var source = toc.ShorthandPath is { } shorthand
			? ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, shorthand)
			: toc.Source.OriginalString;
		source = source.EndsWith("://", StringComparison.OrdinalIgnoreCase) ? source : source.TrimEnd('/') + "/";
		if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
		{
			Emit(collector, navigationFile, toc.Position, $"Source toc entry is not a valid uri: {source}");
			return;
		}

		var pathPrefix = !string.IsNullOrEmpty(toc.PathPrefix) ? toc.PathPrefix : toc.ShorthandPath;
		// A missing path_prefix is reported by SiteNavigationConstraints; the mapping falls back to the source path.
		var sourcePrefix = $"{sourceUri.Host}/{sourceUri.AbsolutePath.TrimStart('/')}";

		mappings[sourceUri] = new NavigationTocMapping
		{
			Source = sourceUri,
			SourcePathPrefix = string.IsNullOrEmpty(pathPrefix) ? sourcePrefix : pathPrefix
		};
		foreach (var child in toc.Children)
			AddTocMappings(child, navigationFile, collector, mappings);
	}

	// Mirrors YamlStreamReader's positioned errors, so the diagnostics match the parser that used to report them.
	private static void Emit(IDiagnosticsCollector collector, IFileInfo navigationFile, SourcePosition? position, string message) =>
		collector.Write(new Diagnostic
		{
			Severity = Severity.Error,
			File = navigationFile.FullName,
			Message = message,
			Line = position?.Line,
			Column = position?.Column,
			Length = position is { } p ? p.Column - p.EndColumn : null
		});

	/// <summary>
	/// Loads the navigation an assembler build uses: parses <paramref name="yaml"/>, filters it as <see cref="ForAssembly"/> does, and checks it
	/// against <see cref="SiteNavigationConstraints"/> once. The result carries every violation (positioned in <paramref name="navigationFile"/>);
	/// it has errors when a rule at error severity failed, and the caller decides whether to stop.
	/// </summary>
	public static Result<SiteNavigationDefinition> Load(
		string yaml,
		IFileInfo navigationFile,
		AssemblyConfiguration assembly,
		bool skipPrivateRepositories,
		IDiagnosticsCollector? collector = null
	) =>
		// A document the parser rejects still throws (its message is what the caller reports); every rule violation rides in the result.
		// With a collector, an unknown key is written to it as a positioned diagnostic instead of failing the parse.
		ForAssembly(
			collector is null ? Deserialize(yaml, navigationFile.FullName) : Deserialize(yaml, collector, navigationFile.FullName),
			assembly,
			skipPrivateRepositories,
			navigationFile.FullName
		);
}

internal class SiteTableOfContentsCollectionYamlConverter(
	SiteTableOfContentsRefYamlConverter refConverter
) : YamlConverter<SiteTableOfContentsDto>
{
	// Parameterless constructor required by the SharpYaml source generator for its static converter
	// fallback field; the real instance is always supplied via YamlSerializerOptions.Converters at
	// runtime, which the generator consults first.
	public SiteTableOfContentsCollectionYamlConverter() : this(new SiteTableOfContentsRefYamlConverter()) { }

	/// <summary>The keys of a mounted <c>toc:</c> entry that are read; any other key is dropped and kept as <c>UnknownKeys</c>.</summary>
	internal static readonly string[] TocEntryKeys = ["toc", "path_prefix", "children", "island", "navigation_title"];

	private static readonly string[] KnownEntryKeys =
	[
		"section",
		"external",
		"children",
		"dropdown",
		"toc",
		"path_prefix",
		"island",
		"navigation_title"
	];
	private static readonly string[] KnownDropdownKeys = ["title", "url"];

	public override SiteTableOfContentsDto Read(YamlReader reader)
	{
		var collection = new SiteTableOfContentsDto();

		if (reader.TokenType != YamlTokenType.StartSequence)
		{
			reader.Skip();
			return collection;
		}

		_ = reader.Read(); // consume StartSequence
		while (reader.TokenType != YamlTokenType.EndSequence)
		{
			var entry = ParseTopLevelEntry(reader);
			if (entry.HasValue)
				collection.Add(entry.Value);
		}
		_ = reader.Read(); // consume EndSequence

		return collection;
	}

	private SiteNavigationEntry? ParseTopLevelEntry(YamlReader reader)
	{
		if (reader.TokenType != YamlTokenType.StartMapping)
		{
			// An entry that is not a mapping (a bare scalar, a nested list) is skipped; left unread, the caller's loop would spin on it forever.
			reader.Skip();
			return null;
		}

		var mappingStart = reader.Start;
		_ = reader.Read(); // consume StartMapping
		var dictionary = new Dictionary<string, object?>();

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();

			if (!KnownEntryKeys.Contains(key))
			{
				reader.SkipOrReportUnmapped(typeof(SiteNavigationEntry), KnownEntryKeys, key, null);
				// Kept as written so the entry still lists it among its UnknownKeys.
				dictionary[key] = null;
				continue;
			}

			object? value = null;
			if (reader.TokenType == YamlTokenType.Scalar)
			{
				value = reader.ScalarValue;
				_ = reader.Read();
			}
			else if (reader.TokenType == YamlTokenType.StartSequence)
			{
				if (key is "children")
				{
					var childrenList = new List<SiteTableOfContentsRef>();
					_ = reader.Read(); // consume StartSequence
					while (reader.TokenType != YamlTokenType.EndSequence)
					{
						var child = refConverter.Read(reader);
						if (child is not null)
							childrenList.Add(child);
					}
					_ = reader.Read(); // consume EndSequence
					value = childrenList;
				}
				else if (key is "dropdown")
					value = ReadDropdown(reader);
				else
					reader.Skip();
			}
			else if (reader.TokenType == YamlTokenType.StartMapping)
				reader.Skip();

			dictionary[key] = value;
		}
		var mappingEnd = reader.End;
		_ = reader.Read(); // consume EndMapping

		if (dictionary.TryGetValue("section", out var sectionTitleVal) && sectionTitleVal is string sectionTitle)
		{
			var externalUrl = dictionary.TryGetValue("external", out var extVal) && extVal is string e && !string.IsNullOrEmpty(e)
				? e
				: null;
			IReadOnlyList<SiteTableOfContentsRef> children = dictionary.TryGetValue("children", out var childrenObj)
				&& childrenObj is List<SiteTableOfContentsRef> refs ? refs : [];
			IReadOnlyList<SiteDropdownLinkRef> dropdownLinks = dictionary.TryGetValue("dropdown", out var dropdownObj)
				&& dropdownObj is List<SiteDropdownLinkRef> dLinks ? dLinks : [];
			return new SiteSectionRef(sectionTitle, externalUrl, children, dropdownLinks)
			{
				UnknownKeys = UnknownYamlKeys.Of(dictionary.Keys, "section", "external", "children", "dropdown"),
				Position = PositionOf(mappingStart, mappingEnd)
			};
		}

		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string sourceString)
		{
			var uriString = sourceString.Contains("://") ? sourceString : $"docs-content://{sourceString}";

			// Wrapped in a YamlException: unlike YamlDotNet, SharpYaml does not wrap exceptions
			// thrown from custom converters, and callers catch YamlException to turn malformed
			// config into diagnostics. The original InvalidOperationException is the inner exception.
			if (!Uri.TryCreate(uriString, UriKind.Absolute, out var source))
			{
				var invalidUri = new InvalidOperationException($"Invalid TOC source: '{sourceString}' could not be parsed as a URI");
				throw new YamlException(invalidUri.Message, invalidUri);
			}

			var pathPrefix = dictionary.TryGetValue("path_prefix", out var pathValue) && pathValue is string path ? path : string.Empty;

			IReadOnlyList<SiteTableOfContentsRef> children = dictionary.TryGetValue("children", out var childrenObj2)
				&& childrenObj2 is List<SiteTableOfContentsRef> tocRefs ? tocRefs : [];

			var island = dictionary.TryGetValue("island", out var islandObj)
				&& islandObj is string islandStr
				&& bool.TryParse(islandStr, out var islandBool)
				&& islandBool;

			var navigationTitle = dictionary.TryGetValue("navigation_title", out var titleObj)
				&& titleObj is string title
				&& !string.IsNullOrWhiteSpace(title) ? title : null;

			return new SiteTableOfContentsRef(source, pathPrefix, children, island, navigationTitle)
			{
				ShorthandPath = sourceString.Contains("://") ? null : sourceString,
				Position = PositionOf(mappingStart, mappingEnd),
				UnknownKeys = UnknownYamlKeys.Of(dictionary.Keys, TocEntryKeys),
				IslandRaw = dictionary.TryGetValue("island", out var rawIsland) ? rawIsland as string : null
			};
		}

		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		throw new YamlException($"toc entry has no 'toc:' key and will be ignored. " + $"Found keys: {keys}. Check for typos.");
	}

	private static List<SiteDropdownLinkRef> ReadDropdown(YamlReader reader)
	{
		var dropdownList = new List<SiteDropdownLinkRef>();
		_ = reader.Read(); // consume StartSequence
		while (reader.TokenType != YamlTokenType.EndSequence)
		{
			if (reader.TokenType != YamlTokenType.StartMapping)
			{
				reader.Skip();
				continue;
			}
			_ = reader.Read(); // consume StartMapping
			string? itemTitle = null;
			string? itemUrl = null;
			while (reader.TokenType != YamlTokenType.EndMapping)
			{
				var itemKey = reader.GetScalarValue();
				_ = reader.Read();
				if (!KnownDropdownKeys.Contains(itemKey))
					reader.SkipOrReportUnmapped(typeof(SiteDropdownLinkRef), KnownDropdownKeys, itemKey, null);
				else if (reader.TokenType == YamlTokenType.Scalar)
				{
					var itemValue = reader.ScalarValue;
					_ = reader.Read();
					if (itemKey is "title")
						itemTitle = itemValue;
					else if (itemKey is "url")
						itemUrl = itemValue;
				}
				else
					reader.Skip();
			}
			_ = reader.Read(); // consume EndMapping
			if (itemTitle is not null && itemUrl is not null)
				dropdownList.Add(new SiteDropdownLinkRef(itemTitle, itemUrl));
		}
		_ = reader.Read(); // consume EndSequence
		return dropdownList;
	}

	public override void Write(YamlWriter writer, SiteTableOfContentsDto value) =>
		throw new NotSupportedException(
			"SiteTableOfContentsDto YAML serialization is not supported; navigation.yml is only ever read from YAML."
		);

	// SharpYaml's Mark.Line/Column are 0-based; +1 to match the 1-based positions diagnostics carry.
	internal static SourcePosition PositionOf(Mark start, Mark end) => new(start.Line + 1, start.Column + 1, end.Line + 1, end.Column + 1);
}

public class SiteTableOfContentsRefYamlConverter : YamlConverter<SiteTableOfContentsRef>
{
	public override SiteTableOfContentsRef? Read(YamlReader reader)
	{
		if (reader.TokenType != YamlTokenType.StartMapping)
		{
			// Not a mapping: skipped, or the caller's children loop would never advance past it.
			reader.Skip();
			return null;
		}

		var mappingStart = reader.Start;
		_ = reader.Read(); // consume StartMapping
		var dictionary = new Dictionary<string, object?>();

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();

			if (!SiteTableOfContentsCollectionYamlConverter.TocEntryKeys.Contains(key))
			{
				reader.SkipOrReportUnmapped(
					typeof(SiteTableOfContentsRef),
					SiteTableOfContentsCollectionYamlConverter.TocEntryKeys,
					key,
					null
				);
				// Kept as written so the entry still lists it among its UnknownKeys.
				dictionary[key] = null;
				continue;
			}

			object? value = null;
			if (reader.TokenType == YamlTokenType.Scalar)
			{
				value = reader.ScalarValue;
				_ = reader.Read();
			}
			else if (reader.TokenType == YamlTokenType.StartSequence)
			{
				if (key == "children")
				{
					var childrenList = new List<SiteTableOfContentsRef>();
					_ = reader.Read(); // consume StartSequence
					while (reader.TokenType != YamlTokenType.EndSequence)
					{
						var child = Read(reader);
						if (child is not null)
							childrenList.Add(child);
					}
					_ = reader.Read(); // consume EndSequence
					value = childrenList;
				}
				else
					reader.Skip();
			}
			else if (reader.TokenType == YamlTokenType.StartMapping)
				reader.Skip();

			dictionary[key] = value;
		}
		var mappingEnd = reader.End;
		_ = reader.Read(); // consume EndMapping

		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string sourceString)
		{
			var uriString = sourceString.Contains("://") ? sourceString : $"docs-content://{sourceString}";

			// Wrapped in a YamlException for the same reason as in SiteTableOfContentsCollectionYamlConverter.
			if (!Uri.TryCreate(uriString, UriKind.Absolute, out var source))
			{
				var invalidUri = new InvalidOperationException($"Invalid TOC source: '{sourceString}' could not be parsed as a URI");
				throw new YamlException(invalidUri.Message, invalidUri);
			}

			var pathPrefix = dictionary.TryGetValue("path_prefix", out var pathValue) && pathValue is string path ? path : string.Empty;

			IReadOnlyList<SiteTableOfContentsRef> children = dictionary.TryGetValue("children", out var childrenObj)
				&& childrenObj is List<SiteTableOfContentsRef> tocRefs ? tocRefs : [];

			var island = dictionary.TryGetValue("island", out var islandObj)
				&& islandObj is string islandStr
				&& bool.TryParse(islandStr, out var islandBool)
				&& islandBool;

			var navigationTitle = dictionary.TryGetValue("navigation_title", out var titleObj)
				&& titleObj is string title
				&& !string.IsNullOrWhiteSpace(title) ? title : null;

			return new SiteTableOfContentsRef(source, pathPrefix, children, island, navigationTitle)
			{
				ShorthandPath = sourceString.Contains("://") ? null : sourceString,
				Position = SiteTableOfContentsCollectionYamlConverter.PositionOf(mappingStart, mappingEnd),
				UnknownKeys = UnknownYamlKeys.Of(dictionary.Keys, SiteTableOfContentsCollectionYamlConverter.TocEntryKeys),
				IslandRaw = dictionary.TryGetValue("island", out var rawIsland) ? rawIsland as string : null
			};
		}

		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		throw new YamlException($"toc entry has no 'toc:' key and will be ignored. " + $"Found keys: {keys}. Check for typos.");
	}

	public override void Write(YamlWriter writer, SiteTableOfContentsRef value) =>
		throw new NotSupportedException(
			"SiteTableOfContentsRef YAML serialization is not supported; navigation.yml is only ever read from YAML."
		);
}
