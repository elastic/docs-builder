// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Navigation;

namespace Elastic.Documentation.Site.Navigation;

/// <summary>
/// Navigation item representing an external URL in the TOC.
/// </summary>
public record ExternalLinkNavigationItem(string ExternalUrl, string Title, INodeNavigationItem<INavigationModel, INavigationItem> Parent, IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot)
	: INavigationItem
{
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = Parent;

	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = NavigationRoot;

	public string Url => ExternalUrl;

	public string NavigationTitle => Title;

	public int NavigationIndex { get; set; }

	public bool Hidden => false;
}
