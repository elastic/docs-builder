// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Documentation.Configuration.Versions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Serialization;

[YamlStaticContext]
[YamlSerializable(typeof(AssemblyConfiguration))]
[YamlSerializable(typeof(Repository))]
[YamlSerializable(typeof(NarrativeRepository))]
[YamlSerializable(typeof(PublishEnvironment))]
[YamlSerializable(typeof(GoogleTagManager))]
[YamlSerializable(typeof(ContentSource))]
[YamlSerializable(typeof(VersionsConfigDto))]
[YamlSerializable(typeof(ProductConfigDto))]
[YamlSerializable(typeof(VersioningSystemDto))]
[YamlSerializable(typeof(ProductDto))]
[YamlSerializable(typeof(LegacyUrlMappingDto))]
[YamlSerializable(typeof(LegacyUrlMappingConfigDto))]
[YamlSerializable(typeof(DocumentationSetFile))]
[YamlSerializable(typeof(TableOfContentsFile))]
[YamlSerializable(typeof(SiteNavigationFile))]
[YamlSerializable(typeof(PhantomRegistration))]
public partial class YamlStaticContext;
