// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>A navigation item the sidebar strikes through when it is deprecated.</summary>
public interface IDeprecatableNavigationItem : INavigationItem
{
	bool Deprecated { get; }
}
