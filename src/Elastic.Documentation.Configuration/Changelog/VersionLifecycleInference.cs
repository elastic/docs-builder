// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Utility for inferring lifecycle from version prerelease monikers.
/// </summary>
public static class VersionLifecycleInference
{
	/// <summary>
	/// Infers the lifecycle from a semantic version string.
	/// </summary>
	/// <param name="version">The version string (e.g., "1.0.0", "1.0.0-beta.1", "1.0.0-alpha")</param>
	/// <returns>The inferred lifecycle string: "ga", "beta", or "preview"</returns>
	public static string InferLifecycle(string version)
	{
		// Parse semver prerelease
		var dashIndex = version.IndexOf('-');
		if (dashIndex < 0)
			return "ga"; // No prerelease = GA

		var prerelease = version[(dashIndex + 1)..].ToLowerInvariant();

		// Extract first segment before '.' if present
		var dotIndex = prerelease.IndexOf('.');
		var tag = dotIndex >= 0 ? prerelease[..dotIndex] : prerelease;

		return tag switch
		{
			"beta" => "beta",
			"alpha" => "preview",
			"preview" => "preview",
			"rc" => "ga", // Release candidate = GA
			_ => "preview" // Unknown prerelease = preview
		};
	}
}
