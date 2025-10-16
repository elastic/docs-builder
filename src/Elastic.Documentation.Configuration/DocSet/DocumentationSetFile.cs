// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Products;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.DocSet;

[YamlSerializable]
public class TableOfContentsFile
{
	[YamlMember(Alias = "project")]
	public string? Project { get; set; }

	[YamlMember(Alias = "toc")]
	public TableOfContents TableOfContents { get; set; } = [];

	public static TableOfContentsFile Deserialize(string json) =>
		ConfigurationFileProvider.Deserializer.Deserialize<TableOfContentsFile>(json);
}

[YamlSerializable]
public class DocumentationSetFile : TableOfContentsFile
{
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

	// TODO remove this
	[YamlMember(Alias = "products")]
	public List<ProductLink> Products { get; set; } = [];

	public static FileRef[] GetFileRefs(ITableOfContentsItem item)
	{
		if (item is FileRef fileRef)
			return [fileRef];
		if (item is FolderRef folderRef)
			return folderRef.Children.SelectMany(GetFileRefs).ToArray();
		if (item is IsolatedTableOfContentsRef tocRef)
			return tocRef.Children.SelectMany(GetFileRefs).ToArray();
		if (item is CrossLinkRef crossLinkRef)
			return [];
		throw new Exception($"Unexpected item type {item.GetType().Name}");
	}

	private static new DocumentationSetFile Deserialize(string json) =>
		ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetFile>(json);

	/// <summary>
	/// Loads a DocumentationSetFile and recursively resolves all IsolatedTableOfContentsRef items,
	/// replacing them with their resolved children and ensuring file paths carry over parent paths.
	/// </summary>
	public static DocumentationSetFile LoadAndResolve(IFileInfo docsetPath, IFileSystem? fileSystem = null)
	{
		fileSystem ??= docsetPath.FileSystem;
		var yaml = fileSystem.File.ReadAllText(docsetPath.FullName);
		var sourceDirectory = docsetPath.Directory!;
		return LoadAndResolve(yaml, sourceDirectory, fileSystem);
	}

	/// <summary>
	/// Loads a DocumentationSetFile from YAML string and recursively resolves all IsolatedTableOfContentsRef items,
	/// replacing them with their resolved children and ensuring file paths carry over parent paths.
	/// </summary>
	public static DocumentationSetFile LoadAndResolve(string yaml, IDirectoryInfo sourceDirectory, IFileSystem fileSystem)
	{
		var docSet = Deserialize(yaml);
		docSet.TableOfContents = ResolveTableOfContents(docSet.TableOfContents, sourceDirectory, fileSystem, parentPath: "");
		return docSet;
	}


	/// <summary>
	/// Recursively resolves all IsolatedTableOfContentsRef items in a table of contents,
	/// loading nested TOC files and prepending parent paths to all file references.
	/// Preserves the hierarchy structure without flattening.
	/// </summary>
	private static TableOfContents ResolveTableOfContents(
		IReadOnlyCollection<ITableOfContentsItem> items,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath
	)
	{
		var resolved = new TableOfContents();

		foreach (var item in items)
		{
			var resolvedItem = item switch
			{
				IsolatedTableOfContentsRef tocRef => ResolveIsolatedToc(tocRef, baseDirectory, fileSystem, parentPath),
				FileRef fileRef => ResolveFileRef(fileRef, baseDirectory, fileSystem, parentPath),
				FolderRef folderRef => ResolveFolderRef(folderRef, baseDirectory, fileSystem, parentPath),
				CrossLinkRef crossLink => ResolveCrossLinkRef(crossLink, baseDirectory, fileSystem, parentPath),
				_ => null
			};

			if (resolvedItem != null)
				resolved.Add(resolvedItem);
		}

		return resolved;
	}

	/// <summary>
	/// Resolves an IsolatedTableOfContentsRef by loading the TOC file and returning a new ref with resolved children.
	/// </summary>
	private static ITableOfContentsItem? ResolveIsolatedToc(
		IsolatedTableOfContentsRef tocRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath
	)
	{
		var tocPath = string.IsNullOrEmpty(parentPath) ? tocRef.Path : $"{parentPath}/{tocRef.Path}";

		var tocDirectory = fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(baseDirectory.FullName, tocPath));

		var tocFilePath = fileSystem.Path.Combine(tocDirectory.FullName, "toc.yml");

		if (!fileSystem.File.Exists(tocFilePath))
			return null;

