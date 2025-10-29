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

namespace Elastic.Documentation.Configuration.DocSet;

public record NavigationTocMapping
{
	public required Uri Source { get; init; }
	public required string SourcePathPrefix { get; init; }
	public required Uri TopLevelSource { get; init; }
	public required Uri ParentSource { get; init; }
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

		foreach (var tocRef in siteNavigation.TableOfContents)
			CollectSource(tocRef, set);

		return set.ToImmutableHashSet();
	}
	private static void CollectSource(SiteTableOfContentsRef tocRef, HashSet<Uri> set)
	{
		_ = set.Add(tocRef.Source);
		// Recursively collect from children
		foreach (var child in tocRef.Children)
			CollectSource(child, set);
	}


	public static ImmutableHashSet<Uri> GetAllPathPrefixes(SiteNavigationFile siteNavigation)
	{
		var set = new HashSet<Uri>();

		foreach (var tocRef in siteNavigation.TableOfContents)
			CollectPathPrefixes(tocRef, set);

		return set.ToImmutableHashSet();
	}

	private static void CollectPathPrefixes(SiteTableOfContentsRef tocRef, HashSet<Uri> set)
	{
		// Add path prefix for this toc ref
		if (!string.IsNullOrEmpty(tocRef.PathPrefix))
		{
			var pathUri = new Uri($"{tocRef.Source.Scheme}://{tocRef.PathPrefix.TrimEnd('/')}/");
			_ = set.Add(pathUri);
		}

		// Recursively collect from children
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

public class SiteTableOfContents : List<SiteTableOfContentsRef>
{
	public SiteTableOfContents() { }

	public SiteTableOfContents(IEnumerable<SiteTableOfContentsRef> items) : base(items) { }
}

public record SiteTableOfContentsRef(Uri Source, string PathPrefix, IReadOnlyCollection<SiteTableOfContentsRef> Children)
	: ITableOfContentsItem
{
	// For site-level TOC refs, the Path is the path prefix (where it will be mounted in the site)
	public string Path => PathPrefix;

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
			var item = rootDeserializer(typeof(SiteTableOfContentsRef));
			if (item is SiteTableOfContentsRef tocRef)
				collection.Add(tocRef);
		}

		return collection;
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

			// Parse the value based on what type it is
			object? value = null;
			if (parser.Accept<Scalar>(out var scalarValue))
			{
				value = scalarValue.Value;
				_ = parser.MoveNext();
			}
			else if (parser.Accept<SequenceStart>(out _))
			{
				// This is a list - parse it manually for "children"
				if (key.Value == "children")
				{
					// Parse the children list manually
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
				{
					// For other lists, just skip them
					parser.SkipThisAndNestedEvents();
				}
			}
			else if (parser.Accept<MappingStart>(out _))
			{
				// This is a nested mapping - skip it
				parser.SkipThisAndNestedEvents();
			}

			dictionary[key.Value] = value;
		}

		var children = GetChildren(dictionary);

		// Check for toc reference - required
		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string sourceString)
		{
			// Convert string to Uri - if no scheme, prepend "docs-content://"
			var uriString = sourceString.Contains("://") ? sourceString : $"docs-content://{sourceString}";

			if (!Uri.TryCreate(uriString, UriKind.Absolute, out var source))
				throw new InvalidOperationException($"Invalid TOC source: '{sourceString}' could not be parsed as a URI");

			var pathPrefix = dictionary.TryGetValue("path_prefix", out var pathValue) && pathValue is string path
				? path
				: string.Empty;

			return new SiteTableOfContentsRef(source, pathPrefix, children);
		}

		return null;
	}

	private IReadOnlyCollection<SiteTableOfContentsRef> GetChildren(Dictionary<string, object?> dictionary)
	{
		if (!dictionary.TryGetValue("children", out var childrenObj))
			return [];

		// Children have already been deserialized as List<SiteTableOfContentsRef>
		if (childrenObj is List<SiteTableOfContentsRef> tocRefs)
			return tocRefs;

		return [];
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}

