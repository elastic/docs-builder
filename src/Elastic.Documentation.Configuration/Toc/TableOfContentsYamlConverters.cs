// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Toc.DetectionRules;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

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
				else if (key.Value == "detection_rules")
				{
					// Parse the children list manually
					var childrenList = new List<string>();
					_ = parser.Consume<SequenceStart>();
					while (!parser.TryConsume<SequenceEnd>(out _))
					{
						if (parser.Accept<Scalar>(out scalarValue))
							childrenList.Add(scalarValue.Value);
						_ = parser.MoveNext();
					}
					value = childrenList.ToArray();
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

		// Context will be set during LoadAndResolve, use empty string as placeholder during deserialization
		const string placeholderContext = "";

		// Check for folder+file combination (e.g., folder: getting-started, file: getting-started.md)
		// This represents a folder with a specific index file
		// The file becomes a child of the folder (as FolderIndexFileRef), and user-specified children follow
		if (dictionary.TryGetValue("folder", out var folderPath) && folderPath is string folder &&
			dictionary.TryGetValue("file", out var filePath) && filePath is string file)
		{
			// Create the index file reference (FolderIndexFileRef to mark it as the folder's index)
			// Store ONLY the file name - the folder path will be prepended during resolution
			// This allows validation to check if the file itself has deep paths
			// PathRelativeToContainer will be set during resolution
			var indexFile = new FolderIndexFileRef(file, file, false, [], placeholderContext);

			// Create a list with the index file first, followed by user-specified children
			var folderChildren = new List<ITableOfContentsItem> { indexFile };
			folderChildren.AddRange(children);

			// Return a FolderRef with the index file and children
			// The folder path can be deep (e.g., "guides/getting-started"), that's OK
			// PathRelativeToContainer will be set during resolution
			return new FolderRef(folder, folder, folderChildren, placeholderContext);
		}
		if (dictionary.TryGetValue("detection_rules", out var detectionRulesObj) && detectionRulesObj is string[] detectionRulesFolders &&
			dictionary.TryGetValue("file", out var detectionRulesFilePath) && detectionRulesFilePath is string detectionRulesFile)
		{
			// Create the index file reference (FolderIndexFileRef to mark it as the folder's index)
			// Store ONLY the file name - the folder path will be prepended during resolution
			// This allows validation to check if the file itself has deep paths
			// PathRelativeToContainer will be set during resolution
			return new DetectionRuleOverviewRef(detectionRulesFile, detectionRulesFile, detectionRulesFolders, children, placeholderContext);
		}

		// Check for file reference (file: or hidden:)
		// PathRelativeToContainer will be set during resolution
		if (dictionary.TryGetValue("file", out var filePathOnly) && filePathOnly is string fileOnly)
		{
			return fileOnly == "index.md"
				? new IndexFileRef(fileOnly, fileOnly, false, children, placeholderContext)
				: new FileRef(fileOnly, fileOnly, false, children, placeholderContext);
		}

		if (dictionary.TryGetValue("hidden", out var hiddenPath) && hiddenPath is string p)
			return p == "index.md" ? new IndexFileRef(p, p, true, children, placeholderContext) : new FileRef(p, p, true, children, placeholderContext);

		// Check for crosslink reference
		if (dictionary.TryGetValue("crosslink", out var crosslink) && crosslink is string crosslinkStr)
		{
			var title = dictionary.TryGetValue("title", out var t) && t is string titleStr ? titleStr : null;
			var isHidden = dictionary.TryGetValue("hidden", out var h) && h is bool hiddenBool && hiddenBool;
			return new CrossLinkRef(new Uri(crosslinkStr), title, isHidden, children, placeholderContext);
		}

		// Check for folder reference
		// PathRelativeToContainer will be set during resolution
		if (dictionary.TryGetValue("folder", out var folderPathOnly) && folderPathOnly is string folderOnly)
			return new FolderRef(folderOnly, folderOnly, children, placeholderContext);

		// Check for toc reference
		// PathRelativeToContainer will be set during resolution
		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string source)
			return new IsolatedTableOfContentsRef(source, source, children, placeholderContext);

		return null;
	}

	private static IReadOnlyCollection<ITableOfContentsItem> GetChildren(Dictionary<string, object?> dictionary)
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
