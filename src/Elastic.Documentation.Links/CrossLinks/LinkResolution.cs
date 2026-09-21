// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Links.CrossLinks;

/// <summary>
/// The result of resolving a cross-link URI. Each case carries exactly the information its
/// name implies and nothing more — callers match exhaustively rather than testing a <c>bool</c>
/// and reading a separate diagnostic message.
/// </summary>
/// <remarks>
/// Only <see cref="LinkResolved"/> and <see cref="LinkSynthesized"/> represent success.
/// All other cases represent a resolution failure and carry a diagnostic message via
/// <see cref="LinkResolutionExtensions.ToDiagnosticMessage"/>.
/// </remarks>
public readonly union LinkResolution(
	LinkResolved,
	LinkSynthesized,
	LinkSchemeNotDeclared,
	LinkNotInIndex,
	LinkAnchorNotFound,
	LinkRedirectAnchorUnhandled,
	LinkRedirectRepositoryMissing,
	LinkRedirectTargetMissing,
	LinkResolutionUnavailable
);

/// <summary>The cross-link resolved to a concrete target URI.</summary>
public sealed record LinkResolved(Uri Uri);

/// <summary>
/// The scheme is declared in the cross-link index but its <c>links.json</c> was not fetched
/// (e.g. in a development environment). A synthesized URL was produced to avoid blocking work.
/// Callers that require a verified resolution should treat this as a warning.
/// </summary>
public sealed record LinkSynthesized(Uri Uri, string Repository);

/// <summary>
/// The URI scheme is not declared as a cross-link repository in the docset. It may be a
/// custom-protocol link (<c>cursor://</c>, <c>vscode://</c>) — those pass through; this case
/// is emitted when the scheme was expected to be a cross-link but was missing from the index.
/// </summary>
public sealed record LinkSchemeNotDeclared(string Scheme);

/// <summary>
/// The repository is in the declared cross-link index but the specified path does not appear
/// in its <c>links.json</c>.
/// </summary>
public sealed record LinkNotInIndex(string OriginalPath, string Scheme, string LinksJsonUrl);

/// <summary>
/// The path resolved successfully but the specified fragment anchor does not exist on the
/// target page.
/// </summary>
public sealed record LinkAnchorNotFound(string Path, string Anchor);

/// <summary>
/// A redirect rule for the path was found but its anchor mapping did not handle the requested
/// fragment.
/// </summary>
public sealed record LinkRedirectAnchorUnhandled(string OriginalPath, string Scheme, string Fragment);

/// <summary>
/// A redirect rule points to a repository for which no <c>links.json</c> was found.
/// </summary>
public sealed record LinkRedirectRepositoryMissing(string RedirectTarget, string TargetRepository);

/// <summary>
/// A redirect rule points to a file that does not appear in the target repository's
/// <c>links.json</c>.
/// </summary>
public sealed record LinkRedirectTargetMissing(string RedirectTarget, string TargetPath, string TargetRepository);

/// <summary>
/// No cross-link resolution was possible — typically because the resolver is a no-op stub used
/// in isolated or step-preview contexts.
/// </summary>
public sealed record LinkResolutionUnavailable;

/// <summary>Extension methods for working with <see cref="LinkResolution"/>.</summary>
public static class LinkResolutionExtensions
{
	/// <summary>
	/// Returns the resolved URI when resolution succeeded (<see cref="LinkResolved"/> or
	/// <see cref="LinkSynthesized"/>), or <c>null</c> for all failure cases.
	/// CS8509 fires here — not at every call site — if the union gains a new case.
	/// </summary>
	public static Uri? ResolvedUri(this LinkResolution resolution) => resolution switch
	{
		LinkResolved(var u) => u,
		LinkSynthesized(var u, _) => u,
		LinkSchemeNotDeclared or LinkNotInIndex or LinkAnchorNotFound or LinkRedirectAnchorUnhandled or LinkRedirectRepositoryMissing or LinkRedirectTargetMissing or LinkResolutionUnavailable =>
			null
	};

	/// <summary>
	/// Returns a human-readable diagnostic message for failure cases. Call only when
	/// <see cref="ResolvedUri"/> returned <c>null</c>.
	/// </summary>
	public static string ToDiagnosticMessage(this LinkResolution resolution, Uri crossLinkUri) => resolution switch
	{
		LinkSchemeNotDeclared(var scheme) =>
			$"'{scheme}' was not found in the cross link index. Ensure it is listed under 'cross_links' in your docset.yml",
		LinkNotInIndex(var path, var scheme, var linksJsonUrl) =>
			$"'{path}' is not a valid link in the '{scheme}' cross link index: {linksJsonUrl}",
		LinkAnchorNotFound(var path, var anchor) => $"'{path}' has no anchor named: '{anchor}'.",
		LinkRedirectAnchorUnhandled(var path, var scheme, var fragment) =>
			$"Redirect rule for '{path}' in '{scheme}' found, but top-level rule did not handle anchor '#{fragment}'.",
		LinkRedirectRepositoryMissing(var redirectTarget, var targetRepo) =>
			$"Redirect target '{redirectTarget}' points to repository '{targetRepo}' for which no links.json was found.",
		LinkRedirectTargetMissing(var redirectTarget, var targetPath, var targetRepo) =>
			$"Redirect target '{redirectTarget}' points to file '{targetPath}' which was not found in repository '{targetRepo}'s links.json.",
		LinkResolutionUnavailable => $"No cross-link resolver is available for '{crossLinkUri.OriginalString}'.",
		// Success cases — protect the switch from CS8509
		LinkResolved or LinkSynthesized => $"'{crossLinkUri.OriginalString}' resolved successfully (no diagnostic).",
	};
}
