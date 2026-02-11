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

namespace Documentation.Builder.Commands;

/// <summary>Boots the MCP server over stdio, bypassing ConsoleAppFramework to keep stdout clean for JSON-RPC.</summary>
public static class McpHost
{
	public static async Task RunAsync(string[] args)
	{
		var builder = Host.CreateApplicationBuilder(args);
		_ = builder.Logging.AddConsole(options =>
			options.LogToStandardErrorThreshold = LogLevel.Trace);

		_ = builder.Services.AddSingleton<ILinkIndexReader>(_ => Aws3LinkIndexReader.CreateAnonymous());
		_ = builder.Services.AddSingleton<LinksIndexCrossLinkFetcher>();
		_ = builder.Services.AddSingleton<ILinkUtilService, LinkUtilService>();

		_ = builder.Services
			.AddMcpServer()
			.WithStdioServerTransport()
			.WithTools<LinkTools>();

		await builder.Build().RunAsync();
	}
}
