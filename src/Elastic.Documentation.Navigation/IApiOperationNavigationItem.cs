// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// An API operation leaf that exposes its HTTP methods and deprecation for sidebar chrome. It is the page's
/// navigation item rather than its model, because the methods of operations collapsed onto the page live there.
/// </summary>
public interface IApiOperationNavigationItem : IDeprecatableNavigationItem
{
	/// <summary>Lowercase HTTP method names (e.g. <c>post</c>, <c>get</c>), the page's dominant method first.</summary>
	IReadOnlyList<string> HttpMethods { get; }
}
