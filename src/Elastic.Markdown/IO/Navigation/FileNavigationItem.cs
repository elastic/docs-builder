// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Markdown.IO.Navigation;

[DebuggerDisplay("Current: {Model.RelativePath}")]
public record FileNavigationItem(MarkdownFile Model, DocumentationGroup Group, bool Hidden = false) : ILeafNavigationItem<MarkdownFile>
{
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = Group;
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = Group.NavigationRoot;
	public string Url => Model.Url;
	public string NavigationTitle => Model.NavigationTitle;
	public int NavigationIndex { get; set; }
}
