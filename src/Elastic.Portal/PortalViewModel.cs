// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Portal.Navigation;

namespace Elastic.Portal;

/// <summary>
/// Base view model for portal pages.
/// </summary>
public abstract class PortalViewModel(PortalRenderContext context)
{
	/// <summary>
	/// Pre-rendered navigation HTML.
	/// </summary>
	public string NavigationHtml { get; } = context.NavigationHtml;

	/// <summary>
	/// Static file content hash provider for cache busting.
	/// </summary>
	public StaticFileContentHashProvider StaticFileContentHashProvider { get; } = context.StaticFileContentHashProvider;

	/// <summary>
	/// The current navigation item being rendered.
	/// </summary>
	public INavigationItem CurrentNavigationItem { get; } = context.CurrentNavigation;

	/// <summary>
	/// The portal navigation root.
	/// </summary>
	public PortalNavigation PortalNavigation { get; } = context.PortalNavigation;

	/// <summary>
	/// The build context.
	/// </summary>
	public BuildContext BuildContext { get; } = context.BuildContext;

	/// <summary>
	/// Creates the global layout model for the page.
	/// </summary>
	public GlobalLayoutViewModel CreateGlobalLayoutModel() =>
		new()
		{
			DocsBuilderVersion = ShortId.Create(BuildContext.Version),
			DocSetName = PortalNavigation.NavigationTitle,
			Description = "Documentation Portal",
			CurrentNavigationItem = CurrentNavigationItem,
			Previous = null,
			Next = null,
			NavigationHtml = NavigationHtml,
			NavigationFileName = string.Empty,
			UrlPathPrefix = BuildContext.UrlPathPrefix,
			AllowIndexing = BuildContext.AllowIndexing,
			CanonicalBaseUrl = BuildContext.CanonicalBaseUrl,
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Features = new FeatureFlags([]),
			StaticFileContentHashProvider = StaticFileContentHashProvider,
			RenderHamburgerIcon = false
		};
}
