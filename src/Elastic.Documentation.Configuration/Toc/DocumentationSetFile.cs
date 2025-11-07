// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Plugins.DetectionRules.TableOfContents;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

[YamlSerializable]
public class TableOfContentsFile
{
	[YamlMember(Alias = "project")]
	public string? Project { get; set; }

	[YamlMember(Alias = "toc")]
	public TableOfContents TableOfContents { get; set; } = [];

	/// <summary>
	/// Set of diagnostic hint types to suppress. Deserialized directly from YAML list of strings.
	/// Valid values: "DeepLinkingVirtualFile", "FolderFileNameMismatch"
	/// </summary>
	[YamlMember(Alias = "suppress")]
	public HashSet<HintType> SuppressDiagnostics { get; set; } = [];

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

	[YamlMember(Alias = "extensions")]
	public List<string> Extensions { get; set; } = [];

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
		if (item is CrossLinkRef)
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
		var docsetPath = fileSystem.Path.Combine(sourceDirectory.FullName, "docset.yml").OptionalWindowsReplace();
		docSet.TableOfContents = ResolveTableOfContents(collector, docSet.TableOfContents, sourceDirectory, fileSystem, parentPath: "", containerPath: "", context: docsetPath, docSet.SuppressDiagnostics);
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
		string containerPath,
		string context,
		HashSet<HintType>? suppressDiagnostics = null
	)
	{
		var resolved = new TableOfContents();

		foreach (var item in items)
		{
			var resolvedItem = item switch
			{
				IsolatedTableOfContentsRef tocRef => ResolveIsolatedToc(collector, tocRef, baseDirectory, fileSystem, parentPath, containerPath, context, suppressDiagnostics),
				RuleOverviewReference ruleOverviewReference => ResolveRuleOverviewReference(collector, ruleOverviewReference, baseDirectory, fileSystem, parentPath, containerPath, context, suppressDiagnostics),
				FileRef fileRef => ResolveFileRef(collector, fileRef, baseDirectory, fileSystem, parentPath, containerPath, context, suppressDiagnostics),
				FolderRef folderRef => ResolveFolderRef(collector, folderRef, baseDirectory, fileSystem, parentPath, containerPath, context, suppressDiagnostics),
				CrossLinkRef crossLink => ResolveCrossLinkRef(collector, crossLink, baseDirectory, fileSystem, parentPath, containerPath, context),
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
#pragma warning disable IDE0060 // Remove unused parameter - suppressDiagnostics is for consistency, nested TOCs use their own suppression config
	private static ITableOfContentsItem? ResolveIsolatedToc(IDiagnosticsCollector collector,
		IsolatedTableOfContentsRef tocRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string parentContext,
		HashSet<HintType>? suppressDiagnostics = null
	)
#pragma warning restore IDE0060
	{
		// TOC paths containing '/' are treated as relative to the context file's directory (full paths).
		// Simple TOC names (no '/') are resolved relative to the parent path in the navigation hierarchy.
		string fullTocPath;
		if (tocRef.PathRelativeToDocumentationSet.Contains('/'))
		{
			// Path contains '/', treat as context-relative (full path from the context file's directory)
			var contextDir = fileSystem.Path.GetDirectoryName(parentContext) ?? "";
			var contextRelativePath = fileSystem.Path.GetRelativePath(baseDirectory.FullName, contextDir);
			if (contextRelativePath == ".")
				contextRelativePath = "";

			fullTocPath = string.IsNullOrEmpty(contextRelativePath)
				? tocRef.PathRelativeToDocumentationSet
				: $"{contextRelativePath}/{tocRef.PathRelativeToDocumentationSet}";
		}
		else
		{
			// Simple name, resolve relative to parent path
			fullTocPath = string.IsNullOrEmpty(parentPath) ? tocRef.PathRelativeToDocumentationSet : $"{parentPath}/{tocRef.PathRelativeToDocumentationSet}";
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

		// PathRelativeToContainer for a TOC is the path relative to its parent container
		var tocPathRelativeToContainer = string.IsNullOrEmpty(containerPath)
			? fullTocPath
			: fullTocPath.Substring(containerPath.Length + 1);

		// If TOC has children in parent YAML, still try to load from toc.yml (prefer toc.yml over parent YAML)
		if (!tocYmlExists)
		{
			// Validate: toc.yml file must exist
			collector.EmitError(parentContext, $"Table of contents file not found: {fullTocPath}/toc.yml");
			return new IsolatedTableOfContentsRef(fullTocPath, tocPathRelativeToContainer, [], parentContext);
		}

		var tocYaml = fileSystem.File.ReadAllText(tocFilePath);
		var nestedTocFile = TableOfContentsFile.Deserialize(tocYaml);

		// this is temporary after this lands in main we can update these files to include
		// suppress:
		//	- DeepLinkingVirtualFile
		string[] skip = [
			"docs-content/solutions/toc.yml",
			"docs-content/manage-data/toc.yml",
			"docs-content/explore-analyze/toc.yml",
			"docs-content/deploy-manage/toc.yml",
			"docs-content/troubleshoot/toc.yml",
			"docs-content/troubleshoot/ingest/opentelemetry/toc.yml",
			"docs-content/reference/security/toc.yml"
		];

		var path = tocFilePath.OptionalWindowsReplace();
		// Hardcode suppression for known problematic files
		if (skip.Any(f => path.Contains(f, StringComparison.OrdinalIgnoreCase)))
			_ = nestedTocFile.SuppressDiagnostics.Add(HintType.DeepLinkingVirtualFile);


		// Recursively resolve children with the FULL TOC path as the parent path
		// This ensures all file paths within the TOC include the TOC directory path
		// The context for children is the toc.yml file that defines them
		// For children of this TOC, the container path is fullTocPath (they're defined in toc.yml at that location)
		var resolvedChildren = ResolveTableOfContents(collector, nestedTocFile.TableOfContents, baseDirectory, fileSystem, fullTocPath, fullTocPath, tocFilePath, nestedTocFile.SuppressDiagnostics);

		// Validate: TOC must have at least one child
		if (resolvedChildren.Count == 0)
			collector.EmitError(tocFilePath, $"Table of contents '{fullTocPath}' has no children defined");

		// Return TOC ref with FULL path and resolved children
		// The context remains the parent context (where this TOC was referenced)
		return new IsolatedTableOfContentsRef(fullTocPath, tocPathRelativeToContainer, resolvedChildren, parentContext);
	}

	/// <summary>
	/// Resolves a FileRef by prepending the parent path to the file path and recursively resolving children.
	/// The parent path provides the correct context for child resolution.
	/// </summary>
	private static ITableOfContentsItem ResolveFileRef(IDiagnosticsCollector collector,
		FileRef fileRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context,
		HashSet<HintType>? suppressDiagnostics = null)
	{
		var fullPath = string.IsNullOrEmpty(parentPath) ? fileRef.PathRelativeToDocumentationSet : $"{parentPath}/{fileRef.PathRelativeToDocumentationSet}";

		// Special validation for FolderIndexFileRef (folder+file combination)
		// Validate BEFORE early return so we catch cases with no children
		if (fileRef is FolderIndexFileRef)
		{
			var fileName = fileRef.PathRelativeToDocumentationSet;
			var fileWithoutExtension = fileName.Replace(".md", "");

			// Validate: deep linking is NOT supported for folder+file combination
			// The file path should be simple (no '/'), or at most folder/file.md after prepending
			if (fileName.Contains('/'))
			{
				collector.EmitError(context,
					$"Deep linking on folder 'file' is not supported. Found file path '{fileName}' with '/'. Use simple file name only.");
			}

			// Best practice: file name should match folder name (from parentPath)
			// Only check if we're in a folder context (parentPath is not empty)
			if (!string.IsNullOrEmpty(parentPath) && fileName != "index.md")
			{
				// Check if this hint type should be suppressed
				if (!suppressDiagnostics.ShouldSuppress(HintType.FolderFileNameMismatch))
				{
					// Extract just the folder name from parentPath (in case it's nested like "guides/getting-started")
					var folderName = parentPath.Contains('/') ? parentPath.Split('/')[^1] : parentPath;

					// Normalize for comparison: remove hyphens, underscores, and lowercase
					// This allows "getting-started" to match "GettingStarted" or "getting_started"
					var normalizedFile = fileWithoutExtension.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();
					var normalizedFolder = folderName.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

					if (!normalizedFile.Equals(normalizedFolder, StringComparison.Ordinal))
					{
						collector.EmitHint(context,
							$"File name '{fileName}' does not match folder name '{folderName}'. Best practice is to name the file the same as the folder (e.g., 'folder: {folderName}, file: {folderName}.md').");
					}
				}
			}
		}

		// Calculate PathRelativeToContainer: the file path relative to its container
		var pathRelativeToContainer = string.IsNullOrEmpty(containerPath)
			? fullPath
			: fullPath.Substring(containerPath.Length + 1);

		if (fileRef.Children.Count == 0)
		{
			// Preserve specific types even when there are no children
			return fileRef switch
			{
				FolderIndexFileRef => new FolderIndexFileRef(fullPath, pathRelativeToContainer, fileRef.Hidden, [], context),
				IndexFileRef => new IndexFileRef(fullPath, pathRelativeToContainer, fileRef.Hidden, [], context),
				_ => new FileRef(fullPath, pathRelativeToContainer, fileRef.Hidden, [], context)
			};
		}

		// Emit hint if file has children and uses deep-linking (path contains '/')
		// This suggests using 'folder' instead of 'file' would be better
		if (fileRef.PathRelativeToDocumentationSet.Contains('/') && fileRef.Children.Count > 0 && fileRef is not FolderIndexFileRef)
		{
			// Check if this hint type should be suppressed
			if (!suppressDiagnostics.ShouldSuppress(HintType.DeepLinkingVirtualFile))
			{
				collector.EmitHint(context,
					$"File '{fileRef.PathRelativeToDocumentationSet}' uses deep-linking with children. Consider using 'folder' instead of 'file' for better navigation structure. Virtual files are primarily intended to group sibling files together.");
			}
		}

		// Children of a file should be resolved in the same directory as the parent file.
		// Special handling for FolderIndexFileRef (folder+file combinations from YAML):
		// - These are created when both folder and file keys exist (e.g., "folder: path/to/dir, file: index.md")
		// - Children should resolve to the folder path, not the parent TOC path
		// Examples:
		// - Top level: "nest/guide.md" (parentPath="") → children resolve to "nest/"
		// - Simple file in folder: "guide.md" (parentPath="guides") → children resolve to "guides/"
		// - User file with subpath: "clients/getting-started.md" (parentPath="guides") → children resolve to "guides/"
		// - Folder+file (FolderIndexFileRef): "observability/apm/apm-server/index.md" → children resolve to directory of fullPath
		string parentPathForChildren;
		if (fileRef is FolderIndexFileRef)
		{
			// Folder+file combination - extract directory from fullPath
			var lastSlashIndex = fullPath.LastIndexOf('/');
			parentPathForChildren = lastSlashIndex >= 0 ? fullPath[..lastSlashIndex] : "";
		}
		else if (string.IsNullOrEmpty(parentPath))
		{
			// Top level - extract directory from file path
			var lastSlashIndex = fullPath.LastIndexOf('/');
			parentPathForChildren = lastSlashIndex >= 0 ? fullPath[..lastSlashIndex] : "";
		}
		else
		{
			// In folder/TOC context - use parentPath directly, ignoring any subdirectory in the file reference
			parentPathForChildren = parentPath;
		}

		// For children of files, the container is still the current context (same container as the file itself)
		var resolvedChildren = ResolveTableOfContents(collector, fileRef.Children, baseDirectory, fileSystem, parentPathForChildren, containerPath, context, suppressDiagnostics);

		// Preserve the specific type when creating the resolved reference
		return fileRef switch
		{
			FolderIndexFileRef => new FolderIndexFileRef(fullPath, pathRelativeToContainer, fileRef.Hidden, resolvedChildren, context),
			IndexFileRef => new IndexFileRef(fullPath, pathRelativeToContainer, fileRef.Hidden, resolvedChildren, context),
			_ => new FileRef(fullPath, pathRelativeToContainer, fileRef.Hidden, resolvedChildren, context)
		};
	}

	/// <summary>
	/// Resolves a FolderRef by prepending the parent path to the folder path and recursively resolving children.
	/// If no children are defined, auto-discovers .md files in the folder directory.
	/// </summary>
	private static ITableOfContentsItem ResolveRuleOverviewReference(IDiagnosticsCollector collector,
		RuleOverviewReference ruleRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context,
		HashSet<HintType>? suppressDiagnostics = null)
	{
		// Folder paths containing '/' are treated as relative to the context file's directory (full paths).
		// Simple folder names (no '/') are resolved relative to the parent path in the navigation hierarchy.
		string fullPath;
		if (ruleRef.PathRelativeToDocumentationSet.Contains('/'))
		{
			// Path contains '/', treat as context-relative (full path from the context file's directory)
			var contextDir = fileSystem.Path.GetDirectoryName(context) ?? "";
			var contextRelativePath = fileSystem.Path.GetRelativePath(baseDirectory.FullName, contextDir);
			if (contextRelativePath == ".")
				contextRelativePath = "";

			fullPath = string.IsNullOrEmpty(contextRelativePath)
				? ruleRef.PathRelativeToDocumentationSet
				: $"{contextRelativePath}/{ruleRef.PathRelativeToDocumentationSet}";
		}
		else
		{
			// Simple name, resolve relative to parent path
			fullPath = string.IsNullOrEmpty(parentPath) ? ruleRef.PathRelativeToDocumentationSet : $"{parentPath}/{ruleRef.PathRelativeToDocumentationSet}";
		}

		// Calculate PathRelativeToContainer: the folder path relative to its container
		var pathRelativeToContainer = string.IsNullOrEmpty(containerPath)
			? fullPath
			: fullPath.Substring(containerPath.Length + 1);

		// For children of folders, the container remains the same as the folder's container
		var resolvedChildren = ResolveTableOfContents(collector, ruleRef.Children, baseDirectory, fileSystem, fullPath, containerPath, context, suppressDiagnostics);

		var fileInfo = fileSystem.NewFileInfo(baseDirectory.FullName, fullPath);
		var tocSourceFolders = ruleRef.DetectionRuleFolders
			.Select(f => fileSystem.NewDirInfo(fileInfo.Directory!.FullName, f))
			.ToList();
		var tomlChildren = RuleOverviewReference.CreateTableOfContentItems(tocSourceFolders, context, baseDirectory);

		var children = resolvedChildren.Concat(tomlChildren).ToList();

		return new RuleOverviewReference(fullPath, pathRelativeToContainer, ruleRef.DetectionRuleFolders, children, context);
	}


	/// <summary>
	/// Resolves a FolderRef by prepending the parent path to the folder path and recursively resolving children.
	/// If no children are defined, auto-discovers .md files in the folder directory.
	/// </summary>
	private static ITableOfContentsItem ResolveFolderRef(IDiagnosticsCollector collector,
		FolderRef folderRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context,
		HashSet<HintType>? suppressDiagnostics = null)
	{
		// Folder paths containing '/' are treated as relative to the context file's directory (full paths).
		// Simple folder names (no '/') are resolved relative to the parent path in the navigation hierarchy.
		string fullPath;
		if (folderRef.PathRelativeToDocumentationSet.Contains('/'))
		{
			// Path contains '/', treat as context-relative (full path from the context file's directory)
			var contextDir = fileSystem.Path.GetDirectoryName(context) ?? "";
			var contextRelativePath = fileSystem.Path.GetRelativePath(baseDirectory.FullName, contextDir);
			if (contextRelativePath == ".")
				contextRelativePath = "";

			fullPath = string.IsNullOrEmpty(contextRelativePath)
				? folderRef.PathRelativeToDocumentationSet
				: $"{contextRelativePath}/{folderRef.PathRelativeToDocumentationSet}";
		}
		else
		{
			// Simple name, resolve relative to parent path
			fullPath = string.IsNullOrEmpty(parentPath) ? folderRef.PathRelativeToDocumentationSet : $"{parentPath}/{folderRef.PathRelativeToDocumentationSet}";
		}

		// Calculate PathRelativeToContainer: the folder path relative to its container
		var pathRelativeToContainer = string.IsNullOrEmpty(containerPath)
			? fullPath
			: fullPath.Substring(containerPath.Length + 1);

		// If children are explicitly defined, resolve them
		if (folderRef.Children.Count > 0)
		{
			// For children of folders, the container remains the same as the folder's container
			var resolvedChildren = ResolveTableOfContents(collector, folderRef.Children, baseDirectory, fileSystem, fullPath, containerPath, context, suppressDiagnostics);
			return new FolderRef(fullPath, pathRelativeToContainer, resolvedChildren, context);
		}

		// No children defined - auto-discover .md files in the folder
		var autoDiscoveredChildren = AutoDiscoverFolderFiles(collector, fullPath, containerPath, baseDirectory, fileSystem, context);
		return new FolderRef(fullPath, pathRelativeToContainer, autoDiscoveredChildren, context);
	}

	/// <summary>
	/// Auto-discovers .md files in a folder directory and creates FileRef items for them.
	/// If index.md exists, it's placed first. Otherwise, files are sorted alphabetically.
	/// Files starting with '_' or '.' are excluded.
	/// </summary>
	private static TableOfContents AutoDiscoverFolderFiles(
		IDiagnosticsCollector collector,
		string folderPath,
		string containerPath,
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
				? new IndexFileRef(indexFile.Name, indexFile.Name, false, [], context)
				: new FileRef(indexFile.Name, indexFile.Name, false, [], context);
			children.Add(indexRef);
		}

		// Add other files sorted alphabetically
		foreach (var file in otherFiles)
		{
			var fileRef = new FileRef(file.Name, file.Name, false, [], context);
			children.Add(fileRef);
		}

		// Resolve the children with the folder path as parent to get correct full paths
		// Auto-discovered items are in the same container as the folder
		return ResolveTableOfContents(collector, children, baseDirectory, fileSystem, folderPath, containerPath, context);
	}

	/// <summary>
	/// Resolves a CrossLinkRef by recursively resolving children (though cross-links typically don't have children).
	/// </summary>
	private static ITableOfContentsItem ResolveCrossLinkRef(IDiagnosticsCollector collector,
		CrossLinkRef crossLinkRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context)
	{
		if (crossLinkRef.Children.Count == 0)
			return new CrossLinkRef(crossLinkRef.CrossLinkUri, crossLinkRef.Title, crossLinkRef.Hidden, [], context);

		// For children of cross-links, the container remains the same
		var resolvedChildren = ResolveTableOfContents(collector, crossLinkRef.Children, baseDirectory, fileSystem, parentPath, containerPath, context);

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
	string PathRelativeToDocumentationSet { get; }

	/// <summary>
	/// The full path of this item relative to the container docset.yml or toc.yml file.
	/// For files: includes .md extension (e.g., "guides/getting-started.md")
	/// For folders: the folder path (e.g., "guides/advanced")
	/// For TOCs: the path to the toc.yml directory (e.g., "development" or "guides/advanced")
	/// </summary>
	string PathRelativeToContainer { get; }

	/// <summary>
	/// The path to the YAML file (docset.yml or toc.yml) that defined this item.
	/// This provides context for where the item was declared in the configuration.
	/// </summary>
	string Context { get; }
}

public record FileRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem;

public record IndexFileRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: FileRef(PathRelativeToDocumentationSet, PathRelativeToContainer, Hidden, Children, Context);

/// <summary>
/// Represents a file reference created from a folder+file combination in YAML (e.g., "folder: path/to/dir, file: index.md").
/// Children of this file should resolve relative to the folder path, not the parent TOC path.
/// </summary>
public record FolderIndexFileRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: IndexFileRef(PathRelativeToDocumentationSet, PathRelativeToContainer, Hidden, Children, Context);

public record CrossLinkRef(Uri CrossLinkUri, string? Title, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem
{
	//TODO ensure we pass these to cross-links to
	// CrossLinks don't have a file system path, so we use the CrossLinkUri as the Path
	public string PathRelativeToDocumentationSet => CrossLinkUri.ToString();

	// CrossLinks don't have a file system path, so we use the CrossLinkUri as the Path
	public string PathRelativeToContainer => CrossLinkUri.ToString();

}

public record FolderRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem;

public record IsolatedTableOfContentsRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
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
			return new RuleOverviewReference(detectionRulesFile, detectionRulesFile, detectionRulesFolders, children, placeholderContext);
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
