// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Repository;

public class RepositoryPublishValidationService(
	ILoggerFactory logFactory,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	FileSystem fileSystem
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<RepositoryPublishValidationService>();

	/// Validates all configured repository's content sources have been published to the link registry.
	public async Task ValidatePublishStatus(IDiagnosticsCollector collector, Cancel ctx)
	{
		// environment does not matter to check the configuration, defaulting to dev
		var context = new AssembleContext(configuration, configurationContext, "dev", collector, fileSystem, fileSystem, null, null);
		ILinkIndexReader linkIndexReader = Aws3LinkIndexReader.CreateAnonymous();
		var fetcher = new AssemblerCrossLinkFetcher(logFactory, context.Configuration, context.Environment, linkIndexReader);
		var links = await fetcher.FetchLinkRegistry(ctx);
		var repositories = context.Configuration.AvailableRepositories;

		var reportPath = context.ConfigurationFileProvider.AssemblerFile;
		_logger.LogInformation("Validating {RepositoriesCount} configured repositories", repositories.Count);
		foreach (var repository in repositories.Values)
		{
			if (!links.Repositories.TryGetValue(repository.Name, out var registryMapping))
			{
				collector.EmitError(reportPath, $"'{repository}' does not exist in link index");
				continue;
			}

			var current = repository.GetBranch(ContentSource.Current);
			var next = repository.GetBranch(ContentSource.Next);
			if (!registryMapping.TryGetValue(next, out _))
			{
				collector.EmitError(reportPath,
					$"'{repository.Name}' has not yet published links.json for configured 'next' content source: '{next}' see  {linkIndexReader.RegistryUrl}");
			}

			if (!registryMapping.TryGetValue(current, out _))
			{
				collector.EmitError(reportPath,
					$"'{repository.Name}' has not yet published links.json for configured 'current' content source: '{current}' see  {linkIndexReader.RegistryUrl}");
			}
		}
	}
}
