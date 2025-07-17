// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using ConsoleAppFramework;
using Documentation.Builder.Cli;
using Elastic.Documentation.Tooling;
using Elastic.Documentation.Tooling.Filters;
using Microsoft.Extensions.Logging;

await using var serviceProvider = DocumentationTooling.CreateServiceProvider(ref args, (s, p) => { });
ConsoleApp.ServiceProvider = serviceProvider;

var app = ConsoleApp.Create();

app.UseFilter<ReplaceLogFilter>();
app.UseFilter<StopwatchFilter>();
app.UseFilter<CatchExceptionFilter>();
app.UseFilter<CheckForUpdatesFilter>();

app.Add<Commands>();
app.Add<InboundLinkCommands>("inbound-links");
app.Add<DiffCommands>("diff");

await app.RunAsync(args).ConfigureAwait(false);


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
