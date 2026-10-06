// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using System.Collections.Frozen;
using System.IO.Abstractions;
using DotNet.Globbing;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc.DetectionRules;
using Elastic.Documentation.Configuration.Toc.Listing;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.FileSystems;
using Nullean.ScopedFileSystem;
using YamlDotNet.Serialization;
using static Elastic.Documentation.SymlinkValidator;

namespace Elastic.Documentation.Configuration.Toc;

[YamlSerializable]
internal class DocumentationSetDto : TableOfContentsDto
{
	[YamlMember(Alias = "max_toc_depth")]
	public int MaxTocDepth { get; set; } = 2;

	[YamlMember(Alias = "dev_docs")]
	public bool DevDocs { get; set; }

	[YamlMember(Alias = "cross_links")]
	public List<string> CrossLinks { get; set; } = [];

	/// <summary>
	/// Products whose changelog content is sourced from the public CDN. Declaring a product here lets
	/// docs-builder prefetch its bundles at startup (consumed by the <c>{changelog}</c> <c>:cdn:</c> mode)
	/// and lets <c>changelog bundle</c> source that product's entries from the CDN.
	/// </summary>
	[YamlMember(Alias = "release_notes")]
	public List<ReleaseNotesProductDto> ReleaseNotes { get; set; } = [];

	[YamlMember(Alias = "exclude")]
	public List<string> Exclude { get; set; } = [];

	[YamlMember(Alias = "extensions")]
	public List<string> Extensions { get; set; } = [];

	[YamlMember(Alias = "subs")]
	public Dictionary<string, string> Subs { get; set; } = [];

	[Obsolete("Use the index.md h1 heading instead. This field will be removed in a future version.")]
	[YamlMember(Alias = "display_name")]
	public string? DisplayName { get; set; }

	[Obsolete("Use the index.md frontmatter description instead. This field will be removed in a future version.")]
	[YamlMember(Alias = "description")]
	public string? Description { get; set; }

	[YamlMember(Alias = "icon")]
	public string? Icon { get; set; }

	[YamlMember(Alias = "registry")]
	public string? Registry { get; set; }

	[YamlMember(Alias = "features")]
	public DocumentationSetFeaturesDto Features { get; set; } = new();

	[YamlMember(Alias = "api")]
	public Dictionary<string, ApiProductSequenceDto> Api { get; set; } = [];

	/// <summary>
	/// Default products for this documentation set. These are merged with page-level frontmatter products.
	/// </summary>
	[YamlMember(Alias = "products")]
	public List<ProductLinkDto> Products { get; set; } = [];

	/// <summary>
	/// Optional codex-specific metadata. Only contains <c>group</c> for codex navigation grouping.
	/// </summary>
	[YamlMember(Alias = "codex")]
	public CodexMembershipDto? Codex { get; set; }

	/// <summary>
	/// Optional white-label branding overrides. When present, all Elastic-specific chrome is suppressed.
	/// </summary>
	[YamlMember(Alias = "branding")]
	public BrandingDto? Branding { get; set; }

	[YamlMember(Alias = "storybook")]
	public StorybookDto? Storybook { get; set; }

	/// <summary>
	/// Named, reusable right-gutter CTA templates. Selected per-page via frontmatter <c>cta: &lt;name&gt;</c>;
	/// pages that omit it fall back to the built-in <c>trial</c> default.
	/// </summary>
	[YamlMember(Alias = "cta")]
	public Dictionary<string, CtaDto> Cta { get; set; } = [];

	private static new DocumentationSetDto Deserialize(string json) =>
		ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetDto>(json);

	/// <summary>
	/// Loads a DocumentationSetDto from a file for reading metadata only (e.g. codex section).
	/// Does not perform full TOC resolution.
	/// </summary>
	public static DocumentationSetDto LoadMetadata(IFileInfo file)
	{
		var yaml = file.FileSystem.File.ReadAllText(file.FullName);
		return Deserialize(yaml);
	}

	/// <summary>
	/// Loads a DocumentationSetDto and recursively resolves all IsolatedTableOfContentsRef items,
	/// replacing them with their resolved children and ensuring file paths carry over parent paths.
	/// Validates the table of contents structure and emits diagnostics for issues.
	/// </summary>
	public static DocumentationSetDto LoadAndResolve(
		IDiagnosticsCollector collector,
		IFileInfo docsetPath,
		ScopedFileSystem? fileSystem = null,
		HashSet<HintType>? noSuppress = null
	)
	{
		fileSystem ??= new CheckoutsFileSystem(docsetPath.Directory!, inner: docsetPath.FileSystem);
		// Validate that the docset.yml is not a symlink (security: prevents path traversal attacks)
		EnsureNotSymlink(docsetPath);
		var yaml = fileSystem.File.ReadAllText(docsetPath.FullName);
		var sourceDirectory = docsetPath.Directory!;
		return LoadAndResolve(collector, yaml, sourceDirectory, fileSystem, noSuppress);
	}

	/// <summary>
	/// File paths excluded from navigation via folder-level <c>exclude</c> in toc.yml.
	/// These files exist on disk but should not be processed by the builder.
	/// </summary>
	/// <summary>The resolved table of contents: every nested toc.yml loaded and every path final.</summary>
	[YamlIgnore]
	public IReadOnlyList<TocItem> TableOfContents { get; internal set; } = [];

	[YamlIgnore]
	public HashSet<string> FolderExcludedFiles { get; private set; } = [];

	/// <summary>Every <c>toc:</c> entry mapping read while resolving, with what the reader hid (unknown keys, raw text).</summary>
	[YamlIgnore]
	public IReadOnlyList<TocEntryAuthoring> TocAuthoring { get; private set; } = [];

	/// <summary>
	/// Pages registered with a default CTA via <c>default_cta</c> on <c>docset.yml</c>, nested <c>toc.yml</c> files,
	/// or individual <c>file:</c> / <c>folder:</c> entries.
	/// Keys are docset-root-relative markdown paths; values are template names from the docset's <c>cta</c> map.
	/// </summary>
	[YamlIgnore]
	public IReadOnlyDictionary<string, string> TocDefaultCtas { get; private set; } = FrozenDictionary<string, string>.Empty;

