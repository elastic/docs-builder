// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// Marker for API navigation rows that group multiple HTTP operations under one logical endpoint.
/// Nav V2 shows a neutral multi-method badge (grid) instead of a single HTTP-method glyph.
/// </summary>
public interface IMultiOperationNavigationItem : INavigationItem;
