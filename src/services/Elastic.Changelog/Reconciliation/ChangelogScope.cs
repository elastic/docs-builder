// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Reconciliation;

/// <summary>The two registry scope families in the changelog bucket key layout.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ChangelogScopeKind>))]
public enum ChangelogScopeKind
{
	/// <summary>A product bundle scope: <c>bundle/{product}/…</c>.</summary>
	Bundle,

	/// <summary>An authoring-pool scope: <c>changelog/{org}/{repo}/{branch}/…</c>.</summary>
	Changelog
}

/// <summary>
/// Identifies one registry scope in the changelog bundles bucket — a product bundle pool
/// (<c>bundle/{product}/</c>) or an authoring changelog pool
/// (<c>changelog/{org}/{repo}/{branch}/</c>) — and derives the scope's key prefix and
/// <c>registry.json</c> key. Segments are validated on construction via
/// <see cref="ChangelogKeys"/>, so a scope instance can always be composed into safe S3 keys.
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
	/// <c>{org}/{repo}/{branch}</c> prefix for a changelog scope.
	/// </summary>
	public string Group { get; }

	/// <summary>The S3 key prefix of every object in this scope, ending in <c>/</c>.</summary>
	public string Prefix => Kind == ChangelogScopeKind.Bundle
		? $"{ChangelogKeys.BundlePrefix}{Group}/"
		: $"{ChangelogKeys.ChangelogPrefix}{Group}/";

	/// <summary>The S3 key of this scope's <c>registry.json</c> manifest.</summary>
	public string RegistryKey => Kind == ChangelogScopeKind.Bundle
		? ChangelogKeys.BundleRegistryKey(Group)
		: ChangelogKeys.ChangelogRegistryKey(Group);

	/// <summary>Creates a bundle scope for <paramref name="product"/>; false when the segment is invalid.</summary>
	public static bool TryCreateBundle(string? product, [NotNullWhen(true)] out ChangelogScope? scope)
	{
		scope = ChangelogKeys.IsValidProduct(product)
			? new ChangelogScope(ChangelogScopeKind.Bundle, product)
			: null;
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

	/// <inheritdoc />
	public override string ToString() => Prefix.TrimEnd('/');
}
