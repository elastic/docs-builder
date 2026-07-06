// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Elastic.SiteSearch.Cli;
using Elastic.SiteSearch.Cli.Commands;
using Elastic.SiteSearch.Cli.ContentStack;
using Elastic.SiteSearch.Cli.Elasticsearch;
using Elastic.SiteSearch.Cli.LabsCrawl;
using Elastic.SiteSearch.Cli.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Nullean.Argh.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var isInteractive =
	string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) &&
	!Console.IsOutputRedirected;

builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddFilter("Polly", LogLevel.Warning);

// In non-interactive / CI mode the progress widgets are suppressed, so surface
// the exporter's Info logs to console so the run isn't silent.
if (!isInteractive)
	_ = builder.Logging.AddFilter("Elastic.SiteSearch.Cli.Elasticsearch", LogLevel.Information);

builder.Logging.AddFilter("Elastic.SiteSearch.Cli.Elasticsearch.SourcingConfiguration", LogLevel.Information);

builder.Logging.AddConsole(o => o.FormatterName = "condensed");
builder.Services.AddSingleton<ConsoleFormatter, CondensedConsoleFormatter>();

builder.Services.AddSingleton(_ => SourcingConfiguration.CreateFromEnvironment());
builder.Services.AddSingleton<ContentStackConfiguration>(sp => sp.GetRequiredService<SourcingConfiguration>().RequireContentStack());

var csHttpClient = builder.Services
	.AddHttpClient<ContentStackClient>()
	.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
	{
		AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
	});

// Rate limiter is added after the resilience pipeline so it applies on each retry attempt.
csHttpClient.AddStandardResilienceHandler(o =>
{
	o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
	o.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(30);
	o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
	o.CircuitBreaker.MinimumThroughput = 20;
	o.Retry.MaxRetryAttempts = 8;
	o.Retry.UseJitter = true;
	o.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
	o.Retry.Delay = TimeSpan.FromSeconds(2);
});
csHttpClient.AddHttpMessageHandler(() => RateLimitingHandler.CreateForContentStack());

builder.Services.AddSingleton<CrawlerSettings>();
builder.Services.AddSingleton<CrawlerRateLimiter>();

static void ConfigureLabsCrawlHttp(IHttpClientBuilder b)
{
	_ = b.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
	{
		AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
	});
	_ = b.AddStandardResilienceHandler(o =>
	{
		o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
		o.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(30);
		o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
		o.CircuitBreaker.MinimumThroughput = 20;
		o.Retry.MaxRetryAttempts = 8;
		o.Retry.UseJitter = true;
		o.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
		o.Retry.Delay = TimeSpan.FromSeconds(2);
	});
}

ConfigureLabsCrawlHttp(builder.Services.AddHttpClient<ISitemapParser, SitemapParser>());
ConfigureLabsCrawlHttp(builder.Services.AddHttpClient<IAdaptiveCrawler, AdaptiveCrawler>());

builder.Services.AddSingleton<SyncCommand>();
builder.Services.AddSingleton<ContentTypesCommand>();
builder.Services.AddSingleton<DumpSamplesCommand>();
builder.Services.AddSingleton<LabsCommands>();
builder.Services.AddSingleton<IndicesCommands>();

builder.Services.AddArgh(
	args,
	argh =>
	{
		_ = argh.UseCliDescription(
			"Elastic Site Search CLI — tooling to ingest and enrich data published on elastic.co (not the Elastic documentation site).");
		_ = argh.MapNamespace<ContentStackCommands>("contentstack");
		_ = argh.MapNamespace<LabsCommands>("labs");
		_ = argh.MapNamespace<IndicesCommands>("indices");
	});

using var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
