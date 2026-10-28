// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Elastic.Documentation.AppliesTo;
using SharpYaml.Serialization;

namespace Elastic.Markdown.Myst.Directives.Settings;

public record YamlSettings
{
	[YamlPropertyName("product")]
	public string? Product { get; set; }
	[YamlPropertyName("collection")]
	public string? Collection { get; set; }
	[YamlPropertyName("id")]
	public string? Id { get; set; }
	[YamlPropertyName("page_description")]
	public string? PageDescription { get; set; }
	[YamlPropertyName("note")]
	public string? Note { get; set; }
	[YamlPropertyName("groups")]
	public SettingsGrouping[] Groups { get; set; } = [];
}

public record SettingsGrouping
{
	[YamlPropertyName("group")]
	public string? Name { get; set; }
	[YamlPropertyName("id")]
	public string? Id { get; set; }
	[YamlPropertyName("legacy_id")]
	public string? LegacyId { get; set; }
	[YamlPropertyName("description")]
	public string? Description { get; set; }
	[YamlPropertyName("note")]
	public string? Note { get; set; }
	[YamlPropertyName("example")]
	public string? Example { get; set; }
	[YamlPropertyName("settings")]
	public Setting[] Settings { get; set; } = [];
}

public record Setting
{
	[YamlPropertyName("setting")]
	public string? Name { get; set; }
	[YamlPropertyName("id")]
	public string? Id { get; set; }
	[YamlPropertyName("description")]
	public string? Description { get; set; }
	[YamlPropertyName("deprecation_details")]
	public string? DeprecationDetails { get; set; }
	[YamlPropertyName("note")]
	public string? Note { get; set; }
	[YamlPropertyName("tip")]
	public string? Tip { get; set; }
	[YamlPropertyName("warning")]
	public string? Warning { get; set; }
	[YamlPropertyName("important")]
	public string? Important { get; set; }
	[YamlPropertyName("default")]
	public object? Default { get; set; }
	[YamlPropertyName("datatype")]
	public string? Datatype { get; set; }
	[YamlPropertyName("example")]
	public string? Example { get; set; }
	[YamlPropertyName("settings")]
	public Setting[] Settings { get; set; } = [];
	[YamlPropertyName("applies_to")]
	public ApplicableTo? AppliesTo { get; set; }

	// Legacy fields maintained for backward compatibility with existing sources.
	[YamlPropertyName("applies")]
	public ApplicableTo? LegacyAppliesTo { get; set; }
	[YamlPropertyName("type")]
	public SettingMutability Mutability { get; set; }
	[YamlPropertyName("options")]
	public AllowedValue[]? Options { get; set; }

	public ApplicableTo? ResolveAppliesTo(ApplicableTo? inheritedAppliesTo) => AppliesTo ?? LegacyAppliesTo ?? inheritedAppliesTo;
}

public record AllowedValue
{
	[YamlPropertyName("option")]
	public string? Option { get; set; }
	[YamlPropertyName("description")]
	public string? Description { get; set; }
}

public enum SettingMutability
{
	Static,
	Dynamic
}

public static class SettingDisplay
{
	public static string? FormatDefault(object? value) => value switch
	{
		null => null,
		string s => string.IsNullOrWhiteSpace(s) ? null : s,
		bool b => b ? "true" : "false",
		IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString()
	};
}

public static class DeploymentFilter
{
	/// <summary>Valid filter tokens accepted by the <c>:deployment:</c> directive option.</summary>
	public static readonly IReadOnlySet<string> ValidValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"ech",
		"ece",
		"eck",
		"self"
	};

	/// <summary>
	/// Returns the <see cref="AppliesCollection"/> for the given deployment filter key,
	/// mapping the canonical <c>ech</c> token to the <c>ess</c> model field.
	/// Returns <c>null</c> when the deployment type is not mentioned (i.e. not available).
	/// </summary>
	public static AppliesCollection? GetForDeployment(this DeploymentApplicability deployment, string key) => key.ToLowerInvariant() switch
	{
		"ech" => deployment.Ess,
		"ece" => deployment.Ece,
		"eck" => deployment.Eck,
		"self" => deployment.Self,
		_ => null
	};

	/// <summary>
	/// Returns <c>true</c> when the setting should be shown for the given deployment filter.
	/// A setting with no <c>applies_to</c> at all is always visible (no restriction).
	/// A setting with <c>applies_to</c> that does not explicitly list the deployment is considered unavailable.
	/// </summary>
	public static bool IsVisibleForDeployment(this Setting setting, string deploymentFilter, ApplicableTo? inheritedAppliesTo)
	{
		var appliesTo = setting.ResolveAppliesTo(inheritedAppliesTo);

		if (appliesTo is null)
			return true;

		if (appliesTo.Deployment is not { } deployment)
			return false;

		var col = deployment.GetForDeployment(deploymentFilter);
		if (col is null)
			return false;

		return col.Any(a => a.Lifecycle is not ProductLifecycle.Removed and not ProductLifecycle.Unavailable);
	}

	/// <summary>Returns <c>true</c> when at least one setting (recursively) in <paramref name="settings"/> is visible.</summary>
	public static bool AnyVisible(Setting[] settings, string deploymentFilter, ApplicableTo? inheritedAppliesTo) =>
		settings.Any(
			s => s.IsVisibleForDeployment(deploymentFilter, inheritedAppliesTo) || AnyVisible(
				s.Settings,
				deploymentFilter,
				s.ResolveAppliesTo(inheritedAppliesTo)
			)
		);
}
