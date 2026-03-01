// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Reflection;
using Elastic.Documentation.Assembler.Mcp;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// Creates MCP tools with profile-based names and descriptions.
/// </summary>
public static class McpToolRegistration
{
	/// <summary>
	/// Creates tools for all enabled modules in the profile.
	/// Uses createTargetFunc so tool instances are resolved from the request's service provider at invocation time.
	/// </summary>
	public static IEnumerable<McpServerTool> CreatePrefixedTools(McpServerProfile profile)
	{
		var resourceNoun = profile.ResourceNoun;
		var docsDescription = profile.DocsDescription;
		var tools = new List<McpServerTool>();

		foreach (var module in profile.Modules)
		{
			if (module.ToolType is null)
				continue;

			var methods = module.ToolType
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);

			foreach (var method in methods)
			{
				var nameAttr = method.GetCustomAttribute<McpToolNameAttribute>()
					?? throw new InvalidOperationException($"Method {method.DeclaringType?.Name}.{method.Name} must have [McpToolName] attribute.");
				var toolName = nameAttr.Template.Replace("{resource}", resourceNoun, StringComparison.Ordinal);

				var descAttr = method.GetCustomAttribute<DescriptionAttribute>();
				var description = descAttr?.Description?.Replace("{docs}", docsDescription, StringComparison.Ordinal);

				var options = new McpServerToolCreateOptions
				{
					Name = toolName,
					Description = description
				};

				var tool = McpServerTool.Create(
					method,
					ctx => (ctx.Services ?? throw new InvalidOperationException("RequestContext.Services is null")).GetRequiredService(module.ToolType),
					options);

				tools.Add(tool);
			}
		}

		return tools;
	}
}
