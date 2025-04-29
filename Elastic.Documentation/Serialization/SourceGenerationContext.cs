// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.Links;
using Elastic.Documentation.State;

namespace Elastic.Documentation.Serialization;

// This configures the source generation for json (de)serialization.

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(GenerationState))]
[JsonSerializable(typeof(LinkReference))]
[JsonSerializable(typeof(GitCheckoutInformation))]
[JsonSerializable(typeof(LinkReferenceRegistry))]
[JsonSerializable(typeof(LinkRegistryEntry))]
public sealed partial class SourceGenerationContext : JsonSerializerContext;
