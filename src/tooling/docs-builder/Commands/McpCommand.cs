// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links.InboundLinks;
using Elastic.Documentation.Assembler.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

/// <summary>Starts an MCP server over stdio for AI assistant integration.</summary>
internal sealed class McpCommand
{
	/// <summary>
	/// Start an MCP (Model Context Protocol) server over stdio.
	/// The server exposes documentation cross-link tools for AI assistants.
	/// </summary>
	[Command("")]
	public async Task Mcp(Cancel ctx = default)
	{
		var builder = Host.CreateApplicationBuilder();
		_ = builder.Logging.AddConsole(options =>
			options.LogToStandardErrorThreshold = LogLevel.Trace);

		_ = builder.Services.AddSingleton<ILinkIndexReader>(_ => Aws3LinkIndexReader.CreateAnonymous());
		_ = builder.Services.AddSingleton<LinksIndexCrossLinkFetcher>();
		_ = builder.Services.AddSingleton<ILinkUtilService, LinkUtilService>();

		_ = builder.Services
			.AddMcpServer()
			.WithStdioServerTransport()
			.WithTools<LinkTools>();

		await builder.Build().RunAsync(ctx);
	}
}
