// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
﻿// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// ReSharper disable RedundantLambdaParameterType

using Actions.Core.Extensions;
using ConsoleAppFramework;
using Documentation.Generator.Cli;
using Documentation.Generator.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


var arguments = Arguments.Filter(args);

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

await using var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
ConsoleApp.ServiceProvider = serviceProvider;
if (!arguments.IsHelp)
	ConsoleApp.Log = msg => logger.LogInformation(msg);
ConsoleApp.LogError = msg => logger.LogError(msg);

var app = ConsoleApp.Create();
app.Add<Commands>();

await app.RunAsync(arguments.Args).ConfigureAwait(false);
