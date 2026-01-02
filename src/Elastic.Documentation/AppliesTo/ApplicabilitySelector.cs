// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.AppliesTo;

/// <summary>
/// Utility class for selecting the most relevant applicability from a collection of applicabilities.
/// </summary>
public static class ApplicabilitySelector
{
	/// <summary>
	/// Selects the most relevant applicability for display: available versions first (highest version), then closest future version
	/// </summary>
	/// <param name="applicabilities">The collection of applicabilities to select from</param>
	/// <param name="currentVersion">The current version to use for comparison</param>
	/// <returns>The most relevant applicability for display</returns>
	public static Applicability GetPrimaryApplicability(IReadOnlyCollection<Applicability> applicabilities, SemVersion currentVersion)
	{
		var availableApplicabilities = applicabilities
			.Where(a => a.Version is null || a.Version is AllVersionsSpec || a.Version.Min <= currentVersion).ToArray();

		if (availableApplicabilities.Length > 0)
		{
			return availableApplicabilities
				.OrderByDescending(a => a.Version?.Min ?? ZeroVersion.Instance)
				.ThenBy(a => ProductLifecycleInfo.GetOrder(a.Lifecycle))
				.First();
		}

		var futureApplicabilities = applicabilities
			.Where(a => a.Version is not null && a.Version is not AllVersionsSpec && a.Version.Min > currentVersion).ToArray();

		if (futureApplicabilities.Length > 0)
		{
			return futureApplicabilities
				.OrderBy(a => a.Version!.Min.CompareTo(currentVersion))
				.ThenBy(a => ProductLifecycleInfo.GetOrder(a.Lifecycle))
				.First();
		}

		return applicabilities.First();
	}
}
