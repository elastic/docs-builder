// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Portal.Navigation;

namespace Elastic.Portal.Category;

/// <summary>
/// View model for category index pages.
/// </summary>
public class CategoryViewModel(PortalRenderContext context) : PortalViewModel(context)
{
	/// <summary>
	/// The category navigation item.
	/// </summary>
	public required CategoryNavigation Category { get; init; }

	/// <summary>
	/// Documentation sets in this category.
	/// </summary>
	public IEnumerable<PortalDocumentationSetInfo> DocumentationSets =>
		PortalNavigation.DocumentationSetInfos.Where(ds => ds.Category == Category.CategoryName);
}
