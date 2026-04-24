// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// A placeholder folder node in the V2 navigation sidebar.
/// Has a URL pointing to a generated stub page; rendered greyed-out
/// to indicate the content is not yet finalised.
/// </summary>
public class PlaceholderNavigationNode : INodeNavigationItem<INavigationModel, INavigationItem>
{
	private readonly PlaceholderIndexLeaf _index;

	public PlaceholderNavigationNode(
		string title,
		string sitePrefix,
		IReadOnlyCollection<INavigationItem> children,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent
	)
	{
		Id = ShortId.Create("placeholder-group", title);
		NavigationTitle = title;
		Url = ComputeUrl(title, sitePrefix);
		NavigationItems = children;
		Parent = parent;
		NavigationRoot = parent?.NavigationRoot!;
		_index = new PlaceholderIndexLeaf(this);
	}

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public string Url { get; }

	/// <inheritdoc />
	public string NavigationTitle { get; }

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public ILeafNavigationItem<INavigationModel> Index => _index;

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	private static string ComputeUrl(string title, string sitePrefix)
	{
		var hash = ShortId.Create("placeholder", title);
		var prefix = string.IsNullOrEmpty(sitePrefix) ? "" : "/" + sitePrefix.Trim('/');
		return $"{prefix}/_placeholder/{hash}";
	}

	private sealed class PlaceholderIndexLeaf(PlaceholderNavigationNode owner)
		: ILeafNavigationItem<INavigationModel>, INavigationModel
	{
		public INavigationModel Model => this;
		public string Url => owner.Url;
		public string NavigationTitle => owner.NavigationTitle;
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => owner.NavigationRoot;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = owner;
		public bool Hidden => true;
		public int NavigationIndex { get; set; }
	}
}
