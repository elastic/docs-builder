// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// MCP server profile that selects which feature modules are enabled and how the server
/// introduces itself. The introduction frames the server's purpose; module WhenToUse
/// bullets provide generic triggers that gain meaning from this context.
/// </summary>
/// <param name="Name">Profile identifier (e.g. "public", "internal").</param>
/// <param name="Introduction">Introduction template with a {capabilities} placeholder replaced at composition time.</param>
/// <param name="Modules">Enabled feature modules.</param>
public sealed record McpServerProfile(string Name, string Introduction, McpFeatureModule[] Modules)
{
	public static McpServerProfile Public { get; } = new(
		"public",
		"Use this server to {capabilities} Elastic product documentation published at elastic.co/docs.",
		[McpFeatureModules.Search, McpFeatureModules.Documents, McpFeatureModules.Coherence, McpFeatureModules.Links, McpFeatureModules.ContentTypes]
	);

	public static McpServerProfile Internal { get; } = new(
		"internal",
		"Use this server to {capabilities} Elastic Internal Docs: team processes, run books, architecture, and other internal knowledge.",
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
	/// Registers all DI services from enabled modules.
	/// </summary>
	public void RegisterAllServices(IServiceCollection services)
	{
		foreach (var module in Modules)
			module.RegisterServices(services);
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
			.ToList();
		var toolGuidance = Modules
			.SelectMany(m => m.ToolGuidance)
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
