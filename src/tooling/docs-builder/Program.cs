// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Builder;
using Documentation.Builder.Commands;
using Documentation.Builder.Commands.Assembler;
using Documentation.Builder.Commands.Codex;
using Documentation.Builder.Middleware;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nullean.Argh;
using Nullean.Argh.Hosting;

// Pre-host fast path: run --help, --version, __schema, __completion directly and exit
// before the host (and its startup logs) are ever constructed.
await ArghApp.TryArghIntrinsicCommand(args);

var builder = Host.CreateApplicationBuilder()
	.AddDocumentationServiceDefaults(args, (s, p) =>
	{
		_ = s.AddSingleton(AssemblyConfiguration.Create(p));
	})
	.AddDocumentationToolingDefaults()
	.AddOpenTelemetryDefaults();

_ = builder.Services.AddArgh(args, app =>
{
	_ = app.UseGlobalOptions<GlobalCliOptions>();

	_ = app.UseMiddleware<InfoLoggerMiddleware>();
	_ = app.UseMiddleware<StopwatchMiddleware>();
	_ = app.UseMiddleware<CatchExceptionMiddleware>();
	_ = app.UseMiddleware<CheckForUpdatesMiddleware>();

	// `docs-builder build` as a named command AND root default (`docs-builder` with no sub-command).
	_ = app.MapAndRootAlias<IsolatedBuildCommand>();

	_ = app.Map<DiffCommand>();
	_ = app.Map<RefactorCommands>();
	_ = app.Map<ServeCommand>();
	_ = app.Map<IndexCommand>();
	_ = app.MapNamespace<ChangelogCommands>("changelog");
	_ = app.MapNamespace<InboundLinkCommands>("inbound-links");

	_ = app.Map<AssembleOneShotCommand>();

	// assembler commands (assemble merged into assembler default)
	_ = app.MapNamespace<AssemblerCommands>("assembler", g =>
	{
		_ = g.MapNamespace<ContentSourceCommands>("content-source");
		_ = g.MapNamespace<DeployCommands>("deploy");
		_ = g.MapNamespace<BloomFilterCommands>("bloom-filter");
		_ = g.MapNamespace<NavigationCommands>("navigation");
		_ = g.MapNamespace<ConfigurationCommand>("config");
		_ = g.Map<AssemblerIndexCommand>();
		_ = g.Map<AssemblerSitemapCommand>();
	});

	// codex commands
	_ = app.MapNamespace<CodexCommands>("codex", g =>
	{
		_ = g.Map<CodexIndexCommand>();
		_ = g.Map<CodexUpdateRedirectsCommand>();
	});
});

using var host = builder.Build();
await host.RunAsync();
