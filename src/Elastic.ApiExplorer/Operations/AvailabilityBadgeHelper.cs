// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>
/// Badge data needed by the applies-to-popover web component.
/// </summary>
public sealed record AvailabilityBadgeData(
	string BadgeKey,
	string BadgeLifecycleText,
	string BadgeVersion,
	string LifecycleClass,
	string LifecycleName,
	bool ShowLifecycleName,
	bool ShowVersion
);

/// <summary>
/// Parses x-state extension values into structured badge data for the applies-to-popover web component.
/// </summary>
public static partial class AvailabilityBadgeHelper
{
	[GeneratedRegex(@"(\d+\.\d+\.\d+)")]
	private static partial Regex SemVersionRegex();

	/// <summary>
	/// Extracts badge data from an OpenAPI operation's x-state extension.
	/// Returns null when no displayable availability information exists.
	/// </summary>
	public static AvailabilityBadgeData? FromOperation(OpenApiOperation operation, VersionsConfiguration? versionsConfig) =>
		FromExtensions(operation.Extensions, versionsConfig);

	/// <summary>
	/// Extracts badge data from an OpenAPI schema's x-state extension.
	/// Returns null when no displayable availability information exists.
	/// </summary>
	public static AvailabilityBadgeData? FromSchema(IOpenApiSchema schema, VersionsConfiguration? versionsConfig) =>
		FromExtensions(schema.Extensions, versionsConfig);

	private static AvailabilityBadgeData? FromExtensions(
		IDictionary<string, IOpenApiExtension>? extensions,
		VersionsConfiguration? versionsConfig)
	{
		if (extensions is null || !extensions.TryGetValue("x-state", out var stateExtension))
			return null;

		if (stateExtension is not JsonNodeExtension jsonNodeExtension)
			return null;

		var stateValue = jsonNodeExtension.Node.GetValue<string>();
		if (string.IsNullOrEmpty(stateValue))
			return null;

		var lifecycle = ParseLifecycle(stateValue);
		var version = ParseVersion(stateValue);

		return BuildBadgeData(lifecycle, version, versionsConfig);
	}

	private static AvailabilityBadgeData BuildBadgeData(
		ProductLifecycle lifecycle,
		VersionSpec? version,
		VersionsConfiguration? versionsConfig)
	{
		var lifecycleClass = ProductLifecycleInfo.GetShortName(lifecycle).ToLowerInvariant().Replace(" ", "-");
		var lifecycleName = ProductLifecycleInfo.GetShortName(lifecycle);

		var versionDisplay = "";
		var showVersion = false;
		var showLifecycleName = lifecycle != ProductLifecycle.GenerallyAvailable;
		var badgeLifecycleText = "";

		if (version is not null && version != AllVersionsSpec.Instance)
		{
			var currentVersion = GetCurrentStackVersion(versionsConfig);
			var isReleased = currentVersion is null || version.Min <= currentVersion;

			if (isReleased)
			{
				versionDisplay = FormatVersion(version);
				showVersion = !string.IsNullOrEmpty(versionDisplay);

				if (lifecycle == ProductLifecycle.Removed && versionDisplay.EndsWith('+'))
					versionDisplay = versionDisplay.TrimEnd('+');
			}
			else
			{
				badgeLifecycleText = lifecycle switch
				{
					ProductLifecycle.Deprecated => "Deprecation planned",
					ProductLifecycle.Removed => "Removal planned",
					_ => "Planned"
				};
				showLifecycleName = false;
			}
		}

		return new AvailabilityBadgeData(
			BadgeKey: "Stack",
			BadgeLifecycleText: badgeLifecycleText,
			BadgeVersion: versionDisplay,
			LifecycleClass: lifecycleClass,
			LifecycleName: lifecycleName,
			ShowLifecycleName: showLifecycleName,
			ShowVersion: showVersion
		);
	}

	private static SemVersion? GetCurrentStackVersion(VersionsConfiguration? versionsConfig)
	{
		if (versionsConfig is null)
			return null;

		try
		{
			var versioningSystem = versionsConfig.GetVersioningSystem(VersioningSystemId.Stack);
			return versioningSystem.Current;
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	private static string FormatVersion(VersionSpec versionSpec)
	{
		var min = versionSpec.Min;
		var minVersion = versionSpec.ShowMinPatch
			? $"{min.Major}.{min.Minor}.{min.Patch}"
			: $"{min.Major}.{min.Minor}";

		return versionSpec.Kind switch
		{
			VersionSpecKind.GreaterThanOrEqual => $"{minVersion}+",
			VersionSpecKind.Exact => minVersion,
			VersionSpecKind.Range when versionSpec.Max is { } max =>
				$"{minVersion}-{(versionSpec.ShowMaxPatch ? $"{max.Major}.{max.Minor}.{max.Patch}" : $"{max.Major}.{max.Minor}")}",
			_ => minVersion
		};
	}

	private static ProductLifecycle ParseLifecycle(string stateValue)
	{
		var lower = stateValue.ToLowerInvariant();

		if (lower.Contains("generally available"))
			return ProductLifecycle.GenerallyAvailable;
		if (lower.Contains("beta"))
			return ProductLifecycle.Beta;
		if (lower.Contains("tech") && lower.Contains("preview"))
			return ProductLifecycle.TechnicalPreview;
		if (lower.Contains("deprecated"))
			return ProductLifecycle.Deprecated;
		if (lower.Contains("removed"))
			return ProductLifecycle.Removed;

		return ProductLifecycle.GenerallyAvailable;
	}

	private static VersionSpec? ParseVersion(string stateValue)
	{
		var match = SemVersionRegex().Match(stateValue);
		if (!match.Success)
			return null;

		var versionString = match.Groups[1].Value;
		return VersionSpec.TryParse(versionString, out var version) ? version : null;
	}
}
