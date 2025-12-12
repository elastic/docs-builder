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

	public static ApplicabilityRenderData RenderApplicability(
		IEnumerable<Applicability> applicabilities,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem)
	{
		var applicabilityList = applicabilities.ToList();
		var allApplications = new AppliesCollection([.. applicabilityList]);

		// Sort by lifecycle priority (GA > Beta > Preview > etc.) to determine display order
		var sortedApplicabilities = applicabilityList
			.OrderBy(a => ProductLifecycleInfo.GetOrder(a.Lifecycle))
			.ThenByDescending(a => a.Version?.Min ?? new SemVersion(0, 0, 0))
			.ToList();

		var primaryLifecycle = sortedApplicabilities.First();
		var primaryBadgeData = GetBadgeData(primaryLifecycle, versioningSystem, allApplications);

		// If the primary lifecycle returns an empty badge text (indicating "use previous lifecycle")
		// and we have multiple lifecycles, use the next lifecycle in priority order
		var applicabilityToDisplay = string.IsNullOrEmpty(primaryBadgeData.BadgeLifecycleText) &&
									 string.IsNullOrEmpty(primaryBadgeData.Version) &&
									 sortedApplicabilities.Count >= 2
			? sortedApplicabilities[1]
			: primaryLifecycle;

		var badgeData = applicabilityToDisplay == primaryLifecycle
			? primaryBadgeData
			: GetBadgeData(applicabilityToDisplay, versioningSystem, allApplications);

		var popoverContent = BuildPopoverContent(applicabilityList, applicabilityDefinition, versioningSystem);

		// Check if there are multiple different lifecycles
		var hasMultipleLifecycles = applicabilityList.Select(a => a.Lifecycle).Distinct().Count() > 1;

		return new ApplicabilityRenderData(
			BadgeLifecycleText: badgeData.BadgeLifecycleText,
			Version: badgeData.Version,
			TooltipText: popoverContent,
			LifecycleClass: badgeData.LifecycleClass,
			LifecycleName: badgeData.LifecycleName,
			ShowLifecycleName: badgeData.ShowLifecycleName || (string.IsNullOrEmpty(badgeData.BadgeLifecycleText) && hasMultipleLifecycles),
			ShowVersion: badgeData.ShowVersion,
			HasMultipleLifecycles: hasMultipleLifecycles
		);
	}

	/// <summary>
	/// Gets the badge display data for a single applicability (used internally for badge rendering decisions).
	/// </summary>
	private static BadgeData GetBadgeData(
		Applicability applicability,
		VersioningSystem versioningSystem,
		AppliesCollection allApplications)
	{
		var lifecycleClass = applicability.GetLifeCycleName().ToLowerInvariant().Replace(" ", "-");
		var badgeLifecycleText = BuildBadgeLifecycleText(applicability, versioningSystem, allApplications);

		var showLifecycle = applicability.Lifecycle != ProductLifecycle.GenerallyAvailable && string.IsNullOrEmpty(badgeLifecycleText);

		// Determine if we should show version based on VersionSpec
		var versionDisplay = GetBadgeVersionText(applicability.Version, versioningSystem);
		var showVersion = !string.IsNullOrEmpty(versionDisplay);

		// Special handling for Removed lifecycle - don't show + suffix
		if (applicability is { Lifecycle: ProductLifecycle.Removed, Version.Kind: VersionSpecKind.GreaterThanOrEqual } &&
			!string.IsNullOrEmpty(versionDisplay))
		{
			versionDisplay = versionDisplay.TrimEnd('+');
		}

		return new BadgeData(
			BadgeLifecycleText: badgeLifecycleText,
			Version: versionDisplay,
			LifecycleClass: lifecycleClass,
			LifecycleName: applicability.GetLifeCycleName(),
			ShowLifecycleName: showLifecycle,
			ShowVersion: showVersion
		);
	}

	private sealed record BadgeData(
		string BadgeLifecycleText,
		string Version,
		string LifecycleClass,
		string LifecycleName,
		bool ShowLifecycleName,
		bool ShowVersion
	);

	private static string BuildPopoverContent(
		List<Applicability> applicabilities,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem)
	{
		var productInfo = ProductDescriptions.GetProductInfo(versioningSystem.Id);
		var productName = GetPlainProductName(applicabilityDefinition.DisplayName);

		var parts = new List<string>();

		// Product description
		if (productInfo is not null && !string.IsNullOrEmpty(productInfo.Description))
		{
			// language=html
			parts.Add($"<p class=\"popover-product-description\">{productInfo.Description}</p>");
		}

		// Availability section - collect items from all applicabilities
		// Order by: available first (by version desc), then future (by version asc)
		var orderedApplicabilities = applicabilities
			.OrderByDescending(a => a.Version is null || a.Version is AllVersionsSpec ||
								   (a.Version is { } vs && vs.Min <= versioningSystem.Current) ? 1 : 0)
			.ThenByDescending(a => a.Version?.Min ?? new SemVersion(0, 0, 0))
			.ThenBy(a => a.Version?.Min ?? new SemVersion(0, 0, 0))
			.ToList();

		var allAvailabilityItems = new List<string>();
		foreach (var applicability in orderedApplicabilities)
		{
			var items = BuildAvailabilityItems(applicability, versioningSystem, productName, applicabilities.Count);
			allAvailabilityItems.AddRange(items);
		}

		if (allAvailabilityItems.Count > 0)
		{
			// language=html
			parts.Add("<p class=\"popover-availability-title\"><strong>Availability</strong></p>");
			parts.Add("<p class=\"popover-availability-intro\">The functionality described here is:</p>");
			parts.Add(string.Join("\n", allAvailabilityItems));
		}

		// Additional availability info
		if (productInfo is { AdditionalAvailabilityInfo: not null })
		{
			// language=html
			parts.Add($"<p class=\"popover-additional-info\">{productInfo.AdditionalAvailabilityInfo}</p>");
		}

		// Version note
		if (productInfo is { IncludeVersionNote: true } && versioningSystem.Base.Major != AllVersionsSpec.Instance.Min.Major)
		{
			// language=html
			parts.Add($"<p class=\"popover-version-note\"><span class=\"popover-note-icon\">ⓘ</span> {ProductDescriptions.VersionNote}</p>");
		}

		return string.Join("\n", parts);
	}

	/// <summary>
	/// Builds the availability list items (details/summary elements) for an applicability.
	/// </summary>
	private static List<string> BuildAvailabilityItems(
		Applicability applicability,
		VersioningSystem versioningSystem,
		string productName,
		int lifecycleCount)
	{
		var items = new List<string>();

		var availabilityText = GenerateAvailabilityText(applicability, versioningSystem, lifecycleCount);

		if (availabilityText is null)
			return items;

		var isReleased = IsVersionReleased(applicability, versioningSystem);
		var lifecycleDescription = LifecycleDescriptions.GetDescriptionWithProduct(
			applicability.Lifecycle,
			isReleased,
			productName
		);

		// Build the details/summary element for collapsible lifecycle description
		if (!string.IsNullOrEmpty(lifecycleDescription))
		{
			// language=html
			items.Add($"""
<details class="popover-availability-item">
<summary class="popover-availability-summary"><span class="popover-availability-text">{availabilityText}</span></summary>
<p class="popover-lifecycle-description">{lifecycleDescription}</p>
</details>
""");
		}
		else
		{
			// No collapsible content, just show the text
			// language=html
			items.Add($"<p class=\"popover-availability-item-simple\">{availabilityText}</p>");
		}

		return items;
	}

	/// <summary>
	/// Generates the dynamic availability text based on version type, lifecycle, release status, and lifecycle count.
	/// Returns null if the item should not be added to the availability list.
	/// </summary>
	private static string? GenerateAvailabilityText(
		Applicability applicability,
		VersioningSystem versioningSystem,
		int lifecycleCount)
	{
		var lifecycle = applicability.Lifecycle;
		var versionSpec = applicability.Version;

		// No version (null or AllVersionsSpec) with unversioned product
		if ((versionSpec is null || versionSpec is AllVersionsSpec) &&
			versioningSystem.Base.Major == AllVersionsSpec.Instance.Min.Major)
		{
			return ProductLifecycleInfo.GetDisplayText(lifecycle);
		}

		// No version with versioned product
		if (versionSpec is null or AllVersionsSpec)
		{
			var baseVersion = $"{versioningSystem.Base.Major}.{versioningSystem.Base.Minor}";
			return lifecycle switch
			{
				ProductLifecycle.Removed => $"Removed in {baseVersion}",
				_ => $"{ProductLifecycleInfo.GetDisplayText(lifecycle)} since {baseVersion}"
			};
		}

		// Get version info
		var min = versionSpec.Min;
		var max = versionSpec.Max;
		var minVersion = $"{min.Major}.{min.Minor}";
		var maxVersion = max is not null ? $"{max.Major}.{max.Minor}" : null;
		var isMinReleased = min <= versioningSystem.Current;
		var isMaxReleased = max is not null && max <= versioningSystem.Current;

		return versionSpec.Kind switch
		{
			// Greater than or equal (x.x+, x.x, x.x.x+, x.x.x)
			VersionSpecKind.GreaterThanOrEqual => GenerateGteAvailabilityText(lifecycle, minVersion, isMinReleased, lifecycleCount),

			// ange (x.x-y.y, x.x.x-y.y.y)
			VersionSpecKind.Range => GenerateRangeAvailabilityText(lifecycle, minVersion, maxVersion!, isMinReleased, isMaxReleased, lifecycleCount),

			// Exact (=x.x, =x.x.x)
			VersionSpecKind.Exact => GenerateExactAvailabilityText(lifecycle, minVersion, isMinReleased, lifecycleCount),

			_ => null
		};
	}

	/// <summary>
	/// Generates availability text for greater-than-or-equal version type.
	/// </summary>
	private static string? GenerateGteAvailabilityText(ProductLifecycle lifecycle, string version, bool isReleased, int lifecycleCount)
	{
		if (isReleased)
		{
			return lifecycle switch
			{
				ProductLifecycle.Removed => $"Removed in {version}",
				ProductLifecycle.Unavailable => $"Unavailable since {version}",
				_ => $"{ProductLifecycleInfo.GetDisplayText(lifecycle)} since {version}"
			};
		}

		// Unreleased
		return lifecycle switch
		{
			ProductLifecycle.Deprecated => "Planned for deprecation",
			ProductLifecycle.Removed => "Planned for removal",
			ProductLifecycle.Unavailable when lifecycleCount == 1 => "Unavailable",
			ProductLifecycle.Unavailable => null, // Do not add to availability list
			_ when lifecycleCount == 1 => "Planned",
			_ => null // Do not add to availability list
		};
	}

	/// <summary>
	/// Generates availability text for range version type.
	/// </summary>
	private static string? GenerateRangeAvailabilityText(
		ProductLifecycle lifecycle, string minVersion, string maxVersion, bool isMinReleased, bool isMaxReleased, int lifecycleCount)
	{
		if (isMaxReleased)
		{
			return lifecycle switch
			{
				ProductLifecycle.Removed => $"Removed in {minVersion}",
				ProductLifecycle.Unavailable => $"Unavailable from {minVersion} to {maxVersion}",
				_ => $"{ProductLifecycleInfo.GetDisplayText(lifecycle)} from {minVersion} to {maxVersion}"
			};
		}

		if (isMinReleased)
		{
			// Max is not released, min is released -> treat as "since min"
			return lifecycle switch
			{
				ProductLifecycle.Removed => $"Removed in {minVersion}",
				ProductLifecycle.Unavailable => $"Unavailable since {minVersion}",
				_ => $"{ProductLifecycleInfo.GetDisplayText(lifecycle)} since {minVersion}"
			};
		}

		// Neither released
		return lifecycle switch
		{
			ProductLifecycle.Deprecated => "Planned for deprecation",
			ProductLifecycle.Removed => "Planned for removal",
			ProductLifecycle.Unavailable => null, // Do not add to availability list
			_ when lifecycleCount == 1 => "Planned",
			_ => null // Do not add to availability list
		};
	}

	/// <summary>
	/// Generates availability text for exact version type.
	/// </summary>
	private static string? GenerateExactAvailabilityText(ProductLifecycle lifecycle, string version, bool isReleased, int lifecycleCount)
	{
		if (isReleased)
		{
			return lifecycle switch
			{
				ProductLifecycle.Removed => $"Removed in {version}",
				ProductLifecycle.Unavailable => $"Unavailable in {version}",
				_ => $"{ProductLifecycleInfo.GetDisplayText(lifecycle)} in {version}"
			};
		}

		// Unreleased
		return lifecycle switch
		{
			ProductLifecycle.Deprecated => "Planned for deprecation",
			ProductLifecycle.Removed => "Planned for removal",
			ProductLifecycle.Unavailable => null, // Do not add to availability list
			_ when lifecycleCount == 1 => "Planned",
			_ => null // Do not add to availability list
		};
	}

	/// <summary>
	/// Gets the plain product name without HTML entities for use in text substitution.
	/// </summary>
	private static string GetPlainProductName(string displayName) =>
		displayName.Replace("&nbsp;", " ");

	/// <summary>
	/// Determines if a version should be considered released.
	/// </summary>
	private static bool IsVersionReleased(Applicability applicability, VersioningSystem versioningSystem)
	{
		var versionSpec = applicability.Version;

		// No version specified - consider released
		if (versionSpec is null or AllVersionsSpec)
			return true;

		// For ranges, check the max version
		if (versionSpec.Kind == VersionSpecKind.Range && versionSpec.Max is not null)
			return versionSpec.Max <= versioningSystem.Current;

		// For GTE and Exact, check the min version
		return versionSpec.Min <= versioningSystem.Current;
	}

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
		// When no version is specified (null or AllVersionsSpec), check if we should show the base version
		switch (versionSpec)
		{
			case AllVersionsSpec:
				return string.Empty;
			case null:
				// Only show base version if the product is versioned
				return versioningSystem.Base.Major != AllVersionsSpec.Instance.Min.Major
					? $"{versioningSystem.Base.Major}.{versioningSystem.Base.Minor}+"
					: string.Empty;
			default:
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
	}
}
