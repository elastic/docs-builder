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

		return isBundlePath
			? await ScrubBundle(content, ctx)
			: await ScrubChangelog(key, content, ctx);
	}

	private async Task<ScrubResult> ScrubBundle(string content, Cancel ctx)
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
			return new ScrubResult { Content = content };
		}

		var result = ReleaseNotesSerialization.SerializeBundle(sanitized);
		LinkAllowlistSanitizer.ValidateNoPrivateReferences(result, allowRepos);
		return new ScrubResult { Content = result };
	}

	private async Task<ScrubResult> ScrubChangelog(string key, string content, Cancel ctx)
	{
		ctx.ThrowIfCancellationRequested();

		var normalized = ReleaseNotesSerialization.NormalizeYaml(content);
		var entry = ReleaseNotesSerialization.DeserializeEntry(normalized);

		// Pure marker: link: <pr_number> with no other content. Return unchanged — there is no URL to scrub.
		if (entry.Link != null)
		{
			var hasContent = !string.IsNullOrEmpty(entry.Title)
				|| entry.Type != ChangelogEntryType.Invalid
				|| entry.Products is { Count: > 0 }
				|| entry.Prs is { Count: > 0 };
			if (!hasContent)
				return new ScrubResult { Content = content };
			// Has link: alongside other fields — fall through to normal scrubbing (link is preserved below).
		}

		// Derive canonical key and markers from the pre-scrub entry while prs: URLs are still intact.
		var (canonicalKey, markers) = BuildCanonicalKeyAndMarkers(key, entry);

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
			return new ScrubResult { Content = content, CanonicalKey = canonicalKey, Markers = markers };
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
		return new ScrubResult { Content = result, CanonicalKey = canonicalKey, Markers = markers };
	}

	private static (string? CanonicalKey, IReadOnlyList<(string Key, string Content)> Markers)
		BuildCanonicalKeyAndMarkers(string sourceKey, ChangelogEntry entry)
	{
		// note-* files are their own anchor; markers have link: already set as their identity.
		var lastSlash = sourceKey.LastIndexOf('/');
		if (lastSlash < 0)
			return (null, []);

		var fileName = sourceKey[(lastSlash + 1)..];
		var keyPrefix = sourceKey[..(lastSlash + 1)];

		if (fileName.StartsWith("note-", StringComparison.OrdinalIgnoreCase) || entry.IsMarker)
			return (null, []);

		var prNumbers = entry.Prs?
			.Select(pr => ChangelogTextUtilities.ExtractPrNumber(pr))
			.Where(n => n.HasValue)
			.Select(n => n!.Value)
			.Distinct()
			.OrderBy(n => n)
			.ToList();

		if (prNumbers is null or { Count: 0 })
			return (null, []);

		var primaryPr = prNumbers[0];
		var canonicalFileName = $"{primaryPr}.yaml";
		var canonicalKey = string.Equals(fileName, canonicalFileName, StringComparison.OrdinalIgnoreCase)
			? null
			: keyPrefix + canonicalFileName;

		if (prNumbers.Count == 1)
			return (canonicalKey, []);

		var markerContent = ReleaseNotesSerialization.SerializeEntry(new ChangelogEntry { Link = primaryPr.ToString(System.Globalization.CultureInfo.InvariantCulture) });
		var markers = prNumbers
			.Skip(1)
			.Select(pr => (keyPrefix + $"{pr}.yaml", markerContent))
			.ToList<(string, string)>();

		return (canonicalKey, markers);
	}
}
