// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;

namespace Elastic.Documentation.Links.CrossLinks;

public interface ICrossLinkResolver
{
	LinkResolution Resolve(Uri crossLinkUri);
	IUriEnvironmentResolver UriResolver { get; }

	/// <summary>
	/// Determines whether the given URI scheme is a declared cross-link repository scheme.
	/// Schemes not declared here (e.g. <c>cursor</c>, <c>vscode</c>) are treated as custom
	/// protocol links and pass through without cross-link validation.
	/// </summary>
	bool IsDeclaredCrossLinkScheme(string scheme);

	/// <summary>
	/// Whether the API page at <paramref name="sitePath"/>, a path from the site root such as
	/// <c>/docs/api/doc/elasticsearch/operation/operation-search</c>, exists in the API sections of the link index.
	/// A resolver that holds no section for that API cannot tell, and answers <see cref="ApiPageStatus.Unknown"/>.
	/// </summary>
	ApiPageStatus CheckApiPage(string sitePath) => ApiPageStatus.Unknown;
}

public enum ApiPageStatus
{
	/// <summary>No API section covers the path, so the page cannot be judged.</summary>
	Unknown,
	Exists,
	Missing
}

public class NoopCrossLinkResolver : ICrossLinkResolver
{
	public static NoopCrossLinkResolver Instance { get; } = new();

	/// <inheritdoc />
	public LinkResolution Resolve(Uri crossLinkUri) => new LinkResolutionUnavailable();

	/// <inheritdoc />
	public IUriEnvironmentResolver UriResolver { get; } = new IsolatedBuildEnvironmentUriResolver();

	/// <inheritdoc />
	public bool IsDeclaredCrossLinkScheme(string scheme) => false;

	private NoopCrossLinkResolver() { }
}

public class CrossLinkResolver(FetchedCrossLinks crossLinks, IUriEnvironmentResolver? uriResolver = null) : ICrossLinkResolver
{
	private FetchedCrossLinks _crossLinks = crossLinks;
	public IUriEnvironmentResolver UriResolver { get; } = uriResolver ?? new IsolatedBuildEnvironmentUriResolver();

	public LinkResolution Resolve(Uri crossLinkUri) => Resolve(_crossLinks, UriResolver, crossLinkUri);

	/// <inheritdoc />
	public bool IsDeclaredCrossLinkScheme(string scheme) => _crossLinks.DeclaredRepositories.Contains(scheme);

	/// <inheritdoc />
	public ApiPageStatus CheckApiPage(string sitePath)
	{
		// API keys are unique across a build, so the key in the path names the one section that can hold the page.
		var path = sitePath.Trim('/');
		var rest = path.StartsWith("docs/", StringComparison.Ordinal) ? path["docs/".Length..] : path;
		if (!rest.StartsWith("api/doc/", StringComparison.Ordinal))
			return ApiPageStatus.Unknown;

		var key = rest["api/doc/".Length..].Split('/', 2)[0];
		foreach (var links in _crossLinks.LinkReferences.Values)
		{
			if (links.Floating is { Count: > 0 } floating && floating.TryGetValue($"api/{key}", out var section))
				return section.ContainsSitePath(rest) ? ApiPageStatus.Exists : ApiPageStatus.Missing;
		}

		return ApiPageStatus.Unknown;
	}

	public FetchedCrossLinks UpdateLinkReference(string repository, RepositoryLinks repositoryLinks)
	{
		var dictionary = _crossLinks.LinkReferences.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		dictionary[repository] = repositoryLinks;
		_crossLinks = _crossLinks with { LinkReferences = dictionary.ToFrozenDictionary() };
		return _crossLinks;
	}

