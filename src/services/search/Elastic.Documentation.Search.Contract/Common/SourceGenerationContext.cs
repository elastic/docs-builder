// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search.Contract;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ISearchDocument))]
[JsonSerializable(typeof(SearchDocumentBase))]
[JsonSerializable(typeof(SiteDocument))]
[JsonSerializable(typeof(LabsDocument))]
[JsonSerializable(typeof(GuideDocument))]
[JsonSerializable(typeof(WebsiteSearchDocument))]
[JsonSerializable(typeof(DocumentationDocument))]
[JsonSerializable(typeof(AppliesToEntry))]
[JsonSerializable(typeof(IndexedProduct))]
[JsonSerializable(typeof(IndexedProduct[]))]
[JsonSerializable(typeof(ParentDocument))]
[JsonSerializable(typeof(string[]))]
public sealed partial class SourceGenerationContext : JsonSerializerContext;
