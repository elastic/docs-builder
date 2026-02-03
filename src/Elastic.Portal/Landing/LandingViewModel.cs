// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Portal.Navigation;

namespace Elastic.Portal.Landing;

/// <summary>
/// View model for the portal landing page.
/// </summary>
public class LandingViewModel(PortalRenderContext context) : PortalViewModel(context)
{
	/// <summary>
	/// The portal index page model.
	/// </summary>
	public required PortalIndexPage IndexPage { get; init; }

	/// <summary>
	/// Information about all documentation sets grouped by category.
	/// </summary>
	public ILookup<string?, PortalDocumentationSetInfo> DocumentationSetsByCategory =>
		PortalNavigation?.DocumentationSetInfos?.ToLookup(ds => ds.Category)
		?? Array.Empty<PortalDocumentationSetInfo>().ToLookup(ds => ds.Category);
}
