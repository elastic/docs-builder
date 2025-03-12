// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Assembler.Sourcing;
using Elastic.Markdown;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Building;

public class AssemblerBuilder(ILoggerFactory logger, AssembleContext context)
{
	private readonly ILogger<AssemblerBuilder> _logger = logger.CreateLogger<AssemblerBuilder>();

	public async Task BuildAllAsync(IReadOnlyCollection<Checkout> checkouts, Cancel ctx)
	{
		var crossLinkResolver = new CrossLinkResolver(new AssemblerCrossLinkFetcher(logger, context.Configuration));

		foreach (var checkout in checkouts)
		{
			try
			{
				await BuildAsync(checkout, crossLinkResolver, ctx);
			}
			catch (Exception e) when (e.Message.Contains("Can not locate docset.yml file in"))
			{
				// TODO: we should only ignore this temporarily while migration is ongoing
				_logger.LogWarning("Skipping {Checkout} as its not yet been migrated to V3", checkout.Directory.FullName);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}
	}

	private async Task BuildAsync(Checkout checkout, CrossLinkResolver crossLinkResolver, Cancel ctx)
	{
		var path = checkout.Directory.FullName;
		var pathPrefix = checkout.Repository.PathPrefix;
		var output = pathPrefix != null ? Path.Combine(context.OutputDirectory.FullName, pathPrefix) : context.OutputDirectory.FullName;

		var buildContext = new BuildContext(context.Collector, context.ReadFileSystem, context.WriteFileSystem, path, output)
		{
			UrlPathPrefix = pathPrefix,
			Force = true,
			AllowIndexing = true
		};

		var set = new DocumentationSet(buildContext, logger, crossLinkResolver);
		var generator = new DocumentationGenerator(set, logger);
		await generator.GenerateAll(ctx);
	}
}
