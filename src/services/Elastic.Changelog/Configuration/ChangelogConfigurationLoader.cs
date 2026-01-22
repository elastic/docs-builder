// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
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

			// Validate that changelog.yml values conform to ChangelogConfiguration defaults
			var defaultConfig = ChangelogConfiguration.Default;
			var validProductIds = configurationContext.ProductsConfiguration.Products.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Build updated config with validated values
			IReadOnlyList<string> availableTypes;
			IReadOnlyList<string> availableSubtypes;
			IReadOnlyList<string> availableLifecycles;

			// If available_types is not specified or empty, use defaults
			if (config.AvailableTypes == null || config.AvailableTypes.Count == 0)
				availableTypes = defaultConfig.AvailableTypes;
			else
			{
				// Validate available_types - must be subset of defaults
				foreach (var type in config.AvailableTypes.Where(t => !defaultConfig.AvailableTypes.Contains(t)))
				{
					collector.EmitError(finalConfigPath, $"Type '{type}' in changelog.yml is not in the list of available types. Available types: {string.Join(", ", defaultConfig.AvailableTypes)}");
					return null;
				}
				availableTypes = config.AvailableTypes;
			}

			// If available_subtypes is not specified or empty, use defaults
			if (config.AvailableSubtypes == null || config.AvailableSubtypes.Count == 0)
				availableSubtypes = defaultConfig.AvailableSubtypes;
			else
			{
				// Validate available_subtypes - must be subset of defaults
				foreach (var subtype in config.AvailableSubtypes.Where(s => !defaultConfig.AvailableSubtypes.Contains(s)))
				{
					collector.EmitError(finalConfigPath, $"Subtype '{subtype}' in changelog.yml is not in the list of available subtypes. Available subtypes: {string.Join(", ", defaultConfig.AvailableSubtypes)}");
					return null;
				}
				availableSubtypes = config.AvailableSubtypes;
			}

			// If available_lifecycles is not specified or empty, use defaults
			if (config.AvailableLifecycles == null || config.AvailableLifecycles.Count == 0)
				availableLifecycles = defaultConfig.AvailableLifecycles;
			else
			{
				// Validate available_lifecycles - must be subset of defaults
				foreach (var lifecycle in config.AvailableLifecycles.Where(l => !defaultConfig.AvailableLifecycles.Contains(l)))
				{
					collector.EmitError(finalConfigPath, $"Lifecycle '{lifecycle}' in changelog.yml is not in the list of available lifecycles. Available lifecycles: {string.Join(", ", defaultConfig.AvailableLifecycles)}");
					return null;
				}
				availableLifecycles = config.AvailableLifecycles;
			}

			// Validate render_blockers types against available_types
			if (config.RenderBlockers != null)
			{
				foreach (var (productKey, blockersEntry) in config.RenderBlockers)
				{
					if (blockersEntry?.Types != null && blockersEntry.Types.Count > 0)
					{
						var invalidType = blockersEntry.Types.FirstOrDefault(type => !availableTypes.Contains(type));
						if (invalidType != null)
						{
							collector.EmitError(finalConfigPath, $"Type '{invalidType}' in render_blockers for '{productKey}' is not in the list of available types. Available types: {string.Join(", ", availableTypes)}");
							return null;
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

			// Return validated configuration with updated values
			return config with
			{
				AvailableTypes = availableTypes,
				AvailableSubtypes = availableSubtypes,
				AvailableLifecycles = availableLifecycles,
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
}
