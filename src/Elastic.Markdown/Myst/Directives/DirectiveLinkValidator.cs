// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Links;
using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives;

/// <summary>
/// Validates and resolves URL values supplied as directive options or in directive YAML
/// bodies. These URLs never pass through Markdig's <c>LinkInlineParser</c>, so without this
/// helper the normal cross-link resolution, missing-file checks, and link-index emission are
/// skipped and broken directive links ship silently.
///
/// Returns the resolved URL (or the original on failure) so callers can write it back into
/// their own data. Errors and hints are emitted against the supplying <see cref="DirectiveBlock"/>.
/// </summary>
internal static class DirectiveLinkValidator
{
	/// <summary>
	/// Resolve cross-link schemes and check that an internal path points at a real file.
	/// Requires a site-absolute path, a cross-link scheme, an anchor, or an external URL.
	/// Use this for directives whose links come from a YAML body or an option.
	/// </summary>
	public static string? ValidateAndResolve(string? url, DirectiveBlock block, ParserContext context) =>
		Resolve(url, block, context, allowRelative: false, checkFileExists: true);

	/// <summary>
	/// Resolve cross-link schemes only, and accept a path relative to the source file.
	/// Use this for directives whose links may target a generated page with no file on disk,
	/// such as the CLI reference, where a file probe reports false positives.
	/// </summary>
	public static string? ResolveWithoutFileCheck(string? url, DirectiveBlock block, ParserContext context) =>
		Resolve(url, block, context, allowRelative: true, checkFileExists: false);

	private static string? Resolve(string? url, DirectiveBlock block, ParserContext context, bool allowRelative, bool checkFileExists)
	{
		if (string.IsNullOrWhiteSpace(url) || block.SkipValidation)
			return url;

		var trimmed = url.Trim();
		if (trimmed.Length == 0 || trimmed[0] == '#')
			return url;

		if (IsExternal(trimmed))
			return url;

		if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && CrossLinkValidator.IsCrossLink(uri))
			return ResolveCrossLink(url, uri, block, context);

		if (!trimmed.StartsWith('/') && !allowRelative)
		{
			block.EmitError($"Directive link `{url}` must be an absolute path starting with `/`, a cross-link scheme (for example `kibana://`), or an external URL.");
			return url;
		}

		if (checkFileExists)
			ValidateInternal(url, block, context);
		return url;
	}

	/// <summary>
	/// Turn a validated URL into a final href. Strips the markdown extension and applies the
	/// site's URL path prefix. External URLs and anchors are returned unchanged.
	/// </summary>
	public static string? ToHref(string? url, string? sitePathPrefix)
	{
		if (string.IsNullOrEmpty(url))
			return url;
		if (IsExternal(url) || url.StartsWith('#'))
			return url;

		var (path, anchor) = SplitAnchor(url);
		path = StripMarkdownExtension(path);

		if (string.IsNullOrEmpty(sitePathPrefix) || !path.StartsWith('/'))
			return path + anchor;

		var prefix = "/" + sitePathPrefix.Trim('/');
		if (path == prefix || path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
			return path + anchor;

		return prefix + path + anchor;
	}

	private static bool IsExternal(string url) =>
		url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
		|| url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
		|| url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);

	private static string StripMarkdownExtension(string path)
	{
		if (path.EndsWith("/index.md", StringComparison.OrdinalIgnoreCase))
			return path[..^"/index.md".Length];
		return path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			? path[..^".md".Length]
			: path;
	}

	private static string ResolveCrossLink(string original, Uri uri, DirectiveBlock block, ParserContext context)
	{
		var resolver = context.CrossLinkResolver;
		if (!resolver.IsDeclaredCrossLinkScheme(uri.Scheme))
		{
			// Custom passthrough protocols (cursor:, vscode:) are left alone.
			if (IsPassthroughCustomProtocolScheme(uri.Scheme))
				return original;
			block.EmitError($"Directive link `{original}` uses cross-link scheme `{uri.Scheme}://` which is not declared under `cross_links` in docset.yml.");
			return original;
		}

		context.Build.Collector.EmitCrossLink(original);
		return resolver.TryResolve(s => block.EmitError(s), uri, out var resolved)
			? resolved.ToString()
			: original;
	}

	private static void ValidateInternal(string url, DirectiveBlock block, ParserContext context)
	{
		// In assembler and codex builds an absolute path may target a file owned by a different
		// docset, because the assembled site is the union of every docset. The current docset's
		// source directory is not the right basis for an existence check there, so flagging would
		// produce false positives. Cross-docset references should use a cross-link scheme instead.
		if (context.Build.BuildType != BuildType.Isolated)
			return;

		var (path, _) = SplitAnchor(url);
		if (string.IsNullOrEmpty(path) || path == "/")
			return;

		var sourceDir = context.Build.DocumentationSourceDirectory.FullName;
		var baseDir = path.StartsWith('/') ? sourceDir : context.MarkdownSourcePath.Directory!.FullName;
		var relativeToBase = path.TrimStart('/');

		foreach (var candidate in ProbeCandidates(relativeToBase))
		{
			if (context.TryFindDocumentByRelativePath(candidate) is not null)
				return;
			if (context.Build.ReadFileSystem.File.Exists(Path.GetFullPath(Path.Join(baseDir, candidate))))
				return;
		}

		if (TryEmitRedirectWarning(url, relativeToBase, block, context))
			return;

		block.EmitError($"Directive link `{url}` does not exist. If it was recently removed add a redirect.");
	}

	// docs-builder URLs usually omit the extension, so /explore-analyze/discover may mean
	// discover.md or discover/index.md. Probe as given first.
	private static string[] ProbeCandidates(string path) =>
		path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			? [path]
			: [path, path + ".md", path.TrimEnd('/') + "/index.md"];

	private static bool TryEmitRedirectWarning(string url, string relativeToBase, DirectiveBlock block, ParserContext context)
	{
		if (context.Configuration.Redirects is null
			|| !context.Configuration.Redirects.TryGetValue(relativeToBase, out var redirect))
			return false;

		var to = redirect.To
			?? (redirect.Many is not null
				? string.Join(", ", redirect.Many.Select(m => m.To))
				: "unknown");
		block.EmitWarning($"Directive link `{url}` has a redirect; update to: {to}");
		return true;
	}

	private static (string Path, string? Anchor) SplitAnchor(string url)
	{
		var hash = url.IndexOf('#');
		return hash < 0 ? (url, null) : (url[..hash], url[hash..]);
	}

	private static bool IsPassthroughCustomProtocolScheme(string scheme) =>
		scheme.Equals("cursor", StringComparison.OrdinalIgnoreCase)
		|| scheme.StartsWith("vscode", StringComparison.OrdinalIgnoreCase);
}
