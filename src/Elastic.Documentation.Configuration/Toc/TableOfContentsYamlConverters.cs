// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Converters;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

internal sealed class TocItemCollectionYamlConverter(TocItemYamlConverter itemConverter) : YamlConverter<TocEntryListDto>
{
	// Parameterless constructor required by the SharpYaml source generator for its static converter
	// fallback field; the real instance (with the resolved ApplicableToYamlConverter) is always supplied
	// via YamlSerializerOptions.Converters at runtime, which the generator consults first.
	public TocItemCollectionYamlConverter() : this(new TocItemYamlConverter(new ApplicableToYamlConverter())) { }

	public override TocEntryListDto Read(YamlReader reader)
	{
		var collection = new TocEntryListDto();

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

	public override void Write(YamlWriter writer, TocEntryListDto value) =>
		throw new NotSupportedException("TocEntryListDto YAML serialization is not supported; toc.yml is only ever read from YAML.");
}

internal sealed class TocItemYamlConverter(ApplicableToYamlConverter appliesToConverter) : YamlConverter<ITocEntryDto>
{
	// Parameterless constructor required by the SharpYaml source generator for its static converter
	// fallback field; the real instance is always supplied via YamlSerializerOptions.Converters at
	// runtime, which the generator consults first.
	public TocItemYamlConverter() : this(new ApplicableToYamlConverter()) { }

	// The keys each entry shape reads. A key written on an entry that its shape does not read is dropped, and one no shape reads is unknown;
	// both are recorded on the TocEntryAuthoring (see Constraints-Conventions.md).
	private static readonly string[] ListingKeys =
	[
		"listing",
		"glob",
		"extension",
		"groups",
		"visual",
		"island",
		"sort",
		"exclude",
		"children"
	];
	private static readonly string[] CliKeys = ["cli", "folder", "title", "navigation_title", "applies_to", "children"];
	private static readonly string[] FolderWithFileKeys = ["folder", "file", "children", "sort", "exclude", "default_cta"];
	private static readonly string[] DetectionRulesKeys = ["detection_rules", "file", "deprecated_file", "children"];
	private static readonly string[] FileKeys = ["file", "children", "default_cta"];
	private static readonly string[] CrossLinkKeys = ["crosslink", "title", "hidden", "children"];
	private static readonly string[] HiddenFileKeys = ["hidden", "children", "default_cta"];
	private static readonly string[] FolderKeys = ["folder", "children", "sort", "exclude", "default_cta"];
	private static readonly string[] NestedTocKeys = ["toc", "island", "children"];

	private static readonly string[] AllKeys =
	[
		.. ListingKeys,
		.. CliKeys,
		.. FolderWithFileKeys,
		.. DetectionRulesKeys,
		.. FileKeys,
		.. CrossLinkKeys,
		.. HiddenFileKeys,
		.. FolderKeys,
		.. NestedTocKeys
	];

	// A key outside these is an unknown key: reported when a collector listens, otherwise skipped.
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

