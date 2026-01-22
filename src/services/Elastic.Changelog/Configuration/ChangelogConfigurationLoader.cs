// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Changelog.Configuration;

/// <summary>
/// Service for loading and validating changelog configuration
/// </summary>
public class ChangelogConfigurationLoader(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IFileSystem fileSystem
)
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogConfigurationLoader>();

	/// <summary>
	/// Loads changelog configuration from file or returns default configuration
	/// </summary>
	public async Task<ChangelogConfiguration?> LoadChangelogConfiguration(
		IDiagnosticsCollector collector,
		string? configPath,
		Cancel ctx
	)
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
			var deserializer = new StaticDeserializerBuilder(new ChangelogYamlStaticContext())
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.WithTypeConverter(new TypeEntryConverter())
				.Build();

			var config = deserializer.Deserialize<ChangelogConfiguration>(yamlContent);

			// Expand comma-separated product IDs in add_blockers
			Dictionary<string, IReadOnlyList<string>>? expandedBlockers = null;
			if (config.AddBlockers != null && config.AddBlockers.Count > 0)
			{
				expandedBlockers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
				foreach (var kvp in config.AddBlockers)
				{
					var productKeys = kvp.Key.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					foreach (var productKey in productKeys)
					{
						if (expandedBlockers.TryGetValue(productKey, out var existingLabels))
						{
							// Merge labels if product key already exists
							var mergedLabels = existingLabels.Union(kvp.Value, StringComparer.OrdinalIgnoreCase).ToList();
							expandedBlockers[productKey] = mergedLabels;
						}
						else
							expandedBlockers[productKey] = kvp.Value.ToList();
					}
				}
			}

			var validProductIds = configurationContext.ProductsConfiguration.Products.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Compute values from pivot configuration
			IReadOnlyList<string> availableTypes;
			IReadOnlyList<string> availableSubtypes;
			IReadOnlyList<string>? availableAreas;
			Dictionary<string, string>? labelToType;
			Dictionary<string, string>? labelToAreas;

			if (config.Pivot != null)
			{
				// Compute available types from pivot.types keys
				if (config.Pivot.Types != null && config.Pivot.Types.Count > 0)
				{
					// Validate types against enum values using TryParse
					foreach (var typeName in config.Pivot.Types.Keys)
					{
						if (!ChangelogEntryTypeExtensions.TryParse(typeName, out _, ignoreCase: true, allowMatchingMetadataAttribute: true))
						{
							collector.EmitError(finalConfigPath, $"Type '{typeName}' in pivot.types is not a valid type. Valid types: {string.Join(", ", ChangelogConfiguration.DefaultTypes)}");
							return null;
						}
					}

					// Validate required types are present
					foreach (var requiredType in ChangelogConfiguration.RequiredTypes)
					{
						var requiredTypeName = requiredType.ToStringFast(true);
						if (!config.Pivot.Types.Keys.Any(k => k.Equals(requiredTypeName, StringComparison.OrdinalIgnoreCase)))
						{
							collector.EmitError(finalConfigPath, $"Required type '{requiredTypeName}' is missing from pivot.types. Required types: {string.Join(", ", ChangelogConfiguration.RequiredTypes.Select(t => t.ToStringFast(true)))}");
							return null;
						}
					}

					// Validate subtypes only appear under breaking-change
					foreach (var (typeName, typeEntry) in config.Pivot.Types)
					{
						if (typeEntry?.Subtypes != null && typeEntry.Subtypes.Count > 0)
						{
							if (!typeName.Equals(ChangelogEntryType.BreakingChange.ToStringFast(true), StringComparison.OrdinalIgnoreCase))
							{
								collector.EmitError(finalConfigPath, $"Type '{typeName}' has subtypes defined, but subtypes are only allowed for 'breaking-change' type.");
								return null;
							}

							// Validate subtype values against enum
							foreach (var subtypeName in typeEntry.Subtypes.Keys)
							{
								if (!ChangelogEntrySubtypeExtensions.TryParse(subtypeName, out _, ignoreCase: true, allowMatchingMetadataAttribute: true))
								{
									collector.EmitError(finalConfigPath, $"Subtype '{subtypeName}' in pivot.types.{typeName}.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}");
									return null;
								}
							}
						}
					}

					availableTypes = config.Pivot.Types.Keys.ToList();
				}
				else
					availableTypes = ChangelogConfiguration.DefaultTypes;

				// Compute available subtypes from pivot.subtypes keys
				if (config.Pivot.Subtypes != null && config.Pivot.Subtypes.Count > 0)
				{
					// Validate subtypes against enum values using TryParse
					foreach (var subtypeName in config.Pivot.Subtypes.Keys)
					{
						if (!ChangelogEntrySubtypeExtensions.TryParse(subtypeName, out _, ignoreCase: true, allowMatchingMetadataAttribute: true))
						{
							collector.EmitError(finalConfigPath, $"Subtype '{subtypeName}' in pivot.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}");
							return null;
						}
					}
					availableSubtypes = config.Pivot.Subtypes.Keys.ToList();
				}
				else
					availableSubtypes = ChangelogConfiguration.DefaultSubtypes;

				// Compute available areas from pivot.areas keys
				availableAreas = config.Pivot.Areas != null && config.Pivot.Areas.Count > 0
					? config.Pivot.Areas.Keys.ToList()
					: null;

				// Build LabelToType mapping (inverted from pivot.types)
				labelToType = BuildLabelToTypeMapping(config.Pivot.Types);

				// Build LabelToAreas mapping (inverted from pivot.areas)
				labelToAreas = BuildLabelToAreasMapping(config.Pivot.Areas);
			}
			else
			{
				// No pivot configuration - use defaults
				availableTypes = ChangelogConfiguration.DefaultTypes;
				availableSubtypes = ChangelogConfiguration.DefaultSubtypes;
				availableAreas = config.AvailableAreas;
				labelToType = config.LabelToType != null
					? new Dictionary<string, string>(config.LabelToType)
					: null;
				labelToAreas = config.LabelToAreas != null
					? new Dictionary<string, string>(config.LabelToAreas)
					: null;
			}

			// Process available_lifecycles
			IReadOnlyList<string> availableLifecycles;
			if (config.AvailableLifecycles == null || config.AvailableLifecycles.Count == 0)
				availableLifecycles = ChangelogConfiguration.DefaultLifecycles;
			else
			{
				// Validate available_lifecycles - must be subset of defaults
				foreach (var lifecycle in config.AvailableLifecycles.Where(l => !ChangelogConfiguration.DefaultLifecycles.Contains(l)))
				{
					collector.EmitError(finalConfigPath, $"Lifecycle '{lifecycle}' in changelog.yml is not in the list of available lifecycles. Available lifecycles: {string.Join(", ", ChangelogConfiguration.DefaultLifecycles)}");
					return null;
				}
				availableLifecycles = config.AvailableLifecycles;
			}

			// Validate render_blockers types against enum and available_types
			if (config.RenderBlockers != null)
			{
				foreach (var (productKey, blockersEntry) in config.RenderBlockers)
				{
					if (blockersEntry?.Types != null && blockersEntry.Types.Count > 0)
					{
						foreach (var type in blockersEntry.Types)
						{
							// First validate against enum
							if (!ChangelogEntryTypeExtensions.TryParse(type, out _, ignoreCase: true, allowMatchingMetadataAttribute: true))
							{
								collector.EmitError(finalConfigPath, $"Type '{type}' in render_blockers for '{productKey}' is not a valid type. Valid types: {string.Join(", ", ChangelogConfiguration.DefaultTypes)}");
								return null;
							}
							// Then validate against configured available types
							if (!availableTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
							{
								collector.EmitError(finalConfigPath, $"Type '{type}' in render_blockers for '{productKey}' is not in the list of configured available types. Configured types: {string.Join(", ", availableTypes)}");
								return null;
							}
						}
					}
				}
			}

			// Validate available_products (if specified) - must be from products.yml
			if (config.AvailableProducts != null && config.AvailableProducts.Count > 0)
			{
				foreach (var product in config.AvailableProducts)
				{
					var normalizedProductId = product.Replace('_', '-');
					if (!validProductIds.Contains(normalizedProductId))
					{
						var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
						collector.EmitError(finalConfigPath, $"Product '{product}' in changelog.yml is not in the list of available products from config/products.yml. Available products: {availableProducts}");
						return null;
					}
				}
			}

			// Validate add_blockers (if specified) - product keys must be from products.yml
			if (expandedBlockers != null && expandedBlockers.Count > 0)
			{
				foreach (var productKey in expandedBlockers.Keys)
				{
					var normalizedProductId = productKey.Replace('_', '-');
					if (!validProductIds.Contains(normalizedProductId))
					{
						var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
						collector.EmitError(finalConfigPath, $"Product '{productKey}' in add_blockers in changelog.yml is not in the list of available products from config/products.yml. Available products: {availableProducts}");
						return null;
					}
				}
			}

			// Return validated configuration with computed values
			return config with
			{
				AvailableTypes = availableTypes,
				AvailableSubtypes = availableSubtypes,
				AvailableLifecycles = availableLifecycles,
				AvailableAreas = availableAreas,
				LabelToType = labelToType,
				LabelToAreas = labelToAreas,
				AddBlockers = expandedBlockers
			};
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

	/// <summary>
	/// Builds LabelToType mapping by inverting pivot.types entries.
	/// Each label in a type entry maps to that type name.
	/// </summary>
	private static Dictionary<string, string>? BuildLabelToTypeMapping(Dictionary<string, TypeEntry?>? types)
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
	private static Dictionary<string, string>? BuildLabelToAreasMapping(Dictionary<string, string?>? areas)
	{
		if (areas == null || areas.Count == 0)
			return null;

		var labelToAreas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var (areaName, labels) in areas)
		{
			if (string.IsNullOrWhiteSpace(labels))
				continue;

			var labelList = labels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (var label in labelList)
				labelToAreas[label] = areaName;
		}

		return labelToAreas.Count > 0 ? labelToAreas : null;
	}
}
