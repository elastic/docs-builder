// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Documentation.Builder.Commands;
using Documentation.Builder.Commands.Assembler;
using Documentation.Builder.Filters;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.ServiceDefaults;
using Elastic.Documentation.Tooling;
using Elastic.Documentation.Tooling.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder()
	.AddDocumentationServiceDefaults(ref args, (s, p) =>
	{
		_ = s.AddSingleton(AssemblyConfiguration.Create(p));
	})
	.AddDocumentationToolingDefaults();

var app = builder.ToConsoleAppBuilder((s) =>
{
	ConsoleApp.ServiceProvider = s;
	return ConsoleApp.Create();
});


app.UseFilter<ReplaceLogFilter>();
app.UseFilter<InfoLoggerFilter>();
app.UseFilter<StopwatchFilter>();
app.UseFilter<CatchExceptionFilter>();
app.UseFilter<CheckForUpdatesFilter>();

app.Add<IsolatedBuildCommand>();
app.Add<InboundLinkCommands>("inbound-links");
app.Add<DiffCommands>("diff");
app.Add<MoveCommand>("mv");
app.Add<ServeCommand>("serve");
app.Add<IndexCommand>("index");

//assembler commands

app.Add<ContentSourceCommands>("assembler content-source");
app.Add<DeployCommands>("assembler deploy");
app.Add<BloomFilterCommands>("assembler bloom-filter");
app.Add<NavigationCommands>("assembler navigation");
app.Add<ConfigurationCommands>("assembler config");
app.Add<AssemblerIndexCommand>("assembler index");
app.Add<AssemblerCommands>("assembler");
app.Add<AssembleCommands>("assemble");

await app.RunAsync(args).ConfigureAwait(false);
