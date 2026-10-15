// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>Tells whether a repository publishes changelog entries to the CDN, before a bundle run depends on them.</summary>
public static class ChangelogPoolProbe
{
	/// <summary>
	/// Whether the registry of the pool exists on the CDN. <c>false</c> means it answered 404, so the repository is not
	/// onboarded. <c>null</c> means the answer is unknown (network error, other status, invalid pool name), and the
	/// bundler reports the real problem.
	/// </summary>
	public static async Task<bool?> ExistsAsync(HttpClient http, Uri baseUri, string owner, string repo, string branch, Cancel ctx)
	{
		if (!ChangelogKeys.IsValidOrg(owner) || !ChangelogKeys.IsValidRepo(repo) || !ChangelogKeys.IsValidBranch(branch))
			return null;

		var segments = new List<string>(ChangelogKeys.PoolSegments(owner, repo, branch)) { ChangelogKeys.RegistryFileName };
		var uri = new Uri($"{baseUri.AbsoluteUri.TrimEnd('/')}/{string.Join('/', segments.Select(Uri.EscapeDataString))}");

		try
		{
			using var request = new HttpRequestMessage(HttpMethod.Head, uri);
			using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctx).ConfigureAwait(false);
			return response.StatusCode switch
			{
				HttpStatusCode.NotFound => false,
				var status when (int)status is >= 200 and < 300 => true,
				_ => null
			};
		}
		catch (HttpRequestException)
		{
			return null;
		}
	}
}
