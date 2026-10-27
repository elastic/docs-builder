// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

public record NavigationTocMapping
{
	public required Uri Source { get; init; }
	public required string SourcePathPrefix { get; init; }
}

public interface ISiteNavigationEntry
{
	IReadOnlyCollection<SiteTableOfContentsRef> Children { get; }
}

/// <summary>A link entry within a <c>dropdown:</c> section.</summary>
public record SiteDropdownLinkRef(string Title, string Url);

public record SiteSectionRef(
	string Title,
	string? ExternalUrl,
	IReadOnlyCollection<SiteTableOfContentsRef> Children,
	IReadOnlyCollection<SiteDropdownLinkRef> DropdownLinks
) : ISiteNavigationEntry
{
	public bool IsExternal => ExternalUrl is not null;
	/// <summary>True when the section carries a dropdown list instead of tree children.</summary>
	public bool IsDropdown => DropdownLinks.Count > 0;
}

public class SiteNavigationFile
{
	[YamlPropertyName("phantoms")]
	public IReadOnlyCollection<PhantomRegistration> Phantoms { get; set; } = [];

	[YamlPropertyName("toc")]
	public SiteTableOfContents TableOfContents { get; set; } = [];

	public static SiteNavigationFile Deserialize(string yaml) => ConfigurationFileProvider.Deserialize<SiteNavigationFile>(yaml);

	/// <summary>
	/// Deserializes <c>navigation.yml</c>, reporting unknown keys to <paramref name="collector"/> instead of
	/// silently skipping them.
	/// </summary>
	public static SiteNavigationFile Deserialize(string yaml, IDiagnosticsCollector collector, string sourceName) =>
		ConfigurationFileProvider.Deserialize<SiteNavigationFile>(yaml, collector, sourceName);

	public static bool ValidatePathPrefixes(IDiagnosticsCollector collector, SiteNavigationFile siteNavigation, IFileInfo navigationFile)
	{
		var sourcePathPrefixes = GetAllPathPrefixes(siteNavigation);
		var pathPrefixSet = new HashSet<string>();
		var valid = true;

		foreach (var pathPrefix in sourcePathPrefixes)
		{
			var prefix = $"{pathPrefix.Host}/{pathPrefix.AbsolutePath.Trim('/')}/";
			if (pathPrefixSet.Add(prefix))
				continue;

			var duplicateOf = sourcePathPrefixes.First(p => p.Host == pathPrefix.Host && p.AbsolutePath == pathPrefix.AbsolutePath);
			collector.EmitError(navigationFile, $"Duplicate path prefix: {pathPrefix} duplicate: {duplicateOf}");
			valid = false;
		}

		return valid;
	}

	public static ImmutableHashSet<Uri> GetAllDeclaredSources(SiteNavigationFile siteNavigation)
	{
		var set = new HashSet<Uri>();
		foreach (var entry in siteNavigation.TableOfContents)
		{
			if (entry is SiteTableOfContentsRef tocRef)
				CollectSource(tocRef, set);
			else
				foreach (var child in entry.Children)
					CollectSource(child, set);
		}
		return set.ToImmutableHashSet();
	}

	private static void CollectSource(SiteTableOfContentsRef tocRef, HashSet<Uri> set)
	{
		_ = set.Add(tocRef.Source);
		foreach (var child in tocRef.Children)
			CollectSource(child, set);
	}

	private static ImmutableHashSet<Uri> GetAllPathPrefixes(SiteNavigationFile siteNavigation)
	{
		var set = new HashSet<Uri>();
		foreach (var entry in siteNavigation.TableOfContents)
		{
			if (entry is SiteTableOfContentsRef tocRef)
				CollectPathPrefixes(tocRef, set);
			else
				foreach (var child in entry.Children)
					CollectPathPrefixes(child, set);
		}
		return set.ToImmutableHashSet();
	}

	private static void CollectPathPrefixes(SiteTableOfContentsRef tocRef, HashSet<Uri> set)
	{
		if (!string.IsNullOrEmpty(tocRef.PathPrefix))
		{
			var pathUri = new Uri($"{tocRef.Source.Scheme}://{tocRef.PathPrefix.TrimEnd('/')}/");
			_ = set.Add(pathUri);
		}

		foreach (var child in tocRef.Children)
			CollectPathPrefixes(child, set);
	}

	public static ImmutableHashSet<Uri> GetPhantomPrefixes(SiteNavigationFile siteNavigation)
	{
		var set = new HashSet<Uri>();

		foreach (var phantom in siteNavigation.Phantoms)
		{
			var source = phantom.Source;
			if (!source.Contains("://"))
				source = ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, source);

			_ = set.Add(new Uri(source));
		}

		return set.ToImmutableHashSet();
	}
}

public class PhantomRegistration
{
	[YamlPropertyName("toc")]
	public string Source { get; set; } = null!;
}

public class SiteTableOfContents : List<ISiteNavigationEntry>;

