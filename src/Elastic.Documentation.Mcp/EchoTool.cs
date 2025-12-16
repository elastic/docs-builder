// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Mcp;

[McpServerToolType]
public static class EchoTool
{
	[McpServerTool, Description("Echoes a greeting message back to the client.")]
	public static string Echo(string message) => $"Hello from docs-builder: {message}";
}

