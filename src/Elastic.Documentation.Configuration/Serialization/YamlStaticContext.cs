// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
using Elastic.Documentation.Publishing;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Serialization;

[YamlStaticContext]
// Codex configuration
[YamlSerializable(typeof(CodexConfigurationDto))]
[YamlSerializable(typeof(CodexGroupDefinitionDto))]
// Assembly configuration
[YamlSerializable(typeof(AssemblyConfigurationDto))]
[YamlSerializable(typeof(RepositoryDto))]
[YamlSerializable(typeof(NarrativeRepositoryDto))]
[YamlSerializable(typeof(PublishEnvironmentDto))]
[YamlSerializable(typeof(GoogleTagManagerDto))]
[YamlSerializable(typeof(OptimizelyDto))]
[YamlSerializable(typeof(ContentSource))]
// Versions configuration
[YamlSerializable(typeof(VersionsConfigDto))]
[YamlSerializable(typeof(ProductConfigDto))]
[YamlSerializable(typeof(VersioningSystemDto))]
[YamlSerializable(typeof(ProductDto))]
// Legacy URL mappings
[YamlSerializable(typeof(LegacyUrlMappingDto))]
[YamlSerializable(typeof(LegacyUrlMappingConfigDto))]
// Table of contents
[YamlSerializable(typeof(DocumentationSetFile))]
[YamlSerializable(typeof(DocumentationSetFeatures))]
[YamlSerializable(typeof(DocumentationSetStorybook))]
[YamlSerializable(typeof(CodexDocSetMetadata))]
[YamlSerializable(typeof(ReleaseNotesProductReference))]
[YamlSerializable(typeof(TableOfContentsFile))]
[YamlSerializable(typeof(SiteNavigationDto))]
[YamlSerializable(typeof(PhantomRegistrationDto))]
[YamlSerializable(typeof(ProductLink))]
// Search configuration
[YamlSerializable(typeof(SearchConfigDto))]
[YamlSerializable(typeof(QueryRuleDto))]
[YamlSerializable(typeof(QueryRuleCriteriaDto))]
[YamlSerializable(typeof(QueryRuleActionsDto))]
// Release notes / changelog YAML DTOs
[YamlSerializable(typeof(ChangelogEntryDto))]
[YamlSerializable(typeof(ProductInfoDto))]
[YamlSerializable(typeof(BundleDto))]
[YamlSerializable(typeof(BundledProductDto))]
[YamlSerializable(typeof(BundledEntryDto))]
[YamlSerializable(typeof(BundledFileDto))]
// Changelog configuration minimal DTOs
[YamlSerializable(typeof(ChangelogConfigMinimalDto))]
[YamlSerializable(typeof(RulesConfigMinimalDto))]
[YamlSerializable(typeof(PublishRulesMinimalDto))]
// Changelog YAML DTOs for CLI configuration (changelog.yml)
[YamlSerializable(typeof(ChangelogConfigurationDto))]
[YamlSerializable(typeof(ChangelogPivotDto))]
[YamlSerializable(typeof(ChangelogTypeEntryDto))]
[YamlSerializable(typeof(ChangelogRulesDto))]
[YamlSerializable(typeof(ChangelogCreateRulesDto))]
[YamlSerializable(typeof(ChangelogBundleRulesDto))]
[YamlSerializable(typeof(ChangelogPublishRulesDto))]
[YamlSerializable(typeof(ChangelogProductsDto))]
[YamlSerializable(typeof(ChangelogDefaultProductDto))]
[YamlSerializable(typeof(ChangelogBundleDto))]
[YamlSerializable(typeof(ChangelogBundleProfileDto))]
[YamlSerializable(typeof(ChangelogBundleReleasesDto))]
[YamlSerializable(typeof(ChangelogExtractDto))]
[YamlSerializable(typeof(YamlLenientList))]
// Related learning catalog
[YamlSerializable(typeof(RelatedLearningConfigDto))]
[YamlSerializable(typeof(RelatedLearningLinkDto))]
public partial class YamlStaticContext;
