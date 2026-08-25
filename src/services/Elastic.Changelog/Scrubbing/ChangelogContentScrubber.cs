// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Scrubbing;

/// <summary>Rewrites private-bucket changelog YAML into its public, allowlist-scrubbed form.</summary>
public interface IChangelogContentScrubber
{
	/// <summary>
	/// Scrubs <paramref name="content"/> for public publication. The key decides the document
	/// shape: <c>bundle/{product}/…</c> is a bundle, everything else a changelog entry. Throws
	/// when the content cannot be proven free of private references.
	/// </summary>
	Task<string> ScrubAsync(string key, string content, Cancel ctx);
}

/// <summary>
/// The scrub pass previously inlined in the scrubber Lambda's <c>Program.cs</c>: applies the
/// repository allowlist via <see cref="LinkAllowlistSanitizer"/> and validates the result before
/// it may reach the public bucket.
/// </summary>
public sealed class ChangelogContentScrubber(ILoggerFactory logFactory, IReadOnlyList<string> allowRepos) : IChangelogContentScrubber
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogContentScrubber>();

	/// <inheritdoc />
	public async Task<string> ScrubAsync(string key, string content, Cancel ctx)
	{
		// Artifact-root layout: bundles live under "bundle/{product}/…", entries under
		// "changelog/{org}/{repo}/{branch}/…". Match the bundle prefix (not a "/bundle/" substring,
		// which no longer appears in the new keys) so bundles are not misclassified as entries.
		var isBundlePath = key.StartsWith(ChangelogKeys.BundlePrefix, StringComparison.OrdinalIgnoreCase);

		return isBundlePath
			? await ScrubBundle(content, ctx)
			: await ScrubChangelog(content, ctx);
	}

	private async Task<string> ScrubBundle(string content, Cancel ctx)
	{
		ctx.ThrowIfCancellationRequested();

		var bundle = ReleaseNotesSerialization.DeserializeBundle(content);
		var owner = bundle.Products.Count > 0 ? bundle.Products[0].Owner ?? "elastic" : "elastic";
		var repo = bundle.Products.Count > 0 ? bundle.Products[0].Repo : null;

		await using var collector = new DiagnosticsCollector([]);
		if (!LinkAllowlistSanitizer.ScrubBundleForPublic(collector, bundle, allowRepos, owner, repo, out var sanitized, out var changed))
			throw new InvalidOperationException($"Failed to scrub bundle for public output; errors: {collector.Errors}");

		if (!changed)
		{
			_logger.LogInformation("Bundle had no private references, writing unchanged");
			LinkAllowlistSanitizer.ValidateNoPrivateReferences(content, allowRepos);
			return content;
		}

		var result = ReleaseNotesSerialization.SerializeBundle(sanitized);
		LinkAllowlistSanitizer.ValidateNoPrivateReferences(result, allowRepos);
		return result;
	}

	private async Task<string> ScrubChangelog(string content, Cancel ctx)
	{
		ctx.ThrowIfCancellationRequested();

		var normalized = ReleaseNotesSerialization.NormalizeYaml(content);
		var entry = ReleaseNotesSerialization.DeserializeEntry(normalized);

		var bundledEntry = new BundledEntry
		{
			Type = entry.Type,
			Title = entry.Title,
			Description = entry.Description,
			Impact = entry.Impact,
			Action = entry.Action,
			Prs = entry.Prs,
			Issues = entry.Issues,
			Areas = entry.Areas,
			Highlight = entry.Highlight,
			Subtype = entry.Subtype,
			Link = entry.Link
		};

		await using var collector = new DiagnosticsCollector([]);
		if (!LinkAllowlistSanitizer.TryApplyChangelogEntry(
			collector, bundledEntry, allowRepos, "elastic", null,
			out var sanitized, out var changed))
			throw new InvalidOperationException($"Failed to apply allowlist to changelog entry; errors: {collector.Errors}");

		if (!changed)
		{
			_logger.LogInformation("Changelog entry had no private references, writing unchanged");
			LinkAllowlistSanitizer.ValidateNoPrivateReferences(content, allowRepos);
			return content;
		}

		var scrubEntry = entry with
		{
			Description = sanitized.Description,
			Impact = sanitized.Impact,
			Action = sanitized.Action,
			Prs = sanitized.Prs,
			Issues = sanitized.Issues,
			Link = entry.Link
		};

		var result = ReleaseNotesSerialization.SerializeEntry(scrubEntry);
		LinkAllowlistSanitizer.ValidateNoPrivateReferences(result, allowRepos);
		return result;
	}
}
