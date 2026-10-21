// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Scrubbing;

/// <summary>
/// The result of scrubbing a changelog artifact for public publication.
/// </summary>
public record ScrubResult
{
	/// <summary>The scrubbed YAML content to write to the public bucket.</summary>
	public required string Content { get; init; }

	/// <summary>
	/// Canonical public key for this object. Null when the source key is already canonical
	/// and no rename is needed; non-null when the source was named non-canonically
	/// (e.g. <c>12345-fix.yaml</c>) and must be written at <c>12345.yaml</c> instead.
	/// </summary>
	public string? CanonicalKey { get; init; }

	/// <summary>
	/// Additional marker objects to write to the public bucket for non-primary PRs
	/// in a multi-PR entry. Each marker's entire content is <c>link: {parentPr}</c>.
	/// Empty for single-PR entries and bundles.
	/// </summary>
	public IReadOnlyList<(string Key, string Content)> Markers { get; init; } = [];

	/// <summary>
	/// True when the content is a pass-through private marker (<c>link:</c> only).
	/// The processor must not overwrite existing canonical public content at the same key
	/// with a marker — private markers derived from raw (pre-allowlist) PR lists can be
	/// stale and their canonical target may already be written to a different public key.
	/// </summary>
	public bool IsMarker { get; init; }
}

/// <summary>Rewrites private-bucket changelog YAML into its public, allowlist-scrubbed form.</summary>
public interface IChangelogContentScrubber
{
	/// <summary>
	/// Scrubs <paramref name="content"/> for public publication. The key decides the document
	/// shape: <c>bundle/{product}/…</c> is a bundle, everything else a changelog entry. Throws
	/// when the content cannot be proven free of private references.
	/// </summary>
	Task<ScrubResult> ScrubAsync(string key, string content, Cancel ctx);
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
	public async Task<ScrubResult> ScrubAsync(string key, string content, Cancel ctx)
	{
		// Artifact-root layout: bundles live under "bundle/{product}/…", entries under
		// "changelog/{org}/{repo}/{branch}/…". Match the bundle prefix (not a "/bundle/" substring,
		// which no longer appears in the new keys) so bundles are not misclassified as entries.
		var isBundlePath = key.StartsWith(ChangelogKeys.BundlePrefix, StringComparison.OrdinalIgnoreCase);

		return isBundlePath ? await ScrubBundle(content, ctx) : ScrubChangelog(key, content, ctx);
	}

	private async Task<ScrubResult> ScrubBundle(string content, Cancel ctx)
	{
		ctx.ThrowIfCancellationRequested();

		var loadedBundle = ReleaseNotesLoader.LoadBundle(content, "bundle.yaml");
		var bundle = loadedBundle.Value ?? throw new InvalidDataException(loadedBundle.Diagnostics.Select(d => d.Message).FirstOrDefault());
		var owner = bundle.Products.Count > 0 ? bundle.Products[0].Owner ?? "elastic" : "elastic";
		var repo = bundle.Products.Count > 0 ? bundle.Products[0].Repo : null;

		await using var collector = new DiagnosticsCollector([]);
		if (!LinkAllowlistSanitizer.ScrubBundleForPublic(collector, bundle, allowRepos, owner, repo, out var sanitized, out var changed))
			throw new InvalidOperationException($"Failed to scrub bundle for public output; errors: {collector.Errors}");

		if (!changed)
		{
			_logger.LogInformation("Bundle had no private references, writing unchanged");
			LinkAllowlistSanitizer.ValidateNoPrivateReferences(content, allowRepos);
			return new ScrubResult { Content = content };
		}

		var result = ReleaseNotesLoader.Serialize(sanitized);
		LinkAllowlistSanitizer.ValidateNoPrivateReferences(result, allowRepos);
		return new ScrubResult { Content = result };
	}

	private ScrubResult ScrubChangelog(string key, string content, Cancel ctx)
	{
		ctx.ThrowIfCancellationRequested();

		var loadedEntry = ReleaseNotesLoader.LoadEntry(content, key);
		var entry = loadedEntry.Value ?? throw new InvalidDataException(loadedEntry.Diagnostics.Select(d => d.Message).FirstOrDefault());

		// Marker: link: <pr_number> with no other content.
		// Re-serialize rather than passing raw content through so private-authored fields
		// (e.g. source-redirect: true) are stripped. source-redirect is processor-owned and
		// must never be settable by private input — if it were passed through, a forged marker
		// could impersonate a scrubber-written source pointer and trigger canonical deletion.
		if (entry.Link != null)
		{
			var hasContent = !string.IsNullOrEmpty(entry.Title)
				|| entry.Type != ChangelogEntryType.Invalid
				|| entry.Products is { Count: > 0 }
				|| entry.Prs is { Count: > 0 };
			if (hasContent)
				throw new InvalidOperationException(
					"Changelog entry has both 'link:' and content fields. A marker must contain only 'link: <pr_number>'."
				);
			// the loader reported a link that is not a PR number; the marker is re-written as it is
			var safeContent = ReleaseNotesLoader.Serialize(ChangelogEntry.Marker(entry.Link).Value!);
			return new ScrubResult { Content = safeContent, IsMarker = true };
		}

		var scrubResult = entry.WithoutPrivateLinks(allowRepos);
		if (scrubResult.Value is not { } scrubbed)
			throw new InvalidOperationException(
				$"Failed to apply allowlist to changelog entry; errors: {scrubResult.Diagnostics.Count(d => d.Severity == Severity.Error)}"
			);
		var changed = scrubbed.Changed;

		// Derive canonical key and markers AFTER allowlist filtering so private-only PRs that are
		// stripped from public output don't become the primary anchor or generate stale markers.
		var publicEntry = changed ? scrubbed.Value : entry;
		var (canonicalKey, markers) = BuildCanonicalKeyAndMarkers(key, publicEntry);

		if (!changed)
		{
			_logger.LogInformation("Changelog entry had no private references, writing unchanged");
			LinkAllowlistSanitizer.ValidateNoPrivateReferences(content, allowRepos);
			return new ScrubResult { Content = content, CanonicalKey = canonicalKey, Markers = markers };
		}

		var result = ReleaseNotesLoader.Serialize(scrubbed.Value);
		LinkAllowlistSanitizer.ValidateNoPrivateReferences(result, allowRepos);
		return new ScrubResult { Content = result, CanonicalKey = canonicalKey, Markers = markers };
	}

	private static (string? CanonicalKey, IReadOnlyList<(string Key, string Content)> Markers) BuildCanonicalKeyAndMarkers(
		string sourceKey,
		ChangelogEntry entry
	)
	{
		// note-* files are their own anchor; markers have link: already set as their identity.
		var lastSlash = sourceKey.LastIndexOf('/');
		if (lastSlash < 0)
			return (null, []);

		var fileName = sourceKey[(lastSlash + 1)..];
		var keyPrefix = sourceKey[..(lastSlash + 1)];

		if (fileName.StartsWith("note-", StringComparison.OrdinalIgnoreCase) || entry.IsMarker)
			return (null, []);

		if (entry.CanonicalKeyAndMarkers(fileName) is not { } placement)
			return (null, []);

		var canonicalKey = placement.IsCanonical ? null : keyPrefix + placement.CanonicalFileName;
		var markers = placement
			.Markers
			.Select(m => (keyPrefix + m.FileName, ReleaseNotesLoader.Serialize(m.Marker)))
			.ToList<(string, string)>();

		return (canonicalKey, markers);
	}
}
