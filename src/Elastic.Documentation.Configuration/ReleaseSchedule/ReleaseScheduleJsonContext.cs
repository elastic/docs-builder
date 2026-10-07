// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Configuration.ReleaseSchedule;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(FutureReleasesResponse))]
[JsonSerializable(typeof(PastReleasesResponse))]
[JsonSerializable(typeof(ElasticBuildManifest))]
[JsonSerializable(typeof(LatestBuildPointer))]
public sealed partial class ReleaseScheduleJsonContext : JsonSerializerContext;
