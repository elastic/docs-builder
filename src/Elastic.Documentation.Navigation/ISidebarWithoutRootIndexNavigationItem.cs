// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// Marker for a root whose sidebar has no row for its own index page, because another control already leads there.
/// The API sidebar's product switcher does that, so its tree starts at the first group.
/// </summary>
public interface ISidebarWithoutRootIndexNavigationItem;
