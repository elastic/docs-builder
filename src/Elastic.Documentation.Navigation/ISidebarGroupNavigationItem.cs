// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// A sidebar heading that labels the items under it without folding them away: no toggle, no page, always open.
/// Used by the API sidebar's Topics, Operations and Types.
/// </summary>
public interface ISidebarGroupNavigationItem : ISidebarHeadingNavigationItem;
