// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

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

[YamlSerializable]
public class SiteNavigationFile
{
	[YamlMember(Alias = "phantoms")]
	public IReadOnlyCollection<PhantomRegistration> Phantoms { get; set; } = [];

	[YamlMember(Alias = "toc")]
	public SiteTableOfContents TableOfContents { get; set; } = [];

	public static SiteNavigationFile Deserialize(string yaml) =>
		ConfigurationFileProvider.Deserializer.Deserialize<SiteNavigationFile>(yaml);

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
	[YamlMember(Alias = "toc")]
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
	string? NavigationTitle = null)
	: ISiteNavigationEntry, ITableOfContentsItem
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

public class SiteTableOfContentsCollectionYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(SiteTableOfContents);

	public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var collection = new SiteTableOfContents();

		if (!parser.TryConsume<SequenceStart>(out _))
			return collection;

		while (!parser.TryConsume<SequenceEnd>(out _))
		{
			var entry = ParseTopLevelEntry(parser, rootDeserializer);
			if (entry is not null)
				collection.Add(entry);
		}

		return collection;
	}

	private static ISiteNavigationEntry? ParseTopLevelEntry(IParser parser, ObjectDeserializer rootDeserializer)
	{
		if (!parser.TryConsume<MappingStart>(out _))
			return null;

		var dictionary = new Dictionary<string, object?>();

		while (!parser.TryConsume<MappingEnd>(out _))
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
			var externalUrl = dictionary.TryGetValue("external", out var extVal) && extVal is string e && !string.IsNullOrEmpty(e) ? e : null;
			IReadOnlyCollection<SiteTableOfContentsRef> children = dictionary.TryGetValue("children", out var childrenObj) && childrenObj is List<SiteTableOfContentsRef> refs
				? refs
				: [];
			IReadOnlyCollection<SiteDropdownLinkRef> dropdownLinks = dictionary.TryGetValue("dropdown", out var dropdownObj) && dropdownObj is List<SiteDropdownLinkRef> dLinks
				? dLinks
				: [];
			return new SiteSectionRef(sectionTitle, externalUrl, children, dropdownLinks);
		}

		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string sourceString)
		{
			var uriString = sourceString.Contains("://") ? sourceString : $"docs-content://{sourceString}";

			if (!Uri.TryCreate(uriString, UriKind.Absolute, out var source))
				throw new InvalidOperationException($"Invalid TOC source: '{sourceString}' could not be parsed as a URI");

			var pathPrefix = dictionary.TryGetValue("path_prefix", out var pathValue) && pathValue is string path
				? path
				: string.Empty;

			IReadOnlyCollection<SiteTableOfContentsRef> children = dictionary.TryGetValue("children", out var childrenObj2) && childrenObj2 is List<SiteTableOfContentsRef> tocRefs
				? tocRefs
				: [];

			var island = dictionary.TryGetValue("island", out var islandObj) && islandObj is string islandStr
				&& bool.TryParse(islandStr, out var islandBool) && islandBool;

			var navigationTitle = dictionary.TryGetValue("navigation_title", out var titleObj) && titleObj is string title
				&& !string.IsNullOrWhiteSpace(title)
				? title
				: null;

			return new SiteTableOfContentsRef(source, pathPrefix, children, island, navigationTitle);
		}

		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		throw new YamlException(
			$"toc entry has no 'toc:' key and will be ignored. " +
			$"Found keys: {keys}. Check for typos.");
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}

public class SiteTableOfContentsRefYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(SiteTableOfContentsRef);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (!parser.TryConsume<MappingStart>(out _))
			return null;

		var dictionary = new Dictionary<string, object?>();

		while (!parser.TryConsume<MappingEnd>(out _))
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

			var pathPrefix = dictionary.TryGetValue("path_prefix", out var pathValue) && pathValue is string path
				? path
				: string.Empty;

			IReadOnlyCollection<SiteTableOfContentsRef> children = dictionary.TryGetValue("children", out var childrenObj) && childrenObj is List<SiteTableOfContentsRef> tocRefs
				? tocRefs
				: [];

			var island = dictionary.TryGetValue("island", out var islandObj) && islandObj is string islandStr
				&& bool.TryParse(islandStr, out var islandBool) && islandBool;

			var navigationTitle = dictionary.TryGetValue("navigation_title", out var titleObj) && titleObj is string title
				&& !string.IsNullOrWhiteSpace(title)
				? title
				: null;

			return new SiteTableOfContentsRef(source, pathPrefix, children, island, navigationTitle);
		}

		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		throw new YamlException(
			$"toc entry has no 'toc:' key and will be ignored. " +
			$"Found keys: {keys}. Check for typos.");
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
