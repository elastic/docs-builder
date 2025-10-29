// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;

namespace Elastic.Documentation.Navigation.Isolated;

public record CrossLinkModel(Uri CrossLinkUri, string NavigationTitle) : IDocumentationFile;

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
	public CrossLinkModel Model { get; init; } = model;

	/// <inheritdoc />
	public string Url { get; init; } = url;

	/// <inheritdoc />
	public bool Hidden { get; init; } = hidden;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => homeAccessor.HomeProvider.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public string NavigationTitle => Model.NavigationTitle;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink => true;

}
