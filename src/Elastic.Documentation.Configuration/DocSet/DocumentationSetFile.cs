// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
	public IReadOnlyCollection<string> CrossLinks { get; set; } = [];

	[YamlMember(Alias = "exclude")]
	public IReadOnlyCollection<string> Exclude { get; set; } = [];

	[YamlMember(Alias = "subs")]
	public IReadOnlyDictionary<string, string> Subs { get; set; } = [];

	[YamlMember(Alias = "features")]
	public DocumentationSetFeatures Features { get; set; } = new();

	[YamlMember(Alias = "api")]
	public IReadOnlyDictionary<string, string> Api { get; set; } = [];

	[YamlMember(Alias = "toc")]
	public IReadOnlyCollection<ITocItem> Toc { get; set; } = [];
}

[YamlSerializable]
public class DocumentationSetFeatures
{
	[YamlMember(Alias = "primary-nav")]
	public bool? PrimaryNav { get; set; }
}

public class TocItemYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(ITocItem);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var deserialized = rootDeserializer.Invoke(typeof(Dictionary<object, object?>));
		if (deserialized is not Dictionary<object, object?> dictionary)
			return null;

		var children = GetChildren(dictionary);

		// Check for file reference (file: or hidden:)
		if (dictionary.TryGetValue("file", out var filePath) && filePath is string file)
			return new FileReference(null!, file, false, children);

		if (dictionary.TryGetValue("hidden", out var hiddenPath) && hiddenPath is string hidden)
			return new FileReference(null!, hidden, true, children);

		// Check for crosslink reference
		if (dictionary.TryGetValue("crosslink", out var crosslink) && crosslink is string crosslinkStr)
		{
			var title = dictionary.TryGetValue("title", out var t) && t is string titleStr ? titleStr : null;
			var isHidden = dictionary.TryGetValue("hidden", out var h) && h is bool hiddenBool && hiddenBool;
			return new CrossLinkReference(null!, new Uri(crosslinkStr), title, isHidden, children);
		}

		// Check for folder reference
		if (dictionary.TryGetValue("folder", out var folderPath) && folderPath is string folder)
			return new FolderReference(null!, folder, children);

		// Check for toc reference
		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string toc)
		{
			// TocReference needs a Uri for Source parameter
			// For now, we'll construct a placeholder URI
			var sourceUri = new Uri($"toc://{toc}", UriKind.Absolute);
			return new TocReference(sourceUri, null!, toc, children);
		}

		return null;
	}

	private IReadOnlyCollection<ITocItem> GetChildren(Dictionary<object, object?> dictionary)
	{
		if (!dictionary.TryGetValue("children", out var childrenObj))
			return [];

		if (childrenObj is not List<object> childrenList)
			return [];

		var children = new List<ITocItem>();
		foreach (var child in childrenList)
		{
			if (child is Dictionary<object, object?> childDict)
			{
				var item = ReadYamlFromDictionary(childDict);
				if (item is not null)
					children.Add(item);
			}
		}

		return children;
	}

	private ITocItem? ReadYamlFromDictionary(Dictionary<object, object?> dictionary)
	{
		var children = GetChildren(dictionary);

		// Check for file reference (file: or hidden:)
		if (dictionary.TryGetValue("file", out var filePath) && filePath is string file)
			return new FileReference(null!, file, false, children);

		if (dictionary.TryGetValue("hidden", out var hiddenPath) && hiddenPath is string hidden)
			return new FileReference(null!, hidden, true, children);

		// Check for crosslink reference
		if (dictionary.TryGetValue("crosslink", out var crosslink) && crosslink is string crosslinkStr)
		{
			var title = dictionary.TryGetValue("title", out var t) && t is string titleStr ? titleStr : null;
			var isHidden = dictionary.TryGetValue("hidden", out var h) && h is bool hiddenBool && hiddenBool;
			return new CrossLinkReference(null!, new Uri(crosslinkStr), title, isHidden, children);
		}

		// Check for folder reference
		if (dictionary.TryGetValue("folder", out var folderPath) && folderPath is string folder)
			return new FolderReference(null!, folder, children);

		// Check for toc reference
		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string toc)
		{
			var sourceUri = new Uri($"toc://{toc}", UriKind.Absolute);
			return new TocReference(sourceUri, null!, toc, children);
		}

		return null;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
