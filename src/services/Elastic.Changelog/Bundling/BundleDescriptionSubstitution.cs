// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Helper for performing placeholder substitution in bundle descriptions.
/// Supports {version}, {lifecycle}, {owner}, and {repo} placeholders.
/// </summary>
internal static class BundleDescriptionSubstitution
{
	/// <summary>
	/// Substitutes placeholders in a description string with provided values.
	/// </summary>
	/// <param name="description">The description string containing placeholders</param>
	/// <param name="version">Version value for {version} placeholder</param>
	/// <param name="lifecycle">Lifecycle value for {lifecycle} placeholder</param>
	/// <param name="owner">Owner value for {owner} placeholder</param>
	/// <param name="repo">Repository value for {repo} placeholder</param>
	/// <param name="validateResolvable">If true, validates that all used placeholders can be resolved</param>
	/// <returns>Description with placeholders substituted</returns>
	/// <exception cref="InvalidOperationException">When validateResolvable is true and placeholders cannot be resolved</exception>
	public static string SubstitutePlaceholders(
		string description,
		string? version,
		string? lifecycle,
		string? owner,
		string? repo,
		bool validateResolvable = false)
	{
		if (string.IsNullOrEmpty(description))
			return description;

		if (validateResolvable)
		{
			var missingValues = new List<string>();
			if (description.Contains("{version}") && string.IsNullOrEmpty(version))
				missingValues.Add("version");
			if (description.Contains("{lifecycle}") && string.IsNullOrEmpty(lifecycle))
				missingValues.Add("lifecycle");
			if (description.Contains("{owner}") && string.IsNullOrEmpty(owner))
				missingValues.Add("owner");
			if (description.Contains("{repo}") && string.IsNullOrEmpty(repo))
				missingValues.Add("repo");

			if (missingValues.Count > 0)
				throw new InvalidOperationException($"Cannot resolve placeholders: {string.Join(", ", missingValues)}");
		}

		return description
			.Replace("{version}", version ?? string.Empty)
			.Replace("{lifecycle}", lifecycle ?? string.Empty)
			.Replace("{owner}", owner ?? string.Empty)
			.Replace("{repo}", repo ?? string.Empty);
	}
}
