// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// Marker for a root whose sidebar opens with an Overview row in line with its first pages, with no rule under it,
/// instead of the separate root row. The API sidebar uses it so the overview sits with the guide pages.
/// </summary>
public interface ISidebarOverviewRowNavigationItem;
