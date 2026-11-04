// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Landing;

/// <summary>The grouping model of a sidebar heading. It has no page, so it renders nothing.</summary>
public record ApiSidebarSection(string Name) : IApiGroupingModel
{
	public Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default) => Task.CompletedTask;
}

/// <summary>
/// A top-level group of the API sidebar, such as Topics or Types: a plain heading with no page of its own. Like
/// <see cref="ClassificationNavigationItem"/>, it points at the product overview, so breadcrumbs fold it into the spec crumb.
/// </summary>
public abstract class ApiSidebarHeadingNavigationItem(
	string title,
	LandingNavigationItem rootNavigation
) : ApiGroupingNavigationItem<ApiSidebarSection, INavigationItem>(
	new ApiSidebarSection(title),
	rootNavigation,
	rootNavigation
), ISidebarHeadingNavigationItem
{
	public LandingNavigationItem Landing => rootNavigation;

	public override string Url => rootNavigation.Index.Url;

	public override string NavigationTitle { get; } = title;

	public override string Id { get; } = ShortId.Create("api-section", title);
}
