// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Links.InboundLinks;

public class LinkIndexService(ILoggerFactory logFactory, IFileSystem fileSystem) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<LinkIndexService>();
	private readonly ILinkIndexReader _linkIndexProvider = Aws3LinkIndexReader.CreateAnonymous();
	private sealed record RepositoryFilter
	{
		public string? LinksTo { get; init; }
		public string? LinksFrom { get; init; }

		public static RepositoryFilter None => new();
	}

	public async Task<bool> CheckAll(IDiagnosticsCollector collector, Cancel ctx)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(logFactory, _linkIndexProvider);
		var crossLinks = await fetcher.FetchCrossLinks(ctx);
		var resolver = new CrossLinkResolver(crossLinks);

		return ValidateCrossLinks(collector, crossLinks, resolver, RepositoryFilter.None);
	}

	public async Task<bool> CheckRepository(IDiagnosticsCollector collector, string? toRepository, string? fromRepository, Cancel ctx)
	{
		var root = fileSystem.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		if (fromRepository == null && toRepository == null)
		{
			fromRepository ??= GitCheckoutInformation.Create(root, fileSystem, logFactory.CreateLogger(nameof(GitCheckoutInformation))).RepositoryName;
			if (fromRepository == null)
				throw new Exception("Unable to determine repository name");
		}
		var fetcher = new LinksIndexCrossLinkFetcher(logFactory, _linkIndexProvider);
		var crossLinks = await fetcher.FetchCrossLinks(ctx);
		var resolver = new CrossLinkResolver(crossLinks);
		var filter = new RepositoryFilter
		{
			LinksTo = toRepository,
			LinksFrom = fromRepository
		};

		return ValidateCrossLinks(collector, crossLinks, resolver, filter);
	}

	public async Task<bool> CheckWithLocalLinksJson(IDiagnosticsCollector collector, string? file = null, string? path = null, Cancel ctx = default)
	{
		file ??= ".artifacts/docs/html/links.json";
		var root = !string.IsNullOrEmpty(path) ? fileSystem.DirectoryInfo.New(path) : fileSystem.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		var repository = GitCheckoutInformation.Create(root, fileSystem, logFactory.CreateLogger(nameof(GitCheckoutInformation))).RepositoryName
						?? throw new Exception("Unable to determine repository name");

		var localLinksJson = fileSystem.FileInfo.New(Path.Combine(root.FullName, file));

		var runningOnCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
		if (runningOnCi && !localLinksJson.Exists)
		{
			_logger.LogInformation("Running on CI after a build that produced no {File}, skipping the validation", localLinksJson.FullName);
			return true;
		}
		if (runningOnCi && !Paths.TryFindDocsFolderFromRoot(fileSystem, root, out _, out _))
		{
			_logger.LogInformation("Running on CI, {Directory} has no documentation, skipping the validation", root.FullName);
			return true;
		}

		if (!fileSystem.File.Exists(localLinksJson.FullName))
		{
			collector.EmitError(localLinksJson.FullName, "Unable to find local links");
			return false;
		}


		_logger.LogInformation("Validating {File} in {Directory}", file, root.FullName);
		var fetcher = new LinksIndexCrossLinkFetcher(logFactory, _linkIndexProvider);
		var crossLinks = await fetcher.FetchCrossLinks(ctx);
		var resolver = new CrossLinkResolver(crossLinks);

		_logger.LogInformation("Checking '{Repository}' with local '{LocalLinksJson}'", repository, localLinksJson);

		try
		{
			var json = await fileSystem.File.ReadAllTextAsync(localLinksJson.FullName, ctx);
			var localLinkReference = RepositoryLinks.Deserialize(json);
			crossLinks = resolver.UpdateLinkReference(repository, localLinkReference);
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to read {LocalLinksJson}", localLinksJson);
			throw;
		}

		_logger.LogInformation("Validating all cross links to {Repository}:// from all repositories published to link-index.json", repository);
		var filter = new RepositoryFilter
		{
			LinksTo = repository
		};

		return ValidateCrossLinks(collector, crossLinks, resolver, filter);
	}

	private bool ValidateCrossLinks(
		IDiagnosticsCollector collector,
		FetchedCrossLinks crossLinks,
		CrossLinkResolver resolver,
		RepositoryFilter filter
	)
	{
		foreach (var (repository, linkReference) in crossLinks.LinkReferences)
		{
			if (!string.IsNullOrEmpty(filter.LinksTo))
				_logger.LogInformation("Validating '{CurrentRepository}://' links in {TargetRepository}", filter.LinksTo, repository);
			else if (!string.IsNullOrEmpty(filter.LinksFrom))
			{
				if (repository != filter.LinksFrom)
					continue;
				_logger.LogInformation("Validating cross_links from {TargetRepository}", filter.LinksFrom);
			}
			else
				_logger.LogInformation("Validating all cross_links in {Repository}", repository);

			foreach (var crossLink in linkReference.CrossLinks)
			{
				// if we are filtering, we only want errors from inbound links to a certain repository
				var uri = new Uri(crossLink);
				if (filter.LinksTo != null && uri.Scheme != filter.LinksTo)
					continue;

				var linksJson = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/{uri.Scheme}/main/links.json";
				if (crossLinks.LinkIndexEntries.TryGetValue(uri.Scheme, out var linkIndexEntry))
					linksJson = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/{linkIndexEntry.Path}";
				_ = resolver.TryResolve(s =>
				{
					if (s.Contains("is not a valid link in the"))
					{
						//
						var error = $"'elastic/{repository}' links to unknown file: " + s;
						error = error.Replace("is not a valid link in the", "in the");
						collector.EmitError(linksJson, error);
						return;
					}

					collector.EmitError(repository, s);
				}, uri, out _);
			}
		}

		return collector.Errors == 0;
	}
}
