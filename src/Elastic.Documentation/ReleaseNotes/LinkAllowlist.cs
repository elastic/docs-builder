// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>A record with the private links taken out of it, and whether anything changed.</summary>
public sealed record PrivateLinkScrub<T>(T Value, bool Changed) where T : class;

/// <summary>
/// The pure core of the link allow-list: which PR and issue references of an entry or a bundle may be published.
/// <c>WithoutPrivateLinks</c> drops what is not allowed (public output); <c>MarkPrivateLinks</c> rewrites it to <c># PRIVATE:</c> sentinels (private-side output).
/// A finding (a reference that was dropped or rewritten, one that could not be parsed) is a diagnostic in the <see cref="Result{T}"/>, in the order it was found.
/// </summary>
public static partial class LinkAllowlist
{
	public const string SentinelPrefix = "# PRIVATE:";

	[GeneratedRegex(@"https?://github\.com/(?<owner>[A-Za-z0-9_.-]+)/(?<repo>[A-Za-z0-9_.-]+)/(?:pull|issues)/\d+", RegexOptions.None)]
	private static partial Regex GitHubUrlRegex();

	[GeneratedRegex(@"(?<![/\w])(?<owner>[A-Za-z0-9_.-]+)/(?<repo>[A-Za-z0-9_.-]+)#\d+", RegexOptions.None)]
	private static partial Regex ShortFormRefRegex();

	/// <summary>The entry with every reference to a repository outside <paramref name="allowRepos"/> removed (<c>prs</c>, <c>issues</c>, and the links in <c>description</c>, <c>impact</c>, <c>action</c>).</summary>
	public static Result<PrivateLinkScrub<BundledEntry>> WithoutPrivateLinks(
		BundledEntry entry,
		IReadOnlyList<string> allowRepos,
		string defaultOwner,
		string? defaultRepo
	)
	{
		var diagnostics = new List<Diagnostic>();
		if (
			!TryScrub(
				diagnostics,
				entry.Prs,
				entry.Issues,
				entry.Description,
				entry.Impact,
				entry.Action,
				allowRepos,
				defaultOwner,
				defaultRepo,
				out var scrubbed
			)
		)
			return Result.Failure<PrivateLinkScrub<BundledEntry>>(diagnostics);

		var value = entry with
		{
			Prs = scrubbed.Prs,
			Issues = scrubbed.Issues,
			Description = scrubbed.Description,
			Impact = scrubbed.Impact,
			Action = scrubbed.Action
		};
		return Result.Success(new PrivateLinkScrub<BundledEntry>(value, scrubbed.Changed), diagnostics);
	}

	/// <summary>The same for a changelog entry.</summary>
	public static Result<PrivateLinkScrub<ChangelogEntry>> WithoutPrivateLinks(
		ChangelogEntry entry,
		IReadOnlyList<string> allowRepos,
		string defaultOwner,
		string? defaultRepo
	)
	{
		var diagnostics = new List<Diagnostic>();
		if (
			!TryScrub(
				diagnostics,
				entry.Prs,
				entry.Issues,
				entry.Description,
				entry.Impact,
				entry.Action,
				allowRepos,
				defaultOwner,
				defaultRepo,
				out var scrubbed
			)
		)
			return Result.Failure<PrivateLinkScrub<ChangelogEntry>>(diagnostics);

		var value = entry with
		{
			Prs = scrubbed.Prs,
			Issues = scrubbed.Issues,
			Description = scrubbed.Description,
			Impact = scrubbed.Impact,
			Action = scrubbed.Action
		};
		var rechecked = DomainOperation.Derived(ChangelogEntryReadConstraints.All, value, diagnostics);
		return Result.Success(new PrivateLinkScrub<ChangelogEntry>(value, scrubbed.Changed), rechecked.Diagnostics);
	}

	/// <summary>The bundle with every entry scrubbed as <see cref="WithoutPrivateLinks(BundledEntry, IReadOnlyList{string}, string, string?)"/> and its description scrubbed of links too.</summary>
	public static Result<PrivateLinkScrub<Bundle>> WithoutPrivateLinks(
		Bundle bundle,
		IReadOnlyList<string> allowRepos,
		string defaultOwner,
		string? defaultBundleRepo
	)
	{
		var diagnostics = new List<Diagnostic>();
		var anyChanged = false;
		var entries = new List<BundledEntry>(bundle.Entries.Count);

		foreach (var entry in bundle.Entries)
		{
			var scrubbed = WithoutPrivateLinks(entry, allowRepos, defaultOwner, defaultBundleRepo);
			diagnostics.AddRange(scrubbed.Diagnostics);
			if (scrubbed.Value is null)
				return Result.Failure<PrivateLinkScrub<Bundle>>(diagnostics);
			anyChanged |= scrubbed.Value.Changed;
			entries.Add(scrubbed.Value.Value);
		}

		var description = ScrubText(bundle.Description, BuildAllowSet(allowRepos), ref anyChanged);
		var value = bundle with { Entries = entries, Description = description };
		var rechecked = DomainOperation.Derived(BundleReadConstraints.All, value, diagnostics);
		return Result.Success(new PrivateLinkScrub<Bundle>(value, anyChanged), rechecked.Diagnostics);
	}

