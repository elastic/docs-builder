// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// Marker for sidebar section headings with child links but no page of their own.
/// Nav V2 renders these as non-clickable labels (see <c>label:</c> in navigation-v2.yml and API <c>x-tagGroups</c>).
/// </summary>
public interface INavigationSidebarLabel;
