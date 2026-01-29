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

	private static readonly IDeserializer IgnoreUnmatchedDeserializer =
		new DeserializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.IgnoreUnmatchedProperties()
			.Build();

	/// <summary>
	/// Deserializes a changelog entry YAML content to domain type.
	/// </summary>
	public static ChangelogEntry DeserializeEntry(string yaml)
	{
		var yamlDto = YamlDeserializer.Deserialize<ChangelogEntryDto>(yaml);
		return ToEntry(yamlDto);
	}

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
		Entries = dto.Entries?.Select(ToBundledEntry).ToList() ?? []
	};

	private static BundledProduct ToBundledProduct(BundledProductDto dto) => new()
	{
		ProductId = dto.Product ?? "",
		Target = dto.Target,
		Lifecycle = ParseLifecycle(dto.Lifecycle)
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
	/// </summary>
	/// <param name="fileSystem">The file system to read from.</param>
	/// <param name="configPath">The path to the changelog.yml configuration file.</param>
	/// <returns>The publish blocker configuration, or null if not found or not configured.</returns>
	public static PublishBlocker? LoadPublishBlocker(IFileSystem fileSystem, string configPath)
	{
		if (!fileSystem.File.Exists(configPath))
			return null;

		var yamlContent = fileSystem.File.ReadAllText(configPath);
		var yamlConfig = IgnoreUnmatchedDeserializer.Deserialize<ChangelogConfigMinimal>(yamlContent);

		return yamlConfig.Block?.Publish switch
		{
			null => null,
			_ => ParsePublishBlocker(yamlConfig.Block.Publish)
		};
	}

	/// <summary>
	/// Parses a PublishBlockerYamlMinimal into a PublishBlocker domain type.
	/// </summary>
	private static PublishBlocker? ParsePublishBlocker(PublishBlockerMinimal? yaml)
	{
		if (yaml == null)
			return null;

		var types = yaml.Types?.Count > 0 ? yaml.Types.ToList() : null;
		var areas = yaml.Areas?.Count > 0 ? yaml.Areas.ToList() : null;

		if (types == null && areas == null)
			return null;

		return new PublishBlocker
		{
			Types = types,
			Areas = areas
		};
	}
}

/// <summary>
/// Minimal DTO for changelog configuration - only includes block configuration.
/// Used for lightweight loading of publish blocker configuration.
/// </summary>
internal sealed class ChangelogConfigMinimal
{
	public BlockConfigMinimal? Block { get; set; }
}

/// <summary>
/// Minimal DTO for block configuration.
/// </summary>
internal sealed class BlockConfigMinimal
{
	public PublishBlockerMinimal? Publish { get; set; }
}

/// <summary>
/// Minimal DTO for publish blocker configuration.
/// </summary>
internal sealed class PublishBlockerMinimal
{
	public List<string>? Types { get; set; }
	public List<string>? Areas { get; set; }
}
