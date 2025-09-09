// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Markdown.IO.Navigation;

[DebuggerDisplay("CrossLink: {Url}")]
public record CrossLinkNavigationItem : ILeafNavigationItem<INavigationModel>
{
	public CrossLinkNavigationItem(Uri crossLinkUri, Uri resolvedUrl, string title, DocumentationGroup group, bool hidden = false)
	{
		CrossLink = crossLinkUri;
		Url = resolvedUrl.ToString();
		NavigationTitle = title;
		Parent = group;
		NavigationRoot = group.NavigationRoot;
		Hidden = hidden;
	}

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	public Uri CrossLink { get; }
	public string Url { get; }
	public string NavigationTitle { get; }
	public int NavigationIndex { get; set; }
	public bool Hidden { get; }
	public bool IsCrossLink => true; // This is always a cross-link
	public INavigationModel Model => null!; // Cross-link has no local model
}
