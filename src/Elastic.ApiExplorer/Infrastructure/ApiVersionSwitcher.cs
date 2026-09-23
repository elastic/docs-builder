// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.ApiExplorer.Infrastructure;

public static class ApiVersionSwitcher
{
	public static IReadOnlyList<ApiVersionSwitcherItem> Build(
		string? urlPathPrefix,
		string apiKey,
		IReadOnlyList<ApiSpecVersion> versions,
		ApiSpecVersion current
	)
	{
		if (versions.Count <= 1)
			return [];

		return versions
			.OrderDescending()
			.Select(
				v => new ApiVersionSwitcherItem(
					Label: v.TryGetMajor(out var major) ? $"v{major}" : "latest",
					Url: $"{ApiUrlBuilder.ProductRoot(urlPathPrefix, ApiUrlBuilder.ProductSuffix(apiKey, v))}/",
					Selected: v == current
				)
			)
			.ToArray();
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
}

internal sealed record ApiVersionDropdownJsonItem(string Name, string? Href, bool Disabled, ApiVersionDropdownJsonItem[]? Children);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiVersionDropdownJsonItem[]))]
internal sealed partial class ApiVersionDropdownJsonContext : JsonSerializerContext;
