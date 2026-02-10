// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.Search.Common;

namespace Elastic.Documentation.Search;

[JsonSerializable(typeof(DocumentationDocument))]
[JsonSerializable(typeof(ParentDocument))]
[JsonSerializable(typeof(RuleQueryMatchCriteria))]
internal sealed partial class EsJsonContext : JsonSerializerContext;
