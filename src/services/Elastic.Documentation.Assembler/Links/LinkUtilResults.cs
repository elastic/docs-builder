// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Assembler.Links;

/// <summary>
/// Result of resolving a cross-link.
/// </summary>
public sealed record CrossLinkResolveResult(string ResolvedUrl, string Repository, string Path, string[]? Anchors, string Fragment);

/// <summary>
/// Information about a repository in the link index.
/// </summary>
public sealed record RepositoryInfo(string Repository, string Branch, string Path, string GitRef, DateTimeOffset UpdatedAt);

/// <summary>
/// Result of listing all repositories.
/// </summary>
public sealed record ListRepositoriesResult(int Count, List<RepositoryInfo> Repositories);

/// <summary>
/// Information about a repository's origin.
/// </summary>
public sealed record OriginInfo(string RepositoryName, string GitRef);

/// <summary>
/// Information about a page in a repository.
/// </summary>
public sealed record PageInfo(string Path, string[]? Anchors, bool Hidden);

/// <summary>
/// Result of getting repository links.
/// </summary>
public sealed record RepositoryLinksResult(string Repository, OriginInfo Origin, string? UrlPathPrefix, int PageCount, int CrossLinkCount, List<PageInfo> Pages);

/// <summary>
/// Information about a cross-link between repositories.
/// </summary>
public sealed record CrossLinkInfo(string FromRepository, string ToRepository, string Link);

/// <summary>
/// Result of finding cross-links.
/// </summary>
public sealed record FindCrossLinksResult(int Count, List<CrossLinkInfo> Links);

/// <summary>
/// Information about a broken link.
/// </summary>
public sealed record BrokenLinkInfo(string FromRepository, string Link, List<string> Errors);

/// <summary>
/// Result of validating cross-links.
/// </summary>
public sealed record ValidateCrossLinksResult(string Repository, int ValidLinks, int BrokenLinks, List<BrokenLinkInfo> Broken);

