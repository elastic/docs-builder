// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Internal.Search;

namespace Elastic.Documentation.Search;

/// <summary>
/// STJ context for docs-builder-internal serialization (PIT cursor, ChangesService payloads).
/// The shared search query path uses <see cref="SourceGenerationContext"/> from the
/// Contract package; this context just covers types that remain docs-builder-private.
/// </summary>
[JsonSerializable(typeof(DocumentationDocument))]
[JsonSerializable(typeof(ParentDocument))]
internal sealed partial class EsJsonContext : JsonSerializerContext;
