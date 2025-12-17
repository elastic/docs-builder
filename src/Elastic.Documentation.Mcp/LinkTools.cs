// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Text.Json;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Links.InboundLinks;
using Elastic.Documentation.Mcp.Responses;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Mcp;

[McpServerToolType]
public class LinkTools(
	ILinkIndexReader linkIndexReader,
	LinksIndexCrossLinkFetcher crossLinkFetcher)
{
	/// <summary>
	/// Resolves a cross-link URI to its target URL.
	/// </summary>
	[McpServerTool, Description("Resolves a cross-link (like 'docs-content://get-started/intro.md') to its target URL and returns available anchors.")]
	public async Task<string> ResolveCrossLink(
		[Description("The cross-link URI to resolve (e.g., 'docs-content://get-started/intro.md')")] string crossLink,
		CancellationToken cancellationToken = default)
	{
		try
		{
			if (!Uri.TryCreate(crossLink, UriKind.Absolute, out var uri))
				return JsonSerializer.Serialize(new ErrorResponse($"Invalid cross-link URI: {crossLink}"), McpJsonContext.Default.ErrorResponse);

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

				return JsonSerializer.Serialize(
					new CrossLinkResolved(resolvedUri.ToString(), uri.Scheme, lookupPath, anchors, uri.Fragment.TrimStart('#')),
					McpJsonContext.Default.CrossLinkResolved);
			}

			return JsonSerializer.Serialize(new ErrorResponse("Failed to resolve cross-link", errors), McpJsonContext.Default.ErrorResponse);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	/// <summary>
	/// Lists all available repositories in the link index.
	/// </summary>
	[McpServerTool, Description("Lists all repositories available in the cross-link index with their metadata.")]
	public async Task<string> ListRepositories(CancellationToken cancellationToken = default)
	{
		try
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

			return JsonSerializer.Serialize(
				new ListRepositoriesResponse(repositories.Count, repositories),
				McpJsonContext.Default.ListRepositoriesResponse);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	/// <summary>
	/// Gets all links published by a repository.
	/// </summary>
	[McpServerTool, Description("Gets all pages and their anchors published by a specific repository.")]
	public async Task<string> GetRepositoryLinks(
		[Description("The repository name (e.g., 'docs-content', 'elasticsearch')")] string repository,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var registry = await linkIndexReader.GetRegistry(cancellationToken);

			if (!registry.Repositories.TryGetValue(repository, out var branches))
			{
				return JsonSerializer.Serialize(
					new ErrorResponse($"Repository '{repository}' not found in link index", AvailableRepositories: registry.Repositories.Keys.ToList()),
					McpJsonContext.Default.ErrorResponse);
			}

			// Get the main/master branch entry
			var entry = branches.TryGetValue("main", out var mainEntry)
				? mainEntry
				: branches.TryGetValue("master", out var masterEntry)
					? masterEntry
					: branches.Values.FirstOrDefault();

			if (entry == null)
			{
				return JsonSerializer.Serialize(
					new ErrorResponse($"No main or master branch found for repository '{repository}'"),
					McpJsonContext.Default.ErrorResponse);
			}

			var links = await linkIndexReader.GetRepositoryLinks(entry.Path, cancellationToken);

			var pages = links.Links.Select(l => new PageInfo(l.Key, l.Value.Anchors, l.Value.Hidden)).ToList();

			return JsonSerializer.Serialize(
				new RepositoryLinksResponse(
					repository,
					new OriginInfo(links.Origin.RepositoryName, links.Origin.Ref),
					links.UrlPathPrefix,
					links.Links.Count,
					links.CrossLinks.Length,
					pages),
				McpJsonContext.Default.RepositoryLinksResponse);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	/// <summary>
	/// Finds all cross-links from one repository to another.
	/// </summary>
	[McpServerTool, Description("Finds all cross-links between repositories. Can filter by source or target repository.")]
	public async Task<string> FindCrossLinks(
		[Description("Source repository to find links FROM (optional)")] string? from = null,
		[Description("Target repository to find links TO (optional)")] string? to = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			if (string.IsNullOrEmpty(from) && string.IsNullOrEmpty(to))
			{
				return JsonSerializer.Serialize(
					new ErrorResponse("Please specify at least one of 'from' or 'to' parameters"),
					McpJsonContext.Default.ErrorResponse);
			}

			var crossLinks = await crossLinkFetcher.FetchCrossLinks(cancellationToken);

			var results = new List<CrossLinkInfo>();

			foreach (var (repository, linkRef) in crossLinks.LinkReferences)
			{
				// Filter by source repository
				if (!string.IsNullOrEmpty(from) && repository != from)
					continue;

				foreach (var crossLink in linkRef.CrossLinks)
				{
					if (!Uri.TryCreate(crossLink, UriKind.Absolute, out var uri))
						continue;

					// Filter by target repository
					if (!string.IsNullOrEmpty(to) && uri.Scheme != to)
						continue;

					results.Add(new CrossLinkInfo(repository, uri.Scheme, crossLink));
				}
			}

			return JsonSerializer.Serialize(
				new FindCrossLinksResponse(results.Count, results),
				McpJsonContext.Default.FindCrossLinksResponse);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	/// <summary>
	/// Validates cross-links and finds broken ones.
	/// </summary>
	[McpServerTool, Description("Validates cross-links to a repository and reports any broken links.")]
	public async Task<string> ValidateCrossLinks(
		[Description("Target repository to validate links TO (e.g., 'docs-content')")] string repository,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var crossLinks = await crossLinkFetcher.FetchCrossLinks(cancellationToken);

			if (!crossLinks.LinkReferences.ContainsKey(repository))
			{
				return JsonSerializer.Serialize(
					new ErrorResponse($"Repository '{repository}' not found in link index", AvailableRepositories: crossLinks.LinkReferences.Keys.ToList()),
					McpJsonContext.Default.ErrorResponse);
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

			return JsonSerializer.Serialize(
				new ValidateCrossLinksResponse(repository, validCount, brokenLinks.Count, brokenLinks),
				McpJsonContext.Default.ValidateCrossLinksResponse);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}
}
