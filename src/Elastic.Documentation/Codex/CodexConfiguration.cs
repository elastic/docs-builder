// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.Codex;

/// <summary>
/// A codex site from <c>codex.yml</c>: where it mounts, which environment it indexes into, and the groups its
/// documentation sets are listed under. Loaded once per codex command by <c>CodexConfigurationLoader</c>.
/// </summary>
[Equatable]
public sealed partial record CodexConfiguration
{
	/// <summary>The URL prefix the site mounts at, normalized to <c>/segment</c>; empty for the root.</summary>
	public string SitePrefix { get; init; } = "/";

	/// <summary>The environment, trimmed and lowercased to match the Elasticsearch index namespace; null when unset.</summary>
	public string? Environment { get; init; }

	public string Title { get; init; } = "Elastic Internal Docs";

	[OrderedEquality]
	public IReadOnlyList<CodexGroupDefinition> Groups { get; init; } = [];

	public string? CanonicalBaseUrl { get; init; }
}

/// <summary>A group of documentation sets on the codex landing page.</summary>
public sealed record CodexGroupDefinition
{
	public string Id { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string? Description { get; init; }
	public string? Icon { get; init; }
}

/// <summary>A documentation set a codex site builds: which repository and branch, and which group it lists under.</summary>
public sealed record CodexDocumentationSetReference
{
	public string Name { get; init; } = string.Empty;
	public string? Origin { get; init; }
	public string Branch { get; init; } = string.Empty;
	public string Path { get; init; } = "docs";
	public string? Group { get; init; }
	public string? RepoName { get; init; }

	[Obsolete("Use the index.md h1 heading instead. This field will be removed in a future version.")]
	public string? DisplayName { get; init; }

	[Obsolete("Use the index.md frontmatter description instead. This field will be removed in a future version.")]
	public string? Description { get; init; }

	public string? Icon { get; init; }

	public string ResolvedRepoName => RepoName ?? Name;

	public string ResolvedOrigin => Origin ?? $"elastic/{Name}";
}
