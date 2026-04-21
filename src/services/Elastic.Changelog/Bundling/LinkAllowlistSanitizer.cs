// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Rewrites PR/issue references that are not in <c>bundle.link_allow_repos</c> to <c># PRIVATE:</c> sentinels for bundle YAML output.
/// </summary>
public static class LinkAllowlistSanitizer
{
	private const string SentinelPrefix = "# PRIVATE:";

	/// <summary>
	/// Applies the allowlist to PR/issue strings. References whose resolved <c>owner/repo</c> is not in
	/// <paramref name="allowRepos"/> are rewritten to <c># PRIVATE:</c> sentinels (warnings are emitted).
	/// Existing sentinels are normalized: if the underlying reference targets an allowed repo, the plain reference is restored.
	/// </summary>
	public static bool TryApplyBundle(
		IDiagnosticsCollector collector,
		Bundle bundle,
		IReadOnlyList<string> allowRepos,
		string defaultOwner,
		string? defaultBundleRepo,
		out Bundle sanitized,
		out bool changesApplied)
	{
		sanitized = bundle;
		changesApplied = false;

		var allow = BuildAllowSet(allowRepos);
		var ownerDefault = string.IsNullOrWhiteSpace(defaultOwner) ? "elastic" : defaultOwner;
		var anyRewritten = false;
		var newEntries = new List<BundledEntry>(bundle.Entries.Count);

		foreach (var entry in bundle.Entries)
		{
			var prs = ApplyToReferenceList(
				collector,
				entry.Prs,
				ownerDefault,
				defaultBundleRepo,
				allow,
				"PR",
				ref anyRewritten);
			if (prs == null && entry.Prs is not null)
				return false;

			var issues = ApplyToReferenceList(
				collector,
				entry.Issues,
				ownerDefault,
				defaultBundleRepo,
				allow,
				"issue",
				ref anyRewritten);
			if (issues == null && entry.Issues is not null)
				return false;

			newEntries.Add(entry with { Prs = prs, Issues = issues });
		}

		sanitized = bundle with { Entries = newEntries };
		changesApplied = anyRewritten;
		return true;
	}

