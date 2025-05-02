// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Documentation.Assembler.Navigation;
using Elastic.Documentation.Legacy;
using Elastic.Documentation.Links;
using Elastic.Markdown;
using Elastic.Markdown.Links.CrossLinks;
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

		var redirects = new Dictionary<string, string>();

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
				var result = await BuildAsync(set, ctx);
				CollectRedirects(redirects, result.Redirects, checkout.Repository.Name, set.DocumentationSet.LinkResolver);
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

	private static void CollectRedirects(
		Dictionary<string, string> allRedirects,
		IReadOnlyDictionary<string, LinkRedirect> redirects,
		string repository,
		ICrossLinkResolver linkResolver
	)
	{
		if (redirects.Count == 0)
			return;

		foreach (var (k, v) in redirects)
		{
			if (v.To is { } to)
				allRedirects[Resolve(k)] = Resolve(to);
			else if (v.Many is { } many)
			{
				var target = many.FirstOrDefault(l => l.To is not null);
				if (target?.To is { } t)
					allRedirects[Resolve(k)] = Resolve(t);
			}
		}
		string Resolve(string relativeMarkdownPath)
		{
			var uri = linkResolver.UriResolver.Resolve(new Uri($"{repository}://{relativeMarkdownPath}"),
				PublishEnvironmentUriResolver.MarkdownPathToUrlPath(relativeMarkdownPath));
			return uri.AbsolutePath;
		}
	}

	private async Task<GenerationResult> BuildAsync(AssemblerDocumentationSet set, Cancel ctx)
	{
		var generator = new DocumentationGenerator(
			set.DocumentationSet,
			logger, HtmlWriter,
			pathProvider,
			legacyUrlMapper: LegacyUrlMapper,
			positionalNavigation: navigation
		);
		return await generator.GenerateAll(ctx);
	}

}
