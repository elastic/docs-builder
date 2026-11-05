// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Documentation.Configuration.Toc.DetectionRules;
using Elastic.Documentation.Diagnostics;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

public class TocItemCollectionYamlConverter(TocItemYamlConverter itemConverter) : YamlConverter<TableOfContents>
{
	// Parameterless constructor required by the SharpYaml source generator for its static converter
	// fallback field; the real instance (with the resolved ApplicableToYamlConverter) is always supplied
	// via YamlSerializerOptions.Converters at runtime, which the generator consults first.
	public TocItemCollectionYamlConverter() : this(new TocItemYamlConverter(new ApplicableToYamlConverter())) { }

	public override TableOfContents Read(YamlReader reader)
	{
		var collection = new TableOfContents();

		if (reader.TokenType != YamlTokenType.StartSequence)
		{
			reader.Skip();
			return collection;
		}

		_ = reader.Read(); // consume StartSequence

		while (reader.TokenType != YamlTokenType.EndSequence)
		{
			var item = itemConverter.Read(reader);
			if (item is not null)
				collection.Add(item);
		}
		_ = reader.Read(); // consume EndSequence

		return collection;
	}

	public override void Write(YamlWriter writer, TableOfContents value) =>
		throw new NotSupportedException("TableOfContents YAML serialization is not supported; toc.yml is only ever read from YAML.");
}

public class TocItemYamlConverter(ApplicableToYamlConverter appliesToConverter) : YamlConverter<ITableOfContentsItem>
{
	// Parameterless constructor required by the SharpYaml source generator for its static converter
	// fallback field; the real instance is always supplied via YamlSerializerOptions.Converters at
	// runtime, which the generator consults first.
	public TocItemYamlConverter() : this(new ApplicableToYamlConverter()) { }

	private static readonly string[] KnownKeys =
	[
		"file",
		"folder",
		"hidden",
		"toc",
		"crosslink",
		"children",
		"sort",
		"exclude",
		"default_cta",
		"listing",
		"glob",
		"extension",
		"groups",
		"visual",
		"island",
		"cli",
		"title",
		"navigation_title",
		"applies_to",
		"detection_rules",
		"deprecated_file",
		"deprecated_detection_rules"
	];

	public override ITableOfContentsItem? Read(YamlReader reader)
	{
		if (reader.TokenType != YamlTokenType.StartMapping)
		{
			// A bare scalar (e.g. "- toc.yml") is not a valid toc entry. Consume the token so
			// the collection converter's while-loop can advance past it; return a sentinel
			// so the resolver can emit a clear diagnostic instead of silently dropping the entry.
			if (reader.TokenType == YamlTokenType.Scalar)
			{
				var scalarValue = reader.ScalarValue ?? string.Empty;
				_ = reader.Read();
				return new InvalidTocItemRef(scalarValue);
			}
			// Anything else unexpected — skip to avoid an infinite loop.
			reader.Skip();
			return null;
		}

		_ = reader.Read(); // consume StartMapping
		var dictionary = new Dictionary<string, object?>();

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();

			if (!KnownKeys.Contains(key))
			{
				reader.SkipOrReportUnmapped(typeof(ITableOfContentsItem), KnownKeys, key, null);
				continue;
			}

			// Parse the value based on what type it is
			object? value = null;
			if (reader.TokenType == YamlTokenType.Scalar)
			{
				value = reader.ScalarValue;
				_ = reader.Read();
			}
			else if (reader.TokenType == YamlTokenType.StartSequence)
			{
				// This is a list - parse it manually for "children"
				if (key == "children")
				{
					// Parse the children list manually
					var childrenList = new List<ITableOfContentsItem>();
					_ = reader.Read(); // consume StartSequence
					while (reader.TokenType != YamlTokenType.EndSequence)
					{
						var child = Read(reader);
						if (child is not null)
							childrenList.Add(child);
					}
					_ = reader.Read(); // consume EndSequence
					value = childrenList;
				}
				else if (key is "detection_rules" or "exclude" or "deprecated_detection_rules" or "groups")
				{
					// Parse the children list manually
					var childrenList = new List<string>();
					_ = reader.Read(); // consume StartSequence
					while (reader.TokenType != YamlTokenType.EndSequence)
					{
						if (reader.TokenType == YamlTokenType.Scalar)
							childrenList.Add(reader.ScalarValue ?? string.Empty);
						_ = reader.Read();
					}
					_ = reader.Read(); // consume EndSequence
					value = childrenList.ToArray();
				}
				else
				{
					// For other lists, just skip them
					reader.Skip();
				}
			}
			else if (reader.TokenType == YamlTokenType.StartMapping)
			{
				if (key == "applies_to")
					value = appliesToConverter.Read(reader);
				else
					reader.Skip();
			}

			dictionary[key] = value;
		}
		_ = reader.Read(); // consume EndMapping

		var children = GetChildren(dictionary);

		// Context will be set during LoadAndResolve, use empty string as placeholder during deserialization
		const string placeholderContext = "";

		// Capture raw sort value; parsing and validation happen during resolution
		var sort = dictionary.TryGetValue("sort", out var sortValue) && sortValue is string sortStr ? sortStr : null;

		// Capture exclude list for folder auto-discovery
		var exclude = dictionary.TryGetValue("exclude", out var excludeObj) && excludeObj is string[] excludeArr ? excludeArr : null;

		// Capture default_cta for file: and folder: entries; validated against docset.yml templates during resolution
		var defaultCta = dictionary.TryGetValue("default_cta", out var defaultCtaObj) && defaultCtaObj is string defaultCtaStr
			? defaultCtaStr
			: null;

