// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Rewrites PR/issue references that are not in <c>bundle.link_allow_repos</c> to <c># PRIVATE:</c> sentinels for bundle YAML output.
/// Also provides scrubbing for individual changelog entries and free-text fields.
/// </summary>
public static partial class LinkAllowlistSanitizer
{
	private const string SentinelPrefix = "# PRIVATE:";
	[GeneratedRegex(@"https?://github\.com/(?<owner>[A-Za-z0-9_.-]+)/(?<repo>[A-Za-z0-9_.-]+)/(?:pull|issues)/\d+", RegexOptions.None)]
	private static partial Regex GitHubUrlRegex();

	[GeneratedRegex(@"(?<![/\w])(?<owner>[A-Za-z0-9_.-]+)/(?<repo>[A-Za-z0-9_.-]+)#\d+", RegexOptions.None)]
	private static partial Regex ShortFormRefRegex();

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

	/// <summary>
	/// Builds a list of allowed <c>owner/repo</c> strings from an <see cref="AssemblyConfiguration"/>
	/// by collecting every reference repository that is not marked <c>private: true</c> or <c>skip: true</c>.
	/// Bare keys (without a slash) are assumed to be under the <c>elastic</c> organization.
	/// </summary>
	public static IReadOnlyList<string> BuildAllowReposFromAssembler(AssemblyConfiguration assembly)
	{
		var result = new List<string>();
		foreach (var kvp in assembly.ReferenceRepositories)
		{
			if (kvp.Value.Private || kvp.Value.Skip)
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
		out bool changesApplied)
	{
		sanitized = entry;
		changesApplied = false;

		var allow = BuildAllowSet(allowRepos);
		var ownerDefault = string.IsNullOrWhiteSpace(defaultOwner) ? "elastic" : defaultOwner;
		var anyRewritten = false;

		var prs = FilterReferenceList(
			collector,
			entry.Prs,
			ownerDefault,
			defaultRepo,
			allow,
			"PR",
			ref anyRewritten);
		if (prs == null && entry.Prs is not null)
			return false;

		var issues = FilterReferenceList(
			collector,
			entry.Issues,
			ownerDefault,
			defaultRepo,
			allow,
			"issue",
			ref anyRewritten);
		if (issues == null && entry.Issues is not null)
			return false;

		var description = ScrubText(entry.Description, allow, ref anyRewritten);
		var impact = ScrubText(entry.Impact, allow, ref anyRewritten);
		var action = ScrubText(entry.Action, allow, ref anyRewritten);

		sanitized = entry with
		{
			Prs = prs,
			Issues = issues,
			Description = description,
			Impact = impact,
			Action = action
		};
		changesApplied = anyRewritten;
		return true;
	}

	/// <summary>
	/// Replaces GitHub references in free text that point to repositories not in
	/// <paramref name="allow"/>. Handles full URLs and <c>owner/repo#N</c> short forms.
	/// </summary>
	internal static string? ScrubText(string? input, HashSet<string> allow, ref bool changed)
	{
		if (string.IsNullOrWhiteSpace(input))
			return input;

		var anyReplaced = false;

		var result = GitHubUrlRegex().Replace(input, match =>
		{
			var owner = match.Groups["owner"].Value;
			var repo = match.Groups["repo"].Value;
			var fullName = $"{owner}/{repo}";
			if (allow.Contains(fullName))
				return match.Value;

			anyReplaced = true;
			return string.Empty;
		});

		result = ShortFormRefRegex().Replace(result, match =>
		{
			var owner = match.Groups["owner"].Value;
			var repo = match.Groups["repo"].Value;
			var fullName = $"{owner}/{repo}";
			if (allow.Contains(fullName))
				return match.Value;

			anyReplaced = true;
			return string.Empty;
		});

		if (anyReplaced)
			changed = true;

		return result;
	}

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
		out bool changesApplied)
	{
		sanitized = bundle;
		changesApplied = false;

		var anyChanged = false;
		var newEntries = new List<BundledEntry>(bundle.Entries.Count);

		foreach (var entry in bundle.Entries)
		{
			if (!TryApplyChangelogEntry(collector, entry, allowRepos, defaultOwner, defaultBundleRepo,
				out var scrubbed, out var entryChanged))
				return false;

			if (entryChanged)
				anyChanged = true;

			newEntries.Add(scrubbed);
		}

		var allow = BuildAllowSet(allowRepos);
		var description = ScrubText(bundle.Description, allow, ref anyChanged);

		sanitized = bundle with { Entries = newEntries, Description = description };
		changesApplied = anyChanged;
		return true;
	}

