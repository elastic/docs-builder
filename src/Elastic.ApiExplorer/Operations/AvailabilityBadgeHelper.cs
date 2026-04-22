// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
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
/// Projects x-state into the applies_to lifecycle format and delegates parsing to <see cref="AppliesCollection"/>.
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

		var lifecycleString = ProjectToLifecycleFormat(stateValue);

		var diagnostics = new List<(Severity, string)>();
		if (!AppliesCollection.TryParse(lifecycleString, diagnostics, out var appliesCollection) || appliesCollection is null)
			return null;

		var applicableTo = new ApplicableTo { Stack = appliesCollection };

		return BuildBadgeData(applicableTo, versionsConfig);
	}

	/// <summary>
	/// Converts an x-state string (e.g. "Added in 7.7.0", "Technical preview",
	/// "Generally available; added in 9.1.0") into the lifecycle format
	/// understood by <see cref="AppliesCollection.TryParse"/> (e.g. "ga 7.7.0", "preview").
	/// </summary>
	internal static string ProjectToLifecycleFormat(string xState)
	{
		var lower = xState.ToLowerInvariant();

		string lifecycle;
		if (lower.Contains("generally available"))
			lifecycle = "ga";
		else if (lower.Contains("beta"))
			lifecycle = "beta";
		else if (lower.Contains("tech") && lower.Contains("preview"))
			lifecycle = "preview";
		else if (lower.Contains("deprecated"))
			lifecycle = "deprecated";
		else if (lower.Contains("removed"))
			lifecycle = "removed";
		else
			lifecycle = "ga";

		var versionMatch = SemVersionRegex().Match(xState);
		return versionMatch.Success ? $"{lifecycle} {versionMatch.Groups[1].Value}" : lifecycle;
	}

	private static AvailabilityBadgeData? BuildBadgeData(
		ApplicableTo applicableTo,
		VersionsConfiguration? versionsConfig)
	{
		if (applicableTo.Stack is null)
			return null;

		var applicability = applicableTo.Stack.First();
		var lifecycleClass = applicability.GetLifeCycleName().ToLowerInvariant().Replace(" ", "-");
		var lifecycleName = applicability.GetLifeCycleName();

		var versionDisplay = "";
		var showVersion = false;
		var showLifecycleName = applicability.Lifecycle != ProductLifecycle.GenerallyAvailable;
		var badgeLifecycleText = "";

		var version = applicability.Version;
		if (version is not null && version != AllVersionsSpec.Instance)
		{
			var currentVersion = GetCurrentStackVersion(versionsConfig);
			var isReleased = currentVersion is null || version.Min <= currentVersion;

			if (isReleased)
			{
				versionDisplay = FormatVersion(version);
				showVersion = !string.IsNullOrEmpty(versionDisplay);

				if (applicability.Lifecycle == ProductLifecycle.Removed && versionDisplay.EndsWith('+'))
					versionDisplay = versionDisplay.TrimEnd('+');
			}
			else
			{
				badgeLifecycleText = applicability.Lifecycle switch
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
}
