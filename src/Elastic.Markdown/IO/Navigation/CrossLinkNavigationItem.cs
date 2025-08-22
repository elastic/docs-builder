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
	// Override Url accessor to use ResolvedUrl if available
	string INavigationItem.Url => ResolvedUrl ?? Url;
	public CrossLinkNavigationItem(string url, string title, DocumentationGroup group, bool hidden = false)
	{
		_url = url;
		NavigationTitle = title;
		Parent = group;
		NavigationRoot = group.NavigationRoot;
		Hidden = hidden;
	}

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	// Original URL from the cross-link
	private readonly string _url;

	// Store resolved URL for rendering
	public string? ResolvedUrl { get; set; }

	// Implement the INavigationItem.Url property to use ResolvedUrl if available
	public string Url => ResolvedUrl ?? _url; public string NavigationTitle { get; }
	public int NavigationIndex { get; set; }
	public bool Hidden { get; }
	public bool IsCrossLink => true; // This is always a cross-link
	public INavigationModel Model => null!; // Cross-link has no local model
}