		// Check for listing (listing: <folder>) — a glob-driven auto-discovered nav entry with generated index.
		// Must come before folder: and file: so those keys can still appear on the entry as children context.
		if (dictionary.TryGetValue("listing", out var listingPath) && listingPath is string listing)
		{
			var glob = dictionary.TryGetValue("glob", out var globObj) && globObj is string globStr ? globStr : null;
			var extension = dictionary.TryGetValue("extension", out var extObj) && extObj is string extStr ? extStr : null;
			var groups = dictionary.TryGetValue("groups", out var groupsObj) && groupsObj is string[] groupsArr
				? (IReadOnlyCollection<string>)groupsArr
				: null;
			var listingVisual = ListingVisual.None;
			if (dictionary.TryGetValue("visual", out var visualObj) && visualObj is string visualStr)
				_ = ListingVisualExtensions.TryParse(visualStr, out listingVisual);
			var island = dictionary.TryGetValue("island", out var islandObj)
				&& islandObj is string islandStr
				&& bool.TryParse(islandStr, out var islandBool)
				&& islandBool;
			// Reuse the already-captured sort and exclude variables from the outer scope
			var options = new ListingOptions(glob, sort, exclude, listingVisual, groups, extension, island);
			return new ListingRef(listing, listing, children, placeholderContext, options);
		}

		// Check for CLI reference (cli: schema.json, optional folder: supplemental-docs/)
		// Must come before the folder+file check to prevent folder: from being consumed by that branch
		if (dictionary.TryGetValue("cli", out var cliSchemaPath) && cliSchemaPath is string cliSchema)
		{
			var supplementalFolder = dictionary.TryGetValue("folder", out var f) && f is string fStr ? fStr : null;
			var title = dictionary.TryGetValue("title", out var t) && t is string titleStr ? titleStr : null;
			var navigationTitle = dictionary.TryGetValue("navigation_title", out var nt) && nt is string navigationTitleStr
				? navigationTitleStr
				: null;
			var appliesTo = dictionary.TryGetValue("applies_to", out var at) && at is ApplicableTo a ? a : null;
			return new CliReferenceRef(
				cliSchema,
				supplementalFolder,
				title,
				navigationTitle,
				cliSchema,
				cliSchema,
				placeholderContext,
				children,
				appliesTo
			);
		}

		// Check for folder+file combination (e.g., folder: getting-started, file: getting-started.md)
		// This represents a folder with a specific index file
		// The file becomes a child of the folder (as FolderIndexFileRef), and user-specified children follow
		if (
			dictionary.TryGetValue("folder", out var folderPath)
			&& folderPath is string folder
			&& dictionary.TryGetValue("file", out var filePath)
			&& filePath is string file
		)
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
			return new FolderRef(folder, folder, folderChildren, placeholderContext, sort, exclude, defaultCta);
		}
		if (
			dictionary.TryGetValue("detection_rules", out var detectionRulesObj)
			&& detectionRulesObj is string[] detectionRulesFolders
			&& dictionary.TryGetValue("file", out var detectionRulesFilePath)
			&& detectionRulesFilePath is string detectionRulesFile
		)
		{
			var deprecatedFile = dictionary.TryGetValue("deprecated_file", out var deprecatedFileObj) && deprecatedFileObj is string df
				? df
				: null;
			return new DetectionRuleOverviewRef(
				detectionRulesFile,
				detectionRulesFile,
				detectionRulesFolders,
				children,
				placeholderContext,
				deprecatedFile
			);
		}

		// Check for file reference (file: or hidden:)
		// PathRelativeToContainer will be set during resolution
		if (dictionary.TryGetValue("file", out var filePathOnly) && filePathOnly is string fileOnly)
		{
			if (fileOnly == "index.md")
				return new IndexFileRef(fileOnly, fileOnly, false, children, placeholderContext, defaultCta);

			// Sugar: childless "file: subdir/index.md" → single-page folder, so it isn't silently dropped competing for the parent's index slot.
			if (children.Count == 0 && fileOnly.EndsWith("/index.md", StringComparison.Ordinal))
			{
				var indexFolderPath = fileOnly[..^"/index.md".Length];
				var indexFile = new FolderIndexFileRef("index.md", "index.md", false, [], placeholderContext);
				return new DeepLinkedFolderRef(indexFolderPath, indexFolderPath, [indexFile], placeholderContext, defaultCta);
			}

			return new FileRef(fileOnly, fileOnly, false, children, placeholderContext, defaultCta);
		}

		if (dictionary.TryGetValue("hidden", out var hiddenPath) && hiddenPath is string p)
			return p == "index.md"
				? new IndexFileRef(p, p, true, children, placeholderContext, defaultCta)
				: new FileRef(p, p, true, children, placeholderContext, defaultCta);

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
			return new FolderRef(folderOnly, folderOnly, children, placeholderContext, sort, exclude, defaultCta);

		// Check for toc reference
		// PathRelativeToContainer will be set during resolution
		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string source)
		{
			var island = dictionary.TryGetValue("island", out var islandObj)
				&& islandObj is string islandStr
				&& bool.TryParse(islandStr, out var islandBool)
				&& islandBool;
			return new IsolatedTableOfContentsRef(source, source, children, placeholderContext, island);
		}

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

	public override void Write(YamlWriter writer, ITableOfContentsItem value) =>
		throw new NotSupportedException("Table of contents items are only ever read from YAML.");
}
