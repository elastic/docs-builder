// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Links;
using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Validates and resolves URL values supplied in hub directive YAML bodies and
/// options. These URLs never pass through Markdig's <c>LinkInlineParser</c>, so
/// without this helper the normal cross-link resolution, missing-file checks,
/// and link-index emission are skipped — broken hub links ship silently.
///
/// Returns the resolved URL (or the original on failure) so callers can write
/// it back into their YAML data records. Errors / hints are emitted against
/// the supplying <see cref="DirectiveBlock"/>.
/// </summary>
internal static class HubLinkValidator
{
	/// <summary>
	/// Validate <paramref name="url"/> and, when applicable, resolve cross-link
	/// schemes to their final form. Returns the (possibly rewritten) URL.
	/// </summary>
	public static string? ValidateAndResolve(string? url, DirectiveBlock block, ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(url) || block.SkipValidation)
			return url;

		var trimmed = url.Trim();
		if (trimmed.Length == 0 || trimmed[0] == '#')
			return url;

		if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
			|| trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
			return url;

		if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && CrossLinkValidator.IsCrossLink(uri))
			return ResolveCrossLink(url, uri, block, context);

		// Treat anything else as a docset-internal path. Hub directives are authored
		// with site-absolute paths (e.g. "/explore-analyze/discover.md"); relative
		// paths don't have a meaningful base because the YAML body isn't anchored
		// to a markdown file location the way an inline [text](url) link is.
		if (!trimmed.StartsWith('/'))
		{
			block.EmitError($"Hub directive link `{url}` must be an absolute path starting with `/`, a cross-link scheme (e.g. `kibana://`), or an external URL.");
			return url;
		}

		ValidateInternal(url, block, context);
		return url;
	}

	private static string ResolveCrossLink(string original, Uri uri, DirectiveBlock block, ParserContext context)
	{
		var resolver = context.CrossLinkResolver;
		if (!resolver.IsDeclaredCrossLinkScheme(uri.Scheme))
		{
			// Custom passthrough protocols (cursor:, vscode:) — leave alone.
			if (IsPassthroughCustomProtocolScheme(uri.Scheme))
				return original;
			block.EmitError($"Hub directive link `{original}` uses cross-link scheme `{uri.Scheme}://` which is not declared under `cross_links` in docset.yml.");
			return original;
		}

		context.Build.Collector.EmitCrossLink(original);
		return resolver.TryResolve(s => block.EmitError(s), uri, out var resolved)
			? resolved.ToString()
			: original;
	}

	private static void ValidateInternal(string url, DirectiveBlock block, ParserContext context)
	{
		// In Assembler/Codex builds an absolute path may target a file owned by a
		// different docset (the assembled site is the union of all docsets), so the
		// current docset's source dir isn't the right basis for an existence check.
		// Cross-docset references should ideally use the cross-link scheme so they
		// resolve through CrossLinkResolver, but we don't have a way to assert that
		// at this layer yet — flagging here would produce false positives.
		if (context.Build.BuildType != BuildType.Isolated)
			return;

		var (path, _) = SplitAnchor(url);
		if (string.IsNullOrEmpty(path) || path == "/")
			return;

		var sourceDir = context.Build.DocumentationSourceDirectory.FullName;
		var fs = context.Build.ReadFileSystem;
		var rel = path.TrimStart('/');

		// Probe candidates in order: as-given, with .md, with /index.md.
		// docs-builder URLs typically omit .md, so authors write /explore-analyze/discover
		// for /explore-analyze/discover.md or /explore-analyze/discover/index.md.
		string[] candidates = path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			? [rel]
			: [rel, rel + ".md", rel.TrimEnd('/') + "/index.md"];

		foreach (var candidate in candidates)
		{
			if (context.TryFindDocumentByRelativePath(candidate) is not null)
				return;
			var pathOnDisk = Path.GetFullPath(Path.Join(sourceDir, candidate));
			if (fs.File.Exists(pathOnDisk))
				return;
		}

		// Honour configured redirects so old paths emit a hint, not an error.
		if (context.Configuration.Redirects is not null
			&& context.Configuration.Redirects.TryGetValue(rel, out var redirect))
		{
			var to = redirect.To
				?? (redirect.Many is not null
					? string.Join(", ", redirect.Many.Select(m => m.To))
					: "unknown");
			block.EmitWarning($"Hub directive link `{url}` has a redirect; update to: {to}");
			return;
		}

		block.EmitError($"Hub directive link `{url}` does not exist. If it was recently removed add a redirect.");
	}

	private static (string Path, string? Anchor) SplitAnchor(string url)
	{
		var hash = url.IndexOf('#');
		return hash < 0 ? (url, null) : (url[..hash], url[(hash + 1)..]);
	}

	private static bool IsPassthroughCustomProtocolScheme(string scheme) =>
		scheme.Equals("cursor", StringComparison.OrdinalIgnoreCase)
		|| scheme.StartsWith("vscode", StringComparison.OrdinalIgnoreCase);
}
