// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text.Json.Serialization;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Links;
using Elastic.Documentation.Search;
using Elastic.Documentation.State;

namespace Elastic.Documentation.Serialization;

// This configures the source generation for JSON (de)serialization.

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(GenerationState))]
[JsonSerializable(typeof(RepositoryLinks))]
[JsonSerializable(typeof(GitCheckoutInformation))]
[JsonSerializable(typeof(LinkRegistry))]
[JsonSerializable(typeof(LinkRegistryEntry))]
[JsonSerializable(typeof(DocumentationDocument))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ApplicableTo))]
[JsonSerializable(typeof(AppliesCollection))]
[JsonSerializable(typeof(DeploymentApplicability))]
[JsonSerializable(typeof(ServerlessProjectApplicability))]
[JsonSerializable(typeof(ProductApplicability))]
[JsonSerializable(typeof(Applicability))]
[JsonSerializable(typeof(ProductLifecycle))]
[JsonSerializable(typeof(SemVersion))]
public sealed partial class SourceGenerationContext : JsonSerializerContext;
