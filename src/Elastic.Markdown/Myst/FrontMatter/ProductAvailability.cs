// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

[YamlSerializable]
public record ProductAvailability
{
	public ProductLifecycle Lifecycle { get; init; }
	public SemVersion? Version { get; init; }

	public static ProductAvailability GenerallyAvailable { get; } = new()
	{
		Lifecycle = ProductLifecycle.GenerallyAvailable,
		Version = AllVersions.Instance
	};

	// <lifecycle> [version]
	public static bool TryParse(string? value, out ProductAvailability? availability)
	{
		if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "all", StringComparison.InvariantCultureIgnoreCase))
		{
			availability = GenerallyAvailable;
			return true;
		}

		var tokens = value.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (tokens.Length < 1)
		{
			availability = null;
			return false;
		}
		var lifecycle = tokens[0].ToLowerInvariant() switch
		{
			"preview" => ProductLifecycle.TechnicalPreview,
			"tech-preview" => ProductLifecycle.TechnicalPreview,
			"beta" => ProductLifecycle.Beta,
			"dev" => ProductLifecycle.Development,
			"development" => ProductLifecycle.Development,
			"deprecated" => ProductLifecycle.Deprecated,
			"coming" => ProductLifecycle.Coming,
			"discontinued" => ProductLifecycle.Discontinued,
			"unavailable" => ProductLifecycle.Unavailable,
			"ga" => ProductLifecycle.GenerallyAvailable,
			_ => throw new ArgumentOutOfRangeException(nameof(tokens), tokens, $"Unknown product lifecycle: {tokens[0]}")
		};

		var version = tokens.Length < 2 ? null : tokens[1] switch
		{
			null => AllVersions.Instance,
			"all" => AllVersions.Instance,
			"" => AllVersions.Instance,
			var t => SemVersionConverter.TryParse(t, out var v) ? v : null
		};
		availability = new ProductAvailability { Version = version, Lifecycle = lifecycle };
		return true;
	}
}
