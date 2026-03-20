// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// A placeholder folder node in the V2 navigation sidebar.
/// Has children and a chevron toggle but no real URL — rendered with <c>cursor-not-allowed</c>.
/// </summary>
public class PlaceholderNavigationNode : INodeNavigationItem<INavigationModel, INavigationItem>
{
	private readonly PlaceholderIndexLeaf _index;

	public PlaceholderNavigationNode(
		string title,
		IReadOnlyCollection<INavigationItem> children,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent
	)
	{
		Id = ShortId.Create("placeholder-group", title);
		NavigationTitle = title;
		NavigationItems = children;
		Parent = parent;
		NavigationRoot = parent?.NavigationRoot!;
		_index = new PlaceholderIndexLeaf(this);
	}

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public string Url => string.Empty;

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

	private sealed class PlaceholderIndexLeaf(PlaceholderNavigationNode owner)
		: ILeafNavigationItem<INavigationModel>, INavigationModel
	{
		public INavigationModel Model => this;
		public string Url => string.Empty;
		public string NavigationTitle => owner.NavigationTitle;
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => owner.NavigationRoot;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = owner;
		public bool Hidden => true;
		public int NavigationIndex { get; set; }
	}
}
