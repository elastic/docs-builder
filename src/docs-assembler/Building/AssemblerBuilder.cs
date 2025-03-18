// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Assembler.Configuration;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Elastic.Markdown;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Building;

public class AssemblerBuilder(ILoggerFactory logger, AssembleContext context, GlobalNavigation globalNavigation)
{
	private readonly ILogger<AssemblerBuilder> _logger = logger.CreateLogger<AssemblerBuilder>();

	public async Task BuildAllAsync(IReadOnlyCollection<Checkout> checkouts, PublishEnvironment environment, Cancel ctx)
	{
		var crossLinkFetcher = new AssemblerCrossLinkFetcher(logger, context.Configuration);
		var uriResolver = new PublishEnvironmentUriResolver(globalNavigation, environment);
		var crossLinkResolver = new CrossLinkResolver(crossLinkFetcher, uriResolver);

		foreach (var checkout in checkouts)
		{
			try
			{
				await BuildAsync(checkout, environment, crossLinkResolver, ctx);
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

	private async Task BuildAsync(Checkout checkout, PublishEnvironment environment, CrossLinkResolver crossLinkResolver, Cancel ctx)
	{
		var path = checkout.Directory.FullName;
		var localPathPrefix = checkout.Repository.PathPrefix;
		var pathPrefix = (environment.PathPrefix, localPathPrefix) switch
		{
			(null or "", null or "") => null,
			(null or "", _) => localPathPrefix,
			(_, null or "") => environment.PathPrefix,
			var (globalPrefix, docsetPrefix) => $"{globalPrefix}/{docsetPrefix}"
		};
		var output = localPathPrefix != null ? Path.Combine(context.OutputDirectory.FullName, localPathPrefix) : context.OutputDirectory.FullName;

		var buildContext = new BuildContext(context.Collector, context.ReadFileSystem, context.WriteFileSystem, path, output)
		{
			StaticUrlPathPrefix = environment.PathPrefix,
			UrlPathPrefix = pathPrefix,
			Force = true,
			AllowIndexing = environment.AllowIndexing
		};

		var set = new DocumentationSet(buildContext, logger, crossLinkResolver);
		var generator = new DocumentationGenerator(set, logger);
		await generator.GenerateAll(ctx);
	}
}
