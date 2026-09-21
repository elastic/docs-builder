// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Reconciliation;

/// <summary>The registry scope families in the changelog bucket key layout.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ChangelogScopeKind>))]
public enum ChangelogScopeKind
{
	/// <summary>A product bundle scope: <c>bundle/{product}/…</c>.</summary>
	Bundle,

	/// <summary>An authoring-pool scope: <c>changelog/{org}/{repo}/{branch}/…</c>.</summary>
	Changelog,

	/// <summary>A repo-level notes scope: <c>changelog/{org}/{repo}/</c> (branch-agnostic).</summary>
	Notes
}

/// <summary>
/// Identifies one registry scope in the changelog bundles bucket — a product bundle pool
/// (<c>bundle/{product}/</c>), an authoring changelog pool
/// (<c>changelog/{org}/{repo}/{branch}/</c>), or a repo-level notes scope
/// (<c>changelog/{org}/{repo}/</c>) — and derives the scope's key prefix. Segments are
/// validated on construction via <see cref="ChangelogKeys"/>, so a scope instance can
/// always be composed into safe S3 keys.
/// </summary>
public sealed record ChangelogScope
{
	private ChangelogScope(ChangelogScopeKind kind, string group)
	{
		Kind = kind;
		Group = group;
	}

	/// <summary>Which scope family this is.</summary>
	public ChangelogScopeKind Kind { get; }

	/// <summary>
	/// The grouping segment(s): the product for a bundle scope, the
	/// <c>{org}/{repo}/{branch}</c> prefix for a changelog scope, or
	/// <c>{org}/{repo}</c> for a notes scope.
	/// </summary>
	public string Group { get; }

	/// <summary>The S3 key prefix of every object in this scope, ending in <c>/</c>.</summary>
	public string Prefix => Kind switch
	{
		ChangelogScopeKind.Bundle => $"{ChangelogKeys.BundlePrefix}{Group}/",
		ChangelogScopeKind.Notes => $"{ChangelogKeys.ChangelogPrefix}{Group}/",
		_ => $"{ChangelogKeys.ChangelogPrefix}{Group}/"
	};

	/// <summary>The S3 key of this scope's <c>registry.json</c> manifest (bundle and changelog scopes only).</summary>
	public string RegistryKey =>
		Kind == ChangelogScopeKind.Bundle ? ChangelogKeys.BundleRegistryKey(Group) : ChangelogKeys.ChangelogRegistryKey(Group);

	/// <summary>Creates a bundle scope for <paramref name="product"/>; false when the segment is invalid.</summary>
	public static bool TryCreateBundle(string? product, [NotNullWhen(true)] out ChangelogScope? scope)
	{
		scope = ChangelogKeys.IsValidProduct(product) ? new ChangelogScope(ChangelogScopeKind.Bundle, product) : null;
		return scope is not null;
	}

	/// <summary>Creates a changelog-pool scope for <paramref name="org"/>/<paramref name="repo"/>/<paramref name="branch"/>; false when any segment is invalid.</summary>
	public static bool TryCreateChangelog(string? org, string? repo, string? branch, [NotNullWhen(true)] out ChangelogScope? scope)
	{
		scope = ChangelogKeys.IsValidOrg(org) && ChangelogKeys.IsValidRepo(repo) && ChangelogKeys.IsValidBranch(branch)
			? new ChangelogScope(ChangelogScopeKind.Changelog, $"{org}/{repo}/{branch}")
			: null;
		return scope is not null;
	}

	/// <summary>Creates a notes scope for <paramref name="org"/>/<paramref name="repo"/>; false when any segment is invalid.</summary>
	public static bool TryCreateNotes(string? org, string? repo, [NotNullWhen(true)] out ChangelogScope? scope)
	{
		scope = ChangelogKeys.IsValidOrg(org) && ChangelogKeys.IsValidRepo(repo)
			? new ChangelogScope(ChangelogScopeKind.Notes, $"{org}/{repo}")
			: null;
		return scope is not null;
	}

	/// <summary>
	/// Derives the scope an object key belongs to — <c>bundle/{product}/{file}</c>,
	/// <c>changelog/{org}/{repo}/{branch}/{file}</c>, or <c>changelog/{org}/{repo}/notes-*.json</c>.
	/// False when the key sits outside all layouts or a segment fails validation.
	/// </summary>
	public static bool TryFromKey(string key, [NotNullWhen(true)] out ChangelogScope? scope)
	{
		scope = null;
		if (ChangelogKeys.ExtractBundleGroup(key) is { } product)
			scope = new ChangelogScope(ChangelogScopeKind.Bundle, product);
		else if (ChangelogKeys.ExtractNotesRepo(key) is { } repo)
			scope = new ChangelogScope(ChangelogScopeKind.Notes, repo);
		else if (ChangelogKeys.ExtractChangelogGroup(key) is { } pool)
			scope = new ChangelogScope(ChangelogScopeKind.Changelog, pool);
		return scope is not null;
	}

	/// <inheritdoc />
	public override string ToString() => Prefix.TrimEnd('/');
}
