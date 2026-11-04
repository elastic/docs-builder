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
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Serialization;

// Root types only — the SharpYaml source generator transitively discovers every member,
// collection, and dictionary type reachable from these roots, unlike YamlDotNet's Vecc
// analyzer, which required every type to be listed explicitly.
[YamlSerializable(typeof(CodexConfiguration))]
[YamlSerializable(typeof(AssemblyConfiguration))]
[YamlSerializable(typeof(VersionsConfigDto))]
[YamlSerializable(typeof(ProductConfigDto))]
[YamlSerializable(typeof(LegacyUrlMappingConfigDto))]
[YamlSerializable(typeof(DocumentationSetFile))]
[YamlSerializable(typeof(TableOfContentsFile))]
[YamlSerializable(typeof(SiteNavigationFile))]
[YamlSerializable(typeof(SearchConfigDto))]
[YamlSerializable(typeof(ChangelogEntryDto))]
[YamlSerializable(typeof(BundleDto))]
[YamlSerializable(typeof(ChangelogConfigMinimalDto))]
[YamlSerializable(typeof(ChangelogConfigurationYaml))]
[YamlSerializable(typeof(RelatedLearningConfigDto))]
[YamlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
// TableOfContents and SiteTableOfContents hold polymorphic interface-typed items
// (ITableOfContentsItem / ISiteNavigationEntry) that the generator can't auto-generate
// metadata for. These collection converters are always registered with real instances
// via YamlSerializerOptions.Converters at runtime (see ConfigurationFileProvider); the
// generator only needs the type here to know the member is handled.
Converters = [typeof(TocItemCollectionYamlConverter), typeof(SiteTableOfContentsCollectionYamlConverter)])]
public partial class YamlStaticContext : YamlSerializerContext;
