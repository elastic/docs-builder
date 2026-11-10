// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Journeys.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nullean.Argh.Hosting;

var builder = Host.CreateApplicationBuilder(args);
_ = builder.Logging.ClearProviders();
_ = builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddSingleton<RunCommand>();
builder.Services.AddSingleton<ListCommand>();

builder.Services.AddArgh(args, argh =>
{
	_ = argh.UseCliDescription("Runs the docs browser journeys against a deployed site or a local build, and lists them.");
	_ = argh.Map<RunCommand>();
	_ = argh.Map<ListCommand>();
});

using var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
