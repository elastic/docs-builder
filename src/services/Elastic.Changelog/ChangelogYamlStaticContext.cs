// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Changelog.Configuration;
using YamlDotNet.Serialization;

namespace Elastic.Changelog;

[YamlStaticContext]
[YamlSerializable(typeof(ChangelogData))]
[YamlSerializable(typeof(ProductInfo))]
[YamlSerializable(typeof(ChangelogConfiguration))]
[YamlSerializable(typeof(RenderBlockersEntry))]
[YamlSerializable(typeof(BundledChangelogData))]
[YamlSerializable(typeof(BundledProduct))]
[YamlSerializable(typeof(BundledEntry))]
[YamlSerializable(typeof(BundledFile))]
public partial class ChangelogYamlStaticContext;