	/// <summary>
	/// When assembler configuration is available, emits warnings for allowlist entries that are missing from
	/// <c>assembler.yml</c> references or marked <c>private: true</c>.
	/// </summary>
	public static void EmitAssemblerDiagnostics(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> linkAllowRepos,
		AssemblyConfiguration? assembly)
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
					$"bundle.link_allow_repos entry '{entry}' is not listed in assembler.yml references (informational).");
				continue;
			}

			if (repository.Private)
			{
				collector.EmitWarning(
					string.Empty,
					$"bundle.link_allow_repos entry '{entry}' is marked private in assembler.yml; verify that published links are intended.");
			}
		}
	}

	private static HashSet<string> BuildAllowSet(IReadOnlyList<string> allowRepos)
	{
		var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var r in allowRepos)
		{
			if (string.IsNullOrWhiteSpace(r))
				continue;

			if (TrySplitOwnerRepo(r.Trim(), out var o, out var repo))
				_ = allow.Add($"{o}/{repo}");
		}

		return allow;
	}

	private static IReadOnlyList<string>? ApplyToReferenceList(
		IDiagnosticsCollector collector,
		IReadOnlyList<string>? refs,
		string defaultOwner,
		string? defaultBundleRepo,
		HashSet<string> allow,
		string referenceKind,
		ref bool anyRewritten)
	{
		if (refs is null)
			return null;

		if (refs.Count == 0)
			return refs;

		var list = new List<string>(refs.Count);
		foreach (var r in refs)
		{
			if (string.IsNullOrWhiteSpace(r))
			{
				list.Add(r);
				continue;
			}

			if (r.StartsWith(SentinelPrefix, StringComparison.OrdinalIgnoreCase))
			{
				var adjusted = ProcessSentinel(collector, r, defaultOwner, defaultBundleRepo, allow, referenceKind, ref anyRewritten);
				if (adjusted == null)
					return null;

				list.Add(adjusted);
				continue;
			}

			var plain = ProcessPlainReference(collector, r, defaultOwner, defaultBundleRepo, allow, referenceKind, ref anyRewritten);
			if (plain == null)
				return null;

			list.Add(plain);
		}

		return list;
	}

	private static string? ProcessSentinel(
		IDiagnosticsCollector collector,
		string sentinelRef,
		string defaultOwner,
		string? defaultBundleRepo,
		HashSet<string> allow,
		string referenceKind,
		ref bool anyRewritten)
	{
		var underlyingRef = sentinelRef.Substring(SentinelPrefix.Length).Trim();

		if (string.IsNullOrWhiteSpace(underlyingRef))
		{
			collector.EmitError(
				string.Empty,
				$"Invalid {referenceKind} sentinel '{sentinelRef}': no underlying reference found. " +
				"Sentinels must have the format '# PRIVATE: <reference>'.");
			return null;
		}

		if (!ChangelogTextUtilities.TryGetGitHubRepo(underlyingRef, defaultOwner, defaultBundleRepo ?? string.Empty, out var owner, out var repo))
		{
			collector.EmitError(
				string.Empty,
				$"Invalid {referenceKind} sentinel '{sentinelRef}': underlying reference '{underlyingRef}' could not be parsed. " +
				"Use a full https://github.com/ URL, owner/repo#number, or a bare number with bundle owner/repo set.");
			return null;
		}

		var fullName = $"{owner}/{repo}";
		if (allow.Contains(fullName))
		{
			if (!string.Equals(sentinelRef, underlyingRef, StringComparison.Ordinal))
				anyRewritten = true;

			return underlyingRef;
		}

		return sentinelRef;
	}

	private static string? ProcessPlainReference(
		IDiagnosticsCollector collector,
		string r,
		string defaultOwner,
		string? defaultBundleRepo,
		HashSet<string> allow,
		string referenceKind,
		ref bool anyRewritten)
	{
		if (!ChangelogTextUtilities.TryGetGitHubRepo(r, defaultOwner, defaultBundleRepo ?? string.Empty, out var owner, out var repo))
		{
			collector.EmitError(
				string.Empty,
				$"Link allowlist filtering could not parse {referenceKind} reference '{r}'. " +
				"Use a full https://github.com/ URL, owner/repo#number, or a bare number with bundle owner/repo set.");
			return null;
		}

		var fullName = $"{owner}/{repo}";
		if (allow.Contains(fullName))
			return r;

		anyRewritten = true;
		collector.EmitWarning(
			string.Empty,
			$"PR/issue reference '{r}' targets repository '{fullName}', which is not in bundle.link_allow_repos. " +
			"It was rewritten to a '# PRIVATE:' sentinel.");
		return $"{SentinelPrefix} {r}";
	}

	private static bool TrySplitOwnerRepo(string entry, out string owner, out string repo)
	{
		owner = string.Empty;
		repo = string.Empty;

		var slash = entry.IndexOf('/');
		if (slash <= 0 || slash >= entry.Length - 1)
			return false;

		if (entry.IndexOf('/', slash + 1) >= 0)
			return false;

		owner = entry[..slash];
		repo = entry[(slash + 1)..];
		return !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo);
	}

	private static bool TryFindReferenceRepository(
		string owner,
		string repo,
		AssemblyConfiguration assembly,
		out Repository? repository)
	{
		var fullName = $"{owner}/{repo}";
		var isElasticOwner = string.Equals(owner, "elastic", StringComparison.OrdinalIgnoreCase);

		foreach (var kvp in assembly.ReferenceRepositories)
		{
			if (string.Equals(kvp.Key, fullName, StringComparison.OrdinalIgnoreCase) ||
				(isElasticOwner && string.Equals(kvp.Key, repo, StringComparison.OrdinalIgnoreCase)))
			{
				repository = kvp.Value;
				return true;
			}
		}

		repository = null;
		return false;
	}
}
