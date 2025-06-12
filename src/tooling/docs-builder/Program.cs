// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Documentation.Builder.Cli;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling;
using Microsoft.Extensions.DependencyInjection;

await using var serviceProvider = DocumentationTooling.CreateServiceProvider(ref args, services => services
	.AddSingleton<DiagnosticsChannel>()
	.AddSingleton<DiagnosticsCollector>()
);
ConsoleApp.ServiceProvider = serviceProvider;

var app = ConsoleApp.Create();
app.Add<Commands>();
app.Add<InboundLinkCommands>("inbound-links");
app.Add<DiffCommands>("diff");

await app.RunAsync(args).ConfigureAwait(false);
