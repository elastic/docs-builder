// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Documentation.Migrate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

using var provider = services.BuildServiceProvider();

ConsoleApp.ServiceProvider = provider;
var app = ConsoleApp.Create();
app.Add<MigrateCommand>();
await app.RunAsync(args).ConfigureAwait(false);
