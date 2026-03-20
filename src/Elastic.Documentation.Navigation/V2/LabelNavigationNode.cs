// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// A non-clickable section heading in the V2 navigation sidebar.
/// Has children but no URL of its own.
/// </summary>
public class LabelNavigationNode : INodeNavigationItem<INavigationModel, INavigationItem>
{
	private readonly LabelIndexLeaf _index;

	public LabelNavigationNode(
		string label,
		bool expandedByDefault,
		IReadOnlyCollection<INavigationItem> children,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent
	)
	{
		Id = ShortId.Create("label");
		NavigationTitle = label;
		ExpandedByDefault = expandedByDefault;
		NavigationItems = children;
		Parent = parent;
		NavigationRoot = parent?.NavigationRoot!;
		_index = new LabelIndexLeaf(this);
	}

	/// <summary>Whether this label section starts expanded by default.</summary>
	public bool ExpandedByDefault { get; }

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

	private sealed class LabelIndexLeaf(LabelNavigationNode owner)
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
