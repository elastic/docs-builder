// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Portal;

/// <summary>
/// Represents a reference to a documentation set in a portal configuration.
/// </summary>
public record PortalDocumentationSetReference
{
	/// <summary>
	/// The name of the documentation set. This is used in the URL path.
	/// </summary>
	[YamlMember(Alias = "name")]
	public required string Name { get; init; }

	/// <summary>
	/// The git origin for the repository. Defaults to "elastic/{name}" if not specified.
	/// </summary>
	[YamlMember(Alias = "origin")]
	public string? Origin { get; init; }

	/// <summary>
	/// The git branch to checkout. Required.
	/// </summary>
	[YamlMember(Alias = "branch")]
	public required string Branch { get; init; }

	/// <summary>
	/// The path within the repository where documentation lives. Defaults to "docs".
	/// </summary>
	[YamlMember(Alias = "path")]
	public string Path { get; init; } = "docs";

	/// <summary>
	/// Optional category for grouping documentation sets. If specified, the URL will be
	/// /{site-prefix}/{category}/{name}/. If not specified, the URL will be /{site-prefix}/{name}/.
	/// </summary>
	[YamlMember(Alias = "category")]
	public string? Category { get; init; }

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
