// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// A disabled placeholder link in the V2 navigation sidebar.
/// Has a title but no URL — rendered with <c>cursor-not-allowed</c>.
/// </summary>
public class PlaceholderNavigationLeaf(
	string title,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent
) : ILeafNavigationItem<INavigationModel>, INavigationModel
{
	/// <inheritdoc />
	public INavigationModel Model => this;

	/// <inheritdoc />
	public string Url => string.Empty;

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
}
