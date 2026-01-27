// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using System.Text.Json.Serialization;
using Elastic.Changelog.Configuration;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using NetEscapades.EnumGenerators;
using YamlDotNet.Core;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Arguments for the RenderChangelogs method
/// </summary>
public record RenderChangelogsArguments
{
	public required IReadOnlyCollection<BundleInput> Bundles { get; init; }
	public string? Output { get; init; }
	public string? Title { get; init; }
	public bool Subsections { get; init; }
	public string[]? HideFeatures { get; init; }
	public string? Config { get; init; }
	public ChangelogFileType FileType { get; init; } = ChangelogFileType.Markdown;
}

/// <summary>
/// Input for a single bundle file with optional directory, repo, and link visibility
/// </summary>
public record BundleInput
{
	public required string BundleFile { get; init; }
	public string? Directory { get; init; }
	public string? Repo { get; init; }
	/// <summary>
	/// Whether to hide PR/issue links for entries from this bundle.
	/// When true, links are commented out in the markdown output.
	/// Defaults to false (links are shown).
	/// </summary>
	public bool HideLinks { get; init; }
}

[EnumExtensions]
public enum ChangelogFileType
{
	[Display(Name = "markdown")]
	[JsonStringEnumMemberName("markdown")]
	Markdown,
	[Display(Name = "asciidoc")]
	[JsonStringEnumMemberName("asciidoc")]
	Asciidoc
}

