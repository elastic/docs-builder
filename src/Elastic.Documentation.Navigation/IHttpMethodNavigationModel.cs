// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// Navigation model for an API operation leaf that exposes its HTTP method for sidebar chrome.
/// </summary>
public interface IHttpMethodNavigationModel : INavigationModel
{
	/// <summary>Lowercase HTTP method name (e.g. <c>get</c>, <c>post</c>).</summary>
	string HttpMethod { get; }
}
