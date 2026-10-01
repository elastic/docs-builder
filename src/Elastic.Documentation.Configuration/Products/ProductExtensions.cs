// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Versions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Products;

public static class ProductExtensions
{
	public static ProductsConfiguration CreateProducts(this ConfigurationFileProvider provider, VersionsConfiguration versionsConfiguration)
	{
		using var reader = provider.ProductsFile.OpenText();
		return CreateProducts(reader, versionsConfiguration);
	}

	internal static ProductsConfiguration CreateProducts(TextReader reader, VersionsConfiguration versionsConfiguration)
	{
		var productsDto = ConfigurationFileProvider.Deserializer.Deserialize<ProductConfigDto>(reader);

		var products = productsDto.Products.ToDictionary(kvp => kvp.Key, kvp =>
		{
			var features = ResolveFeatures(kvp.Key, kvp.Value.Features);
			var versioningSystem = ResolveVersioningSystem(versionsConfiguration, kvp.Value.Versioning ?? kvp.Key);

			versioningSystem ??= !features.PublicReference
				? VersioningSystem.None
				: throw new InvalidOperationException(
					$"Product '{kvp.Key}' has invalid or missing versioning '{kvp.Value.Versioning ?? kvp.Key}' while 'public-reference' is enabled."
				);

			return new Product
			{
				Id = kvp.Key,
				DisplayName = kvp.Value.Display,
				VersioningSystem = versioningSystem,
				Repository = kvp.Value.Repository ?? kvp.Key,
				Features = features
			};
		});

		var publicReferenceProducts = products.Where(kvp => kvp.Value.Features.PublicReference).ToDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value
		);

		var productDisplayNames = productsDto.Products.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Display);

		return new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary(),
			PublicReferenceProducts = publicReferenceProducts.ToFrozenDictionary(),
			ProductDisplayNames = productDisplayNames.ToFrozenDictionary()
		};
	}

	private static VersioningSystem? ResolveVersioningSystem(VersionsConfiguration versionsConfiguration, string id) =>
		VersioningSystemIdExtensions.TryParse(id, out var versioningSystemId, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? versionsConfiguration.GetVersioningSystem(versioningSystemId)
			: null;

	private static ProductFeatures ResolveFeatures(string productId, Dictionary<string, string>? featuresDto)
	{
		if (featuresDto is null)
			return ProductFeatures.All;

		var unknownKeys = featuresDto.Keys.Where(k => !ProductFeatures.KnownKeys.Contains(k)).ToList();

		if (unknownKeys is { Count: > 0 })
		{
			var known = string.Join(", ", ProductFeatures.KnownKeys.Order());
			throw new InvalidOperationException(
				$"Product '{productId}' has unknown feature key(s): {string.Join(", ", unknownKeys)}. Known features: {known}."
			);
		}

		var (releaseNotes, warning) = ResolveReleaseNotesPath(productId, featuresDto);
		if (warning is not null)
			Console.Error.WriteLine(warning);

		return new ProductFeatures
		{
			PublicReference = ResolveBooleanFeature(productId, featuresDto, "public-reference"),
			ReleaseNotes = releaseNotes
		};
	}

	private static bool ResolveBooleanFeature(string productId, Dictionary<string, string> featuresDto, string key)
	{
		if (!featuresDto.TryGetValue(key, out var value))
			return true;
		if (string.IsNullOrWhiteSpace(value))
			throw new InvalidOperationException($"Product '{productId}' has an empty '{key}' value. Allowed values: true, false.");
		if (bool.TryParse(value, out var enabled))
			return enabled;
		throw new InvalidOperationException($"Product '{productId}' has invalid '{key}' value '{value}'. Allowed values: true, false.");
	}

	/// <summary>
	/// Resolves <c>features.release-notes</c> into an onboarding path. Backward compatible with the
	/// historical boolean flag: omitted/<c>true</c> mean on-release participation, <c>false</c> opts
	/// out; the strings <c>prestage</c>/<c>dra</c>/<c>on-release</c> select the path explicitly.
	/// Unknown future values are tolerated and treated as <c>on-release</c> so that a newer
	/// <c>config/products.yml</c> does not crash an older released binary.
	/// </summary>
	/// <returns>The resolved path and an optional warning message for unknown values.</returns>
	private static (ReleaseNotesPath Path, string? Warning) ResolveReleaseNotesPath(
		string productId,
		Dictionary<string, string> featuresDto
	)
	{
		if (!featuresDto.TryGetValue("release-notes", out var value))
			return (ReleaseNotesPath.OnRelease, null);

		if (string.IsNullOrWhiteSpace(value))
			throw new InvalidOperationException(
				$"Product '{productId}' has an empty 'release-notes' value. Allowed values: true, false, dra, on-release."
			);

		if (bool.TryParse(value, out var enabled))
			return (enabled ? ReleaseNotesPath.OnRelease : ReleaseNotesPath.None, null);

		return value.ToLowerInvariant() switch
		{
			"prestage" or "dra" => (ReleaseNotesPath.Prestage, null),
			"on-release" => (ReleaseNotesPath.OnRelease, null),
			_ =>
				(ReleaseNotesPath.OnRelease, $"Product '{productId}' has unrecognised 'release-notes' value '{value}'; treating as 'on-release'. Allowed values: true, false, dra, on-release.")
		};
	}
}

// Private DTOs for deserialization. These match the YAML structure directly.

internal sealed record ProductConfigDto
{
	[YamlMember(Alias = "products")]
	public Dictionary<string, ProductDto> Products { get; set; } = [];
}

internal sealed record ProductDto
{
	[YamlMember(Alias = "display")]
	public string Display { get; set; } = string.Empty;

	[YamlMember(Alias = "versioning")]
	public string? Versioning { get; set; }

	public string? Repository { get; set; }

	/// <summary>
	/// Feature values are strings so <c>release-notes</c> accepts both the historical booleans and
	/// the <c>prestage</c>/<c>on-release</c> path names; parsing happens in <see cref="ProductExtensions"/>.
	/// </summary>
	[YamlMember(Alias = "features")]
	public Dictionary<string, string>? Features { get; set; }
}