	/// <summary>
	/// Scans serialized YAML for GitHub references not in <paramref name="allowRepos"/>.
	/// Throws <see cref="InvalidOperationException"/> if any are found, providing defense-in-depth
	/// against new or renamed fields leaking private references.
	/// </summary>
	public static void ValidateNoPrivateReferences(string serializedYaml, IReadOnlyList<string> allowRepos)
	{
		var allow = BuildAllowSet(allowRepos);
		var violations = new List<string>();

		foreach (var match in GitHubUrlRegex().EnumerateMatches(serializedYaml))
		{
			var text = serializedYaml.Substring(match.Index, match.Length);
			var m = GitHubUrlRegex().Match(text);
			var fullName = $"{m.Groups["owner"].Value}/{m.Groups["repo"].Value}";
			if (!allow.Contains(fullName))
				violations.Add(text);
		}

		foreach (var match in ShortFormRefRegex().EnumerateMatches(serializedYaml))
		{
			var text = serializedYaml.Substring(match.Index, match.Length);
			var m = ShortFormRefRegex().Match(text);
			var fullName = $"{m.Groups["owner"].Value}/{m.Groups["repo"].Value}";
			if (!allow.Contains(fullName))
				violations.Add(text);
		}

		if (serializedYaml.Contains(SentinelPrefix, StringComparison.OrdinalIgnoreCase))
			violations.Add("Residual # PRIVATE: sentinel found");

		if (violations.Count > 0)
			throw new InvalidOperationException(
				$"Post-serialize validation failed: {violations.Count} private reference(s) found in public output: {string.Join(", ", violations)}");
	}

	/// <summary>
	/// Overload that accepts a list of allowed repos (converts to the internal <see cref="HashSet{T}"/>).
	/// </summary>
	internal static string? ScrubText(string? input, IReadOnlyList<string> allowRepos, ref bool changed)
	{
		var allow = BuildAllowSet(allowRepos);
		return ScrubText(input, allow, ref changed);
	}

	private static IReadOnlyList<string>? FilterReferenceList(
		IDiagnosticsCollector collector,
		IReadOnlyList<string>? refs,
		string defaultOwner,
		string? defaultBundleRepo,
		HashSet<string> allow,
		string referenceKind,
		ref bool anyDropped)
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
				var underlyingRef = r.Substring(SentinelPrefix.Length).Trim();
				if (string.IsNullOrWhiteSpace(underlyingRef))
					continue;

				if (!ChangelogTextUtilities.TryGetGitHubRepo(underlyingRef, defaultOwner, defaultBundleRepo ?? string.Empty, out var sOwner, out var sRepo))
					continue;

				if (allow.Contains($"{sOwner}/{sRepo}"))
				{
					list.Add(underlyingRef);
					anyDropped = true;
				}
				else
					anyDropped = true;

				continue;
			}

			if (!ChangelogTextUtilities.TryGetGitHubRepo(r, defaultOwner, defaultBundleRepo ?? string.Empty, out var owner, out var repo))
			{
				collector.EmitError(
					string.Empty,
					$"Link allowlist filtering could not parse {referenceKind} reference '{r}'. " +
					"Use a full https://github.com/ URL, owner/repo#number, or a bare number with bundle owner/repo set.");
				return null;
			}

			if (allow.Contains($"{owner}/{repo}"))
			{
				list.Add(r);
			}
			else
			{
				anyDropped = true;
				collector.EmitWarning(
					string.Empty,
					$"PR/issue reference '{r}' targets repository '{owner}/{repo}', which is not in the allowlist. It was removed from public output.");
			}
		}

		return list;
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
