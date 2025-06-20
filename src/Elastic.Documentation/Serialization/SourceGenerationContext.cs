// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text.Json.Serialization;
using Elastic.Documentation.Links;
using Elastic.Documentation.Search;
using Elastic.Documentation.State;

namespace Elastic.Documentation.Serialization;

// This configures the source generation for JSON (de)serialization.

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(GenerationState))]
[JsonSerializable(typeof(RepositoryLinks))]
[JsonSerializable(typeof(GitCheckoutInformation))]
[JsonSerializable(typeof(LinkRegistry))]
[JsonSerializable(typeof(LinkRegistryEntry))]
[JsonSerializable(typeof(DocumentationDocument))]
[JsonSerializable(typeof(FrozenDictionary<string, string>))]
public sealed partial class SourceGenerationContext : JsonSerializerContext;
