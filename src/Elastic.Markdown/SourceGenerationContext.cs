// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Discovery;
using Elastic.Markdown.IO.State;

namespace Elastic.Markdown;

// This configures the source generation for json (de)serialization.

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GenerationState))]
[JsonSerializable(typeof(LinkReference))]
[JsonSerializable(typeof(GitCheckoutInformation))]
internal partial class SourceGenerationContext : JsonSerializerContext;
