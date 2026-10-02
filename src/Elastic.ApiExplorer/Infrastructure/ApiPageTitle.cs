// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Browser title for an API page. The heading stays the spec title. Search results use this string.
/// </summary>
internal static class ApiPageTitle
{
	internal const string CatalogTitle = "Elastic APIs";

	internal static string Format(string? pageLabel, string? displayName, string? specTitle, string? versionLabel)
	{
		var product = $"{ProductName(displayName, specTitle)} API documentation";
		if (versionLabel is not null)
			product = $"{product} ({versionLabel})";

		var label = pageLabel?.Trim();
		return string.IsNullOrEmpty(label) ? product : $"{label} | {product}";
	}

	internal static string LandingDescription(string? displayName, string? specTitle, string? versionLabel) =>
		$"{Format(null, displayName, specTitle, versionLabel)}.";

	/// <summary>Selected switcher label for a released major (<c>v8</c>, <c>v9</c>). Latest stays unlabeled.</summary>
	internal static string? ReleasedVersionLabel(IReadOnlyList<ApiVersionSwitcherItem> items)
	{
		foreach (var item in items)
		{
			if (item.Selected && IsReleasedLabel(item.Label))
				return item.Label;
		}

		return null;
	}

	internal static string ProductName(string? displayName, string? specTitle)
	{
		if (!string.IsNullOrWhiteSpace(displayName))
			return displayName.Trim();

		if (string.IsNullOrWhiteSpace(specTitle))
			return "API";

		var title = specTitle.Trim();
		if (title.EndsWith(" APIs", StringComparison.Ordinal))
			title = title[..^" APIs".Length].TrimEnd();
		else if (title.EndsWith(" API", StringComparison.Ordinal))
			title = title[..^" API".Length].TrimEnd();

		return title.Length == 0 ? "API" : title;
	}

	private static bool IsReleasedLabel(string label) => label.Length > 1 && label[0] == 'v' && char.IsAsciiDigit(label[1]);
}
