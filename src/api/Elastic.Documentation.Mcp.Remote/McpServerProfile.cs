// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// MCP server profile that selects which feature modules are enabled and how the server
/// introduces itself. Module WhenToUse bullets use {docs} placeholders that are replaced
/// with the profile's DocsDescription at composition time.
/// </summary>
/// <param name="Name">Profile identifier (e.g. "public", "internal").</param>
/// <param name="ResourceNoun">Resource noun for tool names (e.g. "docs", "internal_docs"). Replaces {resource} in tool name templates.</param>
/// <param name="ScopePrefix">Scope prefix for tool names (e.g. "" for public, "internal_" for internal). Replaces {scope} in tool name templates.</param>
/// <param name="DocsDescription">Short noun phrase describing this profile's docs (e.g. "Elastic product documentation"). Used to replace {docs} in trigger templates.</param>
/// <param name="Introduction">Introduction template with a {capabilities} placeholder replaced at composition time.</param>
/// <param name="ExtraTriggers">Profile-specific trigger bullets appended after module triggers.</param>
/// <param name="Modules">Enabled feature modules.</param>
public sealed record McpServerProfile(
	string Name,
	string ResourceNoun,
	string ScopePrefix,
	string DocsDescription,
	string Introduction,
	string[] ExtraTriggers,
	McpFeatureModule[] Modules)
{
	public static McpServerProfile Public { get; } = new(
		"public",
		"docs",
		"",
		"Elastic documentation",
		"Use this server to {capabilities} Elastic product documentation published at elastic.co/docs.",
		["References Elastic product names such as Elasticsearch, Kibana, Fleet, APM, Logstash, Beats, Elastic Security, Elastic Observability, or Elastic Cloud."],
		[McpFeatureModules.Search, McpFeatureModules.Documents, McpFeatureModules.Coherence, McpFeatureModules.Links, McpFeatureModules.ContentTypes]
	);

	public static McpServerProfile Internal { get; } = new(
		"internal",
		"internal_docs",
		"internal_",
		"Elastic internal documentation",
		"Use this server to {capabilities} Elastic internal documentation: team processes, run books, architecture, and other internal knowledge.",
		["Asks about internal team processes, run books, architecture decisions, or operational knowledge."],
		[McpFeatureModules.Search, McpFeatureModules.Documents]
	);

	/// <summary>
	/// Resolves a profile by name. Throws if the name is unknown.
	/// </summary>
	public static McpServerProfile Resolve(string? name)
	{
		var key = string.IsNullOrWhiteSpace(name) ? "public" : name.Trim();
		return key.ToLowerInvariant() switch
		{
			"public" => Public,
			"internal" => Internal,
			_ => throw new ArgumentException($"Unknown MCP server profile: '{name}'. Valid values: public, internal.", nameof(name))
		};
	}

	/// <summary>
	/// Registers all DI services from enabled modules, including tool types for resolution at invocation time.
	/// </summary>
	public void RegisterAllServices(IServiceCollection services)
	{
		foreach (var module in Modules)
		{
			module.RegisterServices(services);
			if (module.ToolType is not null)
				_ = services.AddScoped(module.ToolType);
		}
	}

	/// <summary>
	/// Composes server instructions from the profile introduction and enabled module fragments.
	/// </summary>
	public string ComposeServerInstructions()
	{
		var capabilities = DeriveCapabilities();
		var introduction = Introduction.Replace("{capabilities}", capabilities, StringComparison.Ordinal);

		var whenToUse = Modules
			.SelectMany(m => m.WhenToUse)
			.Distinct()
			.Select(line => line.Replace("{docs}", DocsDescription, StringComparison.Ordinal))
			.Concat(ExtraTriggers)
			.ToList();
		var toolGuidance = Modules
			.SelectMany(m => m.ToolGuidance)
			.Select(line => ReplaceToolPlaceholders(line, ResourceNoun, ScopePrefix))
			.ToList();

		var whenToUseBlock = whenToUse.Count > 0
			? "\n" + string.Join("\n", whenToUse.Select(b => $"- {b}"))
			: "";
		var toolGuidanceBlock = toolGuidance.Count > 0
			? "\n<tool_guidance>\n" + string.Join("\n", toolGuidance.Select(l => $"- {l}")) + "\n</tool_guidance>"
			: "";

		return $"""
			{introduction}

			<triggers>
			Use the server when the user:{whenToUseBlock}
			</triggers>
			{toolGuidanceBlock}
			""";
	}

	private static string ReplaceToolPlaceholders(string line, string resourceNoun, string scopePrefix)
	{
		var sb = new StringBuilder(line.Length);
		var pos = 0;
		int start;
		while ((start = line.IndexOf("{tool:", pos, StringComparison.Ordinal)) >= 0)
		{
			var templateStart = start + 6;
			var depth = 1;
			var end = templateStart;
			while (end < line.Length && depth > 0)
			{
				if (line[end] == '{')
					depth++;
				else if (line[end] == '}')
					depth--;
				end++;
			}
			if (depth != 0)
				break;
			end--;
			var template = line[templateStart..end];
			var resolved = template
				.Replace("{resource}", resourceNoun, StringComparison.Ordinal)
				.Replace("{scope}", scopePrefix, StringComparison.Ordinal);
			_ = sb.Append(line, pos, start - pos);
			_ = sb.Append(resolved);
			pos = end + 1;
		}
		_ = sb.Append(line, pos, line.Length - pos);
		return sb.ToString();
	}

	private string DeriveCapabilities()
	{
		var verbs = Modules
			.Select(m => m.Capability)
			.Where(c => !string.IsNullOrEmpty(c))
			.Distinct()
			.ToList();

		return verbs.Count switch
		{
			0 => "search and retrieve",
			1 => verbs[0]!,
			2 => $"{verbs[0]} and {verbs[1]}",
			_ => string.Join(", ", verbs.Take(verbs.Count - 1)) + ", and " + verbs[^1]
		};
	}
}