	public override ITocEntryDto? Read(YamlReader reader)
	{
		var start = reader.Start;
		if (reader.TokenType != YamlTokenType.StartMapping)
		{
			// A bare scalar (e.g. "- toc.yml") is not a valid toc entry. Consume the token so
			// TocItemCollectionYamlConverter's while-loop can advance past it; return a sentinel
			// so the resolver can emit a clear diagnostic instead of silently dropping the entry.
			if (reader.TokenType == YamlTokenType.Scalar)
			{
				var scalarValue = reader.ScalarValue ?? string.Empty;
				_ = reader.Read();
				return new InvalidTocEntryDto(scalarValue);
			}
			// Anything else unexpected — skip to avoid an infinite loop.
			reader.Skip();
			TocAuthoringSink.Record(new TocEntryAuthoring(TocEntryKind.Dropped, "(not a mapping)") { Location = LocationOf(start) });
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
				reader.SkipOrReportUnmapped(typeof(ITocEntryDto), KnownKeys, key, null);
				// Kept as written so the entry's authoring record still lists it among its unknown keys.
				dictionary[key] = null;
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
					var childrenList = new List<ITocEntryDto>();
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
				? (IReadOnlyList<string>)groupsArr
				: null;
			var listingVisual = ListingVisual.None;
			if (dictionary.TryGetValue("visual", out var visualObj) && visualObj is string visualStr)
				_ = ListingVisualExtensions.TryParse(visualStr, out listingVisual);
			var island = dictionary.TryGetValue("island", out var islandObj)
				&& islandObj is string islandStr
				&& bool.TryParse(islandStr, out var islandBool)
				&& islandBool;
			// Reuse the already-captured sort and exclude variables from the outer scope; an unknown sort keeps path order
			SortOrder? listingSort = SortOrderExtensions.TryParse(sort, out var parsedSort) ? parsedSort : null;
			var options = new ListingOptions(glob, listingSort, exclude, listingVisual, groups, extension, island);
			Note(
				TocEntryKind.Listing,
				$"listing: {listing}",
				dictionary,
				ListingKeys,
				start,
				islandRaw: RawOf(dictionary, "island"),
				visualRaw: RawOf(dictionary, "visual"),
				sortRaw: RawOf(dictionary, "sort")
			);
			return new ListingEntryDto(listing, children, options);
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
			Note(TocEntryKind.CliReference, $"cli: {cliSchema}", dictionary, CliKeys, start);
			return new CliReferenceEntryDto(cliSchema, supplementalFolder, title, navigationTitle, children, appliesTo);
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
			var indexFile = new FolderIndexFileEntryDto(file);

			// Create a list with the index file first, followed by user-specified children
			var folderChildren = new List<ITocEntryDto> { indexFile };
			folderChildren.AddRange(children);

			// Return a FolderRef with the index file and children
			// The folder path can be deep (e.g., "guides/getting-started"), that's OK
			// PathRelativeToContainer will be set during resolution
			Note(TocEntryKind.FolderWithFile, $"folder: {folder}", dictionary, FolderWithFileKeys, start, explicitChildren: true);
			return new FolderEntryDto(folder, folderChildren, sort, exclude, defaultCta);
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
			Note(TocEntryKind.DetectionRules, $"file: {detectionRulesFile}", dictionary, DetectionRulesKeys, start);
			return new DetectionRulesEntryDto(detectionRulesFile, detectionRulesFolders, children, deprecatedFile);
		}

		// Check for file reference (file: or hidden:)
		// PathRelativeToContainer will be set during resolution
		if (dictionary.TryGetValue("file", out var filePathOnly) && filePathOnly is string fileOnly)
		{
			Note(TocEntryKind.File, $"file: {fileOnly}", dictionary, FileKeys, start);
			if (fileOnly == "index.md")
				return new IndexFileEntryDto(fileOnly, false, children, defaultCta);

			// Sugar: childless "file: subdir/index.md" → single-page folder, so it isn't silently dropped competing for the parent's index slot.
			if (children.Count == 0 && fileOnly.EndsWith("/index.md", StringComparison.Ordinal))
			{
				var indexFolderPath = fileOnly[..^"/index.md".Length];
				var indexFile = new FolderIndexFileEntryDto("index.md");
				return new DeepLinkedFolderEntryDto(indexFolderPath, [indexFile], defaultCta);
			}

			return new FileEntryDto(fileOnly, false, children, defaultCta);
		}

		// Check for crosslink reference before hidden:, which on a crosslink is a flag rather than a file
		if (dictionary.TryGetValue("crosslink", out var crosslink) && crosslink is string crosslinkStr)
		{
			var title = dictionary.TryGetValue("title", out var t) && t is string titleStr ? titleStr : null;
			var isHidden = dictionary.TryGetValue("hidden", out var h)
				&& h is string hiddenStr
				&& bool.TryParse(hiddenStr, out var hiddenBool)
				&& hiddenBool;
			Note(
				TocEntryKind.CrossLink,
				$"crosslink: {crosslinkStr}",
				dictionary,
				CrossLinkKeys,
				start,
				hiddenRaw: RawOf(dictionary, "hidden")
			);
			return new CrossLinkEntryDto(new Uri(crosslinkStr), title, isHidden, children);
		}

		if (dictionary.TryGetValue("hidden", out var hiddenPath) && hiddenPath is string p)
		{
			Note(TocEntryKind.HiddenFile, $"hidden: {p}", dictionary, HiddenFileKeys, start);
			return p == "index.md" ? new IndexFileEntryDto(p, true, children, defaultCta) : new FileEntryDto(p, true, children, defaultCta);
		}

		// Check for folder reference
		// PathRelativeToContainer will be set during resolution
		if (dictionary.TryGetValue("folder", out var folderPathOnly) && folderPathOnly is string folderOnly)
		{
			Note(TocEntryKind.Folder, $"folder: {folderOnly}", dictionary, FolderKeys, start, explicitChildren: children.Count > 0);
			return new FolderEntryDto(folderOnly, children, sort, exclude, defaultCta);
		}

		// Check for toc reference
		// PathRelativeToContainer will be set during resolution
		if (dictionary.TryGetValue("toc", out var tocPath) && tocPath is string source)
		{
			var island = dictionary.TryGetValue("island", out var islandObj)
				&& islandObj is string islandStr
				&& bool.TryParse(islandStr, out var islandBool)
				&& islandBool;
			Note(TocEntryKind.NestedToc, $"toc: {source}", dictionary, NestedTocKeys, start, islandRaw: RawOf(dictionary, "island"));
			return new NestedTocEntryDto(source, children, island);
		}

		TocAuthoringSink.Record(new TocEntryAuthoring(TocEntryKind.Dropped, $"{{{string.Join(", ", dictionary.Keys)}}}")
		{
			WrittenKeys = [.. dictionary.Keys],
			Location = LocationOf(start)
		});
		return null;
	}

