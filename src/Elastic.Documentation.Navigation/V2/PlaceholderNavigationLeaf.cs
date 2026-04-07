// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// A placeholder link in the V2 navigation sidebar.
/// Has a URL pointing to a generated stub page but is rendered greyed-out
/// to indicate the content is not yet finalised.
/// </summary>
public class PlaceholderNavigationLeaf(
	string title,
	string sitePrefix,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent
) : ILeafNavigationItem<INavigationModel>, INavigationModel
{
	/// <inheritdoc />
	public INavigationModel Model => this;

	/// <inheritdoc />
	public string Url { get; } = ComputeUrl(title, sitePrefix);

	/// <inheritdoc />
	public string NavigationTitle { get; } = title;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = parent?.NavigationRoot!;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	private static string ComputeUrl(string title, string sitePrefix)
	{
		var hash = ShortId.Create("placeholder", title);
		var prefix = string.IsNullOrEmpty(sitePrefix) ? "" : "/" + sitePrefix.Trim('/');
		return $"{prefix}/_placeholder/{hash}";
	}
}
