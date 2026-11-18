// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

public sealed class SiteNavigationDto
{
	[YamlPropertyName("phantoms")]
	public List<PhantomRegistrationDto> Phantoms { get; set; } = [];

	[YamlPropertyName("toc")]
	public SiteTableOfContentsDto TableOfContents { get; set; } = [];
}

public sealed class PhantomRegistrationDto
{
	[YamlPropertyName("toc")]
	public string Source { get; set; } = null!;
}

/// <summary>The top-level <c>toc:</c> entries of <c>navigation.yml</c>, bound by <see cref="SiteTableOfContentsCollectionYamlConverter"/>.</summary>
public sealed class SiteTableOfContentsDto : List<SiteNavigationEntry>;

/// <summary>Loads <c>navigation.yml</c> into a <see cref="SiteNavigationDefinition"/>.</summary>
public static class SiteNavigationLoader
{
	/// <summary>
	/// Parses <paramref name="yaml"/>, unchecked. A document the parser rejects (an entry without a <c>toc:</c> key, an unparsable source, an unknown key
	/// when there is no collector) throws as it always has.
	/// </summary>
	public static SiteNavigationDefinition Deserialize(string yaml, string? sourceName = null) =>
		FromDto(ConfigurationFileProvider.Deserialize<SiteNavigationDto>(yaml, sourceName));

	/// <summary>Parses <paramref name="yaml"/>, unchecked, reporting unknown keys to <paramref name="collector"/> instead of failing the parse.</summary>
	public static SiteNavigationDefinition Deserialize(string yaml, IDiagnosticsCollector collector, string sourceName) =>
		FromDto(ConfigurationFileProvider.Deserialize<SiteNavigationDto>(yaml, collector, sourceName));

	private static SiteNavigationDefinition FromDto(SiteNavigationDto dto) =>
		new()
		{
			Phantoms = [.. dto.Phantoms.Select(p => new PhantomRegistration { Source = p.Source })],
			TableOfContents = dto.TableOfContents
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
	/// removed (<see cref="SiteNavigationDefinition.WithoutRepositories"/>), and a local docs-builder checkout is mounted so docset changes
	/// can be tried in an assembled build. The result is checked against <see cref="SiteNavigationConstraints"/> once and carries every finding,
	/// positioned in <paramref name="file"/>; the navigation rides along when a rule of error severity is broken, and the caller decides whether to stop.
	/// </summary>
	public static Loaded<SiteNavigationDefinition> ForAssembly(
		SiteNavigationDefinition navigation,
		AssemblyConfiguration assembly,
		bool skipPrivateRepositories,
		string file
	)
	{
		if (assembly.PrivateRepositories.Count == 0 || !skipPrivateRepositories)
			return navigation.Check(file);

		var mountLocalDocsBuilder = assembly.AvailableRepositories.TryGetValue("docs-builder", out var docsBuilder)
			&& docsBuilder is { Skip: false, Path: not null };
		return navigation
			.WithoutRepositories(new HashSet<string>(assembly.PrivateRepositories.Keys))
			.WithAppended(mountLocalDocsBuilder ? Deserialize(LocalDocsBuilderToc).TableOfContents : [])
			.Check(file);
	}

	/// <summary>
	/// Where each mounted table of contents is published: its source (normalized to end in <c>/</c>) and path prefix.
	/// The assembler resolves cross-links through these. A mount without a path prefix is reported by
	/// <see cref="SiteNavigationConstraints"/> and falls back to its source path.
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
			if (entry.Value is SiteTableOfContentsRef toc)
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
		var source = SiteNavigationConstraints.NormalizedSource(toc);
		if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
		{
			Emit(collector, navigationFile, toc.Position, $"Source toc entry is not a valid uri: {source}");
			return;
		}

		var sourcePrefix = $"{sourceUri.Host}/{sourceUri.AbsolutePath.TrimStart('/')}";
		mappings[sourceUri] = new NavigationTocMapping { Source = sourceUri, SourcePathPrefix = toc.DeclaredPathPrefix ?? sourcePrefix };
		foreach (var child in toc.Children)
			AddTocMappings(child, navigationFile, collector, mappings);
	}

	// Mirrors YamlStreamReader's positioned errors, so the diagnostics match the reader that used to report them.
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
}

