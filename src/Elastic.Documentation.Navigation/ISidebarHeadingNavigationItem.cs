// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// Marker for sidebar headings that group children without a dedicated page of their own.
/// Nav V2 renders these as label spans (<c>docs-sidebar-nav-v2__label--*</c>), not accordion folder links.
/// </summary>
/// <remarks>
/// Used by docs <c>label:</c> nodes and API OpenAPI <c>x-tagGroups</c> classifications.
/// Implement alongside <see cref="INodeNavigationItem{TIndex,TChildNavigation}"/>.
/// </remarks>
public interface ISidebarHeadingNavigationItem;
