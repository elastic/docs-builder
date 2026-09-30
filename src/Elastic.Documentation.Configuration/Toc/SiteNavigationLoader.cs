// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

// SiteNavigationDto and PhantomRegistrationDto keep their original names: YamlDotNet puts the type's full name in the
// error for an unknown key, and those messages must not change.
[YamlSerializable]
internal sealed class SiteNavigationDto
{
	[YamlMember(Alias = "phantoms")]
	public List<PhantomRegistrationDto> Phantoms { get; set; } = [];

	[YamlMember(Alias = "toc")]
	public SiteTableOfContentsDto TableOfContents { get; set; } = [];
}

[YamlSerializable]
internal sealed class PhantomRegistrationDto
{
	[YamlMember(Alias = "toc")]
	public string Source { get; set; } = null!;
}

/// <summary>The top-level <c>toc:</c> entries of <c>navigation.yml</c>, bound by <see cref="SiteTableOfContentsCollectionYamlConverter"/>.</summary>
internal sealed class SiteTableOfContentsDto : List<SiteNavigationEntry>;

/// <summary>Loads <c>navigation.yml</c> into a <see cref="SiteNavigationDefinition"/> and validates it.</summary>
public static class SiteNavigationLoader
{
	/// <summary>Parses <paramref name="yaml"/>. Entries without a <c>toc:</c> key, or with an unparsable source, throw.</summary>
	public static SiteNavigationDefinition Deserialize(string yaml)
	{
		var dto = ConfigurationFileProvider.Deserializer.Deserialize<SiteNavigationDto>(yaml) ?? new SiteNavigationDto();
		return new SiteNavigationDefinition
		{
			Phantoms = [.. dto.Phantoms.Select(p => new PhantomRegistration { Source = p.Source })],
			TableOfContents = dto.TableOfContents
		};
	}

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
	/// removed, and a local docs-builder checkout is mounted so docset changes can be tried in an assembled build.
	/// </summary>
	public static SiteNavigationDefinition ForAssembly(
		SiteNavigationDefinition navigation,
		AssemblyConfiguration assembly,
		bool skipPrivateRepositories
	)
	{
		if (assembly.PrivateRepositories.Count == 0 || !skipPrivateRepositories)
			return navigation;

		var filtered = navigation.WithoutRepositories(new HashSet<string>(assembly.PrivateRepositories.Keys));
		if (
			assembly.AvailableRepositories.TryGetValue("docs-builder", out var docsBuilder)
			&& docsBuilder is { Skip: false, Path: not null }
		)
			filtered = filtered with
			{
				TableOfContents = [.. filtered.TableOfContents, .. Deserialize(LocalDocsBuilderToc).TableOfContents]
			};
		return filtered;
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

	/// <summary>Reports every <see cref="SiteNavigationConstraints"/> violation against <paramref name="navigationFile"/>; returns whether there were no errors.</summary>
	public static bool Validate(IDiagnosticsCollector collector, SiteNavigationDefinition siteNavigation, IFileInfo navigationFile)
	{
		var violations = SiteNavigationConstraints.Check(siteNavigation);
		foreach (var violation in violations)
			collector.Write(violation.ToDiagnostic(navigationFile.FullName));
		return violations.All(v => v.Severity != Severity.Error);
	}
}

public class SiteTableOfContentsCollectionYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(SiteTableOfContentsDto);

	public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var collection = new SiteTableOfContentsDto();

		if (!parser.TryConsume<SequenceStart>(out _))
			return collection;

		while (!parser.TryConsume<SequenceEnd>(out _))
		{
			var entry = ParseTopLevelEntry(parser, rootDeserializer);
			if (entry.HasValue)
				collection.Add(entry.Value);
		}

