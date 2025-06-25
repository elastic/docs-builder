// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text.Json;
using Documentation.Assembler.Exporters;
using Documentation.Assembler.Navigation;
using Elastic.Documentation.Legacy;
using Elastic.Documentation.Links;
using Elastic.Documentation.Serialization;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.Links.CrossLinks;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Building;

public enum ExportOption
{
	Html = 0,
	LLMText = 1,
	Elasticsearch = 2
}

public class AssemblerBuilder(
	ILoggerFactory logger,
	AssembleContext context,
	GlobalNavigation navigation,
	GlobalNavigationHtmlWriter writer,
	GlobalNavigationPathProvider pathProvider,
	ILegacyUrlMapper? legacyUrlMapper
)
{
	private readonly ILogger<AssemblerBuilder> _logger = logger.CreateLogger<AssemblerBuilder>();

	private GlobalNavigationHtmlWriter HtmlWriter { get; } = writer;

	private ILegacyUrlMapper? LegacyUrlMapper { get; } = legacyUrlMapper;

	public async Task BuildAllAsync(FrozenDictionary<string, AssemblerDocumentationSet> assembleSets, IReadOnlySet<ExportOption> exportOptions, Cancel ctx)
	{
		if (context.OutputDirectory.Exists)
			context.OutputDirectory.Delete(true);
		context.OutputDirectory.Create();

		var redirects = new Dictionary<string, string>();

		var esExporter =
			Environment.GetEnvironmentVariable("ELASTIC_API_KEY") is { } apiKey &&
			Environment.GetEnvironmentVariable("ELASTIC_URL") is { } url
				? new ElasticsearchMarkdownExporter(logger, context.Collector, url, apiKey)
				: null;

		var markdownExporters = new List<IMarkdownExporter>(3);
		if (exportOptions.Contains(ExportOption.LLMText))
			markdownExporters.Add(new LLMTextExporter());
		if (exportOptions.Contains(ExportOption.Elasticsearch) && esExporter is { })
			markdownExporters.Add(esExporter);
		var noopBuild = !exportOptions.Contains(ExportOption.Html);

		var tasks = markdownExporters.Select(async e => await e.StartAsync(ctx));
		await Task.WhenAll(tasks);

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
				var result = await BuildAsync(set, noopBuild, markdownExporters.ToArray(), ctx);
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
		foreach (var exporter in markdownExporters)
		{
			_logger.LogInformation("Calling FinishExportAsync on {ExporterName}", exporter.GetType().Name);
			_ = await exporter.FinishExportAsync(context.OutputDirectory, ctx);
		}

		await OutputRedirectsAsync(redirects
			.Where(r => !r.Key.TrimEnd('/').Equals(r.Value.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
			.ToDictionary(r => r.Key, r => r.Value), ctx);

		tasks = markdownExporters.Select(async e => await e.StopAsync(ctx));
		await Task.WhenAll(tasks);
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

	private async Task<GenerationResult> BuildAsync(AssemblerDocumentationSet set, bool noop, IMarkdownExporter[]? markdownExporters, Cancel ctx)
	{
		SetFeatureFlags(set);
		var generator = new DocumentationGenerator(
			set.DocumentationSet,
			logger, HtmlWriter,
			pathProvider,
			legacyUrlMapper: LegacyUrlMapper,
			positionalNavigation: navigation,
			documentationExporter: noop ? new NoopDocumentationFileExporter() : null,
			markdownExporters: markdownExporters
		);
		return await generator.GenerateAll(ctx);
	}

	private void SetFeatureFlags(AssemblerDocumentationSet set)
	{
		// Enable primary nav by default
		set.DocumentationSet.Configuration.Features.PrimaryNavEnabled = true;
		foreach (var configurationFeatureFlag in set.AssembleContext.Environment.FeatureFlags)
		{
			_logger.LogInformation("Setting feature flag: {ConfigurationFeatureFlagKey}={ConfigurationFeatureFlagValue}", configurationFeatureFlag.Key, configurationFeatureFlag.Value);
			set.DocumentationSet.Configuration.Features.Set(configurationFeatureFlag.Key, configurationFeatureFlag.Value);
		}
	}

	private async Task OutputRedirectsAsync(Dictionary<string, string> redirects, Cancel ctx)
	{
		var uniqueRedirects = redirects
			.Where(x => !x.Key.TrimEnd('/').Equals(x.Value.TrimEnd('/')))
			.ToDictionary();
		var redirectsFile = context.WriteFileSystem.FileInfo.New(Path.Combine(context.OutputDirectory.FullName, "redirects.json"));
		_logger.LogInformation("Writing {Count} resolved redirects to {Path}", uniqueRedirects.Count, redirectsFile.FullName);

		var redirectsJson = JsonSerializer.Serialize(uniqueRedirects, SourceGenerationContext.Default.DictionaryStringString);
		await context.WriteFileSystem.File.WriteAllTextAsync(redirectsFile.FullName, redirectsJson, ctx);
	}
}