public class SiteTableOfContentsCollectionYamlConverter(
	SiteTableOfContentsRefYamlConverter refConverter
) : YamlConverter<SiteTableOfContentsDto>
{
	// Parameterless constructor required by the SharpYaml source generator for its static converter
	// fallback field; the real instance is always supplied via YamlSerializerOptions.Converters at
	// runtime, which the generator consults first.
	public SiteTableOfContentsCollectionYamlConverter() : this(new SiteTableOfContentsRefYamlConverter()) { }

	private static readonly string[] KnownEntryKeys =
	[
		"section",
		"external",
		"floating",
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
			reader.Skip();
			return null;
		}

		// The position is that of the mapping's start token, which is where the toc mapping reader always pointed its diagnostics.
		var mappingStart = reader.Start;
		var mappingStartEnd = reader.End;
		_ = reader.Read(); // consume StartMapping
		var dictionary = new Dictionary<string, object?>();

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();

			if (!KnownEntryKeys.Contains(key))
			{
				reader.SkipOrReportUnmapped(typeof(SiteNavigationEntry), KnownEntryKeys, key, null);
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
			// floating: <kind> mounts the floating sections of that kind (for example every API) as the section's children
			// when the build has any. Without them the section keeps its other behaviour, such as an external link.
			var floatingKind = dictionary.TryGetValue("floating", out var floatingVal)
				&& floatingVal is string f
				&& !string.IsNullOrEmpty(f) ? f : null;
			return new SiteNavigationEntry(new SiteSectionRef(sectionTitle, externalUrl, children, dropdownLinks, floatingKind));
		}

		return SiteTableOfContentsRefYamlConverter.ToMount(dictionary, mappingStart, mappingStartEnd) is { } mount
			? new SiteNavigationEntry(mount)
			: throw SiteTableOfContentsRefYamlConverter.NoTocKey(dictionary);
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
}

public class SiteTableOfContentsRefYamlConverter : YamlConverter<SiteTableOfContentsRef>
{
	private static readonly string[] KnownKeys = ["toc", "path_prefix", "children", "island", "navigation_title"];

	public override SiteTableOfContentsRef? Read(YamlReader reader)
	{
		if (reader.TokenType != YamlTokenType.StartMapping)
		{
			reader.Skip();
			return null;
		}

		// The position is that of the mapping's start token, which is where the toc mapping reader always pointed its diagnostics.
		var mappingStart = reader.Start;
		var mappingStartEnd = reader.End;
		_ = reader.Read(); // consume StartMapping
		var dictionary = new Dictionary<string, object?>();

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();

			if (!KnownKeys.Contains(key))
			{
				reader.SkipOrReportUnmapped(typeof(SiteTableOfContentsRef), KnownKeys, key, null);
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
		_ = reader.Read(); // consume EndMapping

		return ToMount(dictionary, mappingStart, mappingStartEnd) ?? throw NoTocKey(dictionary);
	}

	/// <summary>The mount an entry's keys describe, or null when it has no <c>toc:</c> key.</summary>
	internal static SiteTableOfContentsRef? ToMount(Dictionary<string, object?> dictionary, Mark start, Mark end)
	{
		if (!dictionary.TryGetValue("toc", out var tocPath) || tocPath is not string sourceString)
			return null;

		var uriString = sourceString.Contains("://") ? sourceString : $"docs-content://{sourceString}";

		// Wrapped in a YamlException: unlike YamlDotNet, SharpYaml does not wrap exceptions
		// thrown from custom converters, and callers throughout this codebase catch
		// YamlException specifically to turn malformed config into diagnostics instead of
		// crashing. The original InvalidOperationException is preserved as the inner exception.
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

		var shorthand = sourceString.Contains("://") ? null : sourceString;
		return new SiteTableOfContentsRef(source, pathPrefix, children, island, navigationTitle)
		{
			ShorthandPath = shorthand,
			DeclaredPathPrefix = DeclaredPathPrefix(dictionary, shorthand),
			Position = PositionOf(start, end)
		};
	}

	// The toc mapping reader took the keys in the order written, so the later of `path_prefix` and a bare `toc:` name wins.
	private static string? DeclaredPathPrefix(Dictionary<string, object?> dictionary, string? shorthand)
	{
		string? declared = null;
		foreach (var (key, value) in dictionary)
		{
			if (key == "toc" && shorthand is not null)
				declared = shorthand;
			else if (key == "path_prefix")
				declared = value as string ?? string.Empty;
		}
		return declared;
	}

	internal static YamlException NoTocKey(Dictionary<string, object?> dictionary)
	{
		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		return new YamlException($"toc entry has no 'toc:' key and will be ignored. " + $"Found keys: {keys}. Check for typos.");
	}

	// SharpYaml's Mark.Line/Column are 0-based; +1 to match the 1-based positions diagnostics carry.
	private static SourcePosition PositionOf(Mark start, Mark end) => new(start.Line + 1, start.Column + 1, end.Line + 1, end.Column + 1);

	public override void Write(YamlWriter writer, SiteTableOfContentsRef value) =>
		throw new NotSupportedException(
			"SiteTableOfContentsRef YAML serialization is not supported; navigation.yml is only ever read from YAML."
		);
}
