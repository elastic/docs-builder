// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Core.Search;

namespace Elastic.Documentation.Api.Core;


[JsonSerializable(typeof(AskAiRequest))]
[JsonSerializable(typeof(SearchRequest))]
[JsonSerializable(typeof(SearchResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class ApiJsonContext : JsonSerializerContext;
