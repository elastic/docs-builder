// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Navigation;

namespace Elastic.ApiExplorer.Infrastructure;

public sealed class ApiVersionSwitcherContext
{
	private readonly Dictionary<PageTargetKey, NavigationDropdownItem[]> _itemsByTarget = new();
	private readonly IReadOnlyList<string> _orderedMonikers;
	private readonly IReadOnlyDictionary<string, string> _productRootByMoniker;

	public ApiVersionSwitcherContext(
		string apiKey,
		IReadOnlyList<string> monikers,
		string currentMoniker,
		int currentMajor,
		ApiCrossVersionPageIndex crossVersionIndex,
		string? urlPathPrefix)
	{
		ApiKey = apiKey;
		CurrentMoniker = currentMoniker;
		CurrentMajor = currentMajor;
		CrossVersionIndex = crossVersionIndex;
		UrlPathPrefix = urlPathPrefix;
		_orderedMonikers = [.. monikers.OrderByDescending(m => m == "main" ? int.MaxValue : ParseMajor(m))];
		_productRootByMoniker = _orderedMonikers.ToDictionary(
			m => m,
			m => ApiUrlBuilder.ProductRoot(urlPathPrefix, ApiUrlBuilder.ProductSuffix(apiKey, m)));
	}

	public string ApiKey { get; }
	public string CurrentMoniker { get; }
	public int CurrentMajor { get; }
	public ApiCrossVersionPageIndex CrossVersionIndex { get; }
	public string? UrlPathPrefix { get; }
	public bool HasMultipleVersions => _orderedMonikers.Count > 1;

	public IReadOnlyList<NavigationDropdownItem> GetItems(ApiPageVersionTarget? pageTarget)
	{
		if (!HasMultipleVersions)
			return [];

		var key = PageTargetKey.From(pageTarget);
		if (_itemsByTarget.TryGetValue(key, out var cached))
			return cached;

		var items = _orderedMonikers
			.Select(m => new NavigationDropdownItem(
				NavigationTitle: FormatLabel(m, CurrentMajor),
				Url: BuildTargetUrl(m, pageTarget),
				IsActive: m == CurrentMoniker))
			.ToArray();
		_itemsByTarget[key] = items;
		return items;
	}

	private string BuildTargetUrl(string targetMoniker, ApiPageVersionTarget? pageTarget)
	{
		var productRoot = _productRootByMoniker[targetMoniker];
		if (pageTarget is null || !CrossVersionIndex.Contains(pageTarget, targetMoniker))
			return $"{productRoot}/";

		return ApiUrlBuilder.PageUrl(productRoot, pageTarget);
	}

	private static string FormatLabel(string moniker, int currentMajor) =>
		moniker == "main"
			? $"{currentMajor}.x (latest)"
			: $"{moniker}.x";

	private static int ParseMajor(string moniker) =>
		int.TryParse(moniker, out var major) ? major : 0;

	private readonly record struct PageTargetKey(ApiPageVersionTargetKind? Kind, string? Identity)
	{
		public static PageTargetKey From(ApiPageVersionTarget? pageTarget) =>
			pageTarget is null
				? new PageTargetKey(null, null)
				: new PageTargetKey(pageTarget.Kind, pageTarget.Identity);
	}
}