	/// <summary>
	/// The bundle with every reference to a repository outside <paramref name="allowRepos"/> rewritten to a <c># PRIVATE:</c> sentinel, and a sentinel whose
	/// repository is allowed restored to the plain reference.
	/// </summary>
	public static Result<PrivateLinkScrub<Bundle>> MarkPrivateLinks(
		Bundle bundle,
		IReadOnlyList<string> allowRepos,
		string defaultOwner,
		string? defaultBundleRepo
	)
	{
		var diagnostics = new List<Diagnostic>();
		var allow = BuildAllowSet(allowRepos);
		var ownerDefault = string.IsNullOrWhiteSpace(defaultOwner) ? "elastic" : defaultOwner;
		var anyRewritten = false;
		var entries = new List<BundledEntry>(bundle.Entries.Count);

		foreach (var entry in bundle.Entries)
		{
			var prs = ApplyToReferenceList(diagnostics, entry.Prs, ownerDefault, defaultBundleRepo, allow, "PR", ref anyRewritten);
			if (prs == null && entry.Prs is not null)
				return Result.Failure<PrivateLinkScrub<Bundle>>(diagnostics);

			var issues = ApplyToReferenceList(diagnostics, entry.Issues, ownerDefault, defaultBundleRepo, allow, "issue", ref anyRewritten);
			if (issues == null && entry.Issues is not null)
				return Result.Failure<PrivateLinkScrub<Bundle>>(diagnostics);

			entries.Add(entry with { Prs = prs, Issues = issues });
		}

		var value = bundle with { Entries = entries };
		var rechecked = DomainOperation.Derived(BundleReadConstraints.All, value, diagnostics);
		return Result.Success(new PrivateLinkScrub<Bundle>(value, anyRewritten), rechecked.Diagnostics);
	}

	private readonly record struct Scrubbed(
		IReadOnlyList<string>? Prs,
		IReadOnlyList<string>? Issues,
		string? Description,
		string? Impact,
		string? Action,
		bool Changed
	);

	private static bool TryScrub(
		List<Diagnostic> diagnostics,
		IReadOnlyList<string>? entryPrs,
		IReadOnlyList<string>? entryIssues,
		string? entryDescription,
		string? entryImpact,
		string? entryAction,
		IReadOnlyList<string> allowRepos,
		string defaultOwner,
		string? defaultRepo,
		out Scrubbed scrubbed
	)
	{
		scrubbed = default;
		var allow = BuildAllowSet(allowRepos);
		var ownerDefault = string.IsNullOrWhiteSpace(defaultOwner) ? "elastic" : defaultOwner;
		var anyRewritten = false;

		var prs = FilterReferenceList(diagnostics, entryPrs, ownerDefault, defaultRepo, allow, "PR", ref anyRewritten);
		if (prs == null && entryPrs is not null)
			return false;

		var issues = FilterReferenceList(diagnostics, entryIssues, ownerDefault, defaultRepo, allow, "issue", ref anyRewritten);
		if (issues == null && entryIssues is not null)
			return false;

		var description = ScrubText(entryDescription, allow, ref anyRewritten);
		var impact = ScrubText(entryImpact, allow, ref anyRewritten);
		var action = ScrubText(entryAction, allow, ref anyRewritten);

		scrubbed = new Scrubbed(prs, issues, description, impact, action, anyRewritten);
		return true;
	}