		var tocYaml = fileSystem.File.ReadAllText(tocFilePath);
		var tocFile = TableOfContentsFile.Deserialize(tocYaml);

		// Recursively resolve children with the TOC path as the parent path
		// This ensures all file paths within the TOC include the TOC directory path
		var resolvedChildren = ResolveTableOfContents(tocFile.TableOfContents, baseDirectory, fileSystem, tocPath);

		// Return a new IsolatedTableOfContentsRef with the resolved children
		return new IsolatedTableOfContentsRef(tocPath, resolvedChildren);
	}

	/// <summary>
	/// Resolves a FileRef by prepending the parent path to the file path and recursively resolving children.
	/// The parent path provides the correct context for child resolution.
	/// </summary>
	private static ITableOfContentsItem ResolveFileRef(
		FileRef fileRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath)
	{
		var fullPath = string.IsNullOrEmpty(parentPath) ? fileRef.Path : $"{parentPath}/{fileRef.Path}";

		if (fileRef.Children.Count == 0)
		{
			return fileRef is IndexFileRef
				? new IndexFileRef(fullPath, fileRef.Hidden, [])
				: new FileRef(fullPath, fileRef.Hidden, []);
		}

		// Determine parent path for children (strip .md extension and /index suffix)
		var parentPathForChildren = fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			? fullPath[..^3]
			: fullPath;

		if (parentPathForChildren.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
			parentPathForChildren = parentPathForChildren[..^6];

		var resolvedChildren = ResolveTableOfContents(fileRef.Children, baseDirectory, fileSystem, parentPathForChildren);

		return fileRef is IndexFileRef
			? new IndexFileRef(fullPath, fileRef.Hidden, resolvedChildren)
			: new FileRef(fullPath, fileRef.Hidden, resolvedChildren);
	}

	/// <summary>
	/// Resolves a FolderRef by prepending the parent path to the folder path and recursively resolving children.
	/// </summary>
	private static ITableOfContentsItem ResolveFolderRef(
		FolderRef folderRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath)
	{
		var fullPath = string.IsNullOrEmpty(parentPath) ? folderRef.Path : $"{parentPath}/{folderRef.Path}";

		if (folderRef.Children.Count == 0)
			return new FolderRef(fullPath, []);

		var resolvedChildren = ResolveTableOfContents(folderRef.Children, baseDirectory, fileSystem, fullPath);

		return new FolderRef(fullPath, resolvedChildren);
	}

	/// <summary>
	/// Resolves a CrossLinkRef by recursively resolving children (though cross-links typically don't have children).
	/// </summary>
	private static ITableOfContentsItem ResolveCrossLinkRef(
		CrossLinkRef crossLinkRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath)
	{
		if (crossLinkRef.Children.Count == 0)
			return crossLinkRef;

		var resolvedChildren = ResolveTableOfContents(crossLinkRef.Children, baseDirectory, fileSystem, parentPath);

		return new CrossLinkRef(crossLinkRef.CrossLinkUri, crossLinkRef.Title, crossLinkRef.Hidden, resolvedChildren);
	}
}

[YamlSerializable]
public class DocumentationSetFeatures
{
	[YamlMember(Alias = "primary-nav", ApplyNamingConventions = false)]
	public bool? PrimaryNav { get; set; }
	[YamlMember(Alias = "disable-github-edit-link", ApplyNamingConventions = false)]
	public bool? DisableGithubEditLink { get; set; }
}

public class TableOfContents : List<ITableOfContentsItem>
{
	public TableOfContents() { }

	public TableOfContents(IEnumerable<ITableOfContentsItem> items) : base(items) { }
}


public interface ITableOfContentsItem;

public record FileRef(string Path, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children)
	: ITableOfContentsItem;

public record IndexFileRef(string Path, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children)
	: FileRef(Path, Hidden, Children);

public record CrossLinkRef(Uri CrossLinkUri, string? Title, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children)
	: ITableOfContentsItem;

public record FolderRef(string Path, IReadOnlyCollection<ITableOfContentsItem> Children)
	: ITableOfContentsItem;

public record IsolatedTableOfContentsRef(string Path, IReadOnlyCollection<ITableOfContentsItem> Children)
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
			return file == "index.md" ? new IndexFileRef(file, false, children) : new FileRef(file, false, children);

		if (dictionary.TryGetValue("hidden", out var hiddenPath) && hiddenPath is string p)
			return p == "index.md" ? new IndexFileRef(p, true, children) : new FileRef(p, true, children);

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
			return new IsolatedTableOfContentsRef(source, children);

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
