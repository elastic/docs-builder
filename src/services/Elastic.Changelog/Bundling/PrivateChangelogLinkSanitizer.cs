// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Rewrites PR/issue references that target private repositories to sentinel strings for bundle YAML output.
/// </summary>
public static class PrivateChangelogLinkSanitizer
{
	private const string SentinelPrefix = "# PRIVATE:";

	/// <summary>
	/// Rewrites PR/issue strings that target repositories marked private in <paramref name="assembly"/> to
	/// <c># PRIVATE:</c> sentinels. Emits errors for unknown repos. The empty <c>references</c> registry error is
	/// emitted only when a parseable PR/issue reference requires classification.
	/// </summary>
	/// <param name="collector">Diagnostic sink for validation errors.</param>
	/// <param name="bundle">Input bundle; unchanged when this method returns false.</param>
	/// <param name="assembly">Parsed <c>assembler.yml</c> (must list every referenced <c>owner/repo</c>).</param>
	/// <param name="defaultOwner">Default GitHub organization for bare numeric references.</param>
	/// <param name="defaultBundleRepo">Bundle repo field (supports <c>repo1+repo2</c>; first segment used for defaults).</param>
	/// <param name="sanitized">Bundle with updated <c>Prs</c>/<c>Issues</c> when return value is true.</param>
	/// <param name="changesApplied">True when at least one reference was rewritten to a sentinel.</param>
	/// <returns>True if all references were validated and any rewrites applied successfully.</returns>
	public static bool TrySanitizeBundle(
		IDiagnosticsCollector collector,
		Bundle bundle,
		AssemblyConfiguration assembly,
		string defaultOwner,
		string? defaultBundleRepo,
		out Bundle sanitized,
		out bool changesApplied)
	{
		sanitized = bundle;
		changesApplied = false;

		var ownerDefault = string.IsNullOrWhiteSpace(defaultOwner) ? "elastic" : defaultOwner;
		var anyRewritten = false;
		var newEntries = new List<BundledEntry>(bundle.Entries.Count);

		foreach (var entry in bundle.Entries)
		{
			var prs = SanitizeReferenceList(collector, entry.Prs, ownerDefault, defaultBundleRepo, assembly, "PR", ref anyRewritten);
			if (prs == null)
				return false;

			var issues = SanitizeReferenceList(collector, entry.Issues, ownerDefault, defaultBundleRepo, assembly, "issue", ref anyRewritten);
			if (issues == null)
				return false;

			newEntries.Add(entry with { Prs = prs, Issues = issues });
		}

		sanitized = bundle with { Entries = newEntries };
		changesApplied = anyRewritten;
		return true;
	}

	private static IReadOnlyList<string>? SanitizeReferenceList(
		IDiagnosticsCollector collector,
		IReadOnlyList<string>? refs,
		string defaultOwner,
		string? defaultBundleRepo,
		AssemblyConfiguration assembly,
		string referenceKind,
		ref bool anyRewritten)
	{
		if (refs is null || refs.Count == 0)
			return refs ?? [];

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
				list.Add(r);
				continue;
			}

			if (!ChangelogTextUtilities.TryGetGitHubRepo(r, defaultOwner, defaultBundleRepo ?? string.Empty, out var o, out var repoName))
			{
				collector.EmitError(
					string.Empty,
					$"Private link sanitization could not parse {referenceKind} reference '{r}'. " +
					"Use a full https://github.com/ URL, owner/repo#number, or a bare number with bundle owner/repo set."
				);
				return null;
			}

			if (assembly.ReferenceRepositories.Count == 0)
			{
				collector.EmitError(
					string.Empty,
					"Private link sanitization requires a non-empty assembler.yml references section. " +
					"Ensure configuration is loaded (for example ./config relative to the current directory, embedded defaults, or --configuration-source). " +
					"See documentation for changelog bundle private link filtering."
				);
				return null;
			}

			if (!TryFindReferenceRepository(o, repoName, assembly, out var repository) || repository is null)
			{
				collector.EmitError(
					string.Empty,
					$"Repository '{o}/{repoName}' referenced in a changelog {referenceKind} is not listed in assembler.yml references. " +
					"Add it under references with private: true or false. " +
					"Ensure assembler configuration is up to date (local ./config, embedded binary, or --configuration-source)."
				);
				return null;
			}

			if (repository.Private)
			{
				list.Add($"{SentinelPrefix} {r}");
				anyRewritten = true;
			}
			else
				list.Add(r);
		}

		return list;
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
