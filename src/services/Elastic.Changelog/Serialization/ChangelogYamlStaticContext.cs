// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Changelog.Serialization;

[YamlStaticContext]
// YAML DTOs for changelog entries
[YamlSerializable(typeof(ChangelogEntryYaml))]
[YamlSerializable(typeof(ProductInfoYaml))]
// YAML DTOs for configuration
[YamlSerializable(typeof(ChangelogConfigurationYaml))]
[YamlSerializable(typeof(PivotConfigurationYaml))]
[YamlSerializable(typeof(TypeEntryYaml))]
[YamlSerializable(typeof(BlockConfigurationYaml))]
[YamlSerializable(typeof(ProductBlockersYaml))]
// YAML DTOs for bundles
[YamlSerializable(typeof(BundleYaml))]
[YamlSerializable(typeof(BundledProductYaml))]
[YamlSerializable(typeof(BundledEntryYaml))]
[YamlSerializable(typeof(BundledFileYaml))]
public partial class ChangelogYamlStaticContext;
