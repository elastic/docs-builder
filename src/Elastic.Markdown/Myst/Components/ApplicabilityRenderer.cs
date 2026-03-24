// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Markdown.Myst.Components;

public static class ApplicabilityRenderer
{
	/// <summary>
	/// Represents a single availability item in the popover (e.g., "Generally available since 9.1").
	/// </summary>
	public record PopoverAvailabilityItem(
		[property: JsonPropertyName("text")] string Text,
		[property: JsonPropertyName("lifecycleDescription")] string? LifecycleDescription
	);

	/// <summary>
	/// Structured data for the popover content, to be serialized as JSON and rendered by the frontend.
	/// </summary>
	public record PopoverData(
		[property: JsonPropertyName("productDescription")] string? ProductDescription,
		[property: JsonPropertyName("availabilityItems")] PopoverAvailabilityItem[] AvailabilityItems,
		[property: JsonPropertyName("additionalInfo")] string? AdditionalInfo,
		[property: JsonPropertyName("showVersionNote")] bool ShowVersionNote,
		[property: JsonPropertyName("versionNote")] string? VersionNote
	);

	public record ApplicabilityRenderData(
		string BadgeLifecycleText,
		string Version,
		PopoverData? PopoverData,
		string LifecycleClass,
		string LifecycleName,
		bool ShowLifecycleName,
		bool ShowVersion,
		bool HasMultipleLifecycles = false
	);

