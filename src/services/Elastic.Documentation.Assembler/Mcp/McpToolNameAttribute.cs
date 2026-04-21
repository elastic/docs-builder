// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Assembler.Mcp;

/// <summary>
/// Specifies the tool name template for an MCP tool method. Use {resource} as a placeholder
/// replaced with the profile's ResourceNoun (e.g. "docs", "internal_docs").
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class McpToolNameAttribute(string template) : Attribute
{
	public string Template { get; } = template;
}
