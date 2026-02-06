// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links.InboundLinks;
using Elastic.Documentation.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options =>
	options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<ILinkIndexReader>(_ => Aws3LinkIndexReader.CreateAnonymous());
builder.Services.AddSingleton<LinksIndexCrossLinkFetcher>();
builder.Services.AddSingleton<ILinkUtilService, LinkUtilService>();

builder.Services.AddHttpClient<ContentTypeProvider>();

builder.Services
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithToolsFromAssembly();

await builder.Build().RunAsync();
