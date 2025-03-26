// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Links.CrossLinks;
using Elastic.Markdown.Links.InboundLinks;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Links.LinkNamespaces;

/// <summary>
/// Validates paths don't conflict with global navigation namespaces.
/// For example if the global navigation defines:
/// <code>
/// - toc: elasticsearch://reference/elasticsearch
///   path_prefix: reference/elasticsearch
///
/// - toc: docs-content://reference/elasticsearch/clients
///   path_prefix: reference/elasticsearch/clients
/// </code>
///
/// This will validate `elasticsearch://` does not create a `elasticsearch://reference/elasticsearch/clients` folder
/// since that is already claimed by `docs-content://reference/elasticsearch/clients`
///
/// </summary>
public class NavigationPrefixChecker(ILoggerFactory logger, ImmutableHashSet<Uri> namespaces)
{
	private readonly Dictionary<string, string> _pathPrefixes = namespaces
		.ToDictionary(n => $"{n.Host}/{n.AbsolutePath.Trim('/')}/", n => n.Scheme);

	private readonly ILogger _logger = logger.CreateLogger<NavigationPrefixChecker>();

	public async Task CheckWithLocalLinksJson(DiagnosticsCollector collector, string repository, string? localLinksJson, CancellationToken ctx)
	{
		if (string.IsNullOrEmpty(repository))
			throw new ArgumentNullException(nameof(repository));
		if (string.IsNullOrEmpty(localLinksJson))
			throw new ArgumentNullException(nameof(localLinksJson));

		_logger.LogInformation("Checking '{Repository}' with local '{LocalLinksJson}'", repository, localLinksJson);

		if (!Path.IsPathRooted(localLinksJson))
			localLinksJson = Path.Combine(Paths.WorkingDirectoryRoot.FullName, localLinksJson);

		var linkReference = await ReadLocalLinksJsonAsync(localLinksJson, ctx);

		foreach (var (relativeLink, _) in linkReference.Links)
		{
			if (!TryGetReservedPathPrefix(relativeLink, out var reservedPathPrefix, out var byRepository))
				continue;
			if (byRepository == repository)
				continue;

			collector.EmitError(repository, $"'{relativeLink}' lives in path_prefix already claimed by '{byRepository}://{reservedPathPrefix}' in global navigation.yml");
		}
	}

	private bool TryGetReservedPathPrefix(
		string path,
		[NotNullWhen(true)] out string? reservedPathPrefix,
		[NotNullWhen(true)] out string? reservedByRepository
	)
	{
		reservedPathPrefix = null;
		reservedByRepository = null;
		foreach (var (prefix, repository) in _pathPrefixes)
		{
			if (!path.StartsWith(prefix))
				continue;
			reservedPathPrefix = prefix;
			reservedByRepository = repository;
			return true;
		}

		return false;
	}

	private async Task<LinkReference> ReadLocalLinksJsonAsync(string localLinksJson, Cancel ctx)
	{
		try
		{
			var json = await File.ReadAllTextAsync(localLinksJson, ctx);
			return LinkReference.Deserialize(json);
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to read {LocalLinksJson}", localLinksJson);
			throw;
		}
	}

	public async Task CheckAllPublishedLinks(DiagnosticsCollector collector, Cancel ctx)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(logger);
		var resolver = new CrossLinkResolver(fetcher);
		var crossLinks = await resolver.FetchLinks(ctx);
		foreach (var (repository, linkReference) in crossLinks.LinkReferences)
		{
			foreach (var (relativeLink, _) in linkReference.Links)
			{

				if (!TryGetReservedPathPrefix(relativeLink, out var reservedPathPrefix, out var byRepository))
					continue;
				if (byRepository == repository)
					continue;

				collector.EmitError(repository, $"'{byRepository}://{reservedPathPrefix}' in navigation.yml conflicts with already published '{repository}://{relativeLink}'");
			}

		}
	}
}
