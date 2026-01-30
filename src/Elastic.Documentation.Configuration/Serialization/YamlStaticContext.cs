// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Serialization;

[YamlStaticContext]
// Assembly configuration
[YamlSerializable(typeof(AssemblyConfiguration))]
[YamlSerializable(typeof(Repository))]
[YamlSerializable(typeof(NarrativeRepository))]
[YamlSerializable(typeof(PublishEnvironment))]
[YamlSerializable(typeof(GoogleTagManager))]
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
[YamlSerializable(typeof(TableOfContentsFile))]
[YamlSerializable(typeof(SiteNavigationFile))]
[YamlSerializable(typeof(PhantomRegistration))]
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
[YamlSerializable(typeof(BlockConfigMinimalDto))]
[YamlSerializable(typeof(PublishBlockerMinimalDto))]
public partial class YamlStaticContext;
