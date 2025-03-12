// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.CrossLinks;

public interface IUriEnvironmentResolver
{
	Uri Resolve(Uri crossLinkUri, string path);
}

public class PreviewEnvironmentUriResolver : IUriEnvironmentResolver
{
	private static Uri BaseUri { get; } = new("https://docs-v3-preview.elastic.dev");

	public Uri Resolve(Uri crossLinkUri, string path)
	{
		var branch = GetBranch(crossLinkUri);
		return new Uri(BaseUri, $"elastic/{crossLinkUri.Scheme}/tree/{branch}/{path}");
	}

	/// Hardcoding these for now, we'll have an index.json pointing to all links.json files
	/// at some point from which we can query the branch soon.
	private static string GetBranch(Uri crossLinkUri)
	{
		var branch = crossLinkUri.Scheme switch
		{
			"docs-content" => "main",
			_ => "main"
		};
		return branch;
	}


}
