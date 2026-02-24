// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Serialization;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Building;

public class AssemblerBuilder(
	ILoggerFactory logFactory,
	AssembleContext context,
	INavigationTraversable navigationTraversable,
	GlobalNavigationHtmlWriter writer,
	GlobalNavigationPathProvider pathProvider,
	ILegacyUrlMapper? legacyUrlMapper
)
{
	private readonly ILogger<AssemblerBuilder> _logger = logFactory.CreateLogger<AssemblerBuilder>();

	private INavigationTraversable NavigationTraversable { get; } = navigationTraversable;

	private GlobalNavigationHtmlWriter HtmlWriter { get; } = writer;

	private ILegacyUrlMapper? LegacyUrlMapper { get; } = legacyUrlMapper;

	public async Task BuildAllAsync(FrozenDictionary<string, AssemblerDocumentationSet> assembleSets, IReadOnlySet<Exporter> exportOptions, Cancel ctx)
	{
		if (context.OutputDirectory.Exists)
			context.OutputDirectory.Delete(true);
		context.OutputDirectory.Create();

		var redirects = new Dictionary<string, string>();
		var buildTimes = new List<(string Name, int FileCount, TimeSpan Duration)>();

		// Create exporters without inferrer - inferrer is created per-repository
		var markdownExporters = exportOptions.CreateMarkdownExporters(logFactory, context, "assembler");
		var tasks = markdownExporters.Select(async e => await e.StartAsync(ctx));
		await Task.WhenAll(tasks);

		var reportPath = context.ConfigurationFileProvider.AssemblerFile;
		foreach (var (_, set) in assembleSets)
		{
			var checkout = set.Checkout;
			if (checkout.Repository.Skip)
			{
				context.Collector.EmitWarning(reportPath, $"Skipping {checkout.Repository.Origin} as its marked as skip in configuration");
				continue;
			}

			// Create inferrer per-repository with git context
			var documentInferrer = new DocumentInferrerService(
				context.ProductsConfiguration,
				context.VersionsConfiguration,
				context.LegacyUrlMappings,
				set.DocumentationSet.Configuration,
				set.DocumentationSet.Context.Git
			);

			var stopwatch = Stopwatch.StartNew();
			try
			{
				var result = await BuildAsync(set, markdownExporters.ToArray(), documentInferrer, ctx);
				CollectRedirects(redirects, result.Redirects, checkout.Repository.Name, set.DocumentationSet.CrossLinkResolver);
			}
			catch (Exception e) when (e.Message.Contains("Can not locate docset.yml file in"))
			{
				context.Collector.EmitWarning(reportPath, $"Skipping {checkout.Repository.Origin} as its not yet been migrated to V3");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
			finally
			{
				stopwatch.Stop();
				buildTimes.Add((checkout.Repository.Name, set.DocumentationSet.Files.Count, stopwatch.Elapsed));
			}
		}

		LogBuildTimes(buildTimes);
		foreach (var exporter in markdownExporters)
		{
			_logger.LogInformation("Calling FinishExportAsync on {ExporterName}", exporter.GetType().Name);
			_ = await exporter.FinishExportAsync(context.OutputWithPathPrefixDirectory, ctx);
		}

		if (exportOptions.Contains(Exporter.Redirects))
		{
			var trimmedRedirects = new Dictionary<string, string>();
			foreach (var r in redirects)
			{
				var key = r.Key.TrimEnd('/');
				if (!key.Equals(r.Value.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
					trimmedRedirects[key] = r.Value;
			}
			await OutputRedirectsAsync(trimmedRedirects, ctx);
		}

		tasks = markdownExporters.Select(async e => await e.StopAsync(ctx));
		await Task.WhenAll(tasks);
	}

	private void CollectRedirects(
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
		string Resolve(string path)
		{
			Uri? uri;
			if (Uri.IsWellFormedUriString(path, UriKind.Absolute)) // Cross-repo links
			{
				_ = linkResolver.TryResolve(
					specificErrorMessage => context.Collector.EmitError(path, $"An error occurred while resolving cross-link {path}", specificErrorMessage),
					new Uri(path),
					out uri);
			}
			else // Relative links
			{
				uri = linkResolver.UriResolver.Resolve(new Uri($"{repository}://{path}"),
					PublishEnvironmentUriResolver.MarkdownPathToUrlPath(path));
			}

			return uri?.AbsolutePath ?? string.Empty;
		}
	}

	private async Task<GenerationResult> BuildAsync(AssemblerDocumentationSet set, IMarkdownExporter[]? markdownExporters, IDocumentInferrerService documentInferrer, Cancel ctx)
	{
		SetFeatureFlags(set);
		var generator = new DocumentationGenerator(
			set.DocumentationSet,
			logFactory, NavigationTraversable, HtmlWriter,
			pathProvider,
			legacyUrlMapper: LegacyUrlMapper,
			markdownExporters: markdownExporters,
			documentInferrer: documentInferrer
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

	private void LogBuildTimes(List<(string Name, int FileCount, TimeSpan Duration)> buildTimes)
	{
		if (buildTimes.Count == 0)
			return;

		var sortedTimes = buildTimes.OrderByDescending(x => x.Duration).ToList();
		var totalDuration = TimeSpan.FromTicks(buildTimes.Sum(x => x.Duration.Ticks));
		var totalFiles = buildTimes.Sum(x => x.FileCount);
		var avgMsPerFile = totalFiles > 0 ? totalDuration.TotalMilliseconds / totalFiles : 0;

		var significantBuilds = sortedTimes.Where(x => x.Duration.TotalSeconds >= 1).ToList();
		var omittedCount = sortedTimes.Count - significantBuilds.Count;

		var maxNameLength = significantBuilds.Count > 0 ? significantBuilds.Max(x => x.Name.Length) : 0;
		var maxFileCountLength = significantBuilds.Count > 0 ? significantBuilds.Max(x => x.FileCount.ToString(CultureInfo.InvariantCulture).Length) : 0;

		_logger.LogInformation("Build times (descending):");
		foreach (var (name, fileCount, duration) in significantBuilds)
		{
			var msPerFile = fileCount > 0 ? duration.TotalMilliseconds / fileCount : 0;
			var paddedTime = $"{duration:mm\\:ss\\.fff}";
			var paddedFiles = fileCount.ToString(CultureInfo.InvariantCulture).PadLeft(maxFileCountLength);
			var paddedName = name.PadRight(maxNameLength);
			var paddedMsPerFile = msPerFile.ToString("F2", CultureInfo.InvariantCulture).PadLeft(5);
			_logger.LogInformation("  {Time}  {Files} files  {MsPerFile} ms/file  {Name}", paddedTime, paddedFiles, paddedMsPerFile, paddedName);
		}

		if (omittedCount > 0)
			_logger.LogInformation("  ... omitted {OmittedCount} repositories with insignificant contribution to build times", omittedCount);

		_logger.LogInformation("Total: {TotalDuration:mm\\:ss\\.fff}  {TotalFiles} files  {AvgMsPerFile:F2} ms/file", totalDuration, totalFiles, avgMsPerFile);
	}

	private async Task OutputRedirectsAsync(Dictionary<string, string> redirects, Cancel ctx)
	{
		var uniqueRedirects = redirects
			.Where(x => !x.Key.TrimEnd('/').Equals(x.Value.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
			.ToDictionary();
		var redirectsFile = context.WriteFileSystem.FileInfo.New(Path.Combine(context.OutputDirectory.FullName, "redirects.json"));
		_logger.LogInformation("Writing {Count} resolved redirects to {Path}", uniqueRedirects.Count, redirectsFile.FullName);

		var redirectsJson = JsonSerializer.Serialize(uniqueRedirects, SourceGenerationContext.Default.DictionaryStringString);
		await context.WriteFileSystem.File.WriteAllTextAsync(redirectsFile.FullName, redirectsJson, ctx);
	}
}
