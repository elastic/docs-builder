// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Elastic.Portal.Navigation;

namespace Elastic.Portal;

/// <summary>
/// Context for rendering portal pages.
/// </summary>
public record PortalRenderContext(
	BuildContext BuildContext,
	PortalNavigation PortalNavigation,
	StaticFileContentHashProvider StaticFileContentHashProvider
) : RenderContext<PortalNavigation>(BuildContext, PortalNavigation)
{
	/// <summary>
	/// Pre-rendered navigation HTML.
	/// </summary>
	public required string NavigationHtml { get; init; }

	/// <summary>
	/// The current navigation item being rendered.
	/// </summary>
	public required INavigationItem CurrentNavigation { get; init; }
}
