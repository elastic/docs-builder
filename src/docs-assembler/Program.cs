// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Assembler.Cli;
using Elastic.Documentation.Tooling;
using Elastic.Documentation.Tooling.Filters;
using Elastic.Markdown.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

await using var serviceProvider = DocumentationTooling.CreateServiceProvider(ref args, services => services
	.AddSingleton<DiagnosticsChannel>()
	.AddSingleton<DiagnosticsCollector>()
);

ConsoleApp.ServiceProvider = serviceProvider;

var app = ConsoleApp.Create();
app.UseFilter<StopwatchFilter>();
app.UseFilter<CatchExceptionFilter>();

app.Add<InboundLinkCommands>("inbound-links");
app.Add<RepositoryCommands>("repo");

var githubActions = ConsoleApp.ServiceProvider.GetService<ICoreService>();
var command = githubActions?.GetInput("COMMAND") ?? Environment.GetEnvironmentVariable("INPUT_COMMAND");
if (!string.IsNullOrEmpty(command))
	args = command.Split(' ');

await app.RunAsync(args);