		return collection;
	}

	private static SiteNavigationEntry? ParseTopLevelEntry(IParser parser, ObjectDeserializer rootDeserializer)
	{
		if (!parser.TryConsume<MappingStart>(out var mappingStart))
			return null;

		var dictionary = new Dictionary<string, object?>();

		MappingEnd? mappingEnd;
		while (!parser.TryConsume<MappingEnd>(out mappingEnd))
		{
			var key = parser.Consume<Scalar>();

			object? value = null;
			if (parser.Accept<Scalar>(out var scalarValue))
			{
				value = scalarValue.Value;
				_ = parser.MoveNext();
			}
			else if (parser.Accept<SequenceStart>(out _))
			{
				if (key.Value is "children")
				{
					var childrenList = new List<SiteTableOfContentsRef>();
					_ = parser.Consume<SequenceStart>();
					while (!parser.TryConsume<SequenceEnd>(out _))
					{
						var child = rootDeserializer(typeof(SiteTableOfContentsRef));
						if (child is SiteTableOfContentsRef childRef)
							childrenList.Add(childRef);
					}
					value = childrenList;
				}
				else if (key.Value is "dropdown")
				{
					var dropdownList = new List<SiteDropdownLinkRef>();
					_ = parser.Consume<SequenceStart>();
					while (!parser.TryConsume<SequenceEnd>(out _))
					{
						if (!parser.TryConsume<MappingStart>(out _))
							continue;
						string? itemTitle = null;
						string? itemUrl = null;
						while (!parser.TryConsume<MappingEnd>(out _))
						{
							var itemKey = parser.Consume<Scalar>();
							if (parser.Accept<Scalar>(out var itemValue))
							{
								_ = parser.MoveNext();
								if (itemKey.Value is "title")
									itemTitle = itemValue.Value;
								else if (itemKey.Value is "url")
									itemUrl = itemValue.Value;
							}
							else
								parser.SkipThisAndNestedEvents();
						}
						if (itemTitle is not null && itemUrl is not null)
							dropdownList.Add(new SiteDropdownLinkRef(itemTitle, itemUrl));
					}
					value = dropdownList;
				}
				else
					parser.SkipThisAndNestedEvents();
			}
			else if (parser.Accept<MappingStart>(out _))
				parser.SkipThisAndNestedEvents();

			dictionary[key.Value] = value;
		}

		if (dictionary.TryGetValue("section", out var sectionTitleVal) && sectionTitleVal is string sectionTitle)
		{
			var externalUrl = dictionary.TryGetValue("external", out var extVal) && extVal is string e && !string.IsNullOrEmpty(e)
				? e
				: null;
			IReadOnlyList<SiteTableOfContentsRef> children = dictionary.TryGetValue("children", out var childrenObj)
				&& childrenObj is List<SiteTableOfContentsRef> refs ? refs : [];
			IReadOnlyList<SiteDropdownLinkRef> dropdownLinks = dictionary.TryGetValue("dropdown", out var dropdownObj)
				&& dropdownObj is List<SiteDropdownLinkRef> dLinks ? dLinks : [];
			return new SiteSectionRef(sectionTitle, externalUrl, children, dropdownLinks);
		}

		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string sourceString)
		{
			var uriString = sourceString.Contains("://") ? sourceString : $"docs-content://{sourceString}";

			if (!Uri.TryCreate(uriString, UriKind.Absolute, out var source))
				throw new InvalidOperationException($"Invalid TOC source: '{sourceString}' could not be parsed as a URI");

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
				Position = PositionOf(mappingStart, mappingEnd)
			};
		}

		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		throw new YamlException($"toc entry has no 'toc:' key and will be ignored. " + $"Found keys: {keys}. Check for typos.");
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) => serializer.Invoke(value, type);

	internal static SourcePosition PositionOf(MappingStart start, MappingEnd? end) =>
		new(
			(int)start.Start.Line,
			(int)start.Start.Column,
			(int)(end?.End.Line ?? start.End.Line),
			(int)(end?.End.Column ?? start.End.Column)
		);
}

public class SiteTableOfContentsRefYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(SiteTableOfContentsRef);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (!parser.TryConsume<MappingStart>(out var mappingStart))
			return null;

		var dictionary = new Dictionary<string, object?>();

		MappingEnd? mappingEnd;
		while (!parser.TryConsume<MappingEnd>(out mappingEnd))
		{
			var key = parser.Consume<Scalar>();

			object? value = null;
			if (parser.Accept<Scalar>(out var scalarValue))
			{
				value = scalarValue.Value;
				_ = parser.MoveNext();
			}
			else if (parser.Accept<SequenceStart>(out _))
			{
				if (key.Value == "children")
				{
					var childrenList = new List<SiteTableOfContentsRef>();
					_ = parser.Consume<SequenceStart>();
					while (!parser.TryConsume<SequenceEnd>(out _))
					{
						var child = rootDeserializer(typeof(SiteTableOfContentsRef));
						if (child is SiteTableOfContentsRef childRef)
							childrenList.Add(childRef);
					}
					value = childrenList;
				}
				else
					parser.SkipThisAndNestedEvents();
			}
			else if (parser.Accept<MappingStart>(out _))
				parser.SkipThisAndNestedEvents();

			dictionary[key.Value] = value;
		}

		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string sourceString)
		{
			var uriString = sourceString.Contains("://") ? sourceString : $"docs-content://{sourceString}";

			if (!Uri.TryCreate(uriString, UriKind.Absolute, out var source))
				throw new InvalidOperationException($"Invalid TOC source: '{sourceString}' could not be parsed as a URI");

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
				Position = SiteTableOfContentsCollectionYamlConverter.PositionOf(mappingStart, mappingEnd)
			};
		}

		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		throw new YamlException($"toc entry has no 'toc:' key and will be ignored. " + $"Found keys: {keys}. Check for typos.");
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) => serializer.Invoke(value, type);
}
