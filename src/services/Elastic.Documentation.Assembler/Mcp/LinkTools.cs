// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Text.Json;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Assembler.Mcp.Responses;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Assembler.Mcp;

[McpServerToolType]
public class LinkTools(ILinkUtilService linkUtilService)
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
			var result = await linkUtilService.ResolveCrossLinkAsync(crossLink, cancellationToken);

			if (result.IsSuccess)
			{
				var value = result.Value;
				return JsonSerializer.Serialize(
					new CrossLinkResolved(value.ResolvedUrl, value.Repository, value.Path, value.Anchors, value.Fragment),
					McpJsonContext.Default.CrossLinkResolved);
			}

			return JsonSerializer.Serialize(
				new ErrorResponse(result.Error.Message, result.Error.Details, result.Error.AvailableRepositories),
				McpJsonContext.Default.ErrorResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
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
			var result = await linkUtilService.ListRepositoriesAsync(cancellationToken);

			if (result.IsSuccess)
			{
				var value = result.Value;
				var repos = value.Repositories.Select(r =>
					new Responses.RepositoryInfo(r.Repository, r.Branch, r.Path, r.GitRef, r.UpdatedAt)).ToList();
				return JsonSerializer.Serialize(
					new ListRepositoriesResponse(value.Count, repos),
					McpJsonContext.Default.ListRepositoriesResponse);
			}

			return JsonSerializer.Serialize(
				new ErrorResponse(result.Error.Message, result.Error.Details, result.Error.AvailableRepositories),
				McpJsonContext.Default.ErrorResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
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
			var result = await linkUtilService.GetRepositoryLinksAsync(repository, cancellationToken);

			if (result.IsSuccess)
			{
				var value = result.Value;
				var pages = value.Pages.Select(p =>
					new Responses.PageInfo(p.Path, p.Anchors, p.Hidden)).ToList();
				return JsonSerializer.Serialize(
					new RepositoryLinksResponse(
						value.Repository,
						new Responses.OriginInfo(value.Origin.RepositoryName, value.Origin.GitRef),
						value.UrlPathPrefix,
						value.PageCount,
						value.CrossLinkCount,
						pages),
					McpJsonContext.Default.RepositoryLinksResponse);
			}

			return JsonSerializer.Serialize(
				new ErrorResponse(result.Error.Message, result.Error.Details, result.Error.AvailableRepositories),
				McpJsonContext.Default.ErrorResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
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
			var result = await linkUtilService.FindCrossLinksAsync(from, to, cancellationToken);

			if (result.IsSuccess)
			{
				var value = result.Value;
				var links = value.Links.Select(l =>
					new Responses.CrossLinkInfo(l.FromRepository, l.ToRepository, l.Link)).ToList();
				return JsonSerializer.Serialize(
					new FindCrossLinksResponse(value.Count, links),
					McpJsonContext.Default.FindCrossLinksResponse);
			}

			return JsonSerializer.Serialize(
				new ErrorResponse(result.Error.Message, result.Error.Details, result.Error.AvailableRepositories),
				McpJsonContext.Default.ErrorResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
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
			var result = await linkUtilService.ValidateCrossLinksAsync(repository, cancellationToken);

			if (result.IsSuccess)
			{
				var value = result.Value;
				var broken = value.Broken.Select(b =>
					new Responses.BrokenLinkInfo(b.FromRepository, b.Link, b.Errors)).ToList();
				return JsonSerializer.Serialize(
					new ValidateCrossLinksResponse(value.Repository, value.ValidLinks, value.BrokenLinks, broken),
					McpJsonContext.Default.ValidateCrossLinksResponse);
			}

			return JsonSerializer.Serialize(
				new ErrorResponse(result.Error.Message, result.Error.Details, result.Error.AvailableRepositories),
				McpJsonContext.Default.ErrorResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}
}
