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

		if (yamlConfig.Rules?.Publish == null)
			return null;

		var globalMatch = ParseMatchMode(yamlConfig.Rules.Match) ?? MatchMode.Any;
		var publishMatchAreas = ParseMatchMode(yamlConfig.Rules.Publish.MatchAreas) ?? globalMatch;

		return ParsePublishBlocker(yamlConfig.Rules.Publish, publishMatchAreas);
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

		// Detect old 'block:' key
		if (yamlConfig.Block != null)
		{
			collector.EmitError(configPath, "'block' is no longer supported. Rename to 'rules'. See changelog.example.yml.");
			return null;
		}

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

		// Process rules configuration
		var rules = ParseRulesConfiguration(collector, yamlConfig.Rules, configPath, validProductIds);
		if (rules == null && collector.Errors > 0)
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
			Rules = rules,
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
				kvp => kvp.Value is null
					? new BundleProfile()
					: new BundleProfile
					{
						Products = kvp.Value.Products,
						Output = kvp.Value.Output,
						OutputProducts = kvp.Value.OutputProducts,
						Repo = kvp.Value.Repo,
						Owner = kvp.Value.Owner,
						HideFeatures = kvp.Value.HideFeatures?.Values
					});
		}

		return new BundleConfiguration
		{
			Directory = yaml.Directory,
			OutputDirectory = yaml.OutputDirectory,
			Resolve = yaml.Resolve ?? true,
			Repo = yaml.Repo,
			Owner = yaml.Owner,
			Profiles = profiles
		};
	}

	/// <summary>
	/// Loads changelog configuration from a specific path, treating a missing file as a hard error.
	/// Used in profile mode when an explicit config path was provided (e.g. in tests).
	/// </summary>
	public async Task<ChangelogConfiguration?> LoadChangelogConfigurationRequired(IDiagnosticsCollector collector, string configPath, Cancel ctx)
	{
		if (!fileSystem.File.Exists(configPath))
		{
			collector.EmitError(
				configPath,
				$"Changelog configuration file not found at '{configPath}'. " +
				"Either run 'docs-builder changelog init' to create one, " +
				"or re-run from the folder where changelog.yml exists."
			);
			return null;
		}

		try
		{
			var yamlContent = await fileSystem.File.ReadAllTextAsync(configPath, ctx);
			var yamlConfig = DeserializeConfiguration(yamlContent);
			return ParseConfiguration(collector, yamlConfig, configPath);
		}
		catch (IOException ex)
		{
			collector.EmitError(configPath, $"I/O error loading changelog configuration: {ex.Message}", ex);
			return null;
		}
		catch (UnauthorizedAccessException ex)
		{
			collector.EmitError(configPath, $"Access denied loading changelog configuration: {ex.Message}", ex);
			return null;
		}
		catch (YamlDotNet.Core.YamlException ex)
		{
			collector.EmitError(configPath, $"YAML parsing error in changelog configuration: {ex.Message}", ex);
			return null;
		}
	}

	/// <summary>
	/// Discovers and loads the changelog configuration for profile mode.
	/// Unlike <see cref="LoadChangelogConfiguration"/>, this method treats a missing config file as a
	/// hard error. It searches for <c>changelog.yml</c> then <c>docs/changelog.yml</c> relative to the
	/// current working directory, so the command works when run from any folder that contains the file.
	/// </summary>
	public async Task<ChangelogConfiguration?> LoadChangelogConfigurationForProfileMode(IDiagnosticsCollector collector, Cancel ctx)
	{
		var cwd = fileSystem.Directory.GetCurrentDirectory();
		var candidates = new[]
		{
			fileSystem.Path.Combine(cwd, "changelog.yml"),
			fileSystem.Path.Combine(cwd, "docs", "changelog.yml")
		};

		var foundPath = candidates.FirstOrDefault(fileSystem.File.Exists);

		if (foundPath == null)
		{
			collector.EmitError(
				string.Empty,
				"changelog.yml not found. Profile-based commands require a changelog configuration file. " +
				"Either run 'docs-builder changelog init' to create one, " +
				"or re-run this command from the folder where changelog.yml exists " +
				"(e.g. the project root if the file is at docs/changelog.yml)."
			);
			return null;
		}

		try
		{
			var yamlContent = await fileSystem.File.ReadAllTextAsync(foundPath, ctx);
			var yamlConfig = DeserializeConfiguration(yamlContent);
			return ParseConfiguration(collector, yamlConfig, foundPath);
		}
		catch (IOException ex)
		{
			collector.EmitError(foundPath, $"I/O error loading changelog configuration: {ex.Message}", ex);
			return null;
		}
		catch (UnauthorizedAccessException ex)
		{
			collector.EmitError(foundPath, $"Access denied loading changelog configuration: {ex.Message}", ex);
			return null;
		}
		catch (YamlDotNet.Core.YamlException ex)
		{
			collector.EmitError(foundPath, $"YAML parsing error in changelog configuration: {ex.Message}", ex);
			return null;
		}
	}

	private RulesConfiguration? ParseRulesConfiguration(
		IDiagnosticsCollector collector,
		RulesConfigurationYaml? rulesYaml,
		string configPath,
		HashSet<string> validProductIds)
	{
		if (rulesYaml == null)
			return null;

		// Parse global match mode
		var globalMatch = MatchMode.Any;
		if (!string.IsNullOrWhiteSpace(rulesYaml.Match))
		{
			var parsed = ParseMatchMode(rulesYaml.Match);
			if (parsed == null)
			{
				collector.EmitError(configPath, $"rules.match: '{rulesYaml.Match}' is not valid. Use 'any' or 'all'.");
				return null;
			}
			globalMatch = parsed.Value;
		}

		// Parse create rules
		var createRules = ParseCreateRules(collector, rulesYaml.Create, configPath, validProductIds, "rules.create", globalMatch);
		if (createRules == null && collector.Errors > 0)
			return null;

		// Parse publish rules
		var publishRules = ParsePublishRules(collector, rulesYaml.Publish, configPath, validProductIds, "rules.publish", globalMatch);
		if (publishRules == null && collector.Errors > 0)
			return null;

		return new RulesConfiguration
		{
			Match = globalMatch,
			Create = createRules,
			Publish = publishRules
		};
	}

	private CreateRules? ParseCreateRules(
		IDiagnosticsCollector collector,
		CreateRulesYaml? yaml,
		string configPath,
		HashSet<string> validProductIds,
		string path,
		MatchMode inheritedMatch)
	{
		if (yaml == null)
			return null;

		// Validate mutual exclusivity
		if (yaml.Exclude?.Values is { Count: > 0 } && yaml.Include?.Values is { Count: > 0 })
		{
			collector.EmitError(configPath, $"{path}: cannot have both 'exclude' and 'include'. Use one or the other.");
			return null;
		}

		// Parse match mode
		var match = inheritedMatch;
		if (!string.IsNullOrWhiteSpace(yaml.Match))
		{
			var parsed = ParseMatchMode(yaml.Match);
			if (parsed == null)
			{
				collector.EmitError(configPath, $"{path}.match: '{yaml.Match}' is not valid. Use 'any' or 'all'.");
				return null;
			}
			match = parsed.Value;
		}

		var mode = yaml.Include?.Values is { Count: > 0 } ? FieldMode.Include : FieldMode.Exclude;
		var labels = mode == FieldMode.Include ? yaml.Include?.Values : yaml.Exclude?.Values;

		// Parse per-product overrides
		Dictionary<string, CreateRules>? byProduct = null;
		if (yaml.Products is { Count: > 0 })
		{
			byProduct = new Dictionary<string, CreateRules>(StringComparer.OrdinalIgnoreCase);
			foreach (var (productKey, productYaml) in yaml.Products)
			{
				var productIds = productKey.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var productId in productIds)
				{
					var normalizedProductId = productId.Replace('_', '-');
					if (!validProductIds.Contains(normalizedProductId))
					{
						var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
						collector.EmitError(configPath, $"{path}.products: '{productId}' not in available products. Available: {availableProducts}");
						return null;
					}

					var productRules = ParseCreateRules(collector, productYaml, configPath, validProductIds, $"{path}.products.{normalizedProductId}", match);
					if (productRules == null && collector.Errors > 0)
						return null;
					if (productRules != null)
						byProduct[normalizedProductId] = productRules;
				}
			}
		}

		return new CreateRules
		{
			Labels = labels,
			Mode = mode,
			Match = match,
			ByProduct = byProduct
		};
	}

	private PublishRules? ParsePublishRules(
		IDiagnosticsCollector collector,
		PublishRulesYaml? yaml,
		string configPath,
		HashSet<string> validProductIds,
		string path,
		MatchMode inheritedMatch)
	{
		if (yaml == null)
			return null;

		// Parse match_areas
		var matchAreas = inheritedMatch;
		if (!string.IsNullOrWhiteSpace(yaml.MatchAreas))
		{
			var parsed = ParseMatchMode(yaml.MatchAreas);
			if (parsed == null)
			{
				collector.EmitError(configPath, $"{path}.match_areas: '{yaml.MatchAreas}' is not valid. Use 'any' or 'all'.");
				return null;
			}
			matchAreas = parsed.Value;
		}

		// Parse global publish blocker
		var blocker = ParsePublishBlockerFromYaml(collector, yaml, configPath, path, matchAreas);
		if (blocker == null && collector.Errors > 0)
			return null;

		// Parse per-product overrides
		Dictionary<string, PublishBlocker>? byProduct = null;
		if (yaml.Products is { Count: > 0 })
		{
			byProduct = new Dictionary<string, PublishBlocker>(StringComparer.OrdinalIgnoreCase);
			foreach (var (productKey, productYaml) in yaml.Products)
			{
				var productIds = productKey.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var productId in productIds)
				{
					var normalizedProductId = productId.Replace('_', '-');
					if (!validProductIds.Contains(normalizedProductId))
					{
						var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
						collector.EmitError(configPath, $"{path}.products: '{productId}' not in available products. Available: {availableProducts}");
						return null;
					}

					if (productYaml == null)
						continue;

					var productMatchAreas = matchAreas;
					if (!string.IsNullOrWhiteSpace(productYaml.MatchAreas))
					{
						var parsed = ParseMatchMode(productYaml.MatchAreas);
						if (parsed == null)
						{
							collector.EmitError(configPath, $"{path}.products.{normalizedProductId}.match_areas: '{productYaml.MatchAreas}' is not valid. Use 'any' or 'all'.");
							return null;
						}
						productMatchAreas = parsed.Value;
					}

					var productBlocker = ParsePublishBlockerFromYaml(collector, productYaml, configPath, $"{path}.products.{normalizedProductId}", productMatchAreas);
					if (productBlocker == null && collector.Errors > 0)
						return null;
					if (productBlocker != null)
						byProduct[normalizedProductId] = productBlocker;
				}
			}
		}

		return new PublishRules
		{
			Blocker = blocker,
			ByProduct = byProduct
		};
	}

	private static PublishBlocker? ParsePublishBlockerFromYaml(
		IDiagnosticsCollector collector,
		PublishRulesYaml yaml,
		string configPath,
		string path,
		MatchMode matchAreas)
	{
		// Validate mutual exclusivity for types
		var excludeTypes = yaml.ExcludeTypes?.Values;
		var includeTypes = yaml.IncludeTypes?.Values;
		if (excludeTypes is { Count: > 0 } && includeTypes is { Count: > 0 })
		{
			collector.EmitError(configPath, $"{path}: cannot have both 'exclude_types' and 'include_types'. Use one or the other.");
			return null;
		}

		// Validate mutual exclusivity for areas
		var excludeAreas = yaml.ExcludeAreas?.Values;
		var includeAreas = yaml.IncludeAreas?.Values;
		if (excludeAreas is { Count: > 0 } && includeAreas is { Count: > 0 })
		{
			collector.EmitError(configPath, $"{path}: cannot have both 'exclude_areas' and 'include_areas'. Use one or the other.");
			return null;
		}

		var types = excludeTypes ?? includeTypes;
		var areas = excludeAreas ?? includeAreas;

		if ((types == null || types.Count == 0) && (areas == null || areas.Count == 0))
			return null;

		var typesMode = includeTypes is { Count: > 0 } ? FieldMode.Include : FieldMode.Exclude;
		var areasMode = includeAreas is { Count: > 0 } ? FieldMode.Include : FieldMode.Exclude;

		return new PublishBlocker
		{
			Types = types?.Count > 0 ? types.ToList() : null,
			TypesMode = typesMode,
			Areas = areas?.Count > 0 ? areas.ToList() : null,
			AreasMode = areasMode,
			MatchAreas = matchAreas
		};
	}

	private static PublishBlocker? ParsePublishBlocker(PublishRulesYaml? yaml, MatchMode matchAreas)
	{
		if (yaml == null)
			return null;

		var excludeTypes = yaml.ExcludeTypes?.Values;
		var includeTypes = yaml.IncludeTypes?.Values;
		var excludeAreas = yaml.ExcludeAreas?.Values;
		var includeAreas = yaml.IncludeAreas?.Values;

		var types = excludeTypes ?? includeTypes;
		var areas = excludeAreas ?? includeAreas;

		if ((types == null || types.Count == 0) && (areas == null || areas.Count == 0))
			return null;

		return new PublishBlocker
		{
			Types = types?.Count > 0 ? types.ToList() : null,
			TypesMode = includeTypes is { Count: > 0 } ? FieldMode.Include : FieldMode.Exclude,
			Areas = areas?.Count > 0 ? areas.ToList() : null,
			AreasMode = includeAreas is { Count: > 0 } ? FieldMode.Include : FieldMode.Exclude,
			MatchAreas = matchAreas
		};
	}

	private static MatchMode? ParseMatchMode(string? value) =>
		value?.ToLowerInvariant() switch
		{
			"any" => MatchMode.Any,
			"all" => MatchMode.All,
			_ => string.IsNullOrWhiteSpace(value) ? null : null
		};

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
