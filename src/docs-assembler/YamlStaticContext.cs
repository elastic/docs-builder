// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Assembler.Configuration;
using Elastic.Markdown.IO.State;
using YamlDotNet.Serialization;

namespace Documentation.Assembler;

[YamlStaticContext]
[YamlSerializable(typeof(AssemblyConfiguration))]
[YamlSerializable(typeof(Repository))]
[YamlSerializable(typeof(NarrativeRepository))]
[YamlSerializable(typeof(PublishEnvironment))]
[YamlSerializable(typeof(GoogleTagManager))]
[YamlSerializable(typeof(ContentSource))]
public partial class YamlStaticContext;
