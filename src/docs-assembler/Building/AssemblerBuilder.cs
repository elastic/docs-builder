// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Assembler.Configuration;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Elastic.Markdown;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Discovery;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Building;

public class AssemblerBuilder(ILoggerFactory logger, AssembleContext context, GlobalNavigation globalNavigation)
{
	public async Task BuildAllAsync(IReadOnlyCollection<Checkout> checkouts, PublishEnvironment environment, Cancel ctx)
	{
		var crossLinkFetcher = new AssemblerCrossLinkFetcher(logger, context.Configuration);
		var uriResolver = new PublishEnvironmentUriResolver(globalNavigation, environment);
		var crossLinkResolver = new CrossLinkResolver(crossLinkFetcher, uriResolver);

		if (context.OutputDirectory.Exists)
			context.OutputDirectory.Delete(true);
		context.OutputDirectory.Create();

		foreach (var checkout in checkouts)
		{
			if (checkout.Repository.Skip)
			{
				context.Collector.EmitWarning(context.ConfigurationPath.FullName, $"Skipping {checkout.Repository.Origin} as its marked as skip in configuration");
				continue;
			}

			try
			{
				await BuildAsync(checkout, environment, crossLinkResolver, ctx);
			}
			catch (Exception e) when (e.Message.Contains("Can not locate docset.yml file in"))
			{
				context.Collector.EmitWarning(context.ConfigurationPath.FullName, $"Skipping {checkout.Repository.Origin} as its not yet been migrated to V3");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}

		context.Collector.Channel.TryComplete();
		await context.Collector.StopAsync(ctx);
	}

	private async Task BuildAsync(Checkout checkout, PublishEnvironment environment, CrossLinkResolver crossLinkResolver, Cancel ctx)
	{
		var path = checkout.Directory.FullName;
		var output = environment.PathPrefix != null ? Path.Combine(context.OutputDirectory.FullName, environment.PathPrefix) : context.OutputDirectory.FullName;

		var gitConfiguration = new GitCheckoutInformation
		{
			RepositoryName = checkout.Repository.Name,
			Ref = checkout.HeadReference,
			Remote = $"elastic/${checkout.Repository.Name}",
			Branch = checkout.Repository.CurrentBranch
		};

		var buildContext = new BuildContext(context.Collector, context.ReadFileSystem, context.WriteFileSystem, path, output, gitConfiguration)
		{
			UrlPathPrefix = environment.PathPrefix,
			Force = false,
			AllowIndexing = environment.AllowIndexing,
			EnableGoogleTagManager = environment.EnableGoogleTagManager ?? false,
			CanonicalBaseUrl = new Uri("https://www.elastic.co"), // Always use the production URL. In case a page is leaked to a search engine, it should point to the production site.
			SkipMetadata = true
		};

		var set = new DocumentationSet(buildContext, logger, crossLinkResolver);
		var generator = new DocumentationGenerator(set, logger, globalNavigation);
		await generator.GenerateAll(ctx);
	}
}
