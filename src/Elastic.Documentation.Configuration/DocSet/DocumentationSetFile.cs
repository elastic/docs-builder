// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
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
	/// Validates the table of contents structure and emits diagnostics for issues.
	/// </summary>
	public static DocumentationSetFile LoadAndResolve(IDiagnosticsCollector collector, IFileInfo docsetPath, IFileSystem? fileSystem = null)
	{
		fileSystem ??= docsetPath.FileSystem;
		var yaml = fileSystem.File.ReadAllText(docsetPath.FullName);
		var sourceDirectory = docsetPath.Directory!;
		return LoadAndResolve(collector, yaml, sourceDirectory, fileSystem);
	}

	/// <summary>
	/// Loads a DocumentationSetFile from YAML string and recursively resolves all IsolatedTableOfContentsRef items,
	/// replacing them with their resolved children and ensuring file paths carry over parent paths.
	/// Validates the table of contents structure and emits diagnostics for issues.
	/// </summary>
	public static DocumentationSetFile LoadAndResolve(IDiagnosticsCollector collector, string yaml, IDirectoryInfo sourceDirectory, IFileSystem? fileSystem = null)
	{
		fileSystem ??= sourceDirectory.FileSystem;
		var docSet = Deserialize(yaml);
		var docsetPath = fileSystem.Path.Combine(sourceDirectory.FullName, "docset.yml");
		docSet.TableOfContents = ResolveTableOfContents(collector, docSet.TableOfContents, sourceDirectory, fileSystem, parentPath: "", context: docsetPath);
		return docSet;
	}


	/// <summary>
	/// Recursively resolves all IsolatedTableOfContentsRef items in a table of contents,
	/// loading nested TOC files and prepending parent paths to all file references.
	/// Preserves the hierarchy structure without flattening.
	/// Validates items and emits diagnostics for issues.
	/// </summary>
	private static TableOfContents ResolveTableOfContents(
		IDiagnosticsCollector collector,
		IReadOnlyCollection<ITableOfContentsItem> items,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string context
	)
	{
		var resolved = new TableOfContents();

		foreach (var item in items)
		{
			var resolvedItem = item switch
			{
				IsolatedTableOfContentsRef tocRef => ResolveIsolatedToc(collector, tocRef, baseDirectory, fileSystem, parentPath, context),
				FileRef fileRef => ResolveFileRef(collector, fileRef, baseDirectory, fileSystem, parentPath, context),
				FolderRef folderRef => ResolveFolderRef(collector, folderRef, baseDirectory, fileSystem, parentPath, context),
				CrossLinkRef crossLink => ResolveCrossLinkRef(collector, crossLink, baseDirectory, fileSystem, parentPath, context),
				_ => null
			};

			if (resolvedItem != null)
				resolved.Add(resolvedItem);
		}

		return resolved;
	}

	/// <summary>
	/// Resolves an IsolatedTableOfContentsRef by loading the TOC file and returning a new ref with resolved children.
	/// Validates that the TOC has no children in parent YAML and that toc.yml exists.
	/// The TOC's path is set to the full path (including parent path) for consistency with files and folders.
	/// </summary>
	private static ITableOfContentsItem? ResolveIsolatedToc(
		IDiagnosticsCollector collector,
		IsolatedTableOfContentsRef tocRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string parentContext
	)
	{
		// TOC paths containing '/' are treated as relative to the context file's directory (full paths).
		// Simple TOC names (no '/') are resolved relative to the parent path in the navigation hierarchy.
		string fullTocPath;
		if (tocRef.Path.Contains('/'))
		{
			// Path contains '/', treat as context-relative (full path from the context file's directory)
			var contextDir = fileSystem.Path.GetDirectoryName(parentContext) ?? "";
			var contextRelativePath = fileSystem.Path.GetRelativePath(baseDirectory.FullName, contextDir);
			if (contextRelativePath == ".")
				contextRelativePath = "";

			fullTocPath = string.IsNullOrEmpty(contextRelativePath)
				? tocRef.Path
				: $"{contextRelativePath}/{tocRef.Path}";
		}
		else
		{
			// Simple name, resolve relative to parent path
			fullTocPath = string.IsNullOrEmpty(parentPath) ? tocRef.Path : $"{parentPath}/{tocRef.Path}";
		}

		var tocDirectory = fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(baseDirectory.FullName, fullTocPath));
		var tocFilePath = fileSystem.Path.Combine(tocDirectory.FullName, "toc.yml");
		var tocYmlExists = fileSystem.File.Exists(tocFilePath);

		// Validate: TOC should not have children defined in parent YAML
		if (tocRef.Children.Count > 0)
		{
			collector.EmitError(parentContext,
				$"TableOfContents '{fullTocPath}' may not contain children, define children in '{fullTocPath}/toc.yml' instead.");
			return null;
		}

		// If TOC has children in parent YAML, still try to load from toc.yml (prefer toc.yml over parent YAML)
		if (!tocYmlExists)
		{
			// Validate: toc.yml file must exist
			collector.EmitError(parentContext, $"Table of contents file not found: {fullTocPath}/toc.yml");
			return new IsolatedTableOfContentsRef(fullTocPath, [], parentContext);
		}

		var tocYaml = fileSystem.File.ReadAllText(tocFilePath);
		var tocFile = TableOfContentsFile.Deserialize(tocYaml);

		// Recursively resolve children with the FULL TOC path as the parent path
		// This ensures all file paths within the TOC include the TOC directory path
		// The context for children is the toc.yml file that defines them
		var resolvedChildren = ResolveTableOfContents(collector, tocFile.TableOfContents, baseDirectory, fileSystem, fullTocPath, tocFilePath);

		// Validate: TOC must have at least one child
		if (resolvedChildren.Count == 0)
		{
			collector.EmitError(tocFilePath, $"Table of contents '{fullTocPath}' has no children defined");
		}

		// Return TOC ref with FULL path and resolved children
		// The context remains the parent context (where this TOC was referenced)
		return new IsolatedTableOfContentsRef(fullTocPath, resolvedChildren, parentContext);
	}

	/// <summary>
	/// Resolves a FileRef by prepending the parent path to the file path and recursively resolving children.
	/// The parent path provides the correct context for child resolution.
	/// </summary>
	private static ITableOfContentsItem ResolveFileRef(
		IDiagnosticsCollector collector,
		FileRef fileRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string context)
	{
		var fullPath = string.IsNullOrEmpty(parentPath) ? fileRef.Path : $"{parentPath}/{fileRef.Path}";

		if (fileRef.Children.Count == 0)
		{
			return fileRef is IndexFileRef
				? new IndexFileRef(fullPath, fileRef.Hidden, [], context)
				: new FileRef(fullPath, fileRef.Hidden, [], context);
		}

		// Determine parent path for children (strip .md extension and /index suffix)
		var parentPathForChildren = fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			? fullPath[..^3]
			: fullPath;

		if (parentPathForChildren.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
			parentPathForChildren = parentPathForChildren[..^6];

		var resolvedChildren = ResolveTableOfContents(collector, fileRef.Children, baseDirectory, fileSystem, parentPathForChildren, context);

		return fileRef is IndexFileRef
			? new IndexFileRef(fullPath, fileRef.Hidden, resolvedChildren, context)
			: new FileRef(fullPath, fileRef.Hidden, resolvedChildren, context);
	}

	/// <summary>
	/// Resolves a FolderRef by prepending the parent path to the folder path and recursively resolving children.
	/// If no children are defined, auto-discovers .md files in the folder directory.
	/// </summary>
	private static ITableOfContentsItem ResolveFolderRef(
		IDiagnosticsCollector collector,
		FolderRef folderRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string context)
	{
		var fullPath = string.IsNullOrEmpty(parentPath) ? folderRef.Path : $"{parentPath}/{folderRef.Path}";

		// If children are explicitly defined, resolve them
		if (folderRef.Children.Count > 0)
		{
			var resolvedChildren = ResolveTableOfContents(collector, folderRef.Children, baseDirectory, fileSystem, fullPath, context);
			return new FolderRef(fullPath, resolvedChildren, context);
		}

		// No children defined - auto-discover .md files in the folder
		var autoDiscoveredChildren = AutoDiscoverFolderFiles(collector, fullPath, baseDirectory, fileSystem, context);
		return new FolderRef(fullPath, autoDiscoveredChildren, context);
	}

	/// <summary>
	/// Auto-discovers .md files in a folder directory and creates FileRef items for them.
	/// If index.md exists, it's placed first. Otherwise, files are sorted alphabetically.
	/// Files starting with '_' or '.' are excluded.
	/// </summary>
	private static TableOfContents AutoDiscoverFolderFiles(
		IDiagnosticsCollector collector,
		string folderPath,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string context)
	{
		var directoryPath = fileSystem.Path.Combine(baseDirectory.FullName, folderPath);
		var directory = fileSystem.DirectoryInfo.New(directoryPath);

		if (!directory.Exists)
			return [];

		// Find all .md files in the directory (not recursive)
		var mdFiles = fileSystem.Directory
			.GetFiles(directoryPath, "*.md")
			.Select(f => fileSystem.FileInfo.New(f))
			.Where(f => !f.Name.StartsWith('_') && !f.Name.StartsWith('.'))
			.OrderBy(f => f.Name)
			.ToList();

		if (mdFiles.Count == 0)
			return [];

		// Separate index.md from other files
		var indexFile = mdFiles.FirstOrDefault(f => f.Name.Equals("index.md", StringComparison.OrdinalIgnoreCase));
		var otherFiles = mdFiles.Where(f => !f.Name.Equals("index.md", StringComparison.OrdinalIgnoreCase)).ToList();

		var children = new TableOfContents();

		// Add index.md first if it exists
		if (indexFile != null)
		{
			var indexRef = indexFile.Name.Equals("index.md", StringComparison.OrdinalIgnoreCase)
				? new IndexFileRef(indexFile.Name, false, [], context)
				: new FileRef(indexFile.Name, false, [], context);
			children.Add(indexRef);
		}

		// Add other files sorted alphabetically
		foreach (var file in otherFiles)
		{
			var fileRef = new FileRef(file.Name, false, [], context);
			children.Add(fileRef);
		}

		// Resolve the children with the folder path as parent to get correct full paths
		return ResolveTableOfContents(collector, children, baseDirectory, fileSystem, folderPath, context);
	}

	/// <summary>
	/// Resolves a CrossLinkRef by recursively resolving children (though cross-links typically don't have children).
	/// </summary>
	private static ITableOfContentsItem ResolveCrossLinkRef(
		IDiagnosticsCollector collector,
		CrossLinkRef crossLinkRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string context)
	{
		if (crossLinkRef.Children.Count == 0)
			return new CrossLinkRef(crossLinkRef.CrossLinkUri, crossLinkRef.Title, crossLinkRef.Hidden, [], context);

		var resolvedChildren = ResolveTableOfContents(collector, crossLinkRef.Children, baseDirectory, fileSystem, parentPath, context);

		return new CrossLinkRef(crossLinkRef.CrossLinkUri, crossLinkRef.Title, crossLinkRef.Hidden, resolvedChildren, context);
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


/// <summary>
/// Represents an item in a table of contents (file, folder, or TOC reference).
/// </summary>
public interface ITableOfContentsItem
{
	/// <summary>
	/// The full path of this item relative to the documentation source directory.
	/// For files: includes .md extension (e.g., "guides/getting-started.md")
	/// For folders: the folder path (e.g., "guides/advanced")
	/// For TOCs: the path to the toc.yml directory (e.g., "development" or "guides/advanced")
	/// </summary>
	string Path { get; }

	/// <summary>
	/// The path to the YAML file (docset.yml or toc.yml) that defined this item.
	/// This provides context for where the item was declared in the configuration.
	/// </summary>
	string Context { get; }
}

public record FileRef(string Path, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem;

public record IndexFileRef(string Path, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: FileRef(Path, Hidden, Children, Context);

public record CrossLinkRef(Uri CrossLinkUri, string? Title, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem
{
	// CrossLinks don't have a file system path, so we use the CrossLinkUri as the Path
	public string Path => CrossLinkUri.ToString();
}

public record FolderRef(string Path, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem;

public record IsolatedTableOfContentsRef(string Path, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
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

		// Context will be set during LoadAndResolve, use empty string as placeholder during deserialization
		const string placeholderContext = "";

		// Check for file reference (file: or hidden:)
		if (dictionary.TryGetValue("file", out var filePath) && filePath is string file)
			return file == "index.md" ? new IndexFileRef(file, false, children, placeholderContext) : new FileRef(file, false, children, placeholderContext);

		if (dictionary.TryGetValue("hidden", out var hiddenPath) && hiddenPath is string p)
			return p == "index.md" ? new IndexFileRef(p, true, children, placeholderContext) : new FileRef(p, true, children, placeholderContext);

		// Check for crosslink reference
		if (dictionary.TryGetValue("crosslink", out var crosslink) && crosslink is string crosslinkStr)
		{
			var title = dictionary.TryGetValue("title", out var t) && t is string titleStr ? titleStr : null;
			var isHidden = dictionary.TryGetValue("hidden", out var h) && h is bool hiddenBool && hiddenBool;
			return new CrossLinkRef(new Uri(crosslinkStr), title, isHidden, children, placeholderContext);
		}

		// Check for folder reference
		if (dictionary.TryGetValue("folder", out var folderPath) && folderPath is string folder)
			return new FolderRef(folder, children, placeholderContext);

		// Check for toc reference
		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string source)
			return new IsolatedTableOfContentsRef(source, children, placeholderContext);

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
