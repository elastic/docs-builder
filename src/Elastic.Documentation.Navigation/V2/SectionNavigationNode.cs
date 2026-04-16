// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// A top-level section that owns an independent sidebar nav tree.
/// Unlike <see cref="LabelNavigationNode"/>, a section has a real URL (the tab link)
/// and an <see cref="Isolated"/> flag that controls whether it appears in the top bar.
/// </summary>
public class SectionNavigationNode : INodeNavigationItem<INavigationModel, INavigationItem>
{
	private readonly SectionIndexLeaf _index;

	public SectionNavigationNode(
		string label,
		string url,
		bool isolated,
		IReadOnlyCollection<INavigationItem> children,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent
	)
	{
		Id = ShortId.Create("section", label);
		NavigationTitle = label;
		Url = url;
		Isolated = isolated;
		NavigationItems = children;
		Parent = parent;
		NavigationRoot = parent?.NavigationRoot!;
		_index = new SectionIndexLeaf(this);
	}

	/// <summary>Whether this section is excluded from the top bar and renders with a back arrow.</summary>
	public bool Isolated { get; }

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

	private sealed class SectionIndexLeaf(SectionNavigationNode owner)
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