	/// <summary>
	/// Replaces GitHub references in free text that point to repositories not in
	/// <paramref name="allow"/>. Handles full URLs and <c>owner/repo#N</c> short forms.
	/// </summary>
	public static string? ScrubText(string? input, HashSet<string> allow, ref bool changed)
	{
		if (string.IsNullOrWhiteSpace(input))
			return input;

		var anyReplaced = false;

		var result = GitHubUrlRegex().Replace(input, match =>
		{
			var fullName = $"{match.Groups["owner"].Value}/{match.Groups["repo"].Value}";
			if (allow.Contains(fullName))
				return match.Value;

			anyReplaced = true;
			return string.Empty;
		});

		result = ShortFormRefRegex().Replace(result, match =>
		{
			var fullName = $"{match.Groups["owner"].Value}/{match.Groups["repo"].Value}";
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
				$"Post-serialize validation failed: {violations.Count} private reference(s) found in public output: {string.Join(", ", violations)}"
			);
	}

	public static HashSet<string> BuildAllowSet(IReadOnlyList<string> allowRepos)
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

	public static bool TrySplitOwnerRepo(string entry, out string owner, out string repo)
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

	private static Diagnostic Warning(string message) => new() { Severity = Severity.Warning, File = string.Empty, Message = message };

	private static Diagnostic Error(string message) => new() { Severity = Severity.Error, File = string.Empty, Message = message };

	private static IReadOnlyList<string>? FilterReferenceList(
		List<Diagnostic> diagnostics,
		IReadOnlyList<string>? refs,
		string defaultOwner,
		string? defaultBundleRepo,
		HashSet<string> allow,
		string referenceKind,
		ref bool anyDropped
	)
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

				if (
					!ChangelogTextUtilities.TryGetGitHubRepo(
						underlyingRef,
						defaultOwner,
						defaultBundleRepo ?? string.Empty,
						out var sOwner,
						out var sRepo
					)
				)
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
				// A bare numeric reference with no repo context (e.g. `prs: ['155500']` loaded by the
				// scrubber Lambda, which doesn't know which repo a per-entry YAML belongs to) carries no
				// repository identity, so it cannot leak a private reference on its own. Keep it as-is —
				// downstream rendering supplies the owner/repo from runtime context — and report a warning so
				// the finding is still visible without failing the whole entry. Anything that still
				// encodes a repo (URL / owner/repo#N short-form) hits the explicit error branch below.
				if (!string.IsNullOrWhiteSpace(r) && uint.TryParse(r.Trim(), out _))
				{
					list.Add(r);
					diagnostics.Add(
						Warning(
							$"Bare {referenceKind} reference '{r}' has no embedded owner/repo and no default repo was supplied; keeping as-is for downstream rendering to resolve."
						)
					);
					continue;
				}

				diagnostics.Add(
					Error(
						$"Link allowlist filtering could not parse {referenceKind} reference '{r}'. " +
							"Use a full https://github.com/ URL, owner/repo#number, or a bare number with bundle owner/repo set."
					)
				);
				return null;
			}

			if (allow.Contains($"{owner}/{repo}"))
				list.Add(r);
			else
			{
				anyDropped = true;
				diagnostics.Add(
					Warning(
						$"PR/issue reference '{r}' targets repository '{owner}/{repo}', which is not in the allowlist. It was removed from public output."
					)
				);
			}
		}

		return list;
	}

	private static IReadOnlyList<string>? ApplyToReferenceList(
		List<Diagnostic> diagnostics,
		IReadOnlyList<string>? refs,
		string defaultOwner,
		string? defaultBundleRepo,
		HashSet<string> allow,
		string referenceKind,
		ref bool anyRewritten
	)
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
				var adjusted = ProcessSentinel(diagnostics, r, defaultOwner, defaultBundleRepo, allow, referenceKind, ref anyRewritten);
				if (adjusted == null)
					return null;

				list.Add(adjusted);
				continue;
			}

			var plain = ProcessPlainReference(diagnostics, r, defaultOwner, defaultBundleRepo, allow, referenceKind, ref anyRewritten);
			if (plain == null)
				return null;

			list.Add(plain);
		}

		return list;
	}

	private static string? ProcessSentinel(
		List<Diagnostic> diagnostics,
		string sentinelRef,
		string defaultOwner,
		string? defaultBundleRepo,
		HashSet<string> allow,
		string referenceKind,
		ref bool anyRewritten
	)
	{
		var underlyingRef = sentinelRef.Substring(SentinelPrefix.Length).Trim();

		if (string.IsNullOrWhiteSpace(underlyingRef))
		{
			diagnostics.Add(
				Error(
					$"Invalid {referenceKind} sentinel '{sentinelRef}': no underlying reference found. " +
						"Sentinels must have the format '# PRIVATE: <reference>'."
				)
			);
			return null;
		}

		if (
			!ChangelogTextUtilities.TryGetGitHubRepo(
				underlyingRef,
				defaultOwner,
				defaultBundleRepo ?? string.Empty,
				out var owner,
				out var repo
			)
		)
		{
			diagnostics.Add(
				Error(
					$"Invalid {referenceKind} sentinel '{sentinelRef}': underlying reference '{underlyingRef}' could not be parsed. " +
						"Use a full https://github.com/ URL, owner/repo#number, or a bare number with bundle owner/repo set."
				)
			);
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
		List<Diagnostic> diagnostics,
		string r,
		string defaultOwner,
		string? defaultBundleRepo,
		HashSet<string> allow,
		string referenceKind,
		ref bool anyRewritten
	)
	{
		if (!ChangelogTextUtilities.TryGetGitHubRepo(r, defaultOwner, defaultBundleRepo ?? string.Empty, out var owner, out var repo))
		{
			diagnostics.Add(
				Error(
					$"Link allowlist filtering could not parse {referenceKind} reference '{r}'. " +
						"Use a full https://github.com/ URL, owner/repo#number, or a bare number with bundle owner/repo set."
				)
			);
			return null;
		}

		var fullName = $"{owner}/{repo}";
		if (allow.Contains(fullName))
			return r;

		anyRewritten = true;
		diagnostics.Add(
			Warning(
				$"PR/issue reference '{r}' targets repository '{fullName}', which is not in bundle.link_allow_repos. " +
					"It was rewritten to a '# PRIVATE:' sentinel."
			)
		);
		return $"{SentinelPrefix} {r}";
	}
}
