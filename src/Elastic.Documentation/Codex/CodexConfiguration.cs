// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.Codex;

/// <summary>
/// A codex site from <c>codex.yml</c>: where it mounts, which environment it indexes into, and the groups its
/// documentation sets are listed under. Loaded once per codex command by <c>CodexConfigurationLoader</c>.
/// </summary>
/// <remarks>
/// Constructed with <c>CodexSiteLoader.Load</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record CodexConfiguration
{
	/// <summary>The URL prefix the site mounts at, normalized to <c>/segment</c>; empty for the root.</summary>
	public string SitePrefix { get; internal init; } = "/";

	/// <summary>The environment, trimmed and lowercased to match the Elasticsearch index namespace; null when unset.</summary>
	public string? Environment { get; internal init; }

	public string Title { get; internal init; } = "Elastic Internal Docs";

	[OrderedEquality]
	public IReadOnlyList<CodexGroupDefinition> Groups { get; internal init; } = [];

	public string? CanonicalBaseUrl { get; internal init; }
}

/// <summary>A group of documentation sets on the codex landing page.</summary>
/// <remarks>
/// Constructed through <c>CodexConfiguration</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
public sealed record CodexGroupDefinition
{
	public string Id { get; internal init; } = string.Empty;
	public string Name { get; internal init; } = string.Empty;
	public string? Description { get; internal init; }
	public string? Icon { get; internal init; }
}

/// <summary>A documentation set a codex site builds: which repository and branch, and which group it lists under.</summary>
/// <remarks>
/// Constructed through <c>CodexConfiguration</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
public sealed record CodexDocumentationSetReference
{
	public string Name { get; internal init; } = string.Empty;
	public string? Origin { get; internal init; }
	public string Branch { get; internal init; } = string.Empty;
	public string Path { get; internal init; } = "docs";
	public string? Group { get; internal init; }
	public string? RepoName { get; internal init; }

	[Obsolete("Use the index.md h1 heading instead. This field will be removed in a future version.")]
	public string? DisplayName { get; internal init; }

	[Obsolete("Use the index.md frontmatter description instead. This field will be removed in a future version.")]
	public string? Description { get; internal init; }

	public string? Icon { get; internal init; }

	/// <summary>The reference to an <c>elastic/{repoName}</c> repository's documentation set at a branch, as the codex clone discovers it from the link registry.</summary>
	public static CodexDocumentationSetReference ForRepository(
		string repoName,
		string branch,
		string docsPath,
		string? group,
		string? icon
	) =>
		new()
		{
			Name = repoName,
			Origin = $"elastic/{repoName}",
			Branch = branch,
			Path = docsPath,
			Group = group,
			Icon = icon
		};

	public string ResolvedRepoName => RepoName ?? Name;

	public string ResolvedOrigin => Origin ?? $"elastic/{Name}";
}
