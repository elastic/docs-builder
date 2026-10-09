// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;

namespace Elastic.Documentation.Site.Components.LinkMenu;

/// <summary>Builds the native version menu from the JSON the page already carries for the legacy dropdown.</summary>
public static class VersionMenu
{
	public static LinkMenuModel? FromJson(string? json, string? currentVersion, string? allVersionsUrl)
	{
		if (string.IsNullOrWhiteSpace(json))
			return null;

		JsonDocument document;
		try
		{
			document = JsonDocument.Parse(json);
		}
		catch (JsonException)
		{
			return null;
		}

		using (document)
		{
			if (document.RootElement.ValueKind != JsonValueKind.Array)
				return null;

			var items = new List<LinkMenuItem>();
			foreach (var element in document.RootElement.EnumerateArray())
				Collect(element, currentVersion, items);

			if (items.Count == 0)
				return null;

			return new LinkMenuModel
			{
				Label = "Version",
				Current = currentVersion,
				Items = items,
				Footer = string.IsNullOrEmpty(allVersionsUrl) ? null : new LinkMenuItem("All versions", allVersionsUrl)
			};
		}
	}

	private static void Collect(JsonElement element, string? currentVersion, List<LinkMenuItem> items)
	{
		var name = element.TryGetProperty("name", out var n) ? n.GetString() : null;
		var href = element.TryGetProperty("href", out var h) && h.ValueKind == JsonValueKind.String ? h.GetString() : null;
		var disabled = element.TryGetProperty("disabled", out var d) && d.ValueKind == JsonValueKind.True;

		if (name is not null && href is not null && !disabled)
			items.Add(new LinkMenuItem(name, href, string.Equals(name, currentVersion, StringComparison.OrdinalIgnoreCase)));

		if (element.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
		{
			foreach (var child in children.EnumerateArray())
				Collect(child, currentVersion, items);
		}
	}
}
