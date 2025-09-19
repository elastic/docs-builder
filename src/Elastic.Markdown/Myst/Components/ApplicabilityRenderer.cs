// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
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
		bool ShowLifecycleName,
		bool ShowVersion
	);

	public ApplicabilityRenderData RenderApplicability(
		Applicability applicability,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem,
		AppliesCollection allApplications)
	{
		var lifecycleClass = applicability.GetLifeCycleName().ToLowerInvariant().Replace(" ", "-");
		var lifecycleFull = GetLifecycleFullText(applicability.Lifecycle);
		var realVersion = TryGetRealVersion(applicability, out var v) ? v : null;

		var tooltipText = BuildTooltipText(applicability, applicabilityDefinition, versioningSystem, realVersion, lifecycleFull);
		var badgeLifecycleText = BuildBadgeLifecycleText(applicability, versioningSystem, realVersion, allApplications);

		var showLifecycle = applicability.Lifecycle != ProductLifecycle.GenerallyAvailable && string.IsNullOrEmpty(badgeLifecycleText);
		var showVersion = applicability.Version is not null and not AllVersions && versioningSystem.Current >= applicability.Version;
		var version = applicability.Version?.ToString() ?? "";
		return new ApplicabilityRenderData(
			BadgeLifecycleText: badgeLifecycleText,
			Version: version,
			TooltipText: tooltipText,
			LifecycleClass: lifecycleClass,
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
		var primaryApplicability = ApplicabilitySelector.GetPrimaryApplicability(applicabilityList, versioningSystem.Current);

		var primaryRenderData = RenderApplicability(primaryApplicability, applicabilityDefinition, versioningSystem, allApplications);
		var combinedTooltip = BuildCombinedTooltipText(applicabilityList, applicabilityDefinition, versioningSystem);

		return primaryRenderData with { TooltipText = combinedTooltip };
	}


	private static string BuildCombinedTooltipText(
		List<Applicability> applicabilities,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem)
	{
		var tooltipParts = new List<string>();

		// Order by the same logic as primary selection: available first (by version desc), then future (by version asc)
		var orderedApplicabilities = applicabilities
			.OrderByDescending(a => a.Version is null || a.Version is AllVersions || a.Version <= versioningSystem.Current ? 1 : 0)
			.ThenByDescending(a => a.Version ?? new SemVersion(0, 0, 0))
			.ThenBy(a => a.Version ?? new SemVersion(0, 0, 0))
			.ToList();

		foreach (var applicability in orderedApplicabilities)
		{
			var realVersion = TryGetRealVersion(applicability, out var v) ? v : null;
			var lifecycleFull = GetLifecycleFullText(applicability.Lifecycle);
			var heading = CreateApplicabilityHeading(applicability, applicabilityDefinition, realVersion);
			var tooltipText = BuildTooltipText(applicability, applicabilityDefinition, versioningSystem, realVersion, lifecycleFull);
			// language=html
			tooltipParts.Add($"<div>{heading}{tooltipText}</div>");
		}

		return string.Join("\n\n", tooltipParts);
	}

	private static string CreateApplicabilityHeading(Applicability applicability, ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition, SemVersion? realVersion)
	{
		var lifecycleName = applicability.GetLifeCycleName();
		var versionText = realVersion is not null ? $" {realVersion}" : "";
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
		ProductLifecycle.Unavailable => "Not available",
		_ => ""
	};

	private static string BuildTooltipText(
		Applicability applicability,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem,
		SemVersion? realVersion,
		string lifecycleFull)
	{
		var tooltipText = "";

		tooltipText = realVersion is not null
			? realVersion <= versioningSystem.Current
				? $"{lifecycleFull} on {applicabilityDefinition.DisplayName} version {realVersion} and later unless otherwise specified."
				: applicability.Lifecycle switch
				{
					ProductLifecycle.GenerallyAvailable
						or ProductLifecycle.Beta
						or ProductLifecycle.TechnicalPreview
						or ProductLifecycle.Planned =>
						$"We plan to add this functionality in a future {applicabilityDefinition.DisplayName} update. Subject to change.",
					ProductLifecycle.Deprecated => $"We plan to deprecate this functionality in a future {applicabilityDefinition.DisplayName} update. Subject to change.",
					ProductLifecycle.Removed => $"We plan to remove this functionality in a future {applicabilityDefinition.DisplayName} update. Subject to change.",
					_ => tooltipText
				}
			: $"{lifecycleFull} on {applicabilityDefinition.DisplayName} unless otherwise specified.";

		var disclaimer = GetDisclaimer(applicability.Lifecycle, versioningSystem.Id);
		if (disclaimer is not null)
			tooltipText = $"{tooltipText}\n\n{disclaimer}";

		return tooltipText;
	}

	private static string? GetDisclaimer(ProductLifecycle lifecycle, VersioningSystemId versioningSystemId) => lifecycle switch
	{
		ProductLifecycle.Beta => "Beta features are subject to change. The design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features.",
		ProductLifecycle.TechnicalPreview => "This functionality may be changed or removed in a future release. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features.",
		ProductLifecycle.GenerallyAvailable => versioningSystemId is VersioningSystemId.Stack
			? "If this functionality is unavailable or behaves differently when deployed on ECH, ECE, ECK, or a self-managed installation, it will be indicated on the page."
			: null,
		_ => null
	};

	private static string BuildBadgeLifecycleText(
		Applicability applicability,
		VersioningSystem versioningSystem,
		SemVersion? realVersion,
		AppliesCollection allApplications)
	{
		var badgeText = "";
		if (realVersion is not null && realVersion > versioningSystem.Current)
		{
			badgeText = applicability.Lifecycle switch
			{
				ProductLifecycle.TechnicalPreview => "Planned",
				ProductLifecycle.Beta => "Planned",
				ProductLifecycle.GenerallyAvailable =>
					allApplications.Any(a => a.Lifecycle is ProductLifecycle.TechnicalPreview or ProductLifecycle.Beta)
						? "GA planned"
						: "Planned",
				ProductLifecycle.Deprecated => "Deprecation planned",
				ProductLifecycle.Removed => "Removal planned",
				ProductLifecycle.Planned => "Planned",
				ProductLifecycle.Unavailable => "Unavailable",
				_ => badgeText
			};
		}

		return badgeText;
	}

	private static bool TryGetRealVersion(Applicability applicability, [NotNullWhen(true)] out SemVersion? version)
	{
		version = null;
		if (applicability.Version is not null && applicability.Version != AllVersions.Instance)
		{
			version = applicability.Version;
			return true;
		}

		return false;
	}
}
