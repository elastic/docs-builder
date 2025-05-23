// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Links;

namespace Elastic.Markdown.Links.CrossLinks;

public interface ICrossLinkResolver
{
	Task<FetchedCrossLinks> FetchLinks(Cancel ctx);
	bool TryResolve(Action<string> errorEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri);
	IUriEnvironmentResolver UriResolver { get; }
}

public class CrossLinkResolver(CrossLinkFetcher fetcher, IUriEnvironmentResolver? uriResolver = null) : ICrossLinkResolver
{
	private FetchedCrossLinks _crossLinks = FetchedCrossLinks.Empty;
	public IUriEnvironmentResolver UriResolver { get; } = uriResolver ?? new IsolatedBuildEnvironmentUriResolver();

	public async Task<FetchedCrossLinks> FetchLinks(Cancel ctx)
	{
		_crossLinks = await fetcher.Fetch(ctx);
		return _crossLinks;
	}

	public bool TryResolve(Action<string> errorEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri) =>
		TryResolve(errorEmitter, _crossLinks, UriResolver, crossLinkUri, out resolvedUri);

	public FetchedCrossLinks UpdateLinkReference(string repository, LinkReference linkReference)
	{
		var dictionary = _crossLinks.LinkReferences.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		dictionary[repository] = linkReference;
		_crossLinks = _crossLinks with
		{
			LinkReferences = dictionary.ToFrozenDictionary()
		};
		return _crossLinks;
	}

	public static bool TryResolve(
		Action<string> errorEmitter,
		FetchedCrossLinks fetchedCrossLinks,
		IUriEnvironmentResolver uriResolver,
		Uri crossLinkUri,
		[NotNullWhen(true)] out Uri? resolvedUri
	)
	{
		resolvedUri = null;

		if (!fetchedCrossLinks.LinkReferences.TryGetValue(crossLinkUri.Scheme, out var sourceLinkReference))
		{
			errorEmitter($"'{crossLinkUri.Scheme}' was not found in the cross link index");
			return false;
		}

		var originalLookupPath = (crossLinkUri.Host + '/' + crossLinkUri.AbsolutePath.TrimStart('/')).Trim('/');
		if (string.IsNullOrEmpty(originalLookupPath) && crossLinkUri.Host.EndsWith(".md"))
			originalLookupPath = crossLinkUri.Host;

		if (sourceLinkReference.Redirects is not null && sourceLinkReference.Redirects.TryGetValue(originalLookupPath, out var redirectRule))
			return ResolveRedirect(errorEmitter, uriResolver, crossLinkUri, redirectRule, originalLookupPath, fetchedCrossLinks, out resolvedUri);

		if (sourceLinkReference.Links.TryGetValue(originalLookupPath, out var directLinkMetadata))
			return ResolveDirectLink(errorEmitter, uriResolver, crossLinkUri, originalLookupPath, directLinkMetadata, out resolvedUri);


		var linksJson = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/{crossLinkUri.Scheme}/main/links.json";
		if (fetchedCrossLinks.LinkIndexEntries.TryGetValue(crossLinkUri.Scheme, out var indexEntry))
			linksJson = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/{indexEntry.Path}";

		errorEmitter($"'{originalLookupPath}' is not a valid link in the '{crossLinkUri.Scheme}' cross link index: {linksJson}");
		resolvedUri = null;
		return false;
	}

	private static bool ResolveDirectLink(Action<string> errorEmitter,
		IUriEnvironmentResolver uriResolver,
		Uri crossLinkUri,
		string lookupPath,
		LinkMetadata linkMetadata,
		[NotNullWhen(true)] out Uri? resolvedUri)
	{
		resolvedUri = null;
		var lookupFragment = crossLinkUri.Fragment;
		var targetUrlPath = ToTargetUrlPath(lookupPath);

		if (!string.IsNullOrEmpty(lookupFragment))
		{
			var anchor = lookupFragment.TrimStart('#');
			if (linkMetadata.Anchors is null || !linkMetadata.Anchors.Contains(anchor))
			{
				errorEmitter($"'{lookupPath}' has no anchor named: '{lookupFragment}'.");
				return false;
			}

			targetUrlPath += lookupFragment;
		}

		resolvedUri = uriResolver.Resolve(crossLinkUri, targetUrlPath);
		return true;
	}

