// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Landing;

public sealed record LandingInfoNote(IReadOnlyList<OpenApiServer> Servers, string? LicenseName, Uri? LicenseUrl, string? Version)
{
	public static LandingInfoNote From(OpenApiDocument document)
	{
		var info = document.Info;
		IReadOnlyList<OpenApiServer> servers = document.Servers is { Count: > 0 } ? [.. document.Servers] : [];
		var licenseName = string.IsNullOrEmpty(info?.License?.Name) ? null : info.License.Name;
		var version = string.IsNullOrEmpty(info?.Version) ? null : info.Version;
		return new(servers, licenseName, info?.License?.Url, version);
	}
}

public class LandingViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiLanding Landing { get; init; }
	public required OpenApiInfo ApiInfo { get; init; }
	public required LandingInfoNote InfoNote { get; init; }

	/// <summary>Flattened overview table rows; built before the slice renders.</summary>
	public required IReadOnlyList<ApiOverviewRow> OverviewRows { get; init; }
}