	public static LinkResolution Resolve(FetchedCrossLinks fetchedCrossLinks, IUriEnvironmentResolver uriResolver, Uri crossLinkUri)
	{
		// First, check if the repository is in the declared repositories list, even if it's not in the link references
		var isDeclaredRepo = fetchedCrossLinks.DeclaredRepositories.Contains(crossLinkUri.Scheme);

		if (!fetchedCrossLinks.LinkReferences.TryGetValue(crossLinkUri.Scheme, out var sourceLinkReference))
		{
			// If it's a declared repository, we might be in a development environment or failed to fetch it,
			// so let's generate a synthesized URL to avoid blocking development
			if (isDeclaredRepo)
			{
				var path = ToTargetUrlPath((crossLinkUri.Host + '/' + crossLinkUri.AbsolutePath.TrimStart('/')).Trim('/'));
				var synthesizedUri = uriResolver.Resolve(crossLinkUri, path);
				return new LinkSynthesized(synthesizedUri, crossLinkUri.Scheme);
			}

			return new LinkSchemeNotDeclared(crossLinkUri.Scheme);
		}

		var originalLookupPath = (crossLinkUri.Host + '/' + crossLinkUri.AbsolutePath.TrimStart('/')).Trim('/');
		if (string.IsNullOrEmpty(originalLookupPath) && crossLinkUri.Host.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			originalLookupPath = crossLinkUri.Host;

		if (
			sourceLinkReference.Redirects is not null && sourceLinkReference.Redirects.TryGetValue(originalLookupPath, out var redirectRule)
		)
			return ResolveRedirect(uriResolver, crossLinkUri, redirectRule, originalLookupPath, fetchedCrossLinks);

		if (sourceLinkReference.Links.TryGetValue(originalLookupPath, out var directLinkMetadata))
			return ResolveDirectLink(uriResolver, crossLinkUri, originalLookupPath, directLinkMetadata);

		if (
			sourceLinkReference.Floating is { Count: > 0 } floating
			&& TryMatchFloatingSection(floating, originalLookupPath, out var sectionKey, out var section, out var version, out var page)
		)
		{
			return ResolveFloatingPage(uriResolver, crossLinkUri, (sectionKey, section, version, page));
		}

		var registryUrl = fetchedCrossLinks.RegistryUrlsByRepository?.GetValueOrDefault(crossLinkUri.Scheme)
			?? "https://elastic-docs-link-index.s3.us-east-2.amazonaws.com";
		var baseUrl = GetLinksJsonBaseUrl(registryUrl);
		var linksJson = fetchedCrossLinks.LinkIndexEntries.TryGetValue(crossLinkUri.Scheme, out var indexEntry)
			? $"{baseUrl}/{indexEntry.Path}"
			: BuildFallbackLinksJsonUrl(baseUrl, crossLinkUri.Scheme, fetchedCrossLinks);

		return new LinkNotInIndex(originalLookupPath, crossLinkUri.Scheme, linksJson);
	}

	/// <summary>
	/// Splits <c>{kind}/{key}[/{version}]/{page}</c> into the section, the version and the page. The version is
	/// optional and is the exact name of a version of that section. Without it the link reaches the current version.
	/// These are a few hash lookups and no scan of the pages.
	/// </summary>
	private static bool TryMatchFloatingSection(
		Dictionary<string, FloatingLinkSection> floating,
		string lookupPath,
		[NotNullWhen(true)] out string? sectionKey,
		[NotNullWhen(true)] out FloatingLinkSection? section,
		out string? version,
		out string page
	)
	{
		version = null;
		page = string.Empty;
		section = null;
		sectionKey = null;

		var segments = lookupPath.Trim('/').Split('/', 3);
		if (segments.Length < 2)
			return false;

		sectionKey = $"{segments[0]}/{segments[1]}";
		if (!floating.TryGetValue(sectionKey, out section))
			return false;

		var rest = segments.Length == 3 ? segments[2] : string.Empty;
		var versionEnd = rest.IndexOf('/');
		var first = versionEnd < 0 ? rest : rest[..versionEnd];
		// A page of the current version wins over a version of the same name, so adding a version never retargets an existing link.
		if (section.HasVersion(first) && !section.TryResolvePage(null, rest.Trim('/'), out _))
		{
			version = first;
			rest = versionEnd < 0 ? string.Empty : rest[(versionEnd + 1)..];
		}

		page = rest.Trim('/');
		return true;
	}

	private static LinkResolution ResolveFloatingPage(
		IUriEnvironmentResolver uriResolver,
		Uri crossLinkUri,
		(string SectionKey, FloatingLinkSection Section, string? Version, string Page) match
	)
	{
		if (!match.Section.TryResolvePage(match.Version, match.Page, out var sitePath))
			return new LinkFloatingPageNotFound(match.Page, match.SectionKey, match.Version);

		return new LinkResolved(uriResolver.ResolveSitePath(crossLinkUri, sitePath + crossLinkUri.Fragment));
	}

	private static LinkResolution ResolveDirectLink(
		IUriEnvironmentResolver uriResolver,
		Uri crossLinkUri,
		string lookupPath,
		LinkMetadata linkMetadata
	)
	{
		var lookupFragment = crossLinkUri.Fragment;
		var targetUrlPath = ToTargetUrlPath(lookupPath);

		if (!string.IsNullOrEmpty(lookupFragment))
		{
			var anchor = lookupFragment.TrimStart('#');
			if (linkMetadata.Anchors is null || !linkMetadata.Anchors.Contains(anchor, StringComparer.OrdinalIgnoreCase))
				return new LinkAnchorNotFound(lookupPath, lookupFragment);

			targetUrlPath += lookupFragment;
		}

		return new LinkResolved(uriResolver.Resolve(crossLinkUri, targetUrlPath));
	}

	private static LinkResolution ResolveRedirect(
		IUriEnvironmentResolver uriResolver,
		Uri originalCrossLinkUri,
		LinkRedirect redirectRule,
		string originalLookupPath,
		FetchedCrossLinks fetchedCrossLinks
	)
	{
		var originalFragment = originalCrossLinkUri.Fragment.TrimStart('#');

		if (!string.IsNullOrEmpty(originalFragment) && redirectRule.Many is { Length: > 0 })
		{
			foreach (var subRule in redirectRule.Many)
			{
				if (string.IsNullOrEmpty(subRule.To))
					continue;

				if (subRule.Anchors is null || subRule.Anchors.Count == 0)
					continue;

				if (subRule.Anchors.TryGetValue("!", out _))
					return FinalizeRedirect(uriResolver, originalCrossLinkUri, subRule.To, null, fetchedCrossLinks);
				if (subRule.Anchors.TryGetValue(originalFragment, out var mappedAnchor))
					return FinalizeRedirect(uriResolver, originalCrossLinkUri, subRule.To, mappedAnchor, fetchedCrossLinks);
			}
		}

		string? finalTargetFragment = null;

		if (!string.IsNullOrEmpty(originalFragment))
		{
			if (redirectRule.Anchors?.TryGetValue("!", out _) ?? false)
				finalTargetFragment = null;
			else if (redirectRule.Anchors?.TryGetValue(originalFragment, out var mappedAnchor) ?? false)
				finalTargetFragment = mappedAnchor;
			else if (redirectRule.Anchors is null || redirectRule.Anchors.Count == 0)
				finalTargetFragment = originalFragment;
			else
				return new LinkRedirectAnchorUnhandled(originalLookupPath, originalCrossLinkUri.Scheme, originalFragment);
		}

		return string.IsNullOrEmpty(redirectRule.To)
			? FinalizeRedirect(uriResolver, originalCrossLinkUri, originalLookupPath, finalTargetFragment, fetchedCrossLinks)
			: FinalizeRedirect(uriResolver, originalCrossLinkUri, redirectRule.To, finalTargetFragment, fetchedCrossLinks);
	}

	private static LinkResolution FinalizeRedirect(
		IUriEnvironmentResolver uriResolver,
		Uri originalProcessingUri,
		string redirectToPath,
		string? targetFragment,
		FetchedCrossLinks fetchedCrossLinks
	)
	{
		string finalPathForResolver;

		if (
			Uri.TryCreate(redirectToPath, UriKind.Absolute, out var targetCrossUri)
			&& targetCrossUri.Scheme != "http"
			&& targetCrossUri.Scheme != "https"
		)
		{
			var lookupPath = $"{targetCrossUri.Host}/{targetCrossUri.AbsolutePath.TrimStart('/')}";
			finalPathForResolver = ToTargetUrlPath(lookupPath);

			if (!string.IsNullOrEmpty(targetFragment) && targetFragment != "!")
				finalPathForResolver += $"#{targetFragment}";

			if (!fetchedCrossLinks.LinkReferences.TryGetValue(targetCrossUri.Scheme, out var targetLinkReference))
				return new LinkRedirectRepositoryMissing(redirectToPath, targetCrossUri.Scheme);

			if (!targetLinkReference.Links.ContainsKey(lookupPath))
				return new LinkRedirectTargetMissing(redirectToPath, lookupPath, targetCrossUri.Scheme);

			return new LinkResolved(uriResolver.Resolve(targetCrossUri, finalPathForResolver));
		}

		finalPathForResolver = ToTargetUrlPath(redirectToPath);
		if (!string.IsNullOrEmpty(targetFragment) && targetFragment != "!")
			finalPathForResolver += $"#{targetFragment}";

		return new LinkResolved(uriResolver.Resolve(originalProcessingUri, finalPathForResolver));
	}

	public static string ToTargetUrlPath(string lookupPath)
	{
		//https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/cloud-account/change-your-password
		var path = lookupPath.Replace(".md", "");
		if (path.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
			path = path[..^6];
		if (path == "index")
			path = string.Empty;
		return path;
	}

	/// <summary>Derives the base URL for links.json from a reader's RegistryUrl (S3 or GitHub).</summary>
	private static string GetLinksJsonBaseUrl(string registryUrl)
	{
		if (registryUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
			return $"{registryUrl.TrimEnd('/')}/blob/main";
		if (registryUrl.Contains("/link-index.json", StringComparison.OrdinalIgnoreCase))
			return registryUrl.Replace("/link-index.json", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
		return registryUrl.TrimEnd('/');
	}

	/// <summary>
	/// Builds a best-effort links.json URL to show in error messages when the index could not be fetched
	/// and no <see cref="LinkRegistryEntry"/> is available. Codex/internal indexes use
	/// <c>{env}/elastic/{scheme}/links.json</c>; the public S3 index uses <c>elastic/{scheme}/main/links.json</c>.
	/// </summary>
	private static string BuildFallbackLinksJsonUrl(string baseUrl, string scheme, FetchedCrossLinks fetchedCrossLinks)
	{
		if (
			fetchedCrossLinks.RegistryByRepository is not null
			&& fetchedCrossLinks.RegistryByRepository.TryGetValue(scheme, out var registry)
			&& registry != DocSetRegistry.Public
		)
		{
			return $"{baseUrl}/{registry.ToStringFast(true)}/elastic/{scheme}/links.json";
		}

		return $"{baseUrl}/elastic/{scheme}/main/links.json";
	}
}
