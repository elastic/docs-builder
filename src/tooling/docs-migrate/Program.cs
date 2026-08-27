// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Migrate;
using Microsoft.Extensions.Hosting;
using Nullean.Argh.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddArgh(
	args,
	app =>
	{
		_ = app.UseCliDescription("docs-migrate — convert Elastic legacy AsciiDoc books to docs-builder Markdown.");
		_ = app.Map<InitCommand>();
		_ = app.Map<ListCommand>();
		_ = app.Map<CloneCommand>();
		_ = app.Map<ConvertCommand>();
		_ = app.Map<ServeCommand>();
	}
);

using var host = builder.Build();
await host.RunAsync();