	/// <summary>
	/// Loads a DocumentationSetDto from YAML string and recursively resolves all IsolatedTableOfContentsRef items,
	/// replacing them with their resolved children and ensuring file paths carry over parent paths.
	/// Validates the table of contents structure and emits diagnostics for issues.
	/// </summary>
	public static DocumentationSetDto LoadAndResolve(
		IDiagnosticsCollector collector,
		string yaml,
		IDirectoryInfo sourceDirectory,
		ScopedFileSystem? fileSystem = null,
		HashSet<HintType>? noSuppress = null
	)
	{
		fileSystem ??= new CheckoutsFileSystem(sourceDirectory, inner: sourceDirectory.FileSystem);
		var docsetPath = fileSystem.Path.Join(sourceDirectory.FullName, "docset.yml").OptionalWindowsReplace();
		var authoring = new List<TocEntryAuthoring>();
		using var authoringScope = TocAuthoringSink.Begin(authoring, docsetPath);
		var docSet = Deserialize(yaml);
		docSet.SuppressDiagnostics.ExceptWith(noSuppress ?? []);
		docSet.TableOfContents = ResolveTableOfContents(
			collector,
			tocAncestors: [],
			docSet.TocEntries,
			sourceDirectory,
			fileSystem,
			parentPath: "",
			containerPath: "",
			context: docsetPath,
			docSet.SuppressDiagnostics
		);
		// Collect excluded paths so they can be skipped during file processing (not just navigation)
		docSet.FolderExcludedFiles = CollectFolderExcludedFiles(docSet.TableOfContents);
		docSet.TocDefaultCtas = CollectTocDefaultCtas(collector, docSet.TableOfContents, docSet.DefaultCta);
		docSet.TocAuthoring = authoring;
		return docSet;
	}

	/// <summary>
	/// Recursively resolves all IsolatedTableOfContentsRef items in a table of contents,
	/// loading nested TOC files and prepending parent paths to all file references.
	/// Preserves the hierarchy structure without flattening.
	/// Validates items and emits diagnostics for issues.
	/// </summary>
	private static IReadOnlyList<TocItem> ResolveTableOfContents(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> tocAncestors,
		IReadOnlyList<ITocEntryDto> items,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context,
		HashSet<HintType>? suppressDiagnostics = null
	)
	{
		var resolved = new List<TocItem>();

		foreach (var item in items)
		{
			var resolvedItem = item switch
			{
				InvalidTocEntryDto invalid => EmitInvalidTocItem(collector, invalid, context),
				NestedTocEntryDto tocRef =>
					ResolveIsolatedToc(
						collector,
						tocAncestors,
						tocRef,
						baseDirectory,
						fileSystem,
						parentPath,
						containerPath,
						context,
						suppressDiagnostics
					),
				DetectionRulesEntryDto ruleOverviewReference =>
					ResolveRuleOverviewReference(
						collector,
						tocAncestors,
						ruleOverviewReference,
						baseDirectory,
						fileSystem,
						parentPath,
						containerPath,
						context,
						suppressDiagnostics
					),
				CliReferenceEntryDto cliRef =>
					ResolveCliReference(collector, tocAncestors, cliRef, baseDirectory, fileSystem, parentPath, containerPath, context),
				ListingEntryDto listingRef =>
					ResolveListingRef(collector, tocAncestors, listingRef, baseDirectory, fileSystem, parentPath, containerPath, context),
				FileEntryDto fileRef =>
					ResolveFileRef(
						collector,
						tocAncestors,
						fileRef,
						baseDirectory,
						fileSystem,
						parentPath,
						containerPath,
						context,
						suppressDiagnostics
					),
				FolderEntryDto folderRef =>
					ResolveFolderRef(
						collector,
						tocAncestors,
						folderRef,
						baseDirectory,
						fileSystem,
						parentPath,
						containerPath,
						context,
						suppressDiagnostics
					),
				CrossLinkEntryDto crossLink =>
					ResolveCrossLinkRef(collector, tocAncestors, crossLink, baseDirectory, fileSystem, parentPath, containerPath, context),
				_ => null
			};

			// A pattern on a union matches its case value, so test the Nullable itself.
			if (resolvedItem.HasValue)
			{
				var value = resolvedItem.Value;
				resolved.Add(value);
				// Emit the deprecated rules overview as a sibling immediately after the active rules ref
				if (value is DetectionRuleOverviewRef { DeprecatedSiblingRef: { } deprecatedSibling })
					resolved.Add(deprecatedSibling);
			}
		}

		return resolved;
	}

	private static TocItem? EmitInvalidTocItem(IDiagnosticsCollector collector, InvalidTocEntryDto invalid, string context)
	{
		var raw = invalid.RawValue;
		var isYmlFile = raw.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
			|| raw.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);

		var hint = isYmlFile
			? $" An adjacent '{raw}' next to docset.yml is not supported. Inline its entries directly in docset.yml, or use 'toc: <folder>' to point to a subdirectory that contains a toc.yml."
			: string.Empty;

		collector.EmitError(
			context,
			$"'{raw}' is not a valid toc entry. Each entry must be a mapping with a 'file:', 'folder:', or 'toc:' key.{hint}"
		);
		return null;
	}

	/// <summary>
	/// Resolves an IsolatedTableOfContentsRef by loading the TOC file and returning a new ref with resolved children.
	/// Validates that the TOC has no children in parent YAML and that toc.yml exists.
	/// The TOC's path is set to the full path (including parent path) for consistency with files and folders.
	/// </summary>
