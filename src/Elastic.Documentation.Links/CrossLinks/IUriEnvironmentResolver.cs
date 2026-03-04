// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;

namespace Elastic.Documentation.Links.CrossLinks;

public interface IUriEnvironmentResolver
{
	Uri Resolve(Uri crossLinkUri, string path);
}

public class IsolatedBuildEnvironmentUriResolver : IUriEnvironmentResolver
{
	private static Uri BaseUri { get; } = new("https://docs-v3-preview.elastic.dev");

	public Uri Resolve(Uri crossLinkUri, string path) =>
		new(BaseUri, $"elastic/{crossLinkUri.Scheme}/tree/{GetBranch(crossLinkUri)}/{path}");

	public static string GetBranch(Uri crossLinkUri) =>
		crossLinkUri.Scheme switch
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
}
