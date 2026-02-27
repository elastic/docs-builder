// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Runtime.CompilerServices;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// Creates MCP tools with profile-based name prefixes.
/// </summary>
public static class McpToolRegistration
{
	/// <summary>
	/// Creates prefixed tools for all enabled modules in the profile.
	/// Uses createTargetFunc so tool instances are resolved from the request's service provider at invocation time.
	/// </summary>
	public static IEnumerable<McpServerTool> CreatePrefixedTools(McpServerProfile profile)
	{
		var prefix = profile.ToolNamePrefix;
		var tools = new List<McpServerTool>();

		foreach (var module in profile.Modules)
		{
			if (module.ToolType is not { } toolType)
				continue;

			var methods = toolType
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);

			foreach (var method in methods)
			{
				var snakeName = ToSnakeCase(method.Name);
				var prefixedName = prefix + snakeName;

				var options = new McpServerToolCreateOptions { Name = prefixedName };

				var tool = McpServerTool.Create(
					method,
					ctx => (ctx.Services ?? throw new InvalidOperationException("RequestContext.Services is null")).GetRequiredService(toolType),
					options);

				tools.Add(tool);
			}
		}

		return tools;
	}

	/// <summary>
	/// Converts PascalCase to snake_case (e.g. SemanticSearch → semantic_search).
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToSnakeCase(string value)
	{
		if (string.IsNullOrEmpty(value))
			return value;

		return string.Concat(value.Select((c, i) =>
			i > 0 && char.IsUpper(c)
				? "_" + char.ToLowerInvariant(c).ToString()
				: char.IsUpper(c)
					? char.ToLowerInvariant(c).ToString()
					: c.ToString()));
	}
}
