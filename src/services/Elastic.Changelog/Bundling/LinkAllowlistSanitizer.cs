// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Rewrites PR/issue references that are not in <c>bundle.link_allow_repos</c> to <c># PRIVATE:</c> sentinels for bundle YAML output.
/// Also provides scrubbing for individual changelog entries and free-text fields.
/// </summary>
public static class LinkAllowlistSanitizer
{
	/// <summary>
	/// Applies the allowlist to PR/issue strings. References whose resolved <c>owner/repo</c> is not in
	/// <paramref name="allowRepos"/> are rewritten to <c># PRIVATE:</c> sentinels (warnings are emitted).
	/// Existing sentinels are normalized: if the underlying reference targets an allowed repo, the plain reference is restored.
	/// The rewrite is <see cref="LinkAllowlist.MarkPrivateLinks"/>; this reports its findings to the collector.
	/// </summary>
	public static bool TryApplyBundle(
		IDiagnosticsCollector collector,
		Bundle bundle,
		IReadOnlyList<string> allowRepos,
		string defaultOwner,
		string? defaultBundleRepo,
		out Bundle sanitized,
		out bool changesApplied
	)
	{
		sanitized = bundle;
		changesApplied = false;

		var result = LinkAllowlist.MarkPrivateLinks(bundle, allowRepos, defaultOwner, defaultBundleRepo);
		if (result.EmitTo(collector) is not { } marked)
			return false;

		sanitized = marked.Value;
		changesApplied = marked.Changed;
		return true;
	}

	/// <summary>
	/// When assembler configuration is available, emits warnings for allowlist entries that are missing from
	/// <c>assembler.yml</c> references or marked <c>private: true</c>.
	/// </summary>
	public static void EmitAssemblerDiagnostics(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> linkAllowRepos,
		AssemblyConfiguration? assembly
	)
	{
		if (assembly == null || linkAllowRepos.Count == 0)
			return;

		foreach (var entry in linkAllowRepos)
		{
			if (string.IsNullOrWhiteSpace(entry))
				continue;

			if (!TrySplitOwnerRepo(entry.Trim(), out var owner, out var repo))
				continue;

			if (!TryFindReferenceRepository(owner, repo, assembly, out var repository) || repository is null)
			{
				collector.EmitWarning(
					string.Empty,
					$"bundle.link_allow_repos entry '{entry}' is not listed in assembler.yml references (informational)."
				);
				continue;
			}

			if (repository.Private)
			{
				collector.EmitWarning(
					string.Empty,
					$"bundle.link_allow_repos entry '{entry}' is marked private in assembler.yml; verify that published links are intended."
				);
			}
		}
	}

	/// <summary>
	/// Builds a list of allowed <c>owner/repo</c> strings from an <see cref="AssemblyConfiguration"/>
	/// by collecting every reference repository that is not marked <c>private: true</c>.
	/// <c>skip: true</c> only means the repository does not publish docs — it says nothing about
	/// visibility, so public skip-repos (e.g. <c>elastic/roadmap</c>) remain linkable.
	/// Bare keys (without a slash) are assumed to be under the <c>elastic</c> organization.
	/// </summary>
	public static IReadOnlyList<string> BuildAllowReposFromAssembler(AssemblyConfiguration assembly)
	{
		var result = new List<string>();
		foreach (var kvp in assembly.ReferenceRepositories)
		{
			if (kvp.Value.Private)
				continue;

			var key = kvp.Key;
			if (!key.Contains('/'))
				key = $"elastic/{key}";

			result.Add(key);
		}

		return result;
	}

	/// <summary>
	/// Applies the allowlist to a single changelog entry.
	/// Scrubs <c>Prs</c>, <c>Issues</c>, <c>Description</c>, <c>Impact</c>, and <c>Action</c> fields.
	/// </summary>
	public static bool TryApplyChangelogEntry(
		IDiagnosticsCollector collector,
		BundledEntry entry,
		IReadOnlyList<string> allowRepos,
		string defaultOwner,
		string? defaultRepo,
		out BundledEntry sanitized,
		out bool changesApplied
	)
	{
		sanitized = entry;
		changesApplied = false;

		if (entry.WithoutPrivateLinks(allowRepos, defaultOwner, defaultRepo).EmitTo(collector) is not { } scrubbed)
			return false;

		sanitized = scrubbed.Value;
		changesApplied = scrubbed.Changed;
		return true;
	}

	/// <summary>
	/// Replaces GitHub references in free text that point to repositories not in
	/// <paramref name="allow"/>. Handles full URLs and <c>owner/repo#N</c> short forms.
	/// </summary>
	internal static string? ScrubText(string? input, HashSet<string> allow, ref bool changed) =>
		LinkAllowlist.ScrubText(input, allow, ref changed);

	/// <summary>
	/// Scrubs a bundle for public output by directly removing disallowed references.
	/// Unlike <see cref="TryApplyBundle"/> (which produces <c># PRIVATE:</c> sentinels for private-side tooling),
	/// this method never creates sentinels. Disallowed PR/issue entries are dropped and text fields are scrubbed.
	/// </summary>
	public static bool ScrubBundleForPublic(
		IDiagnosticsCollector collector,
		Bundle bundle,
		IReadOnlyList<string> allowRepos,
		string defaultOwner,
		string? defaultBundleRepo,
		out Bundle sanitized,
		out bool changesApplied
	)
	{
		sanitized = bundle;
		changesApplied = false;

		if (bundle.WithoutPrivateLinks(allowRepos, defaultOwner, defaultBundleRepo).EmitTo(collector) is not { } scrubbed)
			return false;

		sanitized = scrubbed.Value;
		changesApplied = scrubbed.Changed;
		return true;
	}

	/// <summary>
	/// Scans serialized YAML for GitHub references not in <paramref name="allowRepos"/>.
	/// Throws <see cref="InvalidOperationException"/> if any are found, providing defense-in-depth
	/// against new or renamed fields leaking private references.
	/// </summary>
	public static void ValidateNoPrivateReferences(string serializedYaml, IReadOnlyList<string> allowRepos) =>
		LinkAllowlist.ValidateNoPrivateReferences(serializedYaml, allowRepos);

	/// <summary>
	/// Overload that accepts a list of allowed repos (converts to the internal <see cref="HashSet{T}"/>).
	/// </summary>
	internal static string? ScrubText(string? input, IReadOnlyList<string> allowRepos, ref bool changed) =>
		LinkAllowlist.ScrubText(input, LinkAllowlist.BuildAllowSet(allowRepos), ref changed);

	private static bool TrySplitOwnerRepo(string entry, out string owner, out string repo) =>
		LinkAllowlist.TrySplitOwnerRepo(entry, out owner, out repo);

	private static bool TryFindReferenceRepository(string owner, string repo, AssemblyConfiguration assembly, out Repository? repository)
	{
		var fullName = $"{owner}/{repo}";
		var isElasticOwner = string.Equals(owner, "elastic", StringComparison.OrdinalIgnoreCase);

		foreach (var kvp in assembly.ReferenceRepositories)
		{
			if (
				string.Equals(kvp.Key, fullName, StringComparison.OrdinalIgnoreCase)
				|| (isElasticOwner && string.Equals(kvp.Key, repo, StringComparison.OrdinalIgnoreCase))
			)
			{
				repository = kvp.Value;
				return true;
			}
		}

		repository = null;
		return false;
	}
}