#pragma warning disable IDE0060 // Remove unused parameter - suppressDiagnostics is for consistency, nested TOCs use their own suppression config
	private static TocItem? ResolveIsolatedToc(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> tocAncestors,
		NestedTocEntryDto tocRef,
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
		if (tocRef.Path.Contains('/'))
		{
			// Path contains '/', treat as context-relative (full path from the context file's directory)
			var contextDir = fileSystem.Path.GetDirectoryName(parentContext) ?? "";
			var contextRelativePath = fileSystem.Path.GetRelativePath(baseDirectory.FullName, contextDir);
			if (contextRelativePath == ".")
				contextRelativePath = "";

			fullTocPath = string.IsNullOrEmpty(contextRelativePath) ? tocRef.Path : $"{contextRelativePath}/{tocRef.Path}";
		}
		else
		{
			// Simple name, resolve relative to parent path
			fullTocPath = string.IsNullOrEmpty(parentPath) ? tocRef.Path : $"{parentPath}/{tocRef.Path}";
		}

		var tocDirectory = fileSystem.DirectoryInfo.New(fileSystem.Path.Join(baseDirectory.FullName, fullTocPath));
		var tocFilePath = fileSystem.Path.Join(tocDirectory.FullName, "toc.yml");
		var tocYmlExists = fileSystem.File.Exists(tocFilePath);

		// Validate: TOC should not have children defined in parent YAML
		if (tocRef.Children.Count > 0)
		{
			collector.EmitError(
				parentContext,
				$"TableOfContents '{fullTocPath}' may not contain children, define children in '{fullTocPath}/toc.yml' instead."
			);
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
			return new IsolatedTableOfContentsRef(
				DocsetPath.From(fullTocPath),
				ContainerPath.From(tocPathRelativeToContainer),
				[],
				DeclaringFilePath.From(parentContext),
				tocRef.Island
			);
		}

		// A toc.yml that includes one of its ancestors would recurse until the stack overflows
		var tocKey = fileSystem.Path.GetFullPath(tocFilePath);
		if (tocAncestors.Contains(tocKey, StringComparer.OrdinalIgnoreCase))
		{
			collector.EmitError(
				parentContext,
				$"Table of contents '{fullTocPath}/toc.yml' includes itself: {string.Join(" -> ", [.. tocAncestors, tocKey])}. Remove the 'toc:' entry that closes the loop."
			);
			return new IsolatedTableOfContentsRef(
				DocsetPath.From(fullTocPath),
				ContainerPath.From(tocPathRelativeToContainer),
				[],
				DeclaringFilePath.From(parentContext),
				tocRef.Island
			);
		}

		// Validate that the toc.yml is not a symlink (security: prevents path traversal attacks)
		EnsureNotSymlink(fileSystem, tocFilePath);

		var tocYaml = fileSystem.File.ReadAllText(tocFilePath);
		TableOfContentsDto nestedTocFile;
		using (TocAuthoringSink.InFile(tocFilePath.OptionalWindowsReplace()))
			nestedTocFile = TableOfContentsDto.Deserialize(tocYaml);

		// this is temporary after this lands in main we can update these files to include
		// suppress:
		//	- DeepLinkingVirtualFile
		string[] skip =
		[
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
		var resolvedChildren = ResolveTableOfContents(
			collector,
			[.. tocAncestors, tocKey],
			nestedTocFile.TocEntries,
			baseDirectory,
			fileSystem,
			fullTocPath,
			fullTocPath,
			tocFilePath,
			nestedTocFile.SuppressDiagnostics
		);

		// A nested table of contents without children is checked on the model: see DocumentationSetConstraints.

		// Return TOC ref with FULL path and resolved children.
		// Island flag is OR-ed: either the inline `- toc:` entry or the child toc.yml root can opt in.
		return new IsolatedTableOfContentsRef(
			DocsetPath.From(fullTocPath),
			ContainerPath.From(tocPathRelativeToContainer),
			resolvedChildren,
			DeclaringFilePath.From(parentContext),
			tocRef.Island || nestedTocFile.Island,
			nestedTocFile.DefaultCta
		);
	}

	/// <summary>
	/// Resolves a FileRef by prepending the parent path to the file path and recursively resolving children.
	/// The parent path provides the correct context for child resolution.
	/// </summary>
	private static TocItem ResolveFileRef(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> tocAncestors,
		FileEntryDto fileRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context,
		HashSet<HintType>? suppressDiagnostics = null
	)
	{
		var fullPath = string.IsNullOrEmpty(parentPath) ? fileRef.Path : $"{parentPath}/{fileRef.Path}";

		// Special validation for FolderIndexFileRef (folder+file combination)
		// Validate BEFORE early return so we catch cases with no children
		if (fileRef is FolderIndexFileEntryDto)
		{
			var fileName = fileRef.Path;
			var fileWithoutExtension = fileName.Replace(".md", "");

			// Validate: deep linking is NOT supported for folder+file combination
			// The file path should be simple (no '/'), or at most folder/file.md after prepending
			if (fileName.Contains('/'))
			{
				collector.EmitError(
					context,
					$"Deep linking on folder 'file' is not supported. Found file path '{fileName}' with '/'. Use simple file name only."
				);
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
					var normalizedFile = fileWithoutExtension
						.Replace("-", "", StringComparison.Ordinal)
						.Replace("_", "", StringComparison.Ordinal)
						.ToLowerInvariant();
					var normalizedFolder = folderName
						.Replace("-", "", StringComparison.Ordinal)
						.Replace("_", "", StringComparison.Ordinal)
						.ToLowerInvariant();

					if (!normalizedFile.Equals(normalizedFolder, StringComparison.Ordinal))
					{
						collector.EmitHint(
							context,
							$"File name '{fileName}' does not match folder name '{folderName}'. Best practice is to name the file the same as the folder (e.g., 'folder: {folderName}, file: {folderName}.md')."
						);
					}
				}
			}
		}

		// Calculate PathRelativeToContainer: the file path relative to its container
		var pathRelativeToContainer = string.IsNullOrEmpty(containerPath) ? fullPath : fullPath.Substring(containerPath.Length + 1);

		if (fileRef.Children.Count == 0)
		{
			// Preserve specific types even when there are no children
			return fileRef switch
			{
				FolderIndexFileEntryDto =>
					new FolderIndexFileRef(
						DocsetPath.From(fullPath),
						ContainerPath.From(pathRelativeToContainer),
						fileRef.Hidden,
						[],
						DeclaringFilePath.From(context),
						fileRef.DefaultCta
					),
				IndexFileEntryDto =>
					new IndexFileRef(
						DocsetPath.From(fullPath),
						ContainerPath.From(pathRelativeToContainer),
						fileRef.Hidden,
						[],
						DeclaringFilePath.From(context),
						fileRef.DefaultCta
					),
				_ =>
					new FileRef(
						DocsetPath.From(fullPath),
						ContainerPath.From(pathRelativeToContainer),
						fileRef.Hidden,
						[],
						DeclaringFilePath.From(context),
						fileRef.DefaultCta
					)
			};
		}

		// Emit hint if file has children and uses deep-linking (path contains '/')
		// This suggests using 'folder' instead of 'file' would be better
		if (fileRef.Path.Contains('/') && fileRef.Children.Count > 0 && fileRef is not FolderIndexFileEntryDto)
		{
			// Check if this hint type should be suppressed
			if (!suppressDiagnostics.ShouldSuppress(HintType.DeepLinkingVirtualFile))
			{
				collector.EmitHint(
					context,
					$"File '{fileRef.Path}' uses deep-linking with children. Consider using 'folder' instead of 'file' for better navigation structure. Virtual files are primarily intended to group sibling files together."
				);
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
		if (fileRef is FolderIndexFileEntryDto)
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
		var resolvedChildren = ResolveTableOfContents(
			collector,
			tocAncestors,
			fileRef.Children,
			baseDirectory,
			fileSystem,
			parentPathForChildren,
			containerPath,
			context,
			suppressDiagnostics
		);

		// Preserve the specific type when creating the resolved reference
		return fileRef switch
		{
			FolderIndexFileEntryDto =>
				new FolderIndexFileRef(
					DocsetPath.From(fullPath),
					ContainerPath.From(pathRelativeToContainer),
					fileRef.Hidden,
					resolvedChildren,
					DeclaringFilePath.From(context),
					fileRef.DefaultCta
				),
			IndexFileEntryDto =>
				new IndexFileRef(
					DocsetPath.From(fullPath),
					ContainerPath.From(pathRelativeToContainer),
					fileRef.Hidden,
					resolvedChildren,
					DeclaringFilePath.From(context),
					fileRef.DefaultCta
				),
			_ =>
				new FileRef(
					DocsetPath.From(fullPath),
					ContainerPath.From(pathRelativeToContainer),
					fileRef.Hidden,
					resolvedChildren,
					DeclaringFilePath.From(context),
					fileRef.DefaultCta
				)
		};
	}

	/// <summary>
	/// Resolves a FolderRef by prepending the parent path to the folder path and recursively resolving children.
	/// If no children are defined, auto-discovers .md files in the folder directory.
	/// </summary>
	private static TocItem ResolveRuleOverviewReference(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> tocAncestors,
		DetectionRulesEntryDto detectionRuleRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context,
		HashSet<HintType>? suppressDiagnostics = null
	)
	{
		// Folder paths containing '/' are treated as relative to the context file's directory (full paths).
		// Simple folder names (no '/') are resolved relative to the parent path in the navigation hierarchy.
		string fullPath;
		if (detectionRuleRef.Path.Contains('/'))
		{
			// Path contains '/', treat as context-relative (full path from the context file's directory)
			var contextDir = fileSystem.Path.GetDirectoryName(context) ?? "";
			var contextRelativePath = fileSystem.Path.GetRelativePath(baseDirectory.FullName, contextDir);
			if (contextRelativePath == ".")
				contextRelativePath = "";

			fullPath = string.IsNullOrEmpty(contextRelativePath) ? detectionRuleRef.Path : $"{contextRelativePath}/{detectionRuleRef.Path}";
		}
		else
		{
			// Simple name, resolve relative to parent path
			fullPath = string.IsNullOrEmpty(parentPath) ? detectionRuleRef.Path : $"{parentPath}/{detectionRuleRef.Path}";
		}

		// Calculate PathRelativeToContainer: the folder path relative to its container
		var pathRelativeToContainer = string.IsNullOrEmpty(containerPath) ? fullPath : fullPath.Substring(containerPath.Length + 1);

		// For children of folders, the container remains the same as the folder's container
		var resolvedChildren = ResolveTableOfContents(
			collector,
			tocAncestors,
			detectionRuleRef.Children,
			baseDirectory,
			fileSystem,
			fullPath,
			containerPath,
			context,
			suppressDiagnostics
		);

		var fileInfo = fileSystem.NewFileInfo(baseDirectory.FullName, fullPath);
		var tocSourceFolders = detectionRuleRef.RuleFolders.Select(f => fileSystem.NewDirInfo(fileInfo.Directory!.FullName, f)).ToList();
		var tomlChildren = DetectionRuleDiscovery.CreateTableOfContentItems(tocSourceFolders, context, baseDirectory);

		var children = resolvedChildren.ToList();
		children.AddRange(tomlChildren);

		// Auto-detect _deprecated subdirectories. When found, build the deprecated overview FileRef
		// and attach it as DeprecatedSiblingRef so ResolveTableOfContents can emit it as a sibling,
		// not as a child nested under the active rules.
		FileRef? deprecatedSiblingRef = null;
		var hasDeprecatedRules = tocSourceFolders.Any(
			d => d.Exists && d.EnumerateDirectories("_deprecated", SearchOption.TopDirectoryOnly).Any()
		);
		if (hasDeprecatedRules)
		{
			var deprecatedFileName = detectionRuleRef.DeprecatedFile ?? "deprecated-detection-rules.md";
			var overviewDir = fileSystem.Path.GetDirectoryName(fullPath);
			var deprecatedFullPath = string.IsNullOrEmpty(overviewDir) ? deprecatedFileName : $"{overviewDir}/{deprecatedFileName}";
			var deprecatedPathRelativeToContainer = string.IsNullOrEmpty(containerPath)
				? deprecatedFullPath
				: deprecatedFullPath.Substring(containerPath.Length + 1);
			var deprecatedTomlChildren = DetectionRuleDiscovery.CreateDeprecatedTableOfContentItems(
				tocSourceFolders,
				context,
				baseDirectory
			);
			deprecatedSiblingRef = new FileRef(
				DocsetPath.From(deprecatedFullPath),
				ContainerPath.From(deprecatedPathRelativeToContainer),
				false,
				deprecatedTomlChildren,
				DeclaringFilePath.From(context)
			);
		}

		return new DetectionRuleOverviewRef(
			DocsetPath.From(fullPath),
			ContainerPath.From(pathRelativeToContainer),
			detectionRuleRef.RuleFolders,
			children,
			DeclaringFilePath.From(context),
			detectionRuleRef.DeprecatedFile
		)
		{ DeprecatedSiblingRef = deprecatedSiblingRef };
	}

	private static TocItem? ResolveCliReference(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> tocAncestors,
		CliReferenceEntryDto cliRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context
	)
	{
		// Resolve schema path relative to docset root (context-relative for paths with '/')
		string schemaFullPath;
		if (cliRef.SchemaPath.Contains('/'))
		{
			var contextDir = fileSystem.Path.GetDirectoryName(context) ?? "";
			var contextRelativePath = fileSystem.Path.GetRelativePath(baseDirectory.FullName, contextDir);
			if (contextRelativePath == ".")
				contextRelativePath = "";
			schemaFullPath = string.IsNullOrEmpty(contextRelativePath) ? cliRef.SchemaPath : $"{contextRelativePath}/{cliRef.SchemaPath}";
		}
		else
		{
			schemaFullPath = string.IsNullOrEmpty(parentPath) ? cliRef.SchemaPath : $"{parentPath}/{cliRef.SchemaPath}";
		}

		var schemaFileInfo = fileSystem.FileInfo.New(fileSystem.Path.Join(baseDirectory.FullName, schemaFullPath));
		if (!schemaFileInfo.Exists)
		{
			collector.EmitError(context, $"CLI schema file not found: {cliRef.SchemaPath}");
			return null;
		}

		// Derive virtual root: use SupplementalFolder if set, otherwise schema path without extension
		var virtualRoot = cliRef.SupplementalFolder is not null
			? cliRef.SupplementalFolder.TrimEnd('/')
			: Path.ChangeExtension(schemaFullPath, null);

		var fullVirtualRoot = string.IsNullOrEmpty(parentPath) ? virtualRoot : $"{parentPath}/{virtualRoot}";
		var pathRelativeToContainer = string.IsNullOrEmpty(containerPath) ? fullVirtualRoot : fullVirtualRoot[(containerPath.Length + 1)..];

		// A missing supplemental folder is checked on the model: see DocumentationSetConstraints.

		// Resolve explicit children (regular docs + namespace/folder refs) relative to the virtual root
		var resolvedChildren = cliRef.Children.Count > 0
			? ResolveTableOfContents(
				collector,
				tocAncestors,
				cliRef.Children,
				baseDirectory,
				fileSystem,
				fullVirtualRoot,
				containerPath,
				context
			)
			: [];

		return new CliReferenceRef(
			schemaFullPath,
			cliRef.SupplementalFolder,
			cliRef.Title,
			cliRef.NavigationTitle,
			DocsetPath.From(fullVirtualRoot),
			ContainerPath.From(pathRelativeToContainer),
			DeclaringFilePath.From(context),
			resolvedChildren,
			cliRef.AppliesTo
		);
	}

	/// <summary>
	/// Resolves a FolderRef by prepending the parent path to the folder path and recursively resolving children.
	/// If no children are defined, auto-discovers .md files in the folder directory.
	/// </summary>
	private static TocItem ResolveFolderRef(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> tocAncestors,
		FolderEntryDto folderRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context,
		HashSet<HintType>? suppressDiagnostics = null
	)
	{
		// Folder paths containing '/' are treated as relative to the context file's directory (full paths).
		// Simple folder names (no '/') are resolved relative to the parent path in the navigation hierarchy.
		// DeepLinkedFolderRef (the "file: subdir/index.md" sugar) is the exception: it always resolves
		// relative to the parent, exactly like the FileRef it was expanded from, regardless of '/'.
		string fullPath;
		if (folderRef is not DeepLinkedFolderEntryDto && folderRef.Path.Contains('/'))
		{
			// Path contains '/', treat as context-relative (full path from the context file's directory)
			var contextDir = fileSystem.Path.GetDirectoryName(context) ?? "";
			var contextRelativePath = fileSystem.Path.GetRelativePath(baseDirectory.FullName, contextDir);
			if (contextRelativePath == ".")
				contextRelativePath = "";

			fullPath = string.IsNullOrEmpty(contextRelativePath) ? folderRef.Path : $"{contextRelativePath}/{folderRef.Path}";
		}
		else
		{
			// Simple name (or a DeepLinkedFolderRef), resolve relative to parent path
			fullPath = string.IsNullOrEmpty(parentPath) ? folderRef.Path : $"{parentPath}/{folderRef.Path}";
		}

		// Calculate PathRelativeToContainer: the folder path relative to its container
		var pathRelativeToContainer = string.IsNullOrEmpty(containerPath) ? fullPath : fullPath.Substring(containerPath.Length + 1);

		// Parse and validate sort order; the model only ever holds a valid one
		var sortIsValid = SortOrderExtensions.TryParse(folderRef.Sort, out var sortOrder);
		SortOrder? declaredSort = sortIsValid ? sortOrder : null;
		if (!sortIsValid && folderRef.Sort is not null)
			collector.EmitError(
				context,
				$"Unknown sort order '{folderRef.Sort}' for folder '{folderRef.Path}'." +
					" Valid values are: asc, ascending, desc, descending."
			);

		// If children are explicitly defined, resolve them
		if (folderRef.Children.Count > 0)
		{
			// For children of folders, the container remains the same as the folder's container
			var resolvedChildren = ResolveTableOfContents(
				collector,
				tocAncestors,
				folderRef.Children,
				baseDirectory,
				fileSystem,
				fullPath,
				containerPath,
				context,
				suppressDiagnostics
			);
			// Exclude is intentionally not passed through — it only applies to auto-discovery
			return new FolderRef(
				DocsetPath.From(fullPath),
				ContainerPath.From(pathRelativeToContainer),
				resolvedChildren,
				DeclaringFilePath.From(context),
				declaredSort,
				defaultCta: folderRef.DefaultCta
			);
		}

		// No children defined - auto-discover .md files in the folder
		// null preserves the default alphabetical sorting; non-null enables natural sort for version numbers
		var explicitSortOrder = folderRef.Sort is not null ? sortOrder : (SortOrder?)null;
		var autoDiscoveredChildren = AutoDiscoverFolderFiles(
			collector,
			tocAncestors,
			fullPath,
			containerPath,
			baseDirectory,
			fileSystem,
			context,
			explicitSortOrder,
			folderRef.Exclude
		);
		return new FolderRef(
			DocsetPath.From(fullPath),
			ContainerPath.From(pathRelativeToContainer),
			autoDiscoveredChildren,
			DeclaringFilePath.From(context),
			declaredSort,
			folderRef.Exclude,
			folderRef.DefaultCta
		);
	}

	/// <summary>
	/// Auto-discovers .md files in a folder directory and creates FileRef items for them.
	/// If index.md exists, it's placed first. Other files are sorted according to the specified sort order.
	/// Files starting with '_' or '.' are excluded, as well as any files listed in <paramref name="exclude"/>.
	/// </summary>
	private static IReadOnlyList<TocItem> AutoDiscoverFolderFiles(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> tocAncestors,
		string folderPath,
		string containerPath,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string context,
		SortOrder? sortOrder,
		IReadOnlyList<string>? exclude
	)
	{
		var directoryPath = fileSystem.Path.Join(baseDirectory.FullName, folderPath);
		var directory = fileSystem.DirectoryInfo.New(directoryPath);

		if (!directory.Exists)
			return [];

		// Find all .md files in the directory (not recursive)
		var excludeSet = exclude is { Count: > 0 } ? new HashSet<string>(exclude, StringComparer.OrdinalIgnoreCase) : null;
		var mdFiles = fileSystem
			.Directory
			.GetFiles(directoryPath, "*.md")
			.Select(f => fileSystem.FileInfo.New(f))
			.Where(f => !f.Name.StartsWith('_') && !f.Name.StartsWith('.'))
			.Where(f => excludeSet is null || !excludeSet.Contains(f.Name))
			.ToList();

		if (mdFiles.Count == 0)
			return [];

		// Separate index.md from other files
		var indexFile = mdFiles.FirstOrDefault(f => f.Name.Equals("index.md", StringComparison.OrdinalIgnoreCase));
		var otherFiles = mdFiles.Where(f => !f.Name.Equals("index.md", StringComparison.OrdinalIgnoreCase));

		// When sort is explicitly set (non-null), use natural sort order (handles version numbers correctly: 3_2 < 3_10)
		// When null, preserve the original alphabetical sorting behavior
		var sortedFiles = sortOrder switch
		{
			SortOrder.Descending => otherFiles.OrderByDescending(f => f.Name, NaturalStringComparer.Instance).ToList(),
			SortOrder.Ascending => otherFiles.OrderBy(f => f.Name, NaturalStringComparer.Instance).ToList(),
			_ => otherFiles.OrderBy(f => f.Name).ToList()
		};

		var children = new List<ITocEntryDto>();

		// Add index.md first if it exists
		if (indexFile != null)
			children.Add(new IndexFileEntryDto(indexFile.Name, false, []));

		// Add other files sorted according to the specified order
		foreach (var file in sortedFiles)
		{
			children.Add(new FileEntryDto(file.Name, false, []));
		}

		// Resolve the children with the folder path as parent to get correct full paths
		// Auto-discovered items are in the same container as the folder
		return ResolveTableOfContents(collector, tocAncestors, children, baseDirectory, fileSystem, folderPath, containerPath, context);
	}

	/// <summary>
	/// Traverses the resolved TOC and collects pages registered with a <c>default_cta</c> from
	/// <c>docset.yml</c>, nested <c>toc.yml</c> files, or individual <c>file:</c> / <c>folder:</c> entries.
	/// The nearest declaration wins.
	/// </summary>
	private static FrozenDictionary<string, string> CollectTocDefaultCtas(
		IDiagnosticsCollector collector,
		IReadOnlyList<TocItem> items,
		string? inheritedDefault
	)
	{
		var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		CollectTocDefaultCtas(collector, items, inheritedDefault, defaults);
		return defaults.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
	}

	private static void CollectTocDefaultCtas(
		IDiagnosticsCollector collector,
		IReadOnlyList<TocItem> items,
		string? inheritedDefault,
		Dictionary<string, string> defaults
	)
	{
		foreach (var item in items)
		{
			switch (item)
			{
				case FileRef file:
					var fileDefault = file.DefaultCta ?? inheritedDefault;
					RegisterTocDefaultCta(collector, file.PathRelativeToDocumentationSet, fileDefault, file.Context, defaults);
					if (file.Children.Count > 0)
						CollectTocDefaultCtas(collector, file.Children, fileDefault, defaults);
					break;
				case IsolatedTableOfContentsRef toc:
					CollectTocDefaultCtas(collector, toc.Children, toc.DefaultCta ?? inheritedDefault, defaults);
					break;
				case FolderRef folder:
					CollectTocDefaultCtas(collector, folder.Children, folder.DefaultCta ?? inheritedDefault, defaults);
					break;
				case CrossLinkRef crossLink when crossLink.Children.Count > 0:
					CollectTocDefaultCtas(collector, crossLink.Children, inheritedDefault, defaults);
					break;
			}
		}
	}

	private static void RegisterTocDefaultCta(
		IDiagnosticsCollector collector,
		string relativePath,
		string? defaultCta,
		string context,
		Dictionary<string, string> defaults
	)
	{
		if (string.IsNullOrWhiteSpace(defaultCta))
			return;

		var normalizedPath = NormalizeDocsetRelativePath(relativePath);
		if (defaults.TryGetValue(normalizedPath, out var existing) && !existing.Equals(defaultCta, StringComparison.OrdinalIgnoreCase))
		{
			collector.EmitError(
				context,
				$"'{normalizedPath}' is registered with default CTA '{existing}' and '{defaultCta}'. Each page can only have one default CTA."
			);
			return;
		}

		defaults[normalizedPath] = defaultCta;
	}

	internal static string NormalizeDocsetRelativePath(string relativePath)
	{
		var segments = relativePath.Replace('\\', '/').TrimStart('/').Split('/');
		var stack = new List<string>(segments.Length);
		foreach (var segment in segments)
		{
			if (segment is "" or ".")
				continue;
			if (segment == "..")
			{
				if (stack.Count > 0)
					stack.RemoveAt(stack.Count - 1);
				continue;
			}

			stack.Add(segment);
		}

		return string.Join('/', stack);
	}

	/// <summary>
	/// Traverses the resolved TOC and collects relative paths of files excluded via folder-level <c>exclude</c>.
	/// </summary>
	private static HashSet<string> CollectFolderExcludedFiles(IReadOnlyList<TocItem> items)
	{
		var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		CollectExcluded(items, excluded);
		return excluded;

		static void CollectExcluded(IReadOnlyList<TocItem> items, HashSet<string> result)
		{
			foreach (var item in items)
			{
				if (item is FolderRef { Exclude: { Count: > 0 } exclude } folder)
				{
					foreach (var fileName in exclude)
						_ = result.Add($"{folder.PathRelativeToDocumentationSet}/{fileName}");
				}

				// Recurse into children
				var children = item switch
				{
					FolderRef f => f.Children,
					FileRef f => f.Children,
					IsolatedTableOfContentsRef t => t.Children,
					_ => null
				};
				if (children is { Count: > 0 })
					CollectExcluded(children, result);
			}
		}
	}

	/// <summary>
	/// Resolves a <see cref="ListingRef"/> by glob-matching all content files under the listing folder,
	/// partitioning them into groups based on frontmatter, and emitting hidden <see cref="FileRef"/>s.
	/// </summary>
	private static TocItem? ResolveListingRef(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> tocAncestors,
		ListingEntryDto listingRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context
	)
	{
		// Resolve the full path (same pattern as ResolveFolderRef)
		string fullPath;
		if (listingRef.Path.Contains('/'))
		{
			var contextDir = fileSystem.Path.GetDirectoryName(context) ?? "";
			var contextRelativePath = fileSystem.Path.GetRelativePath(baseDirectory.FullName, contextDir);
			if (contextRelativePath == ".")
				contextRelativePath = "";
			fullPath = string.IsNullOrEmpty(contextRelativePath) ? listingRef.Path : $"{contextRelativePath}/{listingRef.Path}";
		}
		else
		{
			fullPath = string.IsNullOrEmpty(parentPath) ? listingRef.Path : $"{parentPath}/{listingRef.Path}";
		}

		var pathRelativeToContainer = string.IsNullOrEmpty(containerPath) ? fullPath : fullPath[(containerPath.Length + 1)..];

		var options = listingRef.Options;
		var globPattern = options.Glob ?? "**/*.md";

		var hasNonMdGlob = TableOfContentsRules.GlobMayMatchNonMarkdown(globPattern);

		// A listing that survives resolution has its glob checked on the model (DocumentationSetConstraints); one this
		// loader drops never reaches the model, so the same check is reported here before dropping it.
		void ReportGlobNeedingExtension()
		{
			if (hasNonMdGlob && string.IsNullOrEmpty(options.Extension))
				collector.EmitError(context, TableOfContentsRules.ListingGlobNeedsExtension(fullPath, globPattern));
		}

		// Build exclude globs
		var excludeGlobs = options.Exclude is { Count: > 0 } ? options.Exclude.Select(Glob.Parse).ToArray() : [];

		var listingDirAbsolute = fileSystem.Path.Join(baseDirectory.FullName, fullPath);
		var listingDir = fileSystem.DirectoryInfo.New(listingDirAbsolute);

		if (!listingDir.Exists)
		{
			ReportGlobNeedingExtension();
			collector.EmitError(context, $"Listing folder not found: {fullPath}");
			return null;
		}

		// Glob-match files. We enumerate all files in the folder tree and test against the pattern.
		var pattern = Glob.Parse(globPattern);
		var allFiles = listingDir
			.EnumerateFiles("*.*", SearchOption.AllDirectories)
			.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
			.Where(f => !f.Directory!.Attributes.HasFlag(FileAttributes.Hidden) && !f.Directory!.Attributes.HasFlag(FileAttributes.System))
			.Where(f => f.LinkTarget == null)
			.Where(f => !f.Name.StartsWith('_') && !f.Name.StartsWith('.'))
			.Where(f =>
			{
				var rel = Path.GetRelativePath(listingDirAbsolute, f.FullName).Replace('\\', '/');
				return pattern.IsMatch(rel);
			})
			.Where(f =>
			{
				if (excludeGlobs.Length == 0)
					return true;
				var rel = Path.GetRelativePath(listingDirAbsolute, f.FullName).Replace('\\', '/');
				return !excludeGlobs.Any(g => g.IsMatch(rel));
			})
			.ToList();

		if (allFiles.Count == 0)
		{
			ReportGlobNeedingExtension();
			return null;
		}

		// Read listing group from frontmatter of each .md file
		var fileGroups = new Dictionary<IFileInfo, string?>(ReferenceEqualityComparer.Instance);
		foreach (var file in allFiles)
		{
			if (file.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
				fileGroups[file] = ListingFrontMatterReader.ReadGroup(file);
			else
				fileGroups[file] = null; // extension will provide group via TryGetListingGroup
		}

		// Separate root/group index pages from content pages
		var rootIndex = allFiles.FirstOrDefault(
			f => f.FullName.Equals(fileSystem.Path.Join(listingDirAbsolute, "index.md"), StringComparison.OrdinalIgnoreCase)
		);

		var groupIndexFiles = allFiles.Where(
			f => f.Name.Equals("index.md", StringComparison.OrdinalIgnoreCase) && f != rootIndex
		).ToDictionary(
			f => Path.GetRelativePath(listingDirAbsolute, f.Directory!.FullName).Replace('\\', '/'),
			f => f,
			StringComparer.OrdinalIgnoreCase
		);

		var contentFiles = allFiles.Where(f => f != rootIndex && !groupIndexFiles.ContainsValue(f)).ToList();

		// Sort content files
		contentFiles = options.Sort is { } sortOrder
			? sortOrder == SortOrder.Descending
				? contentFiles.OrderByDescending(f => f.Name, NaturalStringComparer.Instance).ToList()
				: contentFiles.OrderBy(f => f.Name, NaturalStringComparer.Instance).ToList()
			: contentFiles.OrderBy(f => Path.GetRelativePath(listingDirAbsolute, f.FullName)).ToList();

		// Partition content files into groups
		var groupedFiles = new Dictionary<string, List<IFileInfo>>(StringComparer.OrdinalIgnoreCase);
		var ungrouped = new List<IFileInfo>();

		foreach (var file in contentFiles)
		{
			var group = fileGroups[file];
			if (!string.IsNullOrEmpty(group))
			{
				if (!groupedFiles.ContainsKey(group))
					groupedFiles[group] = [];
				groupedFiles[group].Add(file);
			}
			else
			{
				ungrouped.Add(file);
			}
		}

		// Determine group order: explicit groups first (in listed order), then remaining alphabetically
		var orderedGroups = new List<string>();
		if (options.Groups is { Count: > 0 })
		{
			foreach (var g in options.Groups)
				orderedGroups.Add(g);
		}
		foreach (var g in groupedFiles.Keys.Where(k => !orderedGroups.Contains(k, StringComparer.OrdinalIgnoreCase)).OrderBy(k => k))
			orderedGroups.Add(g);

		var tocItems = new List<TocItem>();

		// Root index (visible) — always emit; ListingDocsBuilderExtension creates a synthetic page if missing.
		var rootRelPath = $"{fullPath}/index.md";
		var rootRelContainer = string.IsNullOrEmpty(containerPath) ? rootRelPath : rootRelPath[(containerPath.Length + 1)..];
		tocItems.Add(
			new IndexFileRef(DocsetPath.From(rootRelPath), ContainerPath.From(rootRelContainer), false, [], DeclaringFilePath.From(context))
		);

		// Group children
		foreach (var groupKey in orderedGroups)
		{
			if (!groupedFiles.TryGetValue(groupKey, out var groupFiles))
				continue;

			// Always emit an IndexFileRef for the group — points to a real file if one exists,
			// otherwise to the synthetic path that ListingDocsBuilderExtension will register.
			var groupIndexPath = $"{fullPath}/{groupKey}/index.md";

			var groupChildren = new List<TocItem>();

			var groupIdxRelContainer = string.IsNullOrEmpty(containerPath) ? groupIndexPath : groupIndexPath[(containerPath.Length + 1)..];
			groupChildren.Add(
				new IndexFileRef(
					DocsetPath.From(groupIndexPath),
					ContainerPath.From(groupIdxRelContainer),
					false,
					[],
					DeclaringFilePath.From(context)
				)
			);

			// Content pages (always hidden)
			foreach (var file in groupFiles)
			{
				var relToSet = Path.GetRelativePath(baseDirectory.FullName, file.FullName).Replace('\\', '/');
				var relToContainer = string.IsNullOrEmpty(containerPath) ? relToSet : relToSet[(containerPath.Length + 1)..];
				groupChildren.Add(
					new FileRef(DocsetPath.From(relToSet), ContainerPath.From(relToContainer), true, [], DeclaringFilePath.From(context))
				);
			}

			tocItems.Add(
				new ListingGroupRef(
					groupKey,
					DocsetPath.From(fullPath),
					ContainerPath.From(pathRelativeToContainer),
					groupChildren,
					DeclaringFilePath.From(context)
				)
			);
		}

		// Ungrouped pages (always hidden)
		foreach (var file in ungrouped)
		{
			var relToSet = Path.GetRelativePath(baseDirectory.FullName, file.FullName).Replace('\\', '/');
			var relToContainer = string.IsNullOrEmpty(containerPath) ? relToSet : relToSet[(containerPath.Length + 1)..];
			tocItems.Add(
				new FileRef(DocsetPath.From(relToSet), ContainerPath.From(relToContainer), true, [], DeclaringFilePath.From(context))
			);
		}

		// Resolve explicit children (from YAML `children:`) last
		var explicitChildren = listingRef.Children.Count > 0
			? ResolveTableOfContents(
				collector,
				tocAncestors,
				listingRef.Children,
				baseDirectory,
				fileSystem,
				fullPath,
				containerPath,
				context
			)
			: [];
		tocItems.AddRange(explicitChildren);

		return new ListingRef(
			DocsetPath.From(fullPath),
			ContainerPath.From(pathRelativeToContainer),
			tocItems,
			DeclaringFilePath.From(context),
			options
		);
	}

	/// <summary>
	/// Resolves a CrossLinkRef by recursively resolving children (though cross-links typically don't have children).
	/// </summary>
	private static TocItem ResolveCrossLinkRef(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> tocAncestors,
		CrossLinkEntryDto crossLinkRef,
		IDirectoryInfo baseDirectory,
		IFileSystem fileSystem,
		string parentPath,
		string containerPath,
		string context
	)
	{
		if (crossLinkRef.Children.Count == 0)
			return new CrossLinkRef(
				crossLinkRef.CrossLinkUri,
				crossLinkRef.Title,
				crossLinkRef.Hidden,
				[],
				DeclaringFilePath.From(context)
			);

		// For children of cross-links, the container remains the same
		var resolvedChildren = ResolveTableOfContents(
			collector,
			tocAncestors,
			crossLinkRef.Children,
			baseDirectory,
			fileSystem,
			parentPath,
			containerPath,
			context
		);

		return new CrossLinkRef(
			crossLinkRef.CrossLinkUri,
			crossLinkRef.Title,
			crossLinkRef.Hidden,
			resolvedChildren,
			DeclaringFilePath.From(context)
		);
	}
}

[YamlSerializable]
internal class DocumentationSetFeaturesDto
{
	[YamlMember(Alias = "primary-nav", ApplyNamingConventions = false)]
	public bool? PrimaryNav { get; set; }
	[YamlMember(Alias = "disable-github-edit-link", ApplyNamingConventions = false)]
	public bool? DisableGithubEditLink { get; set; }
	[YamlMember(Alias = "guide-nav", ApplyNamingConventions = false)]
	public bool? GuideNav { get; set; }
}

[YamlSerializable]
internal class StorybookDto
{
	[YamlMember(Alias = "registry")]
	public string? Registry { get; set; }
}

/// <summary>
/// A single <c>release_notes</c> entry declaring a product whose changelog content is CDN-backed.
/// </summary>
[YamlSerializable]
internal record ReleaseNotesProductDto
{
	[YamlMember(Alias = "product")]
	public string Product { get; set; } = string.Empty;
}

/// <summary>
/// Codex-specific metadata. Only contains <c>group</c> for navigation grouping in a codex environment.
/// </summary>
[YamlSerializable]
internal class CodexMembershipDto
{
	[YamlMember(Alias = "group")]
	public string? Group { get; set; }
}

/// <summary>
/// White-label branding overrides for isolated builds. Presence of this section removes all Elastic-specific chrome.
/// All image paths are relative to the directory containing <c>docset.yml</c>.
/// </summary>
[YamlSerializable]
internal class BrandingDto
{
	/// <summary>Path to the site icon image, relative to the docs source directory.</summary>
	[YamlMember(Alias = "icon")]
	public string? Icon { get; set; }

	/// <summary>CSS colour value for the header background. Defaults to <c>#000000</c> when not specified.</summary>
	[YamlMember(Alias = "header-bg", ApplyNamingConventions = false)]
	public string? HeaderBg { get; set; }

	/// <summary>Path to the Open Graph image, relative to the docs source directory.</summary>
	[YamlMember(Alias = "og-image", ApplyNamingConventions = false)]
	public string? OgImage { get; set; }

	/// <summary>
	/// Path to the browser favicon, relative to the docs source directory.
	/// When not set, auto-discovered from well-known filenames (favicon.ico, favicon.png, favicon.svg).
	/// </summary>
	[YamlMember(Alias = "favicon", ApplyNamingConventions = false)]
	public string? Favicon { get; set; }

	/// <summary>
	/// Path to the Apple touch icon, relative to the docs source directory.
	/// When not set, auto-discovered from apple-touch-icon.png if present.
	/// </summary>
	[YamlMember(Alias = "apple-touch-icon", ApplyNamingConventions = false)]
	public string? AppleTouchIcon { get; set; }
}

/// <summary>
/// A single named right-gutter CTA template, as declared under <c>docset.yml</c>'s <c>cta</c> map.
/// </summary>
[YamlSerializable]
internal class CtaDto
{
	[YamlMember(Alias = "button")]
	public CtaButtonDto? Button { get; set; }

	[YamlMember(Alias = "benefits")]
	public List<string> Benefits { get; set; } = [];
}

/// <summary>
/// The clickable button portion of a <see cref="CtaDto"/>.
/// </summary>
[YamlSerializable]
internal class CtaButtonDto
{
	[YamlMember(Alias = "label")]
	public string? Label { get; set; }

	[YamlMember(Alias = "url")]
	public string? Url { get; set; }
}
