// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// Marker for a sidebar group that starts expanded on every page, not only on pages inside it.
/// Implement alongside <see cref="INodeNavigationItem{TIndex,TChildNavigation}"/>.
/// </summary>
public interface ISidebarExpandedNavigationItem;
