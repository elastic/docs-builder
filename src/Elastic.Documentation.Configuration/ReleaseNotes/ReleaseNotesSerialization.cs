// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration.Serialization;
using Elastic.Documentation.ReleaseNotes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// Centralized YAML serialization for release notes/changelog operations.
/// Provides static deserializers and serializers configured for different use cases.
/// </summary>
public static partial class ReleaseNotesSerialization
{
	/// <summary>
	/// Regex to normalize "version:" to "target:" in changelog YAML files.
	/// Used for backward compatibility with older changelog formats.
	/// </summary>
	[GeneratedRegex(@"(\s+)version:", RegexOptions.Multiline)]
	public static partial Regex VersionToTargetRegex();

	private static readonly IDeserializer YamlDeserializer =
		new StaticDeserializerBuilder(new YamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

	/// <summary>
	/// Used for loading minimal changelog configuration (publish blocker).
	/// Includes LenientStringListConverter so List&lt;string&gt; fields accept both comma-separated strings and YAML lists.
	/// </summary>
	private static readonly IDeserializer IgnoreUnmatchedDeserializer =
		new StaticDeserializerBuilder(new YamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.WithTypeConverter(new LenientStringListConverter())
			.IgnoreUnmatchedProperties()
			.Build();

	private static readonly ISerializer YamlSerializer =
		new StaticSerializerBuilder(new YamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
			.WithQuotingNecessaryStrings()
			.DisableAliases()
			.Build();

	/// <summary>
	/// Gets the raw YAML deserializer for changelog entry DTOs.
	/// Used by bundling service for direct deserialization with error handling.
	/// </summary>
	public static IDeserializer GetEntryDeserializer() => YamlDeserializer;

	/// <summary>
	/// Deserializes a changelog entry YAML content to domain type.
	/// </summary>
	public static ChangelogEntry DeserializeEntry(string yaml)
	{
		var yamlDto = YamlDeserializer.Deserialize<ChangelogEntryDto>(yaml);
		return ToEntry(yamlDto);
	}

	/// <summary>
	/// Converts a raw YAML DTO to domain type.
	/// Used by bundling service that handles deserialization separately for error handling.
	/// </summary>
	public static ChangelogEntry ConvertEntry(ChangelogEntryDto dto) => ToEntry(dto);

	/// <summary>
	/// Converts a BundledEntry to a ChangelogEntry.
	/// Used when inline entry data is provided in bundles.
	/// </summary>
	public static ChangelogEntry ConvertBundledEntry(BundledEntry entry) => ToEntry(entry);

	/// <summary>
	/// Deserializes bundled changelog data YAML content to domain type.
	/// </summary>
	public static Bundle DeserializeBundle(string yaml)
	{
		var yamlDto = YamlDeserializer.Deserialize<BundleDto>(yaml);
		return ToBundle(yamlDto);
	}

	/// <summary>
	/// Serializes a changelog entry to YAML.
	/// </summary>
	public static string SerializeEntry(ChangelogEntry entry)
	{
		var dto = ToDto(entry);
		return YamlSerializer.Serialize(dto);
	}

	/// <summary>
	/// Serializes bundled changelog data to YAML.
	/// </summary>
	public static string SerializeBundle(Bundle bundle)
	{
		var dto = ToDto(bundle);
		return YamlSerializer.Serialize(dto);
	}

	#region Manual Mapping Methods

	private static ChangelogEntry ToEntry(ChangelogEntryDto dto) => new()
	{
		Pr = dto.Pr,
		Issues = dto.Issues,
		Type = ParseEntryType(dto.Type),
		Subtype = ParseEntrySubtype(dto.Subtype),
		Products = dto.Products?.Select(ToProductReference).ToList(),
		Areas = dto.Areas,
		Title = dto.Title ?? "",
		Description = dto.Description,
		Impact = dto.Impact,
		Action = dto.Action,
		FeatureId = dto.FeatureId,
		Highlight = dto.Highlight
	};

	private static ChangelogEntry ToEntry(BundledEntry entry) => new()
	{
		Pr = entry.Pr,
		Issues = entry.Issues,
		Type = entry.Type ?? ChangelogEntryType.Invalid,
		Subtype = entry.Subtype,
		Products = entry.Products,
		Areas = entry.Areas,
		Title = entry.Title ?? "",
		Description = entry.Description,
		Impact = entry.Impact,
		Action = entry.Action,
		FeatureId = entry.FeatureId,
		Highlight = entry.Highlight
	};

	private static ProductReference ToProductReference(ProductInfoDto dto) => new()
	{
		ProductId = dto.Product ?? "",
		Target = dto.Target,
		Lifecycle = ParseLifecycle(dto.Lifecycle)
	};

	private static Bundle ToBundle(BundleDto dto) => new()
	{
		Products = dto.Products?.Select(ToBundledProduct).ToList() ?? [],
		HideFeatures = dto.HideFeatures ?? [],
		Entries = dto.Entries?.Select(ToBundledEntry).ToList() ?? []
	};

	private static BundledProduct ToBundledProduct(BundledProductDto dto) => new()
	{
		ProductId = dto.Product ?? "",
		Target = dto.Target,
		Lifecycle = ParseLifecycle(dto.Lifecycle),
		Repo = dto.Repo
	};

	private static BundledEntry ToBundledEntry(BundledEntryDto dto) => new()
	{
		File = dto.File != null ? ToBundledFile(dto.File) : null,
		Type = ParseEntryTypeNullable(dto.Type),
		Title = dto.Title,
		Products = dto.Products?.Select(ToProductReference).ToList(),
		Description = dto.Description,
		Impact = dto.Impact,
		Action = dto.Action,
		FeatureId = dto.FeatureId,
		Highlight = dto.Highlight,
		Subtype = ParseEntrySubtype(dto.Subtype),
		Areas = dto.Areas,
		Pr = dto.Pr,
		Issues = dto.Issues
	};

	private static BundledFile ToBundledFile(BundledFileDto dto) => new()
	{
		Name = dto.Name ?? "",
		Checksum = dto.Checksum ?? ""
	};

	private static ChangelogEntryType ParseEntryType(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return ChangelogEntryType.Invalid;

		return ChangelogEntryTypeExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: ChangelogEntryType.Invalid;
	}

	private static ChangelogEntryType? ParseEntryTypeNullable(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		return ChangelogEntryTypeExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: null;
	}

	private static ChangelogEntrySubtype? ParseEntrySubtype(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		return ChangelogEntrySubtypeExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: null;
	}

	private static Lifecycle? ParseLifecycle(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		return LifecycleExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: null;
	}

	// Reverse mappings (Domain → DTO) for serialization

	private static ChangelogEntryDto ToDto(ChangelogEntry entry) => new()
	{
		Pr = entry.Pr,
		Issues = entry.Issues?.ToList(),
		Type = EntryTypeToString(entry.Type),
		Subtype = EntrySubtypeToString(entry.Subtype),
		Products = entry.Products?.Select(ToDto).ToList(),
		Areas = entry.Areas?.ToList(),
		Title = entry.Title,
		Description = entry.Description,
		Impact = entry.Impact,
		Action = entry.Action,
		FeatureId = entry.FeatureId,
		Highlight = entry.Highlight
	};

	private static ProductInfoDto ToDto(ProductReference product) => new()
	{
		Product = product.ProductId,
		Target = product.Target,
		Lifecycle = LifecycleToString(product.Lifecycle)
	};

	private static BundleDto ToDto(Bundle bundle) => new()
	{
		Products = bundle.Products.Select(ToDto).ToList(),
		HideFeatures = bundle.HideFeatures.Count > 0 ? bundle.HideFeatures.ToList() : null,
		Entries = bundle.Entries.Select(ToDto).ToList()
	};

	private static BundledProductDto ToDto(BundledProduct product) => new()
	{
		Product = product.ProductId,
		Target = product.Target,
		Lifecycle = LifecycleToString(product.Lifecycle),
		Repo = product.Repo
	};

	private static BundledEntryDto ToDto(BundledEntry entry) => new()
	{
		File = entry.File != null ? ToDto(entry.File) : null,
		Type = EntryTypeNullableToString(entry.Type),
		Title = entry.Title,
		Products = entry.Products?.Select(ToDto).ToList(),
		Description = entry.Description,
		Impact = entry.Impact,
		Action = entry.Action,
		FeatureId = entry.FeatureId,
		Highlight = entry.Highlight,
		Subtype = EntrySubtypeToString(entry.Subtype),
		Areas = entry.Areas?.ToList(),
		Pr = entry.Pr,
		Issues = entry.Issues?.ToList()
	};

	private static BundledFileDto ToDto(BundledFile file) => new()
	{
		Name = file.Name,
		Checksum = file.Checksum
	};

	// Reverse enum conversion helpers

	private static string? EntryTypeToString(ChangelogEntryType value) =>
		value != ChangelogEntryType.Invalid ? value.ToStringFast(true) : null;

	private static string? EntryTypeNullableToString(ChangelogEntryType? value) =>
		value?.ToStringFast(true);

	private static string? EntrySubtypeToString(ChangelogEntrySubtype? value) =>
		value?.ToStringFast(true);

	private static string? LifecycleToString(Lifecycle? value) =>
		value?.ToStringFast(true);

	#endregion

	/// <summary>
	/// Normalizes a YAML string by converting "version:" fields to "target:" for backward compatibility.
	/// Also strips comment lines.
	/// </summary>
	/// <param name="yaml">The raw YAML content.</param>
	/// <returns>The normalized YAML content.</returns>
	public static string NormalizeYaml(string yaml)
	{
		// Skip comment lines
		var yamlLines = yaml.Split('\n');
		var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));

		// Normalize version to target
		return VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");
	}

	/// <summary>
	/// Loads the publish blocker configuration from a changelog.yml file.
	/// Uses AOT-compatible deserialization via YamlStaticContext.
	/// Supports the new 'rules:' format with include/exclude modes.
	/// </summary>
	/// <param name="fileSystem">The file system to read from.</param>
	/// <param name="configPath">The path to the changelog.yml configuration file.</param>
	/// <param name="productId">Optional product ID to load product-specific blocker.</param>
	/// <returns>The publish blocker configuration, or null if not found or not configured.</returns>
	public static PublishBlocker? LoadPublishBlocker(IFileSystem fileSystem, string configPath, string? productId = null)
	{
		if (!fileSystem.File.Exists(configPath))
			return null;

		var yamlContent = fileSystem.File.ReadAllText(configPath);
		if (string.IsNullOrWhiteSpace(yamlContent))
			return null;

		var yamlConfig = IgnoreUnmatchedDeserializer.Deserialize<ChangelogConfigMinimalDto>(yamlContent);
		if (yamlConfig.Rules is null)
			return null;

		var publish = yamlConfig.Rules.Publish;
		if (publish is null)
			return null;

		// Parse global match mode
		var globalMatch = ParseMatchMode(yamlConfig.Rules.Match);
		var publishMatchAreas = ParseMatchMode(publish.MatchAreas) ?? globalMatch ?? MatchMode.Any;

		// Check product-specific blocker first if productId is specified
		if (!string.IsNullOrWhiteSpace(productId) && publish.Products is { Count: > 0 })
		{
			// Try exact match first, then fall back to case-insensitive match
			if (!publish.Products.TryGetValue(productId, out var productPublish))
			{
				var found = publish.Products.FirstOrDefault(kvp =>
					kvp.Key.Equals(productId, StringComparison.OrdinalIgnoreCase));
				productPublish = found.Value;
			}

			if (productPublish != null)
			{
				var productMatchAreas = ParseMatchMode(productPublish.MatchAreas) ?? publishMatchAreas;
				return ParsePublishBlocker(productPublish, productMatchAreas);
			}
		}

		// Fall back to global publish blocker
		return ParsePublishBlocker(publish, publishMatchAreas);
	}

	/// <summary>
	/// Parses a PublishRulesMinimalDto into a PublishBlocker domain type.
	/// </summary>
	private static PublishBlocker? ParsePublishBlocker(PublishRulesMinimalDto? dto, MatchMode matchAreas)
	{
		if (dto == null)
			return null;

		var excludeTypes = dto.ExcludeTypes?.Count > 0 ? dto.ExcludeTypes.ToList() : null;
		var includeTypes = dto.IncludeTypes?.Count > 0 ? dto.IncludeTypes.ToList() : null;
		var excludeAreas = dto.ExcludeAreas?.Count > 0 ? dto.ExcludeAreas.ToList() : null;
		var includeAreas = dto.IncludeAreas?.Count > 0 ? dto.IncludeAreas.ToList() : null;

		var types = excludeTypes ?? includeTypes;
		var areas = excludeAreas ?? includeAreas;

		if (types == null && areas == null)
			return null;

		return new PublishBlocker
		{
			Types = types,
			TypesMode = includeTypes != null ? FieldMode.Include : FieldMode.Exclude,
			Areas = areas,
			AreasMode = includeAreas != null ? FieldMode.Include : FieldMode.Exclude,
			MatchAreas = matchAreas
		};
	}

	private static MatchMode? ParseMatchMode(string? value) =>
		value?.ToLowerInvariant() switch
		{
			"any" => MatchMode.Any,
			"all" => MatchMode.All,
			_ => null
		};
}

/// <summary>
/// Minimal DTO for changelog configuration - only includes rules configuration.
/// Used for AOT-compatible lightweight loading of publish blocker configuration.
/// Registered with YamlStaticContext for source-generated deserialization.
/// </summary>
public sealed class ChangelogConfigMinimalDto
{
	/// <summary>Rules configuration section (new format).</summary>
	public RulesConfigMinimalDto? Rules { get; set; }
}

/// <summary>
/// Minimal DTO for rules configuration.
/// Registered with YamlStaticContext for source-generated deserialization.
/// </summary>
public sealed class RulesConfigMinimalDto
{
	/// <summary>Global match mode ("any" or "all").</summary>
	public string? Match { get; set; }

	/// <summary>Publish rules configuration.</summary>
	public PublishRulesMinimalDto? Publish { get; set; }
}

/// <summary>
/// Minimal DTO for publish rules configuration.
/// Registered with YamlStaticContext for source-generated deserialization.
/// </summary>
public sealed class PublishRulesMinimalDto
{
	/// <summary>Match mode for areas ("any" or "all").</summary>
	public string? MatchAreas { get; set; }

	/// <summary>Entry types to exclude from publishing.</summary>
	public List<string>? ExcludeTypes { get; set; }

	/// <summary>Entry types to include for publishing.</summary>
	public List<string>? IncludeTypes { get; set; }

	/// <summary>Entry areas to exclude from publishing.</summary>
	public List<string>? ExcludeAreas { get; set; }

	/// <summary>Entry areas to include for publishing.</summary>
	public List<string>? IncludeAreas { get; set; }

	/// <summary>Per-product publish rule overrides.</summary>
	public Dictionary<string, PublishRulesMinimalDto?>? Products { get; set; }
}
