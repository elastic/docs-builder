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

	/// <summary>
	/// Reports to <paramref name="collector"/> the paths two mounts claim at once, and returns whether there are none. This is the check the build runs on
	/// <c>navigation.yml</c> after its mappings are read: the rule is <see cref="SiteNavigationConstraints"/>' <c>path-prefixes-unique</c>, and only its findings are written.
	/// </summary>
	public static bool ValidatePathPrefixes(IDiagnosticsCollector collector, SiteNavigationDefinition navigation, IFileInfo navigationFile)
	{
		var duplicates = navigation.Check(navigationFile.FullName).Findings.Where(d => d.RuleId == "path-prefixes-unique").ToList();
		foreach (var duplicate in duplicates)
			collector.Write(duplicate);
		return duplicates.Count == 0;
	}

	/// <summary>
	/// <see cref="GetTocMappings(SiteNavigationDefinition, IFileInfo, IDiagnosticsCollector)"/> for the text of a navigation file. Unknown keys are not
	/// reported here (the parse that reports them runs separately), and the text must be one the parser accepts.
	/// </summary>
	public static FrozenDictionary<Uri, NavigationTocMapping> GetTocMappings(
		string yaml,
		IFileInfo navigationFile,
		IDiagnosticsCollector collector
	) => GetTocMappings(Deserialize(yaml, new BufferingDiagnosticsCollector(), navigationFile.FullName), navigationFile, collector);

	/// <summary>
	/// Where each mounted table of contents is published: its source (normalized to end in <c>/</c>) and path prefix.
	/// The assembler resolves cross-links through these. A mount without a path prefix is reported (rule <c>mount-has-path-prefix</c>) and falls back to its
	/// source path. A <c>repo:</c> key beside <c>toc:</c> is reported and mounts the repository under the enclosing bare <c>toc:</c> name, as the reader always did.
	/// </summary>
	public static FrozenDictionary<Uri, NavigationTocMapping> GetTocMappings(
		SiteNavigationDefinition navigation,
		IFileInfo navigationFile,
		IDiagnosticsCollector collector
	)
	{
		var missingPrefix = navigation
			.Check(navigationFile.FullName)
			.Findings
			.Where(d => d.RuleId == "mount-has-path-prefix")
			.ToLookup(d => (d.Line, d.Column));
		var mappings = new Dictionary<Uri, NavigationTocMapping>();
		foreach (var entry in navigation.TableOfContents)
		{
			var walk = new Walk(navigationFile, collector, missingPrefix, mappings);
			if (entry.Value is SiteTableOfContentsRef toc)
				AddTocMappings(toc, null, walk);
			else
			{
				if (entry.Value is SiteSectionRef { ChildrenNotAnArray: true })
					ReportChildrenNotAnArray(walk);
				foreach (var child in entry.Children)
					AddTocMappings(child, null, walk);
			}
		}
		return mappings.ToFrozenDictionary();
	}

	private sealed record Walk(
		IFileInfo File,
		IDiagnosticsCollector Collector,
		ILookup<(int? Line, int? Column), Diagnostic> MissingPrefix,
		Dictionary<Uri, NavigationTocMapping> Mappings
	);

	private static void AddTocMappings(SiteTableOfContentsRef toc, string? parent, Walk walk)
	{
		var source = SiteNavigationConstraints.NormalizedSource(toc);
		var pathPrefix = toc.DeclaredPathPrefix;
		if (toc.ShorthandPath is { } shorthand)
			parent = shorthand;
		if (toc.RepoKey is { } repository)
		{
			var declared = toc.ShorthandPath is { } name
				? ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, name)
				: toc.Source.OriginalString;
			Emit(walk, toc.Position, $"toc config defines 'repo' can not be combined with 'toc': {declared}");
			pathPrefix = string.Join("/", [parent, repository]);
			source = ContentSourceMoniker.CreateString(repository, parent);
			source = source.EndsWith("://", StringComparison.OrdinalIgnoreCase) ? source : source.TrimEnd('/') + "/";
		}

		if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
		{
			Emit(walk, toc.Position, $"Source toc entry is not a valid uri: {source}");
			return;
		}

		if (toc.RepoKey is null && toc.Position is { } position)
		{
			foreach (var finding in walk.MissingPrefix[(position.Line, position.Column)])
				walk.Collector.Write(finding);
		}

		var sourcePrefix = $"{sourceUri.Host}/{sourceUri.AbsolutePath.TrimStart('/')}";
		walk.Mappings[sourceUri] = new NavigationTocMapping { Source = sourceUri, SourcePathPrefix = pathPrefix ?? sourcePrefix };
		if (toc.ChildrenNotAnArray)
			ReportChildrenNotAnArray(walk);
		foreach (var child in toc.Children)
			AddTocMappings(child, parent, walk);
	}

	// the toc mapping reader warned, without a position, about a children: with nothing (or a mapping) under it
	private static void ReportChildrenNotAnArray(Walk walk) =>
		walk.Collector.Write(new Diagnostic
		{
			Severity = Severity.Warning,
			File = walk.File.FullName,
			Message = "'children' is not an array"
		});

	// Mirrors YamlStreamReader's positioned errors, so the diagnostics match the reader that used to report them.
	private static void Emit(Walk walk, SourcePosition? position, string message) =>
		walk.Collector.Write(new Diagnostic
		{
			Severity = Severity.Error,
			File = walk.File.FullName,
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
		var childrenNotAnArray = false;

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();

			if (!KnownEntryKeys.Contains(key))
			{
				// `repo:` is not read by the parser; the toc mapping reader honoured it, so its value is kept
				if (key == "repo" && reader.TokenType == YamlTokenType.Scalar)
					dictionary[key] = reader.ScalarValue;
				reader.SkipOrReportUnmapped(typeof(SiteNavigationEntry), KnownEntryKeys, key, null);
				continue;
			}

			object? value = null;
			if (key is "children" && reader.TokenType != YamlTokenType.StartSequence)
				childrenNotAnArray = true;
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
			return new SiteNavigationEntry(new SiteSectionRef(sectionTitle, externalUrl, children, dropdownLinks, floatingKind)
			{
				ChildrenNotAnArray = childrenNotAnArray
			});
		}

		return SiteTableOfContentsRefYamlConverter.ToMount(dictionary, mappingStart, mappingStartEnd, childrenNotAnArray) is { } mount
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
		var childrenNotAnArray = false;

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();

			if (!KnownKeys.Contains(key))
			{
				if (key == "repo" && reader.TokenType == YamlTokenType.Scalar)
					dictionary[key] = reader.ScalarValue;
				reader.SkipOrReportUnmapped(typeof(SiteTableOfContentsRef), KnownKeys, key, null);
				continue;
			}

			object? value = null;
			if (key == "children" && reader.TokenType != YamlTokenType.StartSequence)
				childrenNotAnArray = true;
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

		return ToMount(dictionary, mappingStart, mappingStartEnd, childrenNotAnArray) ?? throw NoTocKey(dictionary);
	}

	/// <summary>The mount an entry's keys describe, or null when it has no <c>toc:</c> key.</summary>
	internal static SiteTableOfContentsRef? ToMount(Dictionary<string, object?> dictionary, Mark start, Mark end, bool childrenNotAnArray)
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
			RepoKey = dictionary.TryGetValue("repo", out var repo) ? repo as string : null,
			Position = PositionOf(start, end),
			ChildrenNotAnArray = childrenNotAnArray
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
		var keys = string.Join(", ", dictionary.Keys.Where(k => k != "repo").Select(k => $"'{k}'"));
		return new YamlException($"toc entry has no 'toc:' key and will be ignored. " + $"Found keys: {keys}. Check for typos.");
	}

	// SharpYaml's Mark.Line/Column are 0-based; +1 to match the 1-based positions diagnostics carry.
	private static SourcePosition PositionOf(Mark start, Mark end) => new(start.Line + 1, start.Column + 1, end.Line + 1, end.Column + 1);

	public override void Write(YamlWriter writer, SiteTableOfContentsRef value) =>
		throw new NotSupportedException(
			"SiteTableOfContentsRef YAML serialization is not supported; navigation.yml is only ever read from YAML."
		);
}
