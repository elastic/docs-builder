// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.Directives.Changelog;

/// <summary>
/// Minimal YAML DTO for reading changelog.yml settings needed by the {changelog} directive.
/// Only the fields relevant to directive rendering are included; everything else is ignored.
/// </summary>
[YamlSerializable]
internal sealed record ChangelogDirectiveConfigYaml
{
	public ChangelogDirectiveBundleConfigYaml? Bundle { get; set; }
}

/// <summary>
/// Minimal bundle section from changelog.yml, containing only directive-relevant settings.
/// </summary>
[YamlSerializable]
internal sealed record ChangelogDirectiveBundleConfigYaml
{
	public bool? ShowReleaseDates { get; set; }
}