	// SharpYaml's Mark.Line/Column are 0-based; +1 to match the 1-based convention used for user-facing locations.
	private static SourceLocation LocationOf(Mark start) => new(start.Line + 1, start.Column + 1);

	/// <summary>The text of a written key; empty when the value is not a scalar; null when the key is absent.</summary>
	private static string? RawOf(Dictionary<string, object?> written, string key) =>
		written.TryGetValue(key, out var value) ? value as string ?? "" : null;

	private static void Note(
		TocEntryKind kind,
		string entry,
		Dictionary<string, object?> written,
		string[] read,
		Mark start,
		bool explicitChildren = false,
		string? islandRaw = null,
		string? hiddenRaw = null,
		string? visualRaw = null,
		string? sortRaw = null
	) =>
		TocAuthoringSink.Record(new TocEntryAuthoring(kind, entry)
		{
			WrittenKeys = [.. written.Keys],
			UnknownKeys = UnknownYamlKeys.Of(written.Keys, AllKeys),
			IgnoredKeys = [.. written.Keys.Where(k => AllKeys.Contains(k) && !read.Contains(k))],
			ExplicitChildren = explicitChildren,
			Location = LocationOf(start),
			IslandRaw = islandRaw,
			HiddenRaw = hiddenRaw,
			VisualRaw = visualRaw,
			SortRaw = sortRaw
		});

	private static IReadOnlyList<ITocEntryDto> GetChildren(Dictionary<string, object?> dictionary)
	{
		if (!dictionary.TryGetValue("children", out var childrenObj))
			return [];

		// Children have already been deserialized as List<ITocEntryDto>
		if (childrenObj is List<ITocEntryDto> tocItems)
			return tocItems;

		return [];
	}

	public override void Write(YamlWriter writer, ITocEntryDto value) =>
		throw new NotSupportedException("Table of contents items are only ever read from YAML.");
}
