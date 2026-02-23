// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Codex;

/// <summary>
/// Represents a reference to a documentation set in a codex configuration.
/// </summary>
public record CodexDocumentationSetReference
{
	/// <summary>
	/// The name of the documentation set. This is used in the URL path.
	/// </summary>
	[YamlMember(Alias = "name")]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// The git origin for the repository. Defaults to "elastic/{name}" if not specified.
	/// </summary>
	[YamlMember(Alias = "origin")]
	public string? Origin { get; set; }

	/// <summary>
	/// The git branch to checkout. Required.
	/// </summary>
	[YamlMember(Alias = "branch")]
	public string Branch { get; set; } = string.Empty;

	/// <summary>
	/// The path within the repository where documentation lives. Defaults to "docs".
	/// </summary>
	[YamlMember(Alias = "path")]
	public string Path { get; set; } = "docs";

	/// <summary>
	/// Optional group id for grouping documentation sets. References a group defined in the groups section.
	/// If specified, the docset appears under /g/{group}/. If not specified, the docset appears at the codex root.
	/// </summary>
	[YamlMember(Alias = "group")]
	public string? Group { get; set; }

	/// <summary>
	/// Optional override for the repository name used in checkout directories and URL paths.
	/// If not specified, defaults to <see cref="Name"/>. This allows including the same
	/// repository multiple times with different URL paths.
	/// </summary>
	[YamlMember(Alias = "repo_name")]
	public string? RepoName { get; set; }

	/// <summary>
	/// Optional display name shown on the codex landing page.
	/// Deprecated: Use the index.md h1 heading instead.
	/// </summary>
	[Obsolete("Use the index.md h1 heading instead. This field will be removed in a future version.")]
	[YamlMember(Alias = "display_name")]
	public string? DisplayName { get; set; }

	/// <summary>
	/// Optional short description shown on the codex landing page card.
	/// </summary>
	[YamlMember(Alias = "description")]
	public string? Description { get; set; }

	/// <summary>
	/// Optional icon identifier for the documentation set card.
	/// Can be a predefined icon name (e.g., "elasticsearch", "kibana", "observability")
	/// or a custom SVG path.
	/// </summary>
	[YamlMember(Alias = "icon")]
	public string? Icon { get; set; }

	/// <summary>
	/// Gets the resolved repository name, defaulting to <see cref="Name"/> if not explicitly set.
	/// This is used for checkout directories and URL paths.
	/// </summary>
	[YamlIgnore]
	public string ResolvedRepoName => RepoName ?? Name;

	/// <summary>
	/// Gets the resolved origin, defaulting to "elastic/{name}" if not explicitly set.
	/// </summary>
	public string ResolvedOrigin => Origin ?? $"elastic/{Name}";

	/// <summary>
	/// Gets the full git URL for cloning, handling GitHub token authentication if available.
	/// </summary>
	public string GetGitUrl()
	{
		var origin = ResolvedOrigin;

		// If origin is already a full URL, return it as-is
		if (origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
			origin.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
			return origin;

		// Otherwise, construct the URL from the short form (e.g., "elastic/repo-name")
		if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
		{
			var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
			return !string.IsNullOrEmpty(token)
				? $"https://oauth2:{token}@github.com/{origin}.git"
				: $"https://github.com/{origin}.git";
		}

		return $"git@github.com:{origin}.git";
	}
}
