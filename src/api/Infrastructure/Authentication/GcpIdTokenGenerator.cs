// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Core.Interfaces;
using System.Text.Json;

namespace Infrastructure.Authentication;

public class GcpIdTokenGenerator : IGcpTokenGenerator
{
	public Task<string> GenerateIdTokenAsync(string serviceAccountKeyPath, string audienceUrl, CancellationToken cancellationToken = default)
	{
		// TODO: Implement GCP ID token generation using AOT-compatible approach
		// This should mirror the logic from the original GcpIdTokenGenerator class
		// For now, returning a placeholder
		throw new NotImplementedException("GCP ID token generation needs to be implemented");
	}
}