	private static bool ResolveRedirect(
		Action<string> errorEmitter,
		IUriEnvironmentResolver uriResolver,
		Uri originalCrossLinkUri,
		LinkRedirect redirectRule,
		string originalLookupPath,
		FetchedCrossLinks fetchedCrossLinks,
		[NotNullWhen(true)] out Uri? resolvedUri)
	{
		resolvedUri = null;
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
					return FinalizeRedirect(errorEmitter, uriResolver, originalCrossLinkUri, subRule.To, null, fetchedCrossLinks, out resolvedUri);
				if (subRule.Anchors.TryGetValue(originalFragment, out var mappedAnchor))
					return FinalizeRedirect(errorEmitter, uriResolver, originalCrossLinkUri, subRule.To, mappedAnchor, fetchedCrossLinks, out resolvedUri);
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
			{
				errorEmitter($"Redirect rule for '{originalLookupPath}' in '{originalCrossLinkUri.Scheme}' found, but top-level rule did not handle anchor '#{originalFragment}'.");
				return false;
			}
		}

		return string.IsNullOrEmpty(redirectRule.To)
			? FinalizeRedirect(errorEmitter, uriResolver, originalCrossLinkUri, originalLookupPath, finalTargetFragment, fetchedCrossLinks, out resolvedUri)
			: FinalizeRedirect(errorEmitter, uriResolver, originalCrossLinkUri, redirectRule.To, finalTargetFragment, fetchedCrossLinks, out resolvedUri);
	}

	private static bool FinalizeRedirect(
		Action<string> errorEmitter,
		IUriEnvironmentResolver uriResolver,
		Uri originalProcessingUri,
		string redirectToPath,
		string? targetFragment,
		FetchedCrossLinks fetchedCrossLinks,
		[NotNullWhen(true)] out Uri? resolvedUri)
	{
		resolvedUri = null;
		string finalPathForResolver;

		if (Uri.TryCreate(redirectToPath, UriKind.Absolute, out var targetCrossUri) && targetCrossUri.Scheme != "http" && targetCrossUri.Scheme != "https")
		{
			var lookupPath = Path.Combine(targetCrossUri.Host, targetCrossUri.AbsolutePath.TrimStart('/'));
			finalPathForResolver = ToTargetUrlPath(lookupPath);

			if (!string.IsNullOrEmpty(targetFragment) && targetFragment != "!")
				finalPathForResolver += $"#{targetFragment}";

			if (!fetchedCrossLinks.LinkReferences.TryGetValue(targetCrossUri.Scheme, out var targetLinkReference))
			{
				errorEmitter($"Redirect target '{redirectToPath}' points to repository '{targetCrossUri.Scheme}' for which no links.json was found.");
				return false;
			}

			if (!targetLinkReference.Links.ContainsKey(lookupPath))
			{
				errorEmitter($"Redirect target '{redirectToPath}' points to file '{lookupPath}' which was not found in repository '{targetCrossUri.Scheme}'s links.json.");
				return false;
			}

			resolvedUri = uriResolver.Resolve(targetCrossUri, finalPathForResolver); // Use targetUri for scheme and base
		}
		else
		{
			finalPathForResolver = ToTargetUrlPath(redirectToPath);
			if (!string.IsNullOrEmpty(targetFragment) && targetFragment != "!")
				finalPathForResolver += $"#{targetFragment}";

			resolvedUri = uriResolver.Resolve(originalProcessingUri, finalPathForResolver); // Use original URI's scheme
		}
		return true;
	}

	private static string ToTargetUrlPath(string lookupPath)
	{
		//https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/cloud-account/change-your-password
		var path = lookupPath.Replace(".md", "");
		if (path.EndsWith("/index"))
			path = path[..^6];
		if (path == "index")
			path = string.Empty;
		return path;
	}
}
