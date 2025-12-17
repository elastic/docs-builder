// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Text.Json;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Links.InboundLinks;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Mcp;

[McpServerToolType]
public class LinkTools(
	ILinkIndexReader linkIndexReader,
	LinksIndexCrossLinkFetcher crossLinkFetcher)
{
	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

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
				return JsonSerializer.Serialize(new { error = $"Invalid cross-link URI: {crossLink}" }, JsonOptions);

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

				return JsonSerializer.Serialize(new
				{
					resolved = resolvedUri.ToString(),
					repository = uri.Scheme,
					path = lookupPath,
					anchors,
					fragment = uri.Fragment.TrimStart('#')
				}, JsonOptions);
			}

			return JsonSerializer.Serialize(new
			{
				error = "Failed to resolve cross-link",
				details = errors
			}, JsonOptions);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
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

			var repositories = new List<object>();
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
					repositories.Add(new
					{
						repository = repoName,
						branch = entry.Branch,
						path = entry.Path,
						gitRef = entry.GitReference,
						updatedAt = entry.UpdatedAt
					});
				}
			}

			return JsonSerializer.Serialize(new
			{
				count = repositories.Count,
				repositories
			}, JsonOptions);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
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
				return JsonSerializer.Serialize(new
				{
					error = $"Repository '{repository}' not found in link index",
					availableRepositories = registry.Repositories.Keys.ToList()
				}, JsonOptions);
			}

			// Get the main/master branch entry
			var entry = branches.TryGetValue("main", out var mainEntry)
				? mainEntry
				: branches.TryGetValue("master", out var masterEntry)
					? masterEntry
					: branches.Values.FirstOrDefault();

			if (entry == null)
			{
				return JsonSerializer.Serialize(new
				{
					error = $"No main or master branch found for repository '{repository}'"
				}, JsonOptions);
			}

			var links = await linkIndexReader.GetRepositoryLinks(entry.Path, cancellationToken);

			return JsonSerializer.Serialize(new
			{
				repository,
				origin = new
				{
					repositoryName = links.Origin.RepositoryName,
					gitRef = links.Origin.Ref
				},
				urlPathPrefix = links.UrlPathPrefix,
				pageCount = links.Links.Count,
				crossLinkCount = links.CrossLinks.Length,
				pages = links.Links.Select(l => new
				{
					path = l.Key,
					anchors = l.Value.Anchors,
					hidden = l.Value.Hidden
				}).ToList()
			}, JsonOptions);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
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
				return JsonSerializer.Serialize(new
				{
					error = "Please specify at least one of 'from' or 'to' parameters"
				}, JsonOptions);
			}

			var crossLinks = await crossLinkFetcher.FetchCrossLinks(cancellationToken);

			var results = new List<object>();

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

					results.Add(new
					{
						fromRepository = repository,
						toRepository = uri.Scheme,
						link = crossLink
					});
				}
			}

			return JsonSerializer.Serialize(new
			{
				count = results.Count,
				links = results
			}, JsonOptions);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
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
				return JsonSerializer.Serialize(new
				{
					error = $"Repository '{repository}' not found in link index",
					availableRepositories = crossLinks.LinkReferences.Keys.ToList()
				}, JsonOptions);
			}

			var resolver = new CrossLinkResolver(crossLinks);
			var brokenLinks = new List<object>();
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
						brokenLinks.Add(new
						{
							fromRepository = sourceRepo,
							link = crossLink,
							errors
						});
					}
				}
			}

			return JsonSerializer.Serialize(new
			{
				repository,
				validLinks = validCount,
				brokenLinks = brokenLinks.Count,
				broken = brokenLinks
			}, JsonOptions);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
		}
	}
}
