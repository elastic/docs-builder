// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Documentation.Assembler.Navigation;
using Elastic.Documentation.Legacy;
using Elastic.Markdown;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Building;

public class AssemblerBuilder(
	ILoggerFactory logger,
	AssembleContext context,
	GlobalNavigation navigation,
	GlobalNavigationHtmlWriter writer,
	GlobalNavigationPathProvider pathProvider,
	ILegacyUrlMapper? legacyUrlMapper
)
{
	private GlobalNavigationHtmlWriter HtmlWriter { get; } = writer;

	private ILegacyUrlMapper? LegacyUrlMapper { get; } = legacyUrlMapper;

	public async Task BuildAllAsync(FrozenDictionary<string, AssemblerDocumentationSet> assembleSets, Cancel ctx)
	{
		if (context.OutputDirectory.Exists)
			context.OutputDirectory.Delete(true);
		context.OutputDirectory.Create();

		foreach (var (_, set) in assembleSets)
		{
			var checkout = set.Checkout;
			if (checkout.Repository.Skip)
			{
				context.Collector.EmitWarning(context.ConfigurationPath.FullName, $"Skipping {checkout.Repository.Origin} as its marked as skip in configuration");
				continue;
			}

			try
			{
				await BuildAsync(set, ctx);
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

		await context.Collector.StopAsync(ctx);
	}

	private async Task BuildAsync(AssemblerDocumentationSet set, Cancel ctx)
	{
		var generator = new DocumentationGenerator(
			set.DocumentationSet,
			logger, HtmlWriter,
			pathProvider,
			legacyUrlMapper: LegacyUrlMapper,
			positionalNavigation: navigation
		);
		await generator.GenerateAll(ctx);
	}

}
