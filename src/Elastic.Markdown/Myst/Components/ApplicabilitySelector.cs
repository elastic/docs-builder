// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Markdown.Myst.Components;

/// <summary>
/// Utility class for selecting the most relevant applicability from a collection of applicabilities.
/// </summary>
public static class ApplicabilitySelector
{
	/// <summary>
	/// Selects the most relevant applicability for display: available versions first (highest version), then closest future version
	/// </summary>
	/// <param name="applicabilities">The collection of applicabilities to select from</param>
	/// <param name="versioningSystem">The versioning system to use for comparison</param>
	/// <returns>The most relevant applicability for display</returns>
	public static Applicability GetPrimaryApplicability(IEnumerable<Applicability> applicabilities, VersioningSystem versioningSystem)
	{
		var applicabilityList = applicabilities.ToList();
		var lifecycleOrder = new Dictionary<ProductLifecycle, int>
		{
			[ProductLifecycle.GenerallyAvailable] = 0,
			[ProductLifecycle.Beta] = 1,
			[ProductLifecycle.TechnicalPreview] = 2,
			[ProductLifecycle.Planned] = 3,
			[ProductLifecycle.Deprecated] = 4,
			[ProductLifecycle.Removed] = 5,
			[ProductLifecycle.Unavailable] = 6
		};

		var availableApplicabilities = applicabilityList
			.Where(a => a.Version is null || a.Version is AllVersions || a.Version <= versioningSystem.Current)
			.ToList();

		if (availableApplicabilities.Count != 0)
		{
			return availableApplicabilities
				.OrderByDescending(a => a.Version ?? new SemVersion(0, 0, 0))
				.ThenBy(a => lifecycleOrder.GetValueOrDefault(a.Lifecycle, 999))
				.First();
		}

		var futureApplicabilities = applicabilityList
			.Where(a => a.Version is not null && a.Version is not AllVersions && a.Version > versioningSystem.Current)
			.ToList();

		if (futureApplicabilities.Count != 0)
		{
			return futureApplicabilities
				.OrderBy(a => a.Version!.CompareTo(versioningSystem.Current))
				.ThenBy(a => lifecycleOrder.GetValueOrDefault(a.Lifecycle, 999))
				.First();
		}

		return applicabilityList.First();
	}
}
