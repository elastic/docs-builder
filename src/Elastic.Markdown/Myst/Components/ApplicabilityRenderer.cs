// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Markdown.Myst.Components;

public class ApplicabilityRenderer
{
	public record ApplicabilityRenderData(
		string BadgeLifecycleText,
		string Version,
		string TooltipText,
		string LifecycleClass,
		string LifecycleName,
		bool ShowLifecycleName,
		bool ShowVersion,
		bool HasMultipleLifecycles = false
	);

	public ApplicabilityRenderData RenderApplicability(
		Applicability applicability,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem,
		AppliesCollection allApplications)
	{
		var lifecycleClass = applicability.GetLifeCycleName().ToLowerInvariant().Replace(" ", "-");
		var lifecycleFull = GetLifecycleFullText(applicability.Lifecycle);

		var tooltipText = BuildTooltipText(applicability, applicabilityDefinition, versioningSystem, lifecycleFull);
		var badgeLifecycleText = BuildBadgeLifecycleText(applicability, versioningSystem, allApplications);

		var showLifecycle = applicability.Lifecycle != ProductLifecycle.GenerallyAvailable && string.IsNullOrEmpty(badgeLifecycleText);

		// Determine if we should show version based on VersionSpec
		var showVersion = false;
		var versionDisplay = string.Empty;

		if (applicability.Version is not null && applicability.Version != AllVersionsSpec.Instance)
		{
			versionDisplay = GetBadgeVersionText(applicability.Version, versioningSystem);
			showVersion = !string.IsNullOrEmpty(versionDisplay);

			// Special handling for Removed lifecycle - don't show + suffix
			if (applicability.Lifecycle == ProductLifecycle.Removed &&
				applicability.Version.Kind == VersionSpecKind.GreaterThanOrEqual &&
				!string.IsNullOrEmpty(versionDisplay))
			{
				versionDisplay = versionDisplay.TrimEnd('+');
			}
		}

		return new ApplicabilityRenderData(
			BadgeLifecycleText: badgeLifecycleText,
			Version: versionDisplay,
			TooltipText: tooltipText,
			LifecycleClass: lifecycleClass,
			LifecycleName: applicability.GetLifeCycleName(),
			ShowLifecycleName: showLifecycle,
			ShowVersion: showVersion
		);
	}

	public ApplicabilityRenderData RenderCombinedApplicability(
		IEnumerable<Applicability> applicabilities,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem,
		AppliesCollection allApplications)
	{
		var applicabilityList = applicabilities.ToList();

		// Sort by lifecycle priority (GA > Beta > Preview > etc.) to determine display order
		var sortedApplicabilities = applicabilityList
			.OrderBy(a => GetLifecycleOrder(a.Lifecycle))
			.ThenByDescending(a => a.Version?.Min ?? new SemVersion(0, 0, 0))
			.ToList();

		var primaryLifecycle = sortedApplicabilities.First();

		var primaryRender = RenderApplicability(primaryLifecycle, applicabilityDefinition, versioningSystem, allApplications);

		// If the primary lifecycle returns an empty badge text (indicating "use previous lifecycle")
		// and we have multiple lifecycles, use the next lifecycle in priority order
		var applicabilityToDisplay = string.IsNullOrEmpty(primaryRender.BadgeLifecycleText) &&
									 string.IsNullOrEmpty(primaryRender.Version) &&
									 sortedApplicabilities.Count >= 2
			? sortedApplicabilities[1]
			: primaryLifecycle;

		var primaryRenderData = RenderApplicability(applicabilityToDisplay, applicabilityDefinition, versioningSystem, allApplications);
		var combinedTooltip = BuildCombinedTooltipText(applicabilityList, applicabilityDefinition, versioningSystem);

		// Check if there are multiple different lifecycles
		var hasMultipleLifecycles = applicabilityList.Select(a => a.Lifecycle).Distinct().Count() > 1;

		return primaryRenderData with
		{
			TooltipText = combinedTooltip,
			HasMultipleLifecycles = hasMultipleLifecycles,
			ShowLifecycleName = primaryRenderData.ShowLifecycleName || (string.IsNullOrEmpty(primaryRenderData.BadgeLifecycleText) && hasMultipleLifecycles)
		};
	}

