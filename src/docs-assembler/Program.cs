// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using Actions.Core.Extensions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Assembler;
using Documentation.Assembler.Cli;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcNet;
using ProcNet.Std;

var services = new ServiceCollection();
services.AddGitHubActionsCore();
services.AddLogging(x =>
{
	x.ClearProviders();
	x.SetMinimumLevel(LogLevel.Information);
	x.AddSimpleConsole(c =>
	{
		c.SingleLine = true;
		c.IncludeScopes = true;
		c.UseUtcTimestamp = true;
		c.TimestampFormat = Environment.UserInteractive ? ":: " : "[yyyy-MM-ddTHH:mm:ss] ";
	});
});
services.AddSingleton<DiagnosticsChannel>();
services.AddSingleton<DiagnosticsCollector>();

await using var serviceProvider = services.BuildServiceProvider();
ConsoleApp.ServiceProvider = serviceProvider;

var app = ConsoleApp.Create();
app.UseFilter<StopwatchFilter>();
app.UseFilter<CatchExceptionFilter>();

app.Add<LinkCommands>("link");
app.Add<RepositoryCommands>("repo");

var githubActions = ConsoleApp.ServiceProvider.GetService<ICoreService>();
var command = githubActions?.GetInput("COMMAND");
if (!string.IsNullOrEmpty(command))
	args = command.Split(' ');

await app.RunAsync(args);
