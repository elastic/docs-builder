// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Changelog.Serialization;

/// <summary>
/// Centralized YAML serialization for changelog operations.
/// Provides static deserializers and serializers configured for different use cases.
/// </summary>
public static class ChangelogYamlSerialization
{
	private static readonly IDeserializer ConfigurationDeserializer =
		new StaticDeserializerBuilder(new ChangelogYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.WithTypeConverter(new TypeEntryYamlConverter())
			.Build();

	private static readonly IDeserializer YamlDeserializer =
		new StaticDeserializerBuilder(new ChangelogYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

	/// <summary>
	/// Gets the raw YAML deserializer for changelog entry DTOs.
	/// Used by bundling service for direct deserialization with error handling.
	/// </summary>
	public static IDeserializer GetEntryDeserializer() => YamlDeserializer;

	private static readonly ISerializer YamlSerializer =
		new StaticSerializerBuilder(new ChangelogYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
			.WithQuotingNecessaryStrings()
			.DisableAliases()
			.Build();

	/// <summary>
	/// Deserializes changelog configuration YAML content.
	/// </summary>
	internal static ChangelogConfigurationYaml DeserializeConfiguration(string yaml) =>
		ConfigurationDeserializer.Deserialize<ChangelogConfigurationYaml>(yaml);

	/// <summary>
	/// Deserializes a changelog entry YAML content to domain type.
	/// </summary>
	public static ChangelogEntry DeserializeEntry(string yaml)
	{
		var yamlDto = YamlDeserializer.Deserialize<ChangelogEntryYaml>(yaml);
		return ChangelogMapper.ToEntry(yamlDto);
	}

	/// <summary>
	/// Converts a raw YAML DTO to domain type.
	/// Used by bundling service that handles deserialization separately for error handling.
	/// </summary>
	public static ChangelogEntry ConvertEntry(ChangelogEntryYaml yamlDto) =>
		ChangelogMapper.ToEntry(yamlDto);

	/// <summary>
	/// Converts a BundledEntry to a ChangelogEntry.
	/// Used when inline entry data is provided in bundles.
	/// </summary>
	public static ChangelogEntry ConvertBundledEntry(BundledEntry entry) =>
		ChangelogMapper.ToEntry(entry);

	/// <summary>
	/// Deserializes bundled changelog data YAML content to domain type.
	/// </summary>
	public static Bundle DeserializeBundle(string yaml)
	{
		var yamlDto = YamlDeserializer.Deserialize<BundleYaml>(yaml);
		return ChangelogMapper.ToBundle(yamlDto);
	}

	/// <summary>
	/// Serializes a changelog entry to YAML.
	/// </summary>
	public static string SerializeEntry(ChangelogEntry entry)
	{
		var yamlDto = ChangelogMapper.ToYaml(entry);
		return YamlSerializer.Serialize(yamlDto);
	}

	/// <summary>
	/// Serializes bundled changelog data to YAML.
	/// </summary>
	public static string SerializeBundle(Bundle bundle)
	{
		var yamlDto = ChangelogMapper.ToYaml(bundle);
		return YamlSerializer.Serialize(yamlDto);
	}
}
