// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Serialization;

// Root types only — the SharpYaml source generator transitively discovers every member,
// collection, and dictionary type reachable from these roots, unlike YamlDotNet's Vecc
// analyzer, which required every type to be listed explicitly.
[YamlSerializable(typeof(CodexConfigurationDto))]
[YamlSerializable(typeof(AssemblyConfigurationDto))]
[YamlSerializable(typeof(VersionsConfigDto))]
[YamlSerializable(typeof(ProductConfigDto))]
[YamlSerializable(typeof(LegacyUrlMappingConfigDto))]
[YamlSerializable(typeof(DocumentationSetDto))]
[YamlSerializable(typeof(TableOfContentsDto))]
[YamlSerializable(typeof(SiteNavigationDto))]
[YamlSerializable(typeof(SearchConfigDto))]
[YamlSerializable(typeof(ChangelogEntryDto))]
[YamlSerializable(typeof(BundleDto))]
[YamlSerializable(typeof(ChangelogConfigMinimalDto))]
[YamlSerializable(typeof(ChangelogConfigurationDto))]
[YamlSerializable(typeof(RelatedLearningConfigDto))]
[YamlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
// TableOfContents and SiteTableOfContents hold polymorphic interface-typed items
// (ITableOfContentsItem / ISiteNavigationEntry) that the generator can't auto-generate
// metadata for. These collection converters are always registered with real instances
// via YamlSerializerOptions.Converters at runtime (see ConfigurationFileProvider); the
// generator only needs the type here to know the member is handled.
Converters = [
	typeof(TocItemCollectionYamlConverter),
	typeof(SiteTableOfContentsCollectionYamlConverter),
	typeof(SiteTableOfContentsRefYamlConverter)
])]
internal partial class YamlStaticContext : YamlSerializerContext;

/// <summary>
/// Reads a configuration file into its DTO as the loaders always have: a document with nothing in it is the DTO with every default,
/// where <see cref="ConfigurationFileProvider.Deserialize{T}(string, string?)"/> would reject it.
/// </summary>
internal static class ConfigurationDocument
{
	/// <summary>Unknown keys fail the read (see <see cref="ConfigurationFileProvider.DeserializerOptions"/>); <paramref name="sourceName"/> names the file in the message.</summary>
	public static T Read<T>(string yaml, string? sourceName = null) where T : class, new() =>
		YamlSerializer.Deserialize<T>(
			yaml,
			sourceName is null
				? ConfigurationFileProvider.DeserializerOptions
				: ConfigurationFileProvider.DeserializerOptions with { SourceName = sourceName }
		)
			?? new T();

	/// <summary>Unknown keys are written to <paramref name="collector"/> as diagnostics instead of failing the read.</summary>
	public static T Read<T>(string yaml, IDiagnosticsCollector collector, string sourceName) where T : class, new() =>
		YamlSerializer.Deserialize<T>(
			yaml,
			ConfigurationFileProvider.DeserializerOptions.WithUnmappedReporting(new YamlUnmappedMemberReporting(collector, sourceName))
		)
			?? new T();
}
