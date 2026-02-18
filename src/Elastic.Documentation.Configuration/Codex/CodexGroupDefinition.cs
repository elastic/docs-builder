// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Codex;

/// <summary>
/// Represents a predefined group in a codex configuration.
/// Groups aggregate documentation sets and are displayed as cards on the landing page.
/// </summary>
public record CodexGroupDefinition
{
	/// <summary>
	/// The unique identifier for the group. Used in URLs (/g/{id}) and referenced by documentation sets.
	/// </summary>
	[YamlMember(Alias = "id")]
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// The display name shown on the codex landing page card.
	/// </summary>
	[YamlMember(Alias = "name")]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Optional short description shown on the codex landing page card.
	/// </summary>
	[YamlMember(Alias = "description")]
	public string? Description { get; set; }

	/// <summary>
	/// Optional icon identifier for the group card.
	/// Can be a predefined icon name (e.g., "elasticsearch", "kibana", "documentation").
	/// </summary>
	[YamlMember(Alias = "icon")]
	public string? Icon { get; set; }
}