	public static ApplicabilityRenderData RenderApplicability(
		IReadOnlyCollection<Applicability> applicabilities,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem)
	{
		var allApplications = new AppliesCollection([.. applicabilities]);

		// Sort by version (highest first), then by lifecycle priority as tiebreaker
		var sortedApplicabilities = applicabilities
			.OrderByDescending(a => a.Version?.Min ?? ZeroVersion.Instance)
			.ThenBy(a => ProductLifecycleInfo.GetOrder(a.Lifecycle))
			.ToList();

		// Find the first lifecycle that returns displayable badge data (non-empty text or version)
		// If all return empty (all unreleased with multiple lifecycles), use the first one and show "Planned"
		BadgeData? badgeData = null;
		BadgeData? firstBadgeData = null;
		Applicability? firstApplicability = null;

		foreach (var applicability in sortedApplicabilities)
		{
			var candidateBadgeData = GetBadgeData(applicability, versioningSystem, allApplications);

			// Keep track of the first one as fallback
			firstBadgeData ??= candidateBadgeData;
			firstApplicability ??= applicability;

			// If this candidate has displayable data, use it
			if (!string.IsNullOrEmpty(candidateBadgeData.BadgeLifecycleText) ||
				!string.IsNullOrEmpty(candidateBadgeData.Version))
			{
				badgeData = candidateBadgeData;
				break;
			}
		}

		// If we've exhausted all options (none had displayable data), use the first one.
		// Only show "Planned" when the first applicability is actually future/unreleased (has a version spec that is not yet released).
		// When the first applicability has no version (null/AllVersionsSpec), it means GA for all versions - keep badge text empty.
		if (badgeData is null && firstBadgeData is not null && firstApplicability is not null && versioningSystem.IsVersioned())
		{
			var versionSpec = firstApplicability.Version;
			var isFutureVersion = versionSpec is not null && versionSpec != AllVersionsSpec.Instance && versionSpec.Min > versioningSystem.Current;
			badgeData = isFutureVersion ? firstBadgeData with { BadgeLifecycleText = "Planned" } : firstBadgeData;
		}

		badgeData ??= GetBadgeData(sortedApplicabilities.First(), versioningSystem, allApplications);

		var popoverData = BuildPopoverData(applicabilities, applicabilityDefinition, versioningSystem);

		// Check if there are multiple different lifecycles
		var hasMultipleLifecycles = applicabilities.Select(a => a.Lifecycle).Distinct().Count() > 1;

		return new ApplicabilityRenderData(
			BadgeLifecycleText: badgeData.BadgeLifecycleText,
			Version: badgeData.Version,
			PopoverData: popoverData,
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

		// Determine if we should show the version based on VersionSpec
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

	private static PopoverData BuildPopoverData(
		IReadOnlyCollection<Applicability> applicabilities,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem)
	{
		var productInfo = ProductDescriptions.GetProductInfo(versioningSystem.Id);
		var productName = GetPlainProductName(applicabilityDefinition.DisplayName);

		// Availability section - collect items from all applicabilities
		// Order by version descending (most recent/future first, then going backwards)
		var orderedApplicabilities = applicabilities
			.OrderByDescending(a => a.Version?.Min ?? ZeroVersion.Instance);

		var showVersionNote = productInfo is { IncludeVersionNote: true } && versioningSystem.IsVersioned();

		return new PopoverData(
			ProductDescription: productInfo?.Description,
			AvailabilityItems: orderedApplicabilities.Select(applicability => BuildAvailabilityItem(applicability, versioningSystem, productName, applicabilities.Count)).OfType<PopoverAvailabilityItem>().ToArray(),
			AdditionalInfo: productInfo?.AdditionalAvailabilityInfo,
			ShowVersionNote: showVersionNote,
			VersionNote: showVersionNote ? ProductDescriptions.VersionNote : null
		);
	}

	/// <summary>
	/// Builds an availability item for an applicability entry.
	/// Returns null if the item should not be added to the availability list.
	/// </summary>
	private static PopoverAvailabilityItem? BuildAvailabilityItem(
		Applicability applicability,
		VersioningSystem versioningSystem,
		string productName,
		int lifecycleCount)
	{
		var availabilityText = GenerateAvailabilityText(applicability, versioningSystem, lifecycleCount);

		if (availabilityText is null)
			return null;

		var isReleased = IsVersionReleased(applicability, versioningSystem);
		var lifecycleDescription = LifecycleDescriptions.GetDescriptionWithProduct(
			applicability.Lifecycle,
			isReleased,
			productName
		);

		return new PopoverAvailabilityItem(
			Text: availabilityText,
			LifecycleDescription: lifecycleDescription
		);
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

		if (versionSpec is null or AllVersionsSpec)
		{
			if (!versioningSystem.IsVersioned())
				return ProductLifecycleInfo.GetDisplayText(lifecycle);

			// When no version is specified, do not use base version in the text
			return lifecycle switch
			{
				ProductLifecycle.Removed => "Removed",
				ProductLifecycle.Unavailable => "Unavailable",
				_ => ProductLifecycleInfo.GetDisplayText(lifecycle)
			};
		}

		// Get version info
		var min = versionSpec.Min;
		var max = versionSpec.Max;
		var showMinPatch = versionSpec.ShowMinPatch;
		var showMaxPatch = versionSpec.ShowMaxPatch;
		var minVersion = showMinPatch ? $"{min.Major}.{min.Minor}.{min.Patch}" : $"{min.Major}.{min.Minor}";
		var maxVersion = max is not null
			? (showMaxPatch ? $"{max.Major}.{max.Minor}.{max.Patch}" : $"{max.Major}.{max.Minor}")
			: null;
		var isMinReleased = min <= versioningSystem.Current;
		var isMaxReleased = max is not null && max <= versioningSystem.Current;

		return versionSpec.Kind switch
		{
			// Greater than or equal (x.x+, x.x, x.x.x+, x.x.x)
			VersionSpecKind.GreaterThanOrEqual => GenerateGteAvailabilityText(lifecycle, minVersion, isMinReleased, lifecycleCount),

			// Range (x.x-y.y, x.x.x-y.y.y)
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
				_ => $"{ProductLifecycleInfo.GetDisplayText(lifecycle)} since {version}"
			};
		}

		return lifecycle switch
		{
			ProductLifecycle.Deprecated => "Planned for deprecation",
			ProductLifecycle.Removed => "Planned for removal",
			ProductLifecycle.Unavailable when lifecycleCount == 1 => "Unavailable",
			_ when lifecycleCount >= 2 => null, // Do not add to availability list
			_ => "Planned"
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
			ProductLifecycle.Unavailable => null,
			_ when lifecycleCount >= 2 => null, // Do not add to availability list
			_ => "Planned"
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
			ProductLifecycle.Unavailable => null,
			_ when lifecycleCount >= 2 => null, // Do not add to availability list
			_ => "Planned"
		};
	}

	/// <summary>
	/// Gets the plain product name without HTML entities for use in text substitution.
	/// </summary>
	private static string GetPlainProductName(string displayName) =>
		displayName.Replace("&nbsp;", " ");

	/// <summary>
	/// Determines if a version should be considered released for lifecycle description purposes
	/// For ranges, if min is released, the feature is currently available
	/// </summary>
	private static bool IsVersionReleased(Applicability applicability, VersioningSystem versioningSystem)
	{
		var versionSpec = applicability.Version;

		// No version specified - consider released
		if (versionSpec is null or AllVersionsSpec)
			return true;

		// For all version spec types, check if min is released
		// This determines whether the feature is currently available
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
		// When no version is specified (null or AllVersionsSpec), do not show a version in the badge
		switch (versionSpec)
		{
			case AllVersionsSpec:
			case null:
				return string.Empty;
			default:
				var kind = versionSpec.Kind;
				var min = versionSpec.Min;
				var max = versionSpec.Max;
				var showMinPatch = versionSpec.ShowMinPatch;
				var showMaxPatch = versionSpec.ShowMaxPatch;

				// Check if versions are released
				var minReleased = min <= versioningSystem.Current;
				var maxReleased = max is not null && max <= versioningSystem.Current;

				// Helper to format version with or without patch
				string FormatMinVersion() => showMinPatch
					? $"{min.Major}.{min.Minor}.{min.Patch}"
					: $"{min.Major}.{min.Minor}";

				string FormatMaxVersion() => showMaxPatch
					? $"{max!.Major}.{max.Minor}.{max.Patch}"
					: $"{max!.Major}.{max.Minor}";

				return kind switch
				{
					VersionSpecKind.GreaterThanOrEqual => minReleased
						? $"{FormatMinVersion()}+"
						: string.Empty,

					VersionSpecKind.Range => maxReleased
						? min.Major == max!.Major && min.Minor == max.Minor && !showMinPatch && !showMaxPatch
							? $"{min.Major}.{min.Minor}" // Same major.minor and no explicit patch, so just show the version once
							: $"{FormatMinVersion()}-{FormatMaxVersion()}"
						: minReleased
							? $"{FormatMinVersion()}+"
							: string.Empty,

					VersionSpecKind.Exact => minReleased
						? FormatMinVersion()
						: string.Empty,

					_ => string.Empty
				};
		}
	}
}
