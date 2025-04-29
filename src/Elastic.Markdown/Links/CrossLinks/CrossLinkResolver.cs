// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation;

namespace Elastic.Markdown.Links.CrossLinks;

public interface ICrossLinkResolver
{
	Task<FetchedCrossLinks> FetchLinks(Cancel ctx);
	bool TryResolve(Action<string> errorEmitter, Action<string> warningEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri);
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

	public bool TryResolve(Action<string> errorEmitter, Action<string> warningEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri) =>
		TryResolve(errorEmitter, warningEmitter, _crossLinks, UriResolver, crossLinkUri, out resolvedUri);

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
		Action<string> warningEmitter,
		FetchedCrossLinks fetchedCrossLinks,
		IUriEnvironmentResolver uriResolver,
		Uri crossLinkUri,
		[NotNullWhen(true)] out Uri? resolvedUri
	)
	{
		resolvedUri = null;
		var lookup = fetchedCrossLinks.LinkReferences;
		if (crossLinkUri.Scheme != "asciidocalypse" && lookup.TryGetValue(crossLinkUri.Scheme, out var linkReference))
			return TryFullyValidate(errorEmitter, uriResolver, fetchedCrossLinks, linkReference, crossLinkUri, out resolvedUri);

		// TODO this is temporary while we wait for all links.json to be published
		// Here we just silently rewrite the cross_link to the url

		var declaredRepositories = fetchedCrossLinks.DeclaredRepositories;
		if (!declaredRepositories.Contains(crossLinkUri.Scheme))
		{
			if (fetchedCrossLinks.FromConfiguration)
				errorEmitter($"'{crossLinkUri.Scheme}' is not declared as valid cross link repository in docset.yml under cross_links: '{crossLinkUri}'");
			else
				warningEmitter($"'{crossLinkUri.Scheme}' is not yet publishing to the links registry: '{crossLinkUri}'");
			return false;
		}

		var lookupPath = (crossLinkUri.Host + '/' + crossLinkUri.AbsolutePath.TrimStart('/')).Trim('/');
		var path = ToTargetUrlPath(lookupPath);
		if (!string.IsNullOrEmpty(crossLinkUri.Fragment))
			path += crossLinkUri.Fragment;

		resolvedUri = uriResolver.Resolve(crossLinkUri, path);
		return true;
	}

	private static bool TryFullyValidate(Action<string> errorEmitter,
		IUriEnvironmentResolver uriResolver,
		FetchedCrossLinks fetchedCrossLinks,
		LinkReference linkReference,
		Uri crossLinkUri,
		[NotNullWhen(true)] out Uri? resolvedUri)
	{
		resolvedUri = null;
		var lookupPath = (crossLinkUri.Host + '/' + crossLinkUri.AbsolutePath.TrimStart('/')).Trim('/');
		if (string.IsNullOrEmpty(lookupPath) && crossLinkUri.Host.EndsWith(".md"))
			lookupPath = crossLinkUri.Host;

		if (!LookupLink(errorEmitter, fetchedCrossLinks, linkReference, crossLinkUri, ref lookupPath, out var link, out var lookupFragment))
			return false;

		var path = ToTargetUrlPath(lookupPath);

		if (!string.IsNullOrEmpty(lookupFragment))
		{
			if (link.Anchors is null)
			{
				errorEmitter($"'{lookupPath}' does not have any anchors so linking to '{crossLinkUri.Fragment}' is impossible.");
				return false;
			}

			if (!link.Anchors.Contains(lookupFragment.TrimStart('#')))
			{
				errorEmitter($"'{lookupPath}' has no anchor named: '{lookupFragment}'.");
				return false;
			}

			path += "#" + lookupFragment.TrimStart('#');
		}

		resolvedUri = uriResolver.Resolve(crossLinkUri, path);
		return true;
	}

	private static bool LookupLink(Action<string> errorEmitter,
		FetchedCrossLinks crossLinks,
		LinkReference linkReference,
		Uri crossLinkUri,
		ref string lookupPath,
		[NotNullWhen(true)] out LinkMetadata? link,
		[NotNullWhen(true)] out string? lookupFragment)
	{
		lookupFragment = null;

		if (linkReference.Redirects is not null && linkReference.Redirects.TryGetValue(lookupPath, out var redirect))
		{
			var targets = (redirect.Many ?? [])
				.Select(r => r)
				.Concat([redirect])
				.Where(s => !string.IsNullOrEmpty(s.To))
				.ToArray();

			return ResolveLinkRedirect(targets, errorEmitter, linkReference, crossLinkUri, ref lookupPath, out link, ref lookupFragment);
		}

		if (linkReference.Links.TryGetValue(lookupPath, out link))
		{
			lookupFragment = crossLinkUri.Fragment;
			return true;
		}

		var linksJson = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/{crossLinkUri.Scheme}/main/links.json";
		if (crossLinks.LinkIndexEntries.TryGetValue(crossLinkUri.Scheme, out var linkIndexEntry))
			linksJson = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/{linkIndexEntry.Path}";

		errorEmitter($"'{lookupPath}' is not a valid link in the '{crossLinkUri.Scheme}' cross link index: {linksJson}");
		return false;
	}

	private static bool ResolveLinkRedirect(
		LinkSingleRedirect[] redirects,
		Action<string> errorEmitter,
		LinkReference linkReference,
		Uri crossLinkUri,
		ref string lookupPath, out LinkMetadata? link, ref string? lookupFragment)
	{
		var fragment = crossLinkUri.Fragment.TrimStart('#');
		link = null;
		foreach (var redirect in redirects)
		{
			if (string.IsNullOrEmpty(redirect.To))
				continue;
			if (!linkReference.Links.TryGetValue(redirect.To, out link))
				continue;

			if (string.IsNullOrEmpty(fragment))
			{
				lookupPath = redirect.To;
				return true;
			}

			if (redirect.Anchors is null || redirect.Anchors.Count == 0)
			{
				if (redirects.Length > 1)
					continue;
				lookupPath = redirect.To;
				lookupFragment = crossLinkUri.Fragment;
				return true;
			}

			if (redirect.Anchors.TryGetValue("!", out _))
			{
				lookupPath = redirect.To;
				lookupFragment = null;
				return true;
			}

			if (!redirect.Anchors.TryGetValue(crossLinkUri.Fragment.TrimStart('#'), out var newFragment))
				continue;

			lookupPath = redirect.To;
			lookupFragment = newFragment;
			return true;
		}

		var targets = string.Join(", ", redirects.Select(r => r.To));
		var failedLookup = lookupFragment is null ? lookupPath : $"{lookupPath}#{lookupFragment.TrimStart('#')}";
		errorEmitter($"'{failedLookup}' is set a redirect but none of redirect '{targets}' match or exist in links.json.");
		return false;
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
