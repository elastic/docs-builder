// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Links.InboundLinks;

namespace Elastic.Documentation.Assembler.Links;

/// <summary>
/// Service for working with cross-links and the link index.
/// </summary>
public class LinkUtilService(
	ILinkIndexReader linkIndexReader,
	LinksIndexCrossLinkFetcher crossLinkFetcher) : ILinkUtilService
{
	/// <inheritdoc />
	public async Task<LinkUtilResult<CrossLinkResolveResult>> ResolveCrossLinkAsync(string crossLink, CancellationToken cancellationToken = default)
	{
		if (!Uri.TryCreate(crossLink, UriKind.Absolute, out var uri))
			return LinkUtilResult<CrossLinkResolveResult>.CreateFailure($"Invalid cross-link URI: {crossLink}");

		var crossLinks = await crossLinkFetcher.FetchCrossLinks(cancellationToken);

		var errors = new List<string>();
		var resolver = new CrossLinkResolver(crossLinks);

		if (resolver.TryResolve(errors.Add, uri, out var resolvedUri))
		{
			// Try to get anchor information
			var lookupPath = (uri.Host + '/' + uri.AbsolutePath.TrimStart('/')).Trim('/');
			if (string.IsNullOrEmpty(lookupPath) && uri.Host.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
				lookupPath = uri.Host;

			string[]? anchors = null;
			if (crossLinks.LinkReferences.TryGetValue(uri.Scheme, out var linkRef) &&
				linkRef.Links.TryGetValue(lookupPath, out var metadata))
			{
				anchors = metadata.Anchors;
			}

			return LinkUtilResult<CrossLinkResolveResult>.CreateSuccess(
				new CrossLinkResolveResult(resolvedUri.ToString(), uri.Scheme, lookupPath, anchors, uri.Fragment.TrimStart('#')));
		}

		return LinkUtilResult<CrossLinkResolveResult>.CreateFailure("Failed to resolve cross-link", errors);
	}

	/// <inheritdoc />
	public async Task<LinkUtilResult<ListRepositoriesResult>> ListRepositoriesAsync(CancellationToken cancellationToken = default)
	{
		var registry = await linkIndexReader.GetRegistry(cancellationToken);

		var repositories = new List<RepositoryInfo>();
		foreach (var (repoName, branches) in registry.Repositories)
		{
			// Get the main/master branch entry
			var entry = branches.TryGetValue("main", out var mainEntry)
				? mainEntry
				: branches.TryGetValue("master", out var masterEntry)
					? masterEntry
					: branches.Values.FirstOrDefault();

			if (entry != null)
			{
				repositories.Add(new RepositoryInfo(repoName, entry.Branch, entry.Path, entry.GitReference, entry.UpdatedAt));
			}
		}

		return LinkUtilResult<ListRepositoriesResult>.CreateSuccess(
			new ListRepositoriesResult(repositories.Count, repositories));
	}

	/// <inheritdoc />
	public async Task<LinkUtilResult<RepositoryLinksResult>> GetRepositoryLinksAsync(string repository, CancellationToken cancellationToken = default)
	{
		var registry = await linkIndexReader.GetRegistry(cancellationToken);

		if (!registry.Repositories.TryGetValue(repository, out var branches))
		{
			return LinkUtilResult<RepositoryLinksResult>.CreateFailure(
				$"Repository '{repository}' not found in link index",
				availableRepositories: registry.Repositories.Keys.ToList());
		}

		// Get the main/master branch entry
		var entry = branches.TryGetValue("main", out var mainEntry)
			? mainEntry
			: branches.TryGetValue("master", out var masterEntry)
				? masterEntry
				: branches.Values.FirstOrDefault();

		if (entry == null)
		{
			return LinkUtilResult<RepositoryLinksResult>.CreateFailure(
				$"No main or master branch found for repository '{repository}'");
		}

		var links = await linkIndexReader.GetRepositoryLinks(entry.Path, cancellationToken);

		var pages = links.Links.Select(l => new PageInfo(l.Key, l.Value.Anchors, l.Value.Hidden)).ToList();

		return LinkUtilResult<RepositoryLinksResult>.CreateSuccess(
			new RepositoryLinksResult(
				repository,
				new OriginInfo(links.Origin.RepositoryName, links.Origin.Ref),
				links.UrlPathPrefix,
				links.Links.Count,
				links.CrossLinks.Length,
				pages));
	}

	/// <inheritdoc />
	public async Task<LinkUtilResult<FindCrossLinksResult>> FindCrossLinksAsync(string? sourceRepository, string? targetRepository, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(sourceRepository) && string.IsNullOrEmpty(targetRepository))
		{
			return LinkUtilResult<FindCrossLinksResult>.CreateFailure(
				"Please specify at least one of 'from' or 'to' parameters");
		}

		var crossLinks = await crossLinkFetcher.FetchCrossLinks(cancellationToken);

		var results = new List<CrossLinkInfo>();

		foreach (var (repository, linkRef) in crossLinks.LinkReferences)
		{
			// Filter by source repository
			if (!string.IsNullOrEmpty(sourceRepository) && repository != sourceRepository)
				continue;

			foreach (var crossLink in linkRef.CrossLinks)
			{
				if (!Uri.TryCreate(crossLink, UriKind.Absolute, out var uri))
					continue;

				// Filter by target repository
				if (!string.IsNullOrEmpty(targetRepository) && uri.Scheme != targetRepository)
					continue;

				results.Add(new CrossLinkInfo(repository, uri.Scheme, crossLink));
			}
		}

		return LinkUtilResult<FindCrossLinksResult>.CreateSuccess(
			new FindCrossLinksResult(results.Count, results));
	}

	/// <inheritdoc />
	public async Task<LinkUtilResult<ValidateCrossLinksResult>> ValidateCrossLinksAsync(string repository, CancellationToken cancellationToken = default)
	{
		var crossLinks = await crossLinkFetcher.FetchCrossLinks(cancellationToken);

		if (!crossLinks.LinkReferences.ContainsKey(repository))
		{
			return LinkUtilResult<ValidateCrossLinksResult>.CreateFailure(
				$"Repository '{repository}' not found in link index",
				availableRepositories: crossLinks.LinkReferences.Keys.ToList());
		}

		var resolver = new CrossLinkResolver(crossLinks);
		var brokenLinks = new List<BrokenLinkInfo>();
		var validCount = 0;

		foreach (var (sourceRepo, linkRef) in crossLinks.LinkReferences)
		{
			foreach (var crossLink in linkRef.CrossLinks)
			{
				if (!Uri.TryCreate(crossLink, UriKind.Absolute, out var uri))
					continue;

				if (uri.Scheme != repository)
					continue;

				var errors = new List<string>();
				if (resolver.TryResolve(errors.Add, uri, out _))
				{
					validCount++;
				}
				else
				{
					brokenLinks.Add(new BrokenLinkInfo(sourceRepo, crossLink, errors));
				}
			}
		}

		return LinkUtilResult<ValidateCrossLinksResult>.CreateSuccess(
			new ValidateCrossLinksResult(repository, validCount, brokenLinks.Count, brokenLinks));
	}
}
