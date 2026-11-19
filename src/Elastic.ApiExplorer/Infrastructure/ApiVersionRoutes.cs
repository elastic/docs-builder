// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Where each version of one API is served. The default version owns <c>/api/{key}</c>, <c>main</c> otherwise lives at
/// <c>/api/{key}/next</c>, and each numeric major at <c>/api/{key}/v{N}</c>. A default numeric major is served twice:
/// at <c>/api/{key}</c> and at its own <c>/api/{key}/v{N}</c>.
/// </summary>
public sealed record ApiVersionRoutes(string ApiKey, string? DefaultMoniker)
{
	/// <summary>Routes for the versions an API declares: the newest numeric major is the default, else <c>main</c>, else the first.</summary>
	public static ApiVersionRoutes For(string apiKey, IEnumerable<string> monikers)
	{
		var all = monikers.ToList();
		return new(apiKey, ApiVersions.LatestMajor(all) ?? (all.Contains("main") ? "main" : all.FirstOrDefault()));
	}

	/// <summary>The URL suffix after <c>/api/</c> that links to <paramref name="moniker"/>.</summary>
	public string Suffix(string moniker) =>
		moniker == DefaultMoniker ? ApiKey : moniker == "main" ? $"{ApiKey}/{ApiUrlBuilder.NextSegment}" : $"{ApiKey}/v{moniker}";

	/// <summary>Every URL suffix that renders <paramref name="moniker"/>, the one <see cref="Suffix"/> returns first.</summary>
	public IReadOnlyList<string> Suffixes(string moniker) =>
		moniker == DefaultMoniker && ApiVersions.TryParseMajor(moniker) is not null ? [ApiKey, $"{ApiKey}/v{moniker}"] : [Suffix(moniker)];
}
