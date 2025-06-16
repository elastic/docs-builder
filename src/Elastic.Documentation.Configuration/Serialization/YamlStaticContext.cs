// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Serialization;

[YamlStaticContext]
[YamlSerializable(typeof(AssemblyConfiguration))]
[YamlSerializable(typeof(Repository))]
[YamlSerializable(typeof(NarrativeRepository))]
[YamlSerializable(typeof(PublishEnvironment))]
[YamlSerializable(typeof(GoogleTagManager))]
[YamlSerializable(typeof(ContentSource))]
[YamlSerializable(typeof(VersionEntry))]
public partial class YamlStaticContext;
