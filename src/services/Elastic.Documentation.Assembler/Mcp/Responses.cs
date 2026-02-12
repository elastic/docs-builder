// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Assembler.Mcp.Responses;

// Common error response
public sealed record ErrorResponse(string Error, List<string>? Details = null, List<string>? AvailableRepositories = null);

// ResolveCrossLink response
public sealed record CrossLinkResolved(string Resolved, string Repository, string Path, string[]? Anchors, string Fragment);

// ListRepositories response
public sealed record RepositoryInfo(string Repository, string Branch, string Path, string GitRef, DateTimeOffset UpdatedAt);
public sealed record ListRepositoriesResponse(int Count, List<RepositoryInfo> Repositories);

// GetRepositoryLinks response
public sealed record OriginInfo(string RepositoryName, string GitRef);
public sealed record PageInfo(string Path, string[]? Anchors, bool Hidden);
public sealed record RepositoryLinksResponse(string Repository, OriginInfo Origin, string? UrlPathPrefix, int PageCount, int CrossLinkCount, List<PageInfo> Pages);

// FindCrossLinks response
public sealed record CrossLinkInfo(string FromRepository, string ToRepository, string Link);
public sealed record FindCrossLinksResponse(int Count, List<CrossLinkInfo> Links);

// ValidateCrossLinks response
public sealed record BrokenLinkInfo(string FromRepository, string Link, List<string> Errors);
public sealed record ValidateCrossLinksResponse(string Repository, int ValidLinks, int BrokenLinks, List<BrokenLinkInfo> Broken);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(CrossLinkResolved))]
[JsonSerializable(typeof(ListRepositoriesResponse))]
[JsonSerializable(typeof(RepositoryLinksResponse))]
[JsonSerializable(typeof(FindCrossLinksResponse))]
[JsonSerializable(typeof(ValidateCrossLinksResponse))]
public sealed partial class McpJsonContext : JsonSerializerContext;
