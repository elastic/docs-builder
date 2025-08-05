// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Assembler.Cli;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.ServiceDefaults;
using Elastic.Documentation.Tooling;
using Elastic.Documentation.Tooling.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder()
	.AddDocumentationServiceDefaults(ref args, (s, p) =>
	{
		_ = s.AddSingleton(AssemblyConfiguration.Create(p));
	})
	.AddDocumentationToolingDefaults();

var app = builder.ToConsoleAppBuilder();

app.UseFilter<ReplaceLogFilter>();
app.UseFilter<InfoLoggerFilter>();
app.UseFilter<StopwatchFilter>();
app.UseFilter<CatchExceptionFilter>();

app.Add<InboundLinkCommands>("inbound-links");
app.Add<RepositoryCommands>("repo");
app.Add<NavigationCommands>("navigation");
app.Add<ContentSourceCommands>("content-source");
app.Add<DeployCommands>("deploy");
app.Add<LegacyDocsCommands>("legacy-docs");

var githubActions = ConsoleApp.ServiceProvider!.GetService<ICoreService>();
var command = githubActions?.GetInput("COMMAND");
if (!string.IsNullOrEmpty(command))
	args = command.Split(' ');

await app.RunAsync(args);

internal sealed class ReplaceLogFilter(ConsoleAppFilter next, ILogger<Program> logger)
	: ConsoleAppFilter(next)
{
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	public override Task InvokeAsync(ConsoleAppContext context, Cancel cancellationToken)
	{
		ConsoleApp.Log = msg => logger.LogInformation(msg);
		ConsoleApp.LogError = msg => logger.LogError(msg);

		return Next.InvokeAsync(context, cancellationToken);
	}
}
