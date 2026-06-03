// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ModelContextProtocol.Protocol;

namespace Elastic.Documentation.Mcp.Remote.Responses;

/// <summary>
/// Factory helpers for building <see cref="CallToolResult"/> values consistently across all tools.
/// </summary>
internal static class McpToolResults
{
	/// <summary>
	/// Wraps a success payload (already serialized to JSON) in a <see cref="CallToolResult"/>.
	/// Equivalent to returning a plain <c>string</c> from a tool, but typed explicitly.
	/// </summary>
	public static CallToolResult Ok(string json) =>
		new() { Content = [new TextContentBlock { Text = json }] };

	/// <summary>
	/// Returns a <see cref="CallToolResult"/> with <see cref="CallToolResult.IsError"/> set so
	/// the MCP client and model know the tool call failed. HTTP response remains 200 per
	/// MCP/JSON-RPC spec.
	/// </summary>
	public static CallToolResult Error(string json) =>
		new() { IsError = true, Content = [new TextContentBlock { Text = json }] };
}
