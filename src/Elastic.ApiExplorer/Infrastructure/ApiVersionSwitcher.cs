// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.ApiExplorer.Infrastructure;

public static class ApiVersionSwitcher
{
	public static IReadOnlyList<ApiVersionSwitcherItem> Build(
		string? urlPathPrefix,
		string apiKey,
		IReadOnlyList<string> monikers,
		string currentMoniker,
		VersioningSystem? versioning = null
	)
	{
		if (monikers.Count <= 1)
			return [];

		var currentLabel = CurrentVersionLabel(versioning, monikers) ?? "Latest";
		return monikers
			.OrderByDescending(m => m == "main" ? int.MaxValue : ParseMajor(m))
			.Select(
				m => new ApiVersionSwitcherItem(
					Label: m == "main" ? currentLabel : $"{m}.x",
					Url: $"{ApiUrlBuilder.ProductRoot(urlPathPrefix, ApiUrlBuilder.ProductSuffix(apiKey, m))}/",
					Selected: m == currentMoniker
				)
			)
			.ToArray();
	}

	/// <summary>
	/// Docs button label is <c>v{this} (Current)</c>. Prefer the product's versioning base
	/// (<c>9.0+</c> for stack), then the highest numeric OpenAPI moniker.
	/// </summary>
	public static string? CurrentVersionLabel(VersioningSystem? versioning, IReadOnlyList<string> monikers)
	{
		if (versioning is { IsVersionless: false })
			return $"{versioning.Base.Major}.{versioning.Base.Minor}+";

		var major = 0;
		foreach (var moniker in monikers)
		{
			if (int.TryParse(moniker, out var parsed) && parsed > major)
				major = parsed;
		}

		return major > 0 ? $"{major}.0+" : null;
	}

	public static string? CurrentVersionLabel(VersioningSystem? versioning, IReadOnlyList<ApiVersionSwitcherItem> items)
	{
		if (versioning is { IsVersionless: false })
			return $"{versioning.Base.Major}.{versioning.Base.Minor}+";

		foreach (var item in items)
		{
			if (item.Label.EndsWith('+'))
				return item.Label;
		}

		return null;
	}

	public static string SerializeDropdownItems(IReadOnlyList<ApiVersionSwitcherItem> items)
	{
		if (items.Count == 0)
			return "[]";

		var payload = items.Select(static i => new ApiVersionDropdownJsonItem(i.Label, i.Url, false, null)).ToArray();
		return JsonSerializer.Serialize(payload, ApiVersionDropdownJsonContext.Default.ApiVersionDropdownJsonItemArray);
	}

	private static int ParseMajor(string moniker) => int.TryParse(moniker, out var major) ? major : 0;
}

internal sealed record ApiVersionDropdownJsonItem(string Name, string? Href, bool Disabled, ApiVersionDropdownJsonItem[]? Children);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiVersionDropdownJsonItem[]))]
internal sealed partial class ApiVersionDropdownJsonContext : JsonSerializerContext;
