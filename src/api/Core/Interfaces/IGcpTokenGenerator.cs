// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Core.Interfaces;

public interface IGcpTokenGenerator
{
	Task<string> GenerateIdTokenAsync(string serviceAccountKeyPath, string audienceUrl, CancellationToken cancellationToken = default);
}
