// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.FrontMatter;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.Settings;

[YamlSerializable]
public record YamlSettings
{
	[YamlMember(Alias = "product")]
	public string? Product { get; set; }
	[YamlMember(Alias = "collection")]
	public string? Collection { get; set; }
	[YamlMember(Alias = "groups")]
	public SettingsGrouping[] Groups { get; set; } = [];
}

[YamlSerializable]
public record SettingsGrouping
{
	[YamlMember(Alias = "group")]
	public string? Name { get; set; }
	[YamlMember(Alias = "self")]
	public string? Id { get; set; }
	[YamlMember(Alias = "settings")]
	public Setting[] Settings { get; set; } = [];
}

[YamlSerializable]
public record Setting
{
	[YamlMember(Alias = "setting")]
	public string? Name { get; set; }
	[YamlMember(Alias = "description")]
	public string? Description { get; set; }
	[YamlMember(Alias = "applies")]
	public Applicability? Applies { get; set; }
	[YamlMember(Alias = "type")]
	public SettingMutability Mutability { get; set; }
	[YamlMember(Alias = "options")]
	public AllowedValue[]? Options { get; set; }
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
