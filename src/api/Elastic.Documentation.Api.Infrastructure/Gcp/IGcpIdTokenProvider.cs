// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Infrastructure.Gcp;

/// <summary>
/// Interface for generating GCP ID tokens.
/// Abstraction allows for testing and alternative implementations.
/// </summary>
public interface IGcpIdTokenProvider
{
	/// <summary>
	/// Generates an ID token for the specified service account and target audience.
	/// </summary>
	/// <param name="serviceAccount">Service account JSON key</param>
	/// <param name="targetAudience">Target audience URL</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>ID token</returns>
	Task<string> GenerateIdTokenAsync(string serviceAccount, string targetAudience, Cancel cancellationToken = default);
}
