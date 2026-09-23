// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Navigation;
using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Infrastructure;

public static class ApiBreadcrumbs
{
	public const string CatalogLabel = "APIs";

	public static INavigationItem[] Build(INavigationItem current, string catalogUrl, string specName, bool onCatalog)
	{
		var parents = current.BreadcrumbParents();
		if (onCatalog)
			return parents;

		var crumbs = new List<INavigationItem>(parents.Length + 1)
		{
			new ApiBreadcrumbLink(catalogUrl, CatalogLabel, current.NavigationRoot)
		};
		var spec = new SpecCrumb(specName, current.NavigationRoot.Url);
		var emittedSpec = false;
		foreach (var parent in parents)
			emittedSpec = AddParent(crumbs, parent, spec, emittedSpec: emittedSpec);

		return [.. crumbs];
	}

	public static string CurrentPageName(INavigationItem current, string specName) =>
		IsSpecLanding(current) ? specName : current.NavigationTitle;

	private static bool AddParent(List<INavigationItem> crumbs, INavigationItem parent, SpecCrumb spec, bool emittedSpec)
	{
		if (!IsSpecRoot(parent, spec.RootUrl))
		{
			crumbs.Add(parent);
			return emittedSpec;
		}

		if (emittedSpec)
			return true;

		crumbs.Add(new ApiBreadcrumbLink(parent.Url, spec.Name, parent.NavigationRoot));
		return true;
	}

	private readonly record struct SpecCrumb(string Name, string RootUrl);

	private static bool IsSpecLanding(INavigationItem item) => item is LandingNavigationItem or ApiIndexLeafNavigation<ApiLanding>;

	private static bool IsSpecRoot(INavigationItem item, string specRootUrl) => IsSpecLanding(item) || SameUrl(item.Url, specRootUrl);

	private static bool SameUrl(string? left, string? right)
	{
		if (left is null || right is null)
			return false;
		return string.Equals(left.TrimEnd('/'), right.TrimEnd('/'), StringComparison.Ordinal);
	}
}

internal sealed class ApiBreadcrumbLink(
	string url,
	string navigationTitle,
	IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot
) : INavigationItem
{
	public string Url { get; } = url;
	public string NavigationTitle { get; } = navigationTitle;
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = navigationRoot;
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public bool Hidden => false;
	public int NavigationIndex { get; set; }
}
