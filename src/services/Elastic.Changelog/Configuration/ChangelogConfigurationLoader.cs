// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Serialization;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Changelog.Configuration;

/// <summary>
/// Service for loading and validating changelog configuration
/// </summary>
public class ChangelogConfigurationLoader(ILoggerFactory logFactory, IConfigurationContext configurationContext, IFileSystem fileSystem)
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogConfigurationLoader>();

	private static readonly IDeserializer ConfigurationDeserializer =
		new StaticDeserializerBuilder(new ChangelogYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.WithTypeConverter(new YamlLenientListConverter())
			.WithTypeConverter(new TypeEntryYamlConverter())
			.Build();

	/// <summary>
	/// Deserializes changelog configuration YAML content.
	/// </summary>
	internal static ChangelogConfigurationYaml DeserializeConfiguration(string yaml) =>
		ConfigurationDeserializer.Deserialize<ChangelogConfigurationYaml>(yaml);

	/// <summary>
	/// Loads the publish blocker configuration from a changelog.
	/// </summary>
	/// <param name="fileSystem">The file system to read from.</param>
	/// <param name="configPath">The path to the changelog.yml configuration file.</param>
	/// <returns>The publish blocker configuration, or null if not found.</returns>
	public static PublishBlocker? LoadPublishBlocker(IFileSystem fileSystem, string configPath)
	{
		if (!fileSystem.File.Exists(configPath))
			return null;

		var yamlContent = fileSystem.File.ReadAllText(configPath);
		var yamlConfig = DeserializeConfiguration(yamlContent);

		return ParsePublishBlocker(yamlConfig.Block?.Publish);
	}

	/// <summary>
	/// Loads changelog configuration from file or returns default configuration
	/// </summary>
	public async Task<ChangelogConfiguration?> LoadChangelogConfiguration(IDiagnosticsCollector collector, string? configPath, Cancel ctx)
	{
		// Determine config file path
		var finalConfigPath = configPath ?? fileSystem.Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "docs", "changelog.yml");

		if (!fileSystem.File.Exists(finalConfigPath))
		{
			// Use default configuration if file doesn't exist
			_logger.LogWarning("Changelog configuration not found at {ConfigPath}, using defaults", finalConfigPath);
			return ChangelogConfiguration.Default;
		}

		try
		{
			var yamlContent = await fileSystem.File.ReadAllTextAsync(finalConfigPath, ctx);
			var yamlConfig = DeserializeConfiguration(yamlContent);

			return ParseConfiguration(collector, yamlConfig, finalConfigPath);
		}
		catch (IOException ex)
		{
			collector.EmitError(finalConfigPath, $"I/O error loading changelog configuration: {ex.Message}", ex);
			return null;
		}
		catch (UnauthorizedAccessException ex)
		{
			collector.EmitError(finalConfigPath, $"Access denied loading changelog configuration: {ex.Message}", ex);
			return null;
		}
		catch (YamlException ex)
		{
			collector.EmitError(finalConfigPath, $"YAML parsing error in changelog configuration: {ex.Message}", ex);
			return null;
		}
	}

	private ChangelogConfiguration? ParseConfiguration(IDiagnosticsCollector collector, ChangelogConfigurationYaml yamlConfig, string configPath)
	{
		var validProductIds = configurationContext.ProductsConfiguration.Products.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

		// Compute values from pivot configuration
		IReadOnlyList<string> availableTypes;
		IReadOnlyList<string> availableSubtypes;
		IReadOnlyList<string>? availableAreas;
		Dictionary<string, string>? labelToType;
		Dictionary<string, string>? labelToAreas;
		PivotConfiguration? pivot = null;

		if (yamlConfig.Pivot != null)
		{
			// Convert YAML pivot to domain pivot
			pivot = ConvertPivot(yamlConfig.Pivot);

			// Compute available types from pivot.types keys
			if (yamlConfig.Pivot.Types is { Count: > 0 })
			{
				// Validate types against enum values using TryParse
				foreach (var typeName in yamlConfig.Pivot.Types.Keys)
				{
					if (ChangelogEntryTypeExtensions.TryParse(typeName, out _, ignoreCase: true, allowMatchingMetadataAttribute: true))
						continue;
					collector.EmitError(configPath, $"Type '{typeName}' in pivot.types is not a valid type. Valid types: {string.Join(", ", ChangelogConfiguration.DefaultTypes)}");
					return null;
				}

				// Validate required types are present
				foreach (var requiredType in ChangelogConfiguration.RequiredTypes)
				{
					var requiredTypeName = requiredType.ToStringFast(true);
					if (yamlConfig.Pivot.Types.Keys.Any(k => k.Equals(requiredTypeName, StringComparison.OrdinalIgnoreCase)))
						continue;
					collector.EmitError(configPath, $"Required type '{requiredTypeName}' is missing from pivot.types. Required types: {string.Join(", ", ChangelogConfiguration.RequiredTypes.Select(t => t.ToStringFast(true)))}");
					return null;
				}

				// Validate subtypes only appear under breaking-change
				foreach (var (typeName, typeEntry) in yamlConfig.Pivot.Types)
				{
					if (typeEntry?.Subtypes is not { Count: > 0 })
						continue;
					if (!typeName.Equals(ChangelogEntryType.BreakingChange.ToStringFast(true), StringComparison.OrdinalIgnoreCase))
					{
						collector.EmitError(configPath, $"Type '{typeName}' has subtypes defined, but subtypes are only allowed for 'breaking-change' type.");
						return null;
					}

					// Validate subtype values against enum
					foreach (var subtypeName in typeEntry.Subtypes.Keys)
					{
						if (ChangelogEntrySubtypeExtensions.TryParse(subtypeName, out _, ignoreCase: true, allowMatchingMetadataAttribute: true))
							continue;
						collector.EmitError(configPath, $"Subtype '{subtypeName}' in pivot.types.{typeName}.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}");
						return null;
					}
				}

				availableTypes = yamlConfig.Pivot.Types.Keys.ToList();
			}
			else
				availableTypes = ChangelogConfiguration.DefaultTypes;

			// Compute available subtypes from pivot.subtypes keys
			if (yamlConfig.Pivot.Subtypes != null && yamlConfig.Pivot.Subtypes.Count > 0)
			{
				// Validate subtypes against enum values using TryParse
				foreach (var subtypeName in yamlConfig.Pivot.Subtypes.Keys)
				{
					if (!ChangelogEntrySubtypeExtensions.TryParse(subtypeName, out _, ignoreCase: true, allowMatchingMetadataAttribute: true))
					{
						collector.EmitError(configPath, $"Subtype '{subtypeName}' in pivot.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}");
						return null;
					}
				}
				availableSubtypes = yamlConfig.Pivot.Subtypes.Keys.ToList();
			}
			else
				availableSubtypes = ChangelogConfiguration.DefaultSubtypes;

			// Compute available areas from pivot.areas keys
			availableAreas = yamlConfig.Pivot.Areas != null && yamlConfig.Pivot.Areas.Count > 0
				? yamlConfig.Pivot.Areas.Keys.ToList()
				: null;

			// Build LabelToType mapping (inverted from pivot.types)
			labelToType = BuildLabelToTypeMapping(yamlConfig.Pivot.Types);

			// Build LabelToAreas mapping (inverted from pivot.areas)
			labelToAreas = BuildLabelToAreasMapping(yamlConfig.Pivot.Areas);
		}
		else
		{
			// No pivot configuration - use defaults
			availableTypes = ChangelogConfiguration.DefaultTypes;
			availableSubtypes = ChangelogConfiguration.DefaultSubtypes;
			availableAreas = null;
			labelToType = null;
			labelToAreas = null;
		}

		// Process lifecycles
		IReadOnlyList<Lifecycle> lifecycles;
		var lifecycleValues = yamlConfig.Lifecycles?.Values;
		if (lifecycleValues == null || lifecycleValues.Count == 0)
			lifecycles = ChangelogConfiguration.DefaultLifecycles;
		else
		{
			var parsedLifecycles = new List<Lifecycle>();
			foreach (var lifecycleStr in lifecycleValues)
			{
				if (!LifecycleExtensions.TryParse(lifecycleStr, out var lifecycle, ignoreCase: true, allowMatchingMetadataAttribute: true))
				{
					collector.EmitError(configPath, $"Lifecycle '{lifecycleStr}' in changelog.yml is not valid. Valid lifecycles: {string.Join(", ", ChangelogConfiguration.DefaultLifecycles.Select(l => l.ToStringFast(true)))}");
					return null;
				}
				parsedLifecycles.Add(lifecycle);
			}
			lifecycles = parsedLifecycles;
		}

		// Process products from products.available
		IReadOnlyList<Product>? products = null;
		var productIdList = yamlConfig.Products?.Available?.Values;
		if (productIdList is { Count: > 0 })
		{
			var resolvedProducts = new List<Product>();
			foreach (var productId in productIdList)
			{
				var normalizedProductId = productId.Replace('_', '-');
				if (!validProductIds.Contains(normalizedProductId))
				{
					var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
					collector.EmitError(configPath, $"Product '{productId}' in changelog.yml is not in the list of available products from config/products.yml. Available products: {availableProducts}");
					return null;
				}
				if (configurationContext.ProductsConfiguration.Products.TryGetValue(normalizedProductId, out var product))
					resolvedProducts.Add(product);
			}
			products = resolvedProducts;
		}

		// Process block configuration
		var block = ParseBlockConfiguration(collector, yamlConfig.Block, configPath, validProductIds);
		if (block == null && collector.Errors > 0)
			return null;

		// Process highlight labels from pivot configuration
		var highlightLabels = yamlConfig.Pivot?.Highlight?.Values;

		// Process products configuration
		ProductsConfig? productsConfig = null;
		if (yamlConfig.Products != null)
			productsConfig = ParseProductsConfig(collector, yamlConfig.Products, configPath, validProductIds);

		// Process bundle configuration
		BundleConfiguration? bundleConfig = null;
		if (yamlConfig.Bundle != null)
			bundleConfig = ParseBundleConfiguration(yamlConfig.Bundle);

		// Process extract configuration
		var extract = new ExtractConfiguration
		{
			ReleaseNotes = yamlConfig.Extract?.ReleaseNotes ?? true,
			Issues = yamlConfig.Extract?.Issues ?? true
		};

		return new ChangelogConfiguration
		{
			Pivot = pivot,
			Types = availableTypes,
			SubTypes = availableSubtypes,
			Lifecycles = lifecycles,
			Areas = availableAreas,
			Products = products,
			LabelToType = labelToType,
			LabelToAreas = labelToAreas,
			Block = block,
			HighlightLabels = highlightLabels,
			ProductsConfiguration = productsConfig,
			Bundle = bundleConfig,
			Extract = extract
		};
	}

	private static PivotConfiguration ConvertPivot(PivotConfigurationYaml yamlPivot)
	{
		Dictionary<string, TypeEntry?>? types = null;
		if (yamlPivot.Types != null)
		{
			types = yamlPivot.Types.ToDictionary(
				kvp => kvp.Key,
				kvp => kvp.Value == null
					? null
					: new TypeEntry
					{
						Labels = kvp.Value.Labels,
						Subtypes = ConvertLenientDictToStringDict(kvp.Value.Subtypes)
					});
		}

		return new PivotConfiguration
		{
			Types = types,
			Subtypes = ConvertLenientDictToStringDict(yamlPivot.Subtypes),
			Areas = ConvertLenientDictToStringDict(yamlPivot.Areas),
			Highlight = JoinLenientList(yamlPivot.Highlight)
		};
	}

	/// <summary>
	/// Converts a dictionary with YamlLenientList values to a dictionary with comma-joined string values.
	/// </summary>
	private static Dictionary<string, string?>? ConvertLenientDictToStringDict(Dictionary<string, YamlLenientList?>? source)
	{
		if (source == null || source.Count == 0)
			return null;

		return source.ToDictionary(
			kvp => kvp.Key,
			kvp => JoinLenientList(kvp.Value)
		);
	}

	/// <summary>
	/// Joins a YamlLenientList into a comma-separated string, or returns null.
	/// </summary>
	private static string? JoinLenientList(YamlLenientList? list) =>
		list?.Values is { Count: > 0 } values ? string.Join(", ", values) : null;

	private ProductsConfig? ParseProductsConfig(
		IDiagnosticsCollector collector,
		ProductsConfigYaml yaml,
		string configPath,
		HashSet<string> validProductIds)
	{
		// Validate available products
		List<string>? available = null;
		var availableValues = yaml.Available?.Values;
		if (availableValues is { Count: > 0 })
		{
			available = [];
			foreach (var productId in availableValues)
			{
				var normalizedProductId = productId.Replace('_', '-');
				if (!validProductIds.Contains(normalizedProductId))
				{
					var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
					collector.EmitError(configPath, $"Product '{productId}' in products_config.available is not in the list of available products from config/products.yml. Available products: {availableProducts}");
					return null;
				}
				available.Add(normalizedProductId);
			}
		}

		// Parse default products
		List<DefaultProduct>? defaultProducts = null;
		if (yaml.Default is { Count: > 0 })
		{
			defaultProducts = [];
			foreach (var defaultYaml in yaml.Default)
			{
				if (string.IsNullOrWhiteSpace(defaultYaml.Product))
				{
					collector.EmitError(configPath, "Default product in products_config.default must have a product ID");
					return null;
				}

				var normalizedProductId = defaultYaml.Product.Replace('_', '-');
				if (!validProductIds.Contains(normalizedProductId))
				{
					var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
					collector.EmitError(configPath, $"Product '{defaultYaml.Product}' in products_config.default is not in the list of available products from config/products.yml. Available products: {availableProducts}");
					return null;
				}

				defaultProducts.Add(new DefaultProduct
				{
					Product = normalizedProductId,
					Lifecycle = defaultYaml.Lifecycle ?? "ga"
				});
			}
		}

		return new ProductsConfig
		{
			Available = available,
			Default = defaultProducts
		};
	}

	private static BundleConfiguration ParseBundleConfiguration(BundleConfigurationYaml yaml)
	{
		Dictionary<string, BundleProfile>? profiles = null;
		if (yaml.Profiles is { Count: > 0 })
		{
			profiles = yaml.Profiles.ToDictionary(
				kvp => kvp.Key,
				kvp => new BundleProfile
				{
					Products = kvp.Value.Products,
					Output = kvp.Value.Output,
					HideFeatures = kvp.Value.HideFeatures?.Values
				});
		}

		return new BundleConfiguration
		{
			Directory = yaml.Directory,
			OutputDirectory = yaml.OutputDirectory,
			Resolve = yaml.Resolve ?? true,
			Profiles = profiles
		};
	}

	private BlockConfiguration? ParseBlockConfiguration(
		IDiagnosticsCollector collector,
		BlockConfigurationYaml? blockYaml,
		string configPath,
		HashSet<string> validProductIds)
	{
		if (blockYaml == null)
			return null;

		Dictionary<string, ProductBlockers>? byProduct = null;

		if (blockYaml.Product is { Count: > 0 })
		{
			byProduct = new Dictionary<string, ProductBlockers>(StringComparer.OrdinalIgnoreCase);

			foreach (var (productKey, productBlockersYaml) in blockYaml.Product)
			{
				// Handle comma-separated product IDs
				var productIds = productKey.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var productId in productIds)
				{
					var normalizedProductId = productId.Replace('_', '-');
					if (!validProductIds.Contains(normalizedProductId))
					{
						var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
						collector.EmitError(configPath, $"Product '{productId}' in block.product in changelog.yml is not in the list of available products from config/products.yml. Available products: {availableProducts}");
						return null;
					}

					var productBlockers = new ProductBlockers
					{
						Create = productBlockersYaml?.Create?.Values,
						Publish = ParsePublishBlocker(productBlockersYaml?.Publish)
					};
					byProduct[normalizedProductId] = productBlockers;
				}
			}
		}

		return new BlockConfiguration
		{
			Create = blockYaml.Create?.Values,
			Publish = ParsePublishBlocker(blockYaml.Publish),
			ByProduct = byProduct
		};
	}

	/// <summary>
	/// Parses a PublishBlockerYaml into a PublishBlocker domain type.
	/// </summary>
	private static PublishBlocker? ParsePublishBlocker(PublishBlockerYaml? yaml)
	{
		if (yaml == null)
			return null;

		var types = yaml.Types?.Values;
		var areas = yaml.Areas?.Values;

		if (types == null && areas == null)
			return null;

		return new PublishBlocker
		{
			Types = types,
			Areas = areas
		};
	}

	/// <summary>
	/// Builds LabelToType mapping by inverting pivot.types entries.
	/// Each label in a type entry maps to that type name.
	/// </summary>
	private static Dictionary<string, string>? BuildLabelToTypeMapping(Dictionary<string, TypeEntryYaml?>? types)
	{
		if (types == null || types.Count == 0)
			return null;

		var labelToType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var (typeName, entry) in types)
		{
			if (entry?.Labels == null)
				continue;

			var labels = entry.Labels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (var label in labels)
				labelToType[label] = typeName;
		}

		return labelToType.Count > 0 ? labelToType : null;
	}

	/// <summary>
	/// Builds LabelToAreas mapping by inverting pivot.areas entries.
	/// Each label in an area entry maps to that area name.
	/// </summary>
	private static Dictionary<string, string>? BuildLabelToAreasMapping(Dictionary<string, YamlLenientList?>? areas)
	{
		if (areas == null || areas.Count == 0)
			return null;

		var labelToAreas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var (areaName, labelList) in areas)
		{
			if (labelList?.Values == null)
				continue;

			foreach (var label in labelList.Values)
				labelToAreas[label] = areaName;
		}

		return labelToAreas.Count > 0 ? labelToAreas : null;
	}
}
