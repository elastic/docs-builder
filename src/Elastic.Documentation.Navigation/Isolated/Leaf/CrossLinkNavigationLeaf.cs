// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;

namespace Elastic.Documentation.Navigation.Isolated.Leaf;

/// <summary>
/// Represents a cross-link to an external documentation resource.
/// </summary>
/// <param name="CrossLinkUri">The URI pointing to the external resource</param>
/// <param name="NavigationTitle">The title to display in navigation</param>
public record CrossLinkModel(Uri CrossLinkUri, string NavigationTitle) : IDocumentationFile
{
	/// <inheritdoc />
	public string Title => NavigationTitle;

	/// <inheritdoc />
	public string? Description => null;
}

[DebuggerDisplay("{Url}")]
public class CrossLinkNavigationLeaf(
	CrossLinkModel model,
	string url,
	bool hidden,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent,
	INavigationHomeAccessor homeAccessor
)
	: ILeafNavigationItem<CrossLinkModel>
{
	/// <inheritdoc />
	public CrossLinkModel Model { get; } = model;

	/// <inheritdoc />
	public string Url { get; } = url;

	/// <inheritdoc />
	public bool Hidden { get; } = hidden;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => homeAccessor.HomeProvider.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public string NavigationTitle => Model.NavigationTitle;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

}
