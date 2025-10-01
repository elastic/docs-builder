// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Configuration.Serialization;
using Elastic.Documentation.Configuration.TableOfContents;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.DocSet;

[YamlSerializable]
public class DocumentationSetFile
{
	[YamlMember(Alias = "project")]
	public string? Project { get; set; }

	[YamlMember(Alias = "max_toc_depth")]
	public int MaxTocDepth { get; set; } = 2;

	[YamlMember(Alias = "dev_docs")]
	public bool DevDocs { get; set; }

	[YamlMember(Alias = "cross_links")]
	public List<string> CrossLinks { get; set; } = [];

	[YamlMember(Alias = "exclude")]
	public List<string> Exclude { get; set; } = [];

	[YamlMember(Alias = "subs")]
	public Dictionary<string, string> Subs { get; set; } = [];

	[YamlMember(Alias = "features")]
	public DocumentationSetFeatures Features { get; set; } = new();

	[YamlMember(Alias = "api")]
	public Dictionary<string, string> Api { get; set; } = [];

	[YamlMember(Alias = "toc")]
	public TableOfContents Toc { get; set; } = [];

	public static DocumentationSetFile Deserialize(string json) =>
		ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(json);
}

[YamlSerializable]
public class DocumentationSetFeatures
{
	[YamlMember(Alias = "primary-nav")]
	public bool? PrimaryNav { get; set; }
}

public class TableOfContents : List<ITableOfContentsItem>
{
	public TableOfContents() { }

	public TableOfContents(IEnumerable<ITableOfContentsItem> items) : base(items) { }
}


public interface ITableOfContentsItem;


public record FileRef(string RelativePath, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children)
	: ITableOfContentsItem;

public record CrossLinkRef(Uri CrossLinkUri, string? Title, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children)
	: ITableOfContentsItem;

public record FolderRef(string RelativePath, IReadOnlyCollection<ITableOfContentsItem> Children)
	: ITableOfContentsItem;

public record TableOfContentsRef(string Source, IReadOnlyCollection<ITableOfContentsItem> Children)
	: ITableOfContentsItem;


public class TocItemCollectionYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(TableOfContents);

	public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var collection = new TableOfContents();

		if (!parser.TryConsume<SequenceStart>(out _))
			return collection;

		while (!parser.TryConsume<SequenceEnd>(out _))
		{
			var item = rootDeserializer(typeof(ITableOfContentsItem));
			if (item is ITableOfContentsItem tocItem)
				collection.Add(tocItem);
		}

		return collection;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}

public class TocItemYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(ITableOfContentsItem);

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
					var childrenList = new List<ITableOfContentsItem>();
					_ = parser.Consume<SequenceStart>();
					while (!parser.TryConsume<SequenceEnd>(out _))
					{
						var child = rootDeserializer(typeof(ITableOfContentsItem));
						if (child is ITableOfContentsItem tocItem)
							childrenList.Add(tocItem);
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

		// Check for file reference (file: or hidden:)
		if (dictionary.TryGetValue("file", out var filePath) && filePath is string file)
			return new FileRef(file, false, children);

		if (dictionary.TryGetValue("hidden", out var hiddenPath) && hiddenPath is string hidden)
			return new FileRef(hidden, true, children);

		// Check for crosslink reference
		if (dictionary.TryGetValue("crosslink", out var crosslink) && crosslink is string crosslinkStr)
		{
			var title = dictionary.TryGetValue("title", out var t) && t is string titleStr ? titleStr : null;
			var isHidden = dictionary.TryGetValue("hidden", out var h) && h is bool hiddenBool && hiddenBool;
			return new CrossLinkRef(new Uri(crosslinkStr), title, isHidden, children);
		}

		// Check for folder reference
		if (dictionary.TryGetValue("folder", out var folderPath) && folderPath is string folder)
			return new FolderRef(folder, children);

		// Check for toc reference
		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string source)
			return new TableOfContentsRef(source, children);

		return null;
	}

	private IReadOnlyCollection<ITableOfContentsItem> GetChildren(Dictionary<string, object?> dictionary)
	{
		if (!dictionary.TryGetValue("children", out var childrenObj))
			return [];

		// Children have already been deserialized as List<ITableOfContentsItem>
		if (childrenObj is List<ITableOfContentsItem> tocItems)
			return tocItems;

		return [];
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
