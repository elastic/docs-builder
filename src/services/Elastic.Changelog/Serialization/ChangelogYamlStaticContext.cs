// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Changelog.Serialization;

[YamlStaticContext]
// YAML DTOs for CLI configuration (changelog.yml)
[YamlSerializable(typeof(ChangelogConfigurationYaml))]
[YamlSerializable(typeof(PivotConfigurationYaml))]
[YamlSerializable(typeof(TypeEntryYaml))]
[YamlSerializable(typeof(RulesConfigurationYaml))]
[YamlSerializable(typeof(CreateRulesYaml))]
[YamlSerializable(typeof(PublishRulesYaml))]
[YamlSerializable(typeof(ProductsConfigYaml))]
[YamlSerializable(typeof(DefaultProductYaml))]
[YamlSerializable(typeof(BundleConfigurationYaml))]
[YamlSerializable(typeof(BundleProfileYaml))]
[YamlSerializable(typeof(ExtractConfigurationYaml))]
public partial class ChangelogYamlStaticContext;