	private static string BuildCombinedTooltipText(
		List<Applicability> applicabilities,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem)
	{
		var tooltipParts = new List<string>();

		// Order by the same logic as primary selection: available first (by version desc), then future (by version asc)
		var orderedApplicabilities = applicabilities
			.OrderByDescending(a => a.Version is null || a.Version is AllVersionsSpec ||
								   (a.Version is { } vs && vs.Min <= versioningSystem.Current) ? 1 : 0)
			.ThenByDescending(a => a.Version?.Min ?? new SemVersion(0, 0, 0))
			.ThenBy(a => a.Version?.Min ?? new SemVersion(0, 0, 0))
			.ToList();

		foreach (var applicability in orderedApplicabilities)
		{
			var lifecycleFull = GetLifecycleFullText(applicability.Lifecycle);
			var heading = CreateApplicabilityHeading(applicability, applicabilityDefinition);
			var tooltipText = BuildTooltipText(applicability, applicabilityDefinition, versioningSystem, lifecycleFull);
			// language=html
			tooltipParts.Add($"<div>{heading}{tooltipText}</div>");
		}

		return string.Join("\n\n", tooltipParts);
	}

	private static string CreateApplicabilityHeading(Applicability applicability, ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition)
	{
		var lifecycleName = applicability.GetLifeCycleName();
		var versionText = applicability.Version is not null ? $" {applicability.Version.Min}" : "";
		// language=html
		return $"""<strong>{applicabilityDefinition.DisplayName} {lifecycleName}{versionText}:</strong>""";
	}

	private static string GetLifecycleFullText(ProductLifecycle lifecycle) => lifecycle switch
	{
		ProductLifecycle.GenerallyAvailable => "Available",
		ProductLifecycle.Beta => "Available in beta",
		ProductLifecycle.TechnicalPreview => "Available in technical preview",
		ProductLifecycle.Deprecated => "Deprecated",
		ProductLifecycle.Removed => "Removed",
		ProductLifecycle.Unavailable => "Unavailable",
		_ => ""
	};

	private static string BuildTooltipText(
		Applicability applicability,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem,
		string lifecycleFull)
	{
		var tooltipText = "";

		// Check if a specific version is provided
		if (applicability.Version is not null && applicability.Version != AllVersionsSpec.Instance)
		{
			tooltipText = applicability.Version.Min <= versioningSystem.Current
				? $"{lifecycleFull} on {applicabilityDefinition.DisplayName} version {applicability.Version.Min} and later unless otherwise specified."
				: applicability.Lifecycle switch
				{
					ProductLifecycle.GenerallyAvailable
						or ProductLifecycle.Beta
						or ProductLifecycle.TechnicalPreview
						or ProductLifecycle.Planned =>
						$"We plan to add this functionality in a future {applicabilityDefinition.DisplayName} update. Subject to change.",
					ProductLifecycle.Deprecated =>
						$"We plan to deprecate this functionality in a future {applicabilityDefinition.DisplayName} update. Subject to change.",
					ProductLifecycle.Removed =>
						$"We plan to remove this functionality in a future {applicabilityDefinition.DisplayName} update. Subject to change.",
					_ => tooltipText
				};
		}
		else
		{
			// No version specified - check if we should show base version
			tooltipText = versioningSystem.Base.Major != AllVersionsSpec.Instance.Min.Major
				? applicability.Lifecycle switch
				{
					ProductLifecycle.Removed =>
						$"Removed in {applicabilityDefinition.DisplayName} {versioningSystem.Base.Major}.{versioningSystem.Base.Minor}.",
					_ =>
						$"{lifecycleFull} since {versioningSystem.Base.Major}.{versioningSystem.Base.Minor}."
				}
				: $"{lifecycleFull} on {applicabilityDefinition.DisplayName} unless otherwise specified.";
		}

		var disclaimer = GetDisclaimer(applicability.Lifecycle, versioningSystem.Id);
		if (disclaimer is not null)
			tooltipText = $"{tooltipText}\n\n{disclaimer}";

		return tooltipText;
	}

