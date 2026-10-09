// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.LinkMenu;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Documentation.Site.Components.NavSide;

public sealed record NavSideModel
{
	public required NavigationRenderModel Navigation { get; init; }

	/// <summary>A menu of sibling sections, shown when the build has several top-level sections.</summary>
	public LinkMenuModel? SectionMenu { get; init; }
}
