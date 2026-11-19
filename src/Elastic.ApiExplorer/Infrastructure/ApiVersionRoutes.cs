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
	/// <summary>Routes for the versions an API declares, with <see cref="ApiVersions.DefaultTree"/> as the default.</summary>
	public static ApiVersionRoutes For(string apiKey, IEnumerable<string> monikers) => new(apiKey, ApiVersions.DefaultTree(monikers));

	/// <summary>The URL suffix after <c>/api/</c> that links to <paramref name="moniker"/>.</summary>
	public string Suffix(string moniker) => moniker == DefaultMoniker ? ApiKey : Numbered(moniker);

	/// <summary>The second URL suffix that renders <paramref name="moniker"/>: the numbered copy of a default numeric major.</summary>
	public string? MirrorSuffix(string moniker) =>
		moniker == DefaultMoniker && ApiVersions.TryParseMajor(moniker) is not null ? Numbered(moniker) : null;

	/// <summary>Every URL suffix that renders <paramref name="moniker"/>: <see cref="Suffix"/>, then <see cref="MirrorSuffix"/> when there is one.</summary>
	public IReadOnlyList<string> Suffixes(string moniker) =>
		MirrorSuffix(moniker) is { } mirror ? [Suffix(moniker), mirror] : [Suffix(moniker)];

	private string Numbered(string moniker) => moniker == "main" ? $"{ApiKey}/{ApiUrlBuilder.NextSegment}" : $"{ApiKey}/v{moniker}";
}
