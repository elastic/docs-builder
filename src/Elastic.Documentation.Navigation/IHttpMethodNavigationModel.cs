// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// Navigation model for an API operation leaf that exposes its HTTP method and path for sidebar chrome.
/// </summary>
public interface IHttpMethodNavigationModel : INavigationModel
{
	/// <summary>Lowercase HTTP method name (e.g. <c>get</c>, <c>post</c>).</summary>
	string HttpMethod { get; }

	/// <summary>The endpoint path template, with named placeholders for its dynamic segments (e.g. <c>/_ml/anomaly_detectors/{job_id}</c>).</summary>
	string HttpPath { get; }
}