/// <param name="Island">
/// When <c>true</c>, the resolved navigation node is marked as an island from the assembler side.
/// OR-ed with any <c>island: true</c> the content set already declares — can only enable, never disable.
/// </param>
/// <param name="NavigationTitle">
/// Optional assembler-side label for this TOC root. When set, replaces the index page title
/// in the assembled navigation (dropdowns, back-links, sidebar root row). Does not change the page H1.
/// </param>
public record SiteTableOfContentsRef(
	Uri Source,
	string PathPrefix,
	IReadOnlyCollection<SiteTableOfContentsRef> Children,
	bool Island = false,
	string? NavigationTitle = null
) : ISiteNavigationEntry, ITableOfContentsItem
{
	// For site-level TOC refs, the Path is the path prefix (where it will be mounted in the site)
	public string PathRelativeToDocumentationSet => PathPrefix;

	// For site-level TOC refs, PathRelativeToContainer is the same as PathRelativeToDocumentationSet
	// since they're all defined in the same navigation.yml file
	public string PathRelativeToContainer => PathPrefix;

	// For site-level TOC refs, the Context is the navigation.yml file path
	// This will be set during site navigation loading
	public string Context { get; init; } = "";
}

public class SiteTableOfContentsCollectionYamlConverter(
	SiteTableOfContentsRefYamlConverter refConverter
) : YamlConverter<SiteTableOfContents>
{
	// Parameterless constructor required by the SharpYaml source generator for its static converter
	// fallback field; the real instance is always supplied via YamlSerializerOptions.Converters at
	// runtime, which the generator consults first.
	public SiteTableOfContentsCollectionYamlConverter() : this(new SiteTableOfContentsRefYamlConverter()) { }

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

	public override SiteTableOfContents Read(YamlReader reader)
	{
		var collection = new SiteTableOfContents();

		if (reader.TokenType != YamlTokenType.StartSequence)
		{
			reader.Skip();
			return collection;
		}

		_ = reader.Read(); // consume StartSequence
		while (reader.TokenType != YamlTokenType.EndSequence)
		{
			var entry = ParseTopLevelEntry(reader);
			if (entry is not null)
				collection.Add(entry);
		}
		_ = reader.Read(); // consume EndSequence

		return collection;
	}

	private ISiteNavigationEntry? ParseTopLevelEntry(YamlReader reader)
	{
		if (reader.TokenType != YamlTokenType.StartMapping)
		{
			reader.Skip();
			return null;
		}

		_ = reader.Read(); // consume StartMapping
		var dictionary = new Dictionary<string, object?>();

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();

			if (!KnownEntryKeys.Contains(key))
			{
				reader.SkipOrReportUnmapped(typeof(ISiteNavigationEntry), KnownEntryKeys, key, null);
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
						_ = reader.Read(); // consume MappingStart
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
						_ = reader.Read(); // consume MappingEnd
						if (itemTitle is not null && itemUrl is not null)
							dropdownList.Add(new SiteDropdownLinkRef(itemTitle, itemUrl));
					}
					_ = reader.Read(); // consume EndSequence
					value = dropdownList;
				}
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
			IReadOnlyCollection<SiteTableOfContentsRef> children = dictionary.TryGetValue("children", out var childrenObj)
				&& childrenObj is List<SiteTableOfContentsRef> refs ? refs : [];
			IReadOnlyCollection<SiteDropdownLinkRef> dropdownLinks = dictionary.TryGetValue("dropdown", out var dropdownObj)
				&& dropdownObj is List<SiteDropdownLinkRef> dLinks ? dLinks : [];
			return new SiteSectionRef(sectionTitle, externalUrl, children, dropdownLinks);
		}

		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string sourceString)
		{
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

			IReadOnlyCollection<SiteTableOfContentsRef> children = dictionary.TryGetValue("children", out var childrenObj2)
				&& childrenObj2 is List<SiteTableOfContentsRef> tocRefs ? tocRefs : [];

			var island = dictionary.TryGetValue("island", out var islandObj)
				&& islandObj is string islandStr
				&& bool.TryParse(islandStr, out var islandBool)
				&& islandBool;

			var navigationTitle = dictionary.TryGetValue("navigation_title", out var titleObj)
				&& titleObj is string title
				&& !string.IsNullOrWhiteSpace(title) ? title : null;

			return new SiteTableOfContentsRef(source, pathPrefix, children, island, navigationTitle);
		}

		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		throw new YamlException($"toc entry has no 'toc:' key and will be ignored. " + $"Found keys: {keys}. Check for typos.");
	}

	public override void Write(YamlWriter writer, SiteTableOfContents value) =>
		throw new NotSupportedException(
			"SiteTableOfContents YAML serialization is not supported; navigation.yml is only ever read from YAML."
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

		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string sourceString)
		{
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

			IReadOnlyCollection<SiteTableOfContentsRef> children = dictionary.TryGetValue("children", out var childrenObj)
				&& childrenObj is List<SiteTableOfContentsRef> tocRefs ? tocRefs : [];

			var island = dictionary.TryGetValue("island", out var islandObj)
				&& islandObj is string islandStr
				&& bool.TryParse(islandStr, out var islandBool)
				&& islandBool;

			var navigationTitle = dictionary.TryGetValue("navigation_title", out var titleObj)
				&& titleObj is string title
				&& !string.IsNullOrWhiteSpace(title) ? title : null;

			return new SiteTableOfContentsRef(source, pathPrefix, children, island, navigationTitle);
		}

		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		throw new YamlException($"toc entry has no 'toc:' key and will be ignored. " + $"Found keys: {keys}. Check for typos.");
	}

	public override void Write(YamlWriter writer, SiteTableOfContentsRef value) =>
		throw new NotSupportedException(
			"SiteTableOfContentsRef YAML serialization is not supported; navigation.yml is only ever read from YAML."
		);
}
