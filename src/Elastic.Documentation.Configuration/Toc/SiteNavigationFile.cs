// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

[YamlSerializable]
internal sealed class SiteNavigationFileDto
{
	[YamlMember(Alias = "phantoms")]
	public List<PhantomRegistrationDto> Phantoms { get; set; } = [];

	[YamlMember(Alias = "toc")]
	public SiteTableOfContents TableOfContents { get; set; } = [];
}

[YamlSerializable]
internal sealed class PhantomRegistrationDto
{
	[YamlMember(Alias = "toc")]
	public string Source { get; set; } = null!;
}

/// <summary>The top-level <c>toc:</c> entries of <c>navigation.yml</c>, bound by <see cref="SiteTableOfContentsCollectionYamlConverter"/>.</summary>
public sealed class SiteTableOfContents : List<SiteNavigationEntry>;

/// <summary>Loads <c>navigation.yml</c> into a <see cref="SiteNavigationDefinition"/> and validates it.</summary>
public static class SiteNavigationLoader
{
	/// <summary>Parses <paramref name="yaml"/>. Entries without a <c>toc:</c> key, or with an unparsable source, throw.</summary>
	public static SiteNavigationDefinition Deserialize(string yaml)
	{
		var dto = ConfigurationFileProvider.Deserializer.Deserialize<SiteNavigationFileDto>(yaml) ?? new SiteNavigationFileDto();
		return new SiteNavigationDefinition
		{
			Phantoms = [.. dto.Phantoms.Select(p => new PhantomRegistration { Source = p.Source })],
			TableOfContents = dto.TableOfContents
		};
	}

	/// <summary>Reports every <c>path_prefix</c> mounted twice; returns whether all were unique.</summary>
	public static bool ValidatePathPrefixes(
		IDiagnosticsCollector collector,
		SiteNavigationDefinition siteNavigation,
		IFileInfo navigationFile
	)
	{
		var sourcePathPrefixes = SiteNavigationDefinition.GetAllPathPrefixes(siteNavigation);
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
			if (entry.HasValue)
				collection.Add(entry.Value);
		}

		return collection;
	}

	private static SiteNavigationEntry? ParseTopLevelEntry(IParser parser, ObjectDeserializer rootDeserializer)
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

			return new SiteTableOfContentsRef(source, pathPrefix, children, island, navigationTitle);
		}

		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		throw new YamlException($"toc entry has no 'toc:' key and will be ignored. " + $"Found keys: {keys}. Check for typos.");
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) => serializer.Invoke(value, type);
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

			return new SiteTableOfContentsRef(source, pathPrefix, children, island, navigationTitle);
		}

		var keys = string.Join(", ", dictionary.Keys.Select(k => $"'{k}'"));
		throw new YamlException($"toc entry has no 'toc:' key and will be ignored. " + $"Found keys: {keys}. Check for typos.");
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) => serializer.Invoke(value, type);
}
