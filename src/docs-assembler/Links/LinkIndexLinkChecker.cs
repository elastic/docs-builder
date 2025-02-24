// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Actions.Core.Services;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Assembler.Links;

public class LinkIndexLinkChecker(ILoggerFactory logger)
{
	private readonly ILogger _logger = logger.CreateLogger<LinkIndexLinkChecker>();

	public async Task<int> CheckAll(ICoreService githubActionsService, Cancel ctx)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(logger);
		var resolver = new CrossLinkResolver(fetcher);
		//todo add ctx
		var crossLinks = await resolver.FetchLinks();

		return await ValidateCrossLinks(githubActionsService, crossLinks, resolver, null, ctx);
	}

	public async Task<int> CheckWithLocalLinksJson(
		ICoreService githubActionsService,
		string repository,
		string localLinksJson,
		Cancel ctx
	)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(logger);
		var resolver = new CrossLinkResolver(fetcher);
		var crossLinks = await resolver.FetchLinks();

		if (string.IsNullOrEmpty(repository))
			throw new ArgumentNullException(nameof(repository));
		if (string.IsNullOrEmpty(localLinksJson))
			throw new ArgumentNullException(nameof(repository));

		_logger.LogInformation("Checking '{Repository}' with local '{LocalLinksJson}'", repository, localLinksJson);

		if (!Path.IsPathRooted(localLinksJson))
			localLinksJson = Path.Combine(Paths.Root.FullName, localLinksJson);

		try
		{
			var json = await File.ReadAllTextAsync(localLinksJson, ctx);
			var localLinkReference = LinkReference.Deserialize(json);
			crossLinks = resolver.UpdateLinkReference(repository, localLinkReference);
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to read {LocalLinksJson}", localLinksJson);
			throw;
		}

		_logger.LogInformation("Validating all cross links to {Repository}:// from all repositories published to link-index.json", repository);

		return await ValidateCrossLinks(githubActionsService, crossLinks, resolver, repository, ctx);
	}

	private async Task<int> ValidateCrossLinks(
		ICoreService githubActionsService,
		FetchedCrossLinks crossLinks,
		CrossLinkResolver resolver,
		string? currentRepository,
		Cancel ctx)
	{
		var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);
		_ = collector.StartAsync(ctx);
		foreach (var (repository, linkReference) in crossLinks.LinkReferences)
		{
			_logger.LogInformation("Validating {Repository}", repository);
			foreach (var crossLink in linkReference.CrossLinks)
			{
				// if we are filtering we only want errors from inbound links to a certain
				// repository
				var uri = new Uri(crossLink);
				if (currentRepository != null && uri.Scheme != currentRepository)
					continue;

				_ = resolver.TryResolve(s =>
				{
					if (s.Contains("is not a valid link in the"))
					{
						var error = $"'elastic/{repository}' links to unknown file: " + s;
						error = error.Replace("is not a valid link in the", "in the");
						collector.EmitError(repository, error);
						return;
					}

					collector.EmitError(repository, s);

				}, uri, out _);
			}
		}
		collector.Channel.TryComplete();
		await collector.StopAsync(ctx);
		return collector.Errors + collector.Warnings;
	}
}
