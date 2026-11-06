// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Browser title for an API page. The heading stays the spec title. Search results use this string.
/// </summary>
internal static class ApiPageTitle
{
	internal static string Format(string? pageLabel, string? displayName, string? specTitle, string? versionLabel)
	{
		var product = $"{ProductName(displayName, specTitle)} API docs";
		if (versionLabel is not null)
			product = $"{product} ({versionLabel})";

		var label = pageLabel?.Trim();
		return string.IsNullOrEmpty(label) ? product : $"{label} | {product}";
	}

	/// <summary>The landing heading: the spec title, plus the version label on a released major so v8 and v9 read differently.</summary>
	internal static string Heading(string? title, string? versionLabel)
	{
		var heading = title ?? "";
		_ = versionLabel;
		return heading;
	}

	/// <summary>Selected switcher label for a released major (<c>v8</c>, <c>v9</c>). Latest stays unlabeled.</summary>
	internal static string? ReleasedVersionLabel(IReadOnlyList<ApiVersionSwitcherItem> items)
	{
		foreach (var item in items)
		{
			var label = item.ShortLabel ?? item.Label;
			if (item.Selected && IsReleasedLabel(label))
				return label;
		}

		return null;
	}

	/// <summary>
	/// The product display name, unless the spec title names something else. Several APIs can bind to one product
	/// (Cloud Billing and Cloud Hosted both map to Elastic Cloud Hosted), and a title that does not mention the
	/// display name carries the API's own identity.
	/// </summary>
	internal static string ProductName(string? displayName, string? specTitle)
	{
		var fromSpec = FromSpecTitle(specTitle);
		if (string.IsNullOrWhiteSpace(displayName))
			return fromSpec;

		var product = displayName.Trim();
		var specNamesOtherApi = fromSpec != "API" && !fromSpec.Contains(product, StringComparison.OrdinalIgnoreCase);
		return specNamesOtherApi ? fromSpec : product;
	}

	private static string FromSpecTitle(string? specTitle)
	{
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
