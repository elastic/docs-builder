// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;

namespace Elastic.Documentation.Links.CrossLinks;

public interface IUriEnvironmentResolver
{
	Uri Resolve(Uri crossLinkUri, string path);

	/// <summary>
	/// Resolves a page of a floating section, such as an API page. These pages own a URL root at the site root,
	/// so the repository mappings that <see cref="Resolve"/> applies do not apply to them.
	/// </summary>
	Uri ResolveSitePath(Uri crossLinkUri, string sitePath) => Resolve(crossLinkUri, sitePath);

	/// <summary>
	/// Whether the host this resolver links to still proxies the legacy <c>/docs/api/doc/</c> API URLs to bump.sh, so
	/// links to API pages take that form.
	/// </summary>
	bool LegacyApiUrls => false;
}

public class IsolatedBuildEnvironmentUriResolver : IUriEnvironmentResolver
{
	private static Uri BaseUri { get; } = new("https://docs-v3-preview.elastic.dev");

	public Uri Resolve(Uri crossLinkUri, string path) =>
		new(BaseUri, $"elastic/{crossLinkUri.Scheme}/tree/{GetBranch(crossLinkUri)}/{path}");

	// Floating sections are not part of a repository's source tree, so an isolated build links to the published page.
	public Uri ResolveSitePath(Uri crossLinkUri, string sitePath) => new(PublishedBaseUri, $"docs/{sitePath}");

	/// <inheritdoc />
	/// <remarks>Follows prod's API Explorer flag, since isolated builds link API pages to www.elastic.co (elastic/docs-eng-team#725).</remarks>
	public bool LegacyApiUrls => false;

	private static Uri PublishedBaseUri { get; } = new("https://www.elastic.co");

	public static string GetBranch(Uri crossLinkUri) => crossLinkUri.Scheme switch
	{
		"cloud" => "master",
		_ => "main"
	};
}

/// <summary>
/// Resolves cross-link URIs for codex-hosted repos.
/// When <paramref name="useRelativePaths"/> is <c>true</c> (codex/assembler builds), produces path-only <c>/r/{repo}/{path}</c> for htmx navigation.
/// When <c>false</c> (isolated builds), produces absolute <c>https://codex.elastic.dev/r/{repo}/{path}</c>.
/// </summary>
public class CodexAwareUriResolver(FrozenSet<string> codexRepositories, bool useRelativePaths = false) : IUriEnvironmentResolver
{
	private static readonly Uri CodexBaseUri = new("https://codex.elastic.dev");
	private static readonly IsolatedBuildEnvironmentUriResolver PublicResolver = new();

	public Uri Resolve(Uri crossLinkUri, string path) =>
		codexRepositories.Contains(crossLinkUri.Scheme)
			? useRelativePaths
				? new Uri($"/r/{crossLinkUri.Scheme}/{path}", UriKind.Relative)
				: new Uri(CodexBaseUri, $"r/{crossLinkUri.Scheme}/{path}")
			: PublicResolver.Resolve(crossLinkUri, path);

	public Uri ResolveSitePath(Uri crossLinkUri, string sitePath) => PublicResolver.ResolveSitePath(crossLinkUri, sitePath);

	/// <inheritdoc />
	public bool LegacyApiUrls => PublicResolver.LegacyApiUrls;
}
