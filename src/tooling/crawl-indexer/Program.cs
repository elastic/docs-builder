// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using CrawlIndexer;
using CrawlIndexer.Commands;
using CrawlIndexer.Display;
using CrawlIndexer.Filters;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder()
	.AddDocumentationServiceDefaults(ref args)
	.AddCrawlIndexerToolingDefaults()
	.AddOpenTelemetryDefaults();

// Add filters to suppress noisy HTTP/Polly logs
_ = builder.Logging.AddNoisyLogFilters();

var app = builder.ToConsoleAppBuilder();

app.UseFilter<ReplaceLogFilter>();
app.UseFilter<InfoLoggerFilter>();
app.UseFilter<StopwatchFilter>();
app.UseFilter<CatchExceptionFilter>();
app.UseFilter<CheckForUpdatesFilter>();

app.Add<GuideCommand>("guide");
app.Add<SiteCommand>("site");

await app.RunAsync(args).ConfigureAwait(false);
