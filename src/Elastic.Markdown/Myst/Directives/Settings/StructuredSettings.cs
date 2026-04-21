// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Elastic.Documentation.AppliesTo;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.Directives.Settings;

[YamlSerializable]
public record YamlSettings
{
	[YamlMember(Alias = "product")]
	public string? Product { get; set; }
	[YamlMember(Alias = "collection")]
	public string? Collection { get; set; }
	[YamlMember(Alias = "id")]
	public string? Id { get; set; }
	[YamlMember(Alias = "page_description")]
	public string? PageDescription { get; set; }
	[YamlMember(Alias = "note")]
	public string? Note { get; set; }
	[YamlMember(Alias = "groups")]
	public SettingsGrouping[] Groups { get; set; } = [];
}

[YamlSerializable]
public record SettingsGrouping
{
	[YamlMember(Alias = "group")]
	public string? Name { get; set; }
	[YamlMember(Alias = "id")]
	public string? Id { get; set; }
	[YamlMember(Alias = "legacy_id")]
	public string? LegacyId { get; set; }
	[YamlMember(Alias = "description")]
	public string? Description { get; set; }
	[YamlMember(Alias = "note")]
	public string? Note { get; set; }
	[YamlMember(Alias = "example")]
	public string? Example { get; set; }
	[YamlMember(Alias = "settings")]
	public Setting[] Settings { get; set; } = [];
}

[YamlSerializable]
public record Setting
{
	[YamlMember(Alias = "setting")]
	public string? Name { get; set; }
	[YamlMember(Alias = "id")]
	public string? Id { get; set; }
	[YamlMember(Alias = "description")]
	public string? Description { get; set; }
	[YamlMember(Alias = "deprecation_details")]
	public string? DeprecationDetails { get; set; }
	[YamlMember(Alias = "note")]
	public string? Note { get; set; }
	[YamlMember(Alias = "tip")]
	public string? Tip { get; set; }
	[YamlMember(Alias = "warning")]
	public string? Warning { get; set; }
	[YamlMember(Alias = "important")]
	public string? Important { get; set; }
	[YamlMember(Alias = "default")]
	public object? Default { get; set; }
	[YamlMember(Alias = "datatype")]
	public string? Datatype { get; set; }
	[YamlMember(Alias = "example")]
	public string? Example { get; set; }
	[YamlMember(Alias = "settings")]
	public Setting[] Settings { get; set; } = [];
	[YamlMember(Alias = "applies_to")]
	public ApplicableTo? AppliesTo { get; set; }

	// Legacy fields maintained for backward compatibility with existing sources.
	[YamlMember(Alias = "applies")]
	public ApplicableTo? LegacyAppliesTo { get; set; }
	[YamlMember(Alias = "type")]
	public SettingMutability Mutability { get; set; }
	[YamlMember(Alias = "options")]
	public AllowedValue[]? Options { get; set; }

	public ApplicableTo? ResolveAppliesTo(ApplicableTo? inheritedAppliesTo) =>
		AppliesTo ?? LegacyAppliesTo ?? inheritedAppliesTo;
}

[YamlSerializable]
public record AllowedValue
{
	[YamlMember(Alias = "option")]
	public string? Option { get; set; }
	[YamlMember(Alias = "description")]
	public string? Description { get; set; }
}

[YamlSerializable]
public enum SettingMutability
{
	[YamlMember(Alias = "static")]
	Static,
	[YamlMember(Alias = "dynamic")]
	Dynamic
}

public static class SettingDisplay
{
	public static string? FormatDefault(object? value) =>
		value switch
		{
			null => null,
			string s => string.IsNullOrWhiteSpace(s) ? null : s,
			bool b => b ? "true" : "false",
			IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString()
		};
}