/// <summary>
/// Service for rendering changelog output (markdown or asciidoc)
/// </summary>
public class ChangelogRenderingService(
	ILoggerFactory logFactory,
	IConfigurationContext? configurationContext = null,
	IFileSystem? fileSystem = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogRenderingService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

	public async Task<bool> RenderChangelogs(
		IDiagnosticsCollector collector,
		RenderChangelogsArguments input,
		Cancel ctx
	)
	{
		try
		{
			// Validate input
			if (input.Bundles.Count == 0)
			{
				collector.EmitError(string.Empty, "At least one bundle file is required. Use --input to specify bundle files.");
				return false;
			}

			// Validation phase: Load and validate all bundles
			var validationService = new BundleValidationService(_fileSystem);
			var validationResult = await validationService.ValidateBundlesAsync(collector, input.Bundles, ctx);

			if (!validationResult.IsValid || collector.Errors > 0)
				return false;

			// Merge phase: Resolve all entries from validated bundles
			var resolver = new BundleDataResolver(_fileSystem);
			var resolvedResult = await resolver.ResolveEntriesAsync(validationResult.Bundles, ctx);

			if (resolvedResult.Entries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries to render");
				return false;
			}

			// Setup output
			var outputSetup = SetupOutput(collector, input, resolvedResult.AllProducts);

			// Load changelog configuration
			var configLoader = new ChangelogConfigurationLoader(logFactory, configurationContext!, _fileSystem);
			var config = await configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);
			if (config == null)
			{
				collector.EmitError(string.Empty, "Failed to load changelog configuration");
				return false;
			}

			// Load feature IDs to hide
			var featureHidingLoader = new FeatureHidingLoader(_fileSystem);
			var featureHidingResult = await featureHidingLoader.LoadFeatureIdsAsync(collector, input.HideFeatures, ctx);
			if (!featureHidingResult.IsValid)
				return false;

			// Emit warnings for hidden entries
			EmitHiddenEntryWarnings(collector, resolvedResult.Entries, featureHidingResult.FeatureIdsToHide);

			// Validate entry types
			if (!ValidateEntryTypes(collector, resolvedResult.Entries, config.Types))
				return false;

			// Build render context
			var context = BuildRenderContext(input, outputSetup, resolvedResult, featureHidingResult.FeatureIdsToHide, config);

			// Render output
			var renderer = new ChangelogRenderer(_fileSystem, _logger);
			await renderer.RenderAsync(input.FileType, context, ctx);

			return true;
		}
		catch (IOException ioEx)
		{
			collector.EmitError(string.Empty, $"IO error rendering changelogs: {ioEx.Message}", ioEx);
			return false;
		}
		catch (UnauthorizedAccessException uaEx)
		{
			collector.EmitError(string.Empty, $"Access denied rendering changelogs: {uaEx.Message}", uaEx);
			return false;
		}
		catch (YamlException yamlEx)
		{
			collector.EmitError(string.Empty, $"YAML parsing error: {yamlEx.Message}", yamlEx);
			return false;
		}
	}

	private OutputSetup SetupOutput(
		IDiagnosticsCollector collector,
		RenderChangelogsArguments input,
		IReadOnlySet<(string product, string target)> allProducts)
	{
		// Determine output directory
		var outputDir = input.Output ?? _fileSystem.Directory.GetCurrentDirectory();
		if (!_fileSystem.Directory.Exists(outputDir))
			_ = _fileSystem.Directory.CreateDirectory(outputDir);

		// Extract version from products (use first product's target if available, or "unknown")
		var version = allProducts.Count > 0
			? allProducts.OrderBy(p => p.product).ThenBy(p => p.target).First().target
			: "unknown";

		if (string.IsNullOrWhiteSpace(version))
			version = "unknown";

		// Warn if --title was not provided and version defaults to "unknown"
		if (string.IsNullOrWhiteSpace(input.Title) && version == "unknown")
			collector.EmitWarning(string.Empty, "No --title option provided and bundle files do not contain 'target' values. Output folder and markdown titles will default to 'unknown'. Consider using --title to specify a custom title.");

		// Use title from input or default to version
		var title = input.Title ?? version;
		var titleSlug = ChangelogTextUtilities.TitleToSlug(title);

		return new OutputSetup(outputDir, title, titleSlug);
	}

	private static void EmitHiddenEntryWarnings(
		IDiagnosticsCollector collector,
		IReadOnlyList<ResolvedEntry> entries,
		HashSet<string> featureIdsToHide)
	{
		// Track hidden entries for warnings
		foreach (var resolved in entries)
		{
			if (!string.IsNullOrWhiteSpace(resolved.Entry.FeatureId) && featureIdsToHide.Contains(resolved.Entry.FeatureId))
				collector.EmitWarning(string.Empty, $"Changelog entry '{resolved.Entry.Title}' with feature-id '{resolved.Entry.FeatureId}' will be commented out in markdown output");
		}
	}

	private static bool ValidateEntryTypes(
		IDiagnosticsCollector collector,
		IReadOnlyList<ResolvedEntry> entries,
		IReadOnlyList<string> availableTypes)
	{
		var isValid = true;

		// Check for invalid types (unrecognized type strings)
		var invalidEntries = entries.Where(e => e.Entry.Type == ChangelogEntryType.Invalid).ToList();
		if (invalidEntries.Count > 0)
		{
			foreach (var entry in invalidEntries)
				collector.EmitError(string.Empty, $"Changelog entry '{entry.Entry.Title}' has an invalid or unrecognized type. Valid types are: {string.Join(", ", availableTypes)}.");
			isValid = false;
		}

		// All valid enum values (except Invalid) are handled in rendering
		var handledTypes = new HashSet<ChangelogEntryType>(
			ChangelogEntryTypeExtensions.GetValues().Where(t => t != ChangelogEntryType.Invalid));
		var availableTypesSet = new HashSet<string>(availableTypes, StringComparer.OrdinalIgnoreCase);

		var entriesByType = entries
			.Where(e => e.Entry.Type != ChangelogEntryType.Invalid)
			.GroupBy(e => e.Entry.Type)
			.ToDictionary(g => g.Key, g => g.Count());

		foreach (var (entryType, count) in entriesByType)
		{
			var typeString = entryType.ToStringFast(true);
			if (availableTypesSet.Contains(typeString) && !handledTypes.Contains(entryType))
				collector.EmitWarning(string.Empty, $"Changelog type '{typeString}' is valid according to configuration but is not handled in rendering output. {count} entry/entries of this type will not be included in the generated markdown files.");
		}

		return isValid;
	}

	private static ChangelogRenderContext BuildRenderContext(
		RenderChangelogsArguments input,
		OutputSetup outputSetup,
		ResolvedEntriesResult resolved,
		HashSet<string> featureIdsToHide,
		ChangelogConfiguration? config)
	{
		// Group entries by type
		var entriesByType = resolved.Entries
			.Select(e => e.Entry)
			.GroupBy(e => e.Type)
			.ToDictionary(g => g.Key, g => (IReadOnlyCollection<ChangelogEntry>)g.ToArray().AsReadOnly())
			.AsReadOnly();

		// Create mappings from entries to their metadata
		var entryToBundleProducts = new Dictionary<ChangelogEntry, HashSet<string>>();
		var entryToRepo = new Dictionary<ChangelogEntry, string>();
		var entryToHideLinks = new Dictionary<ChangelogEntry, bool>();

		foreach (var entry in resolved.Entries)
		{
			entryToBundleProducts[entry.Entry] = entry.BundleProductIds;
			entryToRepo[entry.Entry] = entry.Repo;
			entryToHideLinks[entry.Entry] = entry.HideLinks;
		}

		// Use first repo found for section anchors, or default
		var repoForAnchors = resolved.Entries.Count > 0 ? resolved.Entries[0].Repo : "elastic";

		return new ChangelogRenderContext
		{
			OutputDir = outputSetup.OutputDir,
			Title = outputSetup.Title,
			TitleSlug = outputSetup.TitleSlug,
			Repo = repoForAnchors,
			EntriesByType = entriesByType,
			Subsections = input.Subsections,
			FeatureIdsToHide = featureIdsToHide,
			EntryToBundleProducts = entryToBundleProducts,
			EntryToRepo = entryToRepo,
			EntryToHideLinks = entryToHideLinks,
			Configuration = config
		};
	}

	private record OutputSetup(string OutputDir, string Title, string TitleSlug);
}