	private static string? GetDisclaimer(ProductLifecycle lifecycle, VersioningSystemId versioningSystemId) => lifecycle switch
	{
		ProductLifecycle.Beta =>
			"Beta features are subject to change. The design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features.",
		ProductLifecycle.TechnicalPreview =>
			"This functionality may be changed or removed in a future release. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features.",
		ProductLifecycle.GenerallyAvailable => versioningSystemId is VersioningSystemId.Stack
			? "If this functionality is unavailable or behaves differently when deployed on ECH, ECE, ECK, or a self-managed installation, it will be indicated on the page."
			: null,
		_ => null
	};

	private static string BuildBadgeLifecycleText(
		Applicability applicability,
		VersioningSystem versioningSystem,
		AppliesCollection allApplications)
	{
		var badgeText = "";
		var versionSpec = applicability.Version;

		if (versionSpec is not null && versionSpec != AllVersionsSpec.Instance)
		{
			var isMinReleased = versionSpec.Min <= versioningSystem.Current;
			var isMaxReleased = versionSpec.Max is not null && versionSpec.Max <= versioningSystem.Current;

			// Determine if we should show "Planned" badge
			var shouldShowPlanned = (versionSpec.Kind == VersionSpecKind.GreaterThanOrEqual && !isMinReleased)
									|| (versionSpec.Kind == VersionSpecKind.Range && !isMaxReleased && !isMinReleased)
									|| (versionSpec.Kind == VersionSpecKind.Exact && !isMinReleased);

			// Check lifecycle count for "use previous lifecycle" logic
			if (shouldShowPlanned)
			{
				var lifecycleCount = allApplications.Count;

				// If lifecycle count >= 2, we should use previous lifecycle instead of showing "Planned"
				if (lifecycleCount >= 2)
					return string.Empty;

				// Otherwise show planned badge (lifecycle count == 1)
				badgeText = applicability.Lifecycle switch
				{
					ProductLifecycle.TechnicalPreview => "Planned",
					ProductLifecycle.Beta => "Planned",
					ProductLifecycle.GenerallyAvailable => "Planned",
					ProductLifecycle.Deprecated => "Deprecation planned",
					ProductLifecycle.Removed => "Removal planned",
					ProductLifecycle.Planned => "Planned",
					ProductLifecycle.Unavailable => "Unavailable",
					_ => badgeText
				};
			}
		}

		return badgeText;
	}

	/// <summary>
	/// Gets the version to display in badges, handling VersionSpec kinds
	/// </summary>
	private static string GetBadgeVersionText(VersionSpec? versionSpec, VersioningSystem versioningSystem)
	{
		// When no version is specified, check if we should show the base version
		if (versionSpec is null || versionSpec == AllVersionsSpec.Instance)
		{
			if (versioningSystem.Base.Major != AllVersionsSpec.Instance.Min.Major)
				return $"{versioningSystem.Base.Major}.{versioningSystem.Base.Minor}+";

			// Otherwise, this is an unversioned product, show no version
			return string.Empty;
		}

		var kind = versionSpec.Kind;
		var min = versionSpec.Min;
		var max = versionSpec.Max;

		// Check if versions are released
		var minReleased = min <= versioningSystem.Current;
		var maxReleased = max is not null && max <= versioningSystem.Current;

		return kind switch
		{
			VersionSpecKind.GreaterThanOrEqual => minReleased
				? $"{min.Major}.{min.Minor}+"
				: string.Empty,

			VersionSpecKind.Range => maxReleased
				? $"{min.Major}.{min.Minor}-{max!.Major}.{max.Minor}"
				: minReleased
					? $"{min.Major}.{min.Minor}+"
					: string.Empty,

			VersionSpecKind.Exact => minReleased
				? $"{min.Major}.{min.Minor}"
				: string.Empty,

			_ => string.Empty
		};
	}
	private static int GetLifecycleOrder(ProductLifecycle lifecycle) => lifecycle switch
	{
		ProductLifecycle.GenerallyAvailable => 0,
		ProductLifecycle.Beta => 1,
		ProductLifecycle.TechnicalPreview => 2,
		ProductLifecycle.Planned => 3,
		ProductLifecycle.Deprecated => 4,
		ProductLifecycle.Removed => 5,
		ProductLifecycle.Unavailable => 6,
		_ => 999
	};

	/// <summary>
	/// Checks if a version should be considered released
	/// </summary>
	private static bool IsVersionReleased(SemVersion version, VersioningSystem versioningSystem) => version <= versioningSystem.Current;
}
