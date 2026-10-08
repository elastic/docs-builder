// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Serialization;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Service for loading and validating changelog configuration
/// </summary>
public class ChangelogConfigurationLoader(ILoggerFactory logFactory, IConfigurationContext configurationContext, IFileSystem fileSystem)
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogConfigurationLoader>();

	private static readonly IDeserializer ConfigurationDeserializer = new StaticDeserializerBuilder(new YamlStaticContext())
		.WithNamingConvention(UnderscoredNamingConvention.Instance)
		.WithTypeConverter(new YamlLenientListConverter())
		.WithTypeConverter(new TypeEntryYamlConverter())
		.Build();

	/// <summary>
	/// Deserializes changelog configuration YAML content.
	/// </summary>
	internal static ChangelogConfigurationDto DeserializeConfiguration(string yaml) =>
		// an empty document (or one of only comments) deserializes to null: it is a configuration that says nothing, as every other config loader reads it
		ConfigurationDeserializer.Deserialize<ChangelogConfigurationDto>(yaml)
			?? new ChangelogConfigurationDto();

	/// <summary>
	/// Returns the bundle profile names declared in a <c>changelog.yml</c> YAML string,
	/// in definition order. Returns an empty list when the YAML has no <c>bundle.profiles</c> block.
	/// </summary>
	public static IReadOnlyList<string> ReadBundleProfileNames(string changelogYaml)
	{
		var config = DeserializeConfiguration(changelogYaml);
		return config.Bundle?.Profiles?.Keys.ToList() ?? [];
	}

	/// <summary>
	/// Loads the publish blocker configuration from a changelog.
	/// </summary>
	/// <param name="fileSystem">The file system to read from.</param>
	/// <param name="configPath">The path to the changelog.yml configuration file.</param>
	/// <returns>The publish blocker configuration, or null if not found.</returns>
	internal static PublishBlocker? LoadPublishBlocker(IFileSystem fileSystem, string configPath)
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
	/// Loads changelog configuration from file or returns the (checked) default configuration. Every diagnostic rides in the result; an error means no value.
	/// </summary>
	public Task<Result<ChangelogConfiguration>> Load(string? configPath, Cancel ctx) =>
		Run(collector => LoadCore(collector, configPath, ctx));

	/// <summary>
	/// Loads changelog configuration from a specific path, treating a missing file as a hard error.
	/// Used in profile mode when an explicit config path was provided (e.g. in tests).
	/// </summary>
	public Task<Result<ChangelogConfiguration>> LoadRequired(string configPath, Cancel ctx) =>
		Run(collector => LoadRequiredCore(collector, configPath, ctx));

	/// <summary>
	/// Discovers and loads the changelog configuration for profile mode. Unlike <see cref="Load"/>, a missing file is a hard error. It searches for
	/// <c>changelog.yml</c> then <c>docs/changelog.yml</c> relative to the current working directory, so the command works when run from any folder that contains the file.
	/// </summary>
	public Task<Result<ChangelogConfiguration>> LoadForProfileMode(Cancel ctx) => Run(collector => LoadForProfileModeCore(collector, ctx));

	// The parse helpers below report to a collector as they find things; a private buffer turns that into the Result (this load's
	// diagnostics only, in emission order). Not cached: a command loads changelog.yml once or twice and a changed file must be seen.
	private static async Task<Result<ChangelogConfiguration>> Run(Func<IDiagnosticsCollector, Task<ChangelogConfiguration?>> load)
	{
		var buffer = new BufferingDiagnosticsCollector();
		var configuration = await load(buffer);
		return configuration is null || buffer.Errors > 0
			? Result.Failure<ChangelogConfiguration>(buffer.Diagnostics)
			: Result.Success(configuration, buffer.Diagnostics);
	}

	private async Task<ChangelogConfiguration?> LoadCore(IDiagnosticsCollector collector, string? configPath, Cancel ctx)
	{
		// Determine config file path
		var finalConfigPath = configPath ?? fileSystem.Path.Join(fileSystem.Directory.GetCurrentDirectory(), "docs", "changelog.yml");

		if (!fileSystem.File.Exists(finalConfigPath))
		{
			// Use default configuration if file doesn't exist. The defaults go through the same set as a file would (and are
			// expected to satisfy it; a test pins that), so no record leaves this loader unchecked.
			_logger.LogWarning("Changelog configuration not found at {ConfigPath}, using defaults", finalConfigPath);
			return Check(collector, ChangelogConfiguration.Default, finalConfigPath);
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

	private ChangelogConfiguration? ParseConfiguration(
		IDiagnosticsCollector collector,
		ChangelogConfigurationDto yamlConfig,
		string configPath
	)
	{
		var validProductIds = configurationContext.Catalog.Products.Products.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

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
		Dictionary<string, List<string>>? labelToAreas;
		Dictionary<string, string>? labelToProducts;
		Dictionary<string, string>? labelToFeatures;
		PivotConfiguration? pivot = null;

		if (yamlConfig.Pivot != null)
		{
			// Convert YAML pivot to domain pivot
			pivot = ConvertPivot(yamlConfig.Pivot);

			// Compute available types and subtypes from the pivot keys; ChangelogConfigurationConstraints checks they are valid
			availableTypes = yamlConfig.Pivot.Types is { Count: > 0 }
				? yamlConfig.Pivot.Types.Keys.ToList()
				: ChangelogConfiguration.DefaultTypes;
			availableSubtypes = yamlConfig.Pivot.Subtypes is { Count: > 0 }
				? yamlConfig.Pivot.Subtypes.Keys.ToList()
				: ChangelogConfiguration.DefaultSubtypes;

			// Compute available areas from pivot.areas keys
			availableAreas = yamlConfig.Pivot.Areas != null && yamlConfig.Pivot.Areas.Count > 0
				? yamlConfig.Pivot.Areas.Keys.ToList()
				: null;

			// Build LabelToType mapping (inverted from pivot.types)
			labelToType = BuildLabelToTypeMapping(yamlConfig.Pivot.Types);

			// Build LabelToAreas mapping (inverted from pivot.areas)
			labelToAreas = BuildLabelToAreasMapping(yamlConfig.Pivot.Areas);

			labelToProducts = BuildLabelToProductsMapping(yamlConfig.Pivot.Products);
			labelToFeatures = BuildLabelToFeaturesMapping(yamlConfig.Pivot.Features);
		}
		else
		{
			// No pivot configuration - use defaults
			availableTypes = ChangelogConfiguration.DefaultTypes;
			availableSubtypes = ChangelogConfiguration.DefaultSubtypes;
			availableAreas = null;
			labelToType = null;
			labelToAreas = null;
			labelToProducts = null;
			labelToFeatures = null;
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
					collector.EmitError(
						configPath,
						$"Lifecycle '{lifecycleStr}' in changelog.yml is not valid. Valid lifecycles: {string.Join(", ", ChangelogConfiguration.DefaultLifecycles.Select(l => l.ToStringFast(true)))}"
					);
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
				var normalizedProductId = ProductNames.Normalize(productId);
				if (!validProductIds.Contains(normalizedProductId))
				{
					var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
					collector.EmitError(
						configPath,
						$"Product '{productId}' in changelog.yml is not in the list of available products from config/products.yml. Available products: {availableProducts}"
					);
					return null;
				}
				if (configurationContext.Catalog.Products.Products.TryGetValue(normalizedProductId, out var product))
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
		{
			productsConfig = ParseProductsConfig(collector, yamlConfig.Products, configPath, validProductIds);
			if (productsConfig == null)
				return null;
		}

		// Process bundle configuration
		BundleConfiguration? bundleConfig = null;
		if (yamlConfig.Bundle != null)
		{
			bundleConfig = ParseBundleConfiguration(collector, configPath, yamlConfig.Bundle, validProductIds);
			if (bundleConfig == null)
				return null;
		}

		// Process extract configuration
		var extract = new ExtractConfiguration
		{
			ReleaseNotes = yamlConfig.Extract?.ReleaseNotes ?? true,
			Issues = yamlConfig.Extract?.Issues ?? true,
			StripTitlePrefix = yamlConfig.Extract?.StripTitlePrefix ?? false
		};

		// Process filename strategy
		var filenameStrategy = FilenameStrategy.Pr;
		if (!string.IsNullOrWhiteSpace(yamlConfig.Filename))
		{
			if (
				!FilenameStrategyExtensions.TryParse(
					yamlConfig.Filename,
					out var parsed,
					ignoreCase: true,
					allowMatchingMetadataAttribute: true
				)
			)
			{
				collector.EmitError(
					configPath,
					$"filename: '{yamlConfig.Filename}' is not valid. The only supported value is 'pr'. Changelog entries are keyed by PR number; for items with no PR use 'changelog note'."
				);
				return null;
			}

			filenameStrategy = parsed;
		}

		var labelToAreasReadOnly = labelToAreas?.ToDictionary(
			kvp => kvp.Key,
			kvp => (IReadOnlyList<string>)kvp.Value,
			StringComparer.OrdinalIgnoreCase
		);

		var configuration = new ChangelogConfiguration
		{
			Pivot = pivot,
			Types = availableTypes,
			SubTypes = availableSubtypes,
			Lifecycles = lifecycles,
			Areas = availableAreas,
			Products = products,
			LabelToType = labelToType,
			LabelToAreas = labelToAreasReadOnly,
			LabelToProducts = labelToProducts,
			LabelToFeatures = labelToFeatures,
			Rules = rules,
			HighlightLabels = highlightLabels,
			ProductsConfiguration = productsConfig,
			Bundle = bundleConfig,
			Extract = extract,
			Filename = filenameStrategy
		};

		return Check(collector, configuration, configPath);
	}

	// The one check of a changelog configuration, whether it was read from a file or is the built-in default.
	private ChangelogConfiguration? Check(IDiagnosticsCollector collector, ChangelogConfiguration configuration, string configPath)
	{
		var report = ChangelogConfigurationConstraints.All.Report(
			new ChangelogConfigurationInCatalog(configuration, configurationContext.Catalog.Products)
		);
		return collector.Write(report, configPath) ? configuration : null;
	}

	private static PivotConfiguration ConvertPivot(ChangelogPivotDto yamlPivot)
	{
		Dictionary<string, TypeEntry?>? types = null;
		if (yamlPivot.Types != null)
		{
			types = yamlPivot.Types.ToDictionary(
				kvp => kvp.Key,
				kvp => kvp.Value == null
					? null
					: new TypeEntry { Labels = kvp.Value.Labels, Subtypes = ConvertLenientDictToStringDict(kvp.Value.Subtypes) }
			);
		}

		return new PivotConfiguration
		{
			Types = types,
			Subtypes = ConvertLenientDictToStringDict(yamlPivot.Subtypes),
			Areas = ConvertLenientDictToStringDict(yamlPivot.Areas),
			Products = ConvertLenientDictToStringDict(yamlPivot.Products),
			Features = ConvertLenientDictToStringDict(yamlPivot.Features),
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

		return source.ToDictionary(kvp => kvp.Key, kvp => JoinLenientList(kvp.Value));
	}

	/// <summary>
	/// Joins a YamlLenientList into a comma-separated string, or returns null.
	/// </summary>
	private static string? JoinLenientList(YamlLenientList? list) =>
		list?.Values is { Count: > 0 } values ? string.Join(", ", values) : null;

	private ProductsConfig? ParseProductsConfig(
		IDiagnosticsCollector collector,
		ChangelogProductsDto yaml,
		string configPath,
		HashSet<string> validProductIds
	)
	{
		// products.available ids were resolved against products.yml with the resolved product list
		var available = yaml.Available?.Values is { Count: > 0 } availableValues
			? availableValues.Select(id => ProductNames.Normalize(id)).ToList()
			: null;

		// Parse default products
		List<DefaultProduct>? defaultProducts = null;
		if (yaml.Default is { Count: > 0 })
		{
			defaultProducts = [];
			foreach (var defaultYaml in yaml.Default)
			{
				// A missing id is kept, for ChangelogConfigurationConstraints to report
				var normalizedProductId = ProductNames.Normalize(defaultYaml.Product);
				if (!string.IsNullOrWhiteSpace(normalizedProductId) && !validProductIds.Contains(normalizedProductId))
				{
					var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
					collector.EmitError(
						configPath,
						$"Product '{defaultYaml.Product}' in products_config.default is not in the list of available products from config/products.yml. Available products: {availableProducts}"
					);
					return null;
				}

				defaultProducts.Add(new DefaultProduct { Product = normalizedProductId, Lifecycle = defaultYaml.Lifecycle ?? "ga" });
			}
		}

		return new ProductsConfig { Available = available, Default = defaultProducts };
	}

	private static BundleConfiguration? ParseBundleConfiguration(
		IDiagnosticsCollector collector,
		string configPath,
		ChangelogBundleDto yaml,
		HashSet<string> validProductIds
	)
	{
		if (yaml.Resolve != null)
			collector.EmitWarning(
				configPath,
				"bundle.resolve is deprecated and ignored. Resolved bundles are now the only format. Remove 'resolve' from bundle in changelog.yml."
			);

		// Entries are trimmed; one that isn't owner/repo is kept as authored, for ChangelogConfigurationConstraints to report
		IReadOnlyList<string>? linkAllowRepos = yaml.LinkAllowRepos is { } linkAllow
			? [.. (linkAllow.Values ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => IsOwnerRepo(v.Trim()) ? v.Trim() : v)]
			: null;

		Dictionary<string, BundleProfile>? profiles = null;
		if (yaml.Profiles is { Count: > 0 })
		{
			profiles = [];
			foreach (var (profileName, profileYaml) in yaml.Profiles)
			{
				if (profileYaml is null)
				{
					profiles[profileName] = new BundleProfile();
					continue;
				}

				string? product = null;
				if (!string.IsNullOrWhiteSpace(profileYaml.Product))
				{
					var normalizedProduct = ProductNames.Normalize(profileYaml.Product);
					if (!validProductIds.Contains(normalizedProduct))
					{
						var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
						collector.EmitError(
							configPath,
							$"bundle.profiles.{profileName}.product: '{profileYaml.Product}' is not in products.yml. Available products: {availableProducts}"
						);
						return null;
					}
					product = normalizedProduct;
				}

#pragma warning disable CS0618
				profiles[profileName] = new BundleProfile
				{
					Product = product,
					Products = profileYaml.Products,
					Output = profileYaml.Output,
					OutputDirectory = profileYaml.OutputDirectory,
					OutputProducts = profileYaml.OutputProducts,
					Description = profileYaml.Description,
					Repo = profileYaml.Repo,
					Owner = profileYaml.Owner,
					Branch = profileYaml.Branch,
					HideFeatures = profileYaml.HideFeatures?.Values,
					ReleaseDates = profileYaml.ReleaseDates,
					Source = profileYaml.Source
				};
#pragma warning restore CS0618
			}
		}

		var releases = ParseBundleReleases(collector, configPath, yaml.Releases, profiles, validProductIds);
		if (releases == null && collector.Errors > 0)
			return null;

		return new BundleConfiguration
		{
			Directory = yaml.Directory,
			OutputDirectory = yaml.OutputDirectory,
			UseLocalChangelogs = yaml.UseLocalChangelogs ?? false,
			Description = yaml.Description,
			Repo = yaml.Repo,
			Owner = yaml.Owner,
			Branch = yaml.Branch,
			ReleaseDates = yaml.ReleaseDates,
			LinkAllowRepos = linkAllowRepos,
			Profiles = profiles,
			Releases = releases
		};
	}

	private static BundleReleases? ParseBundleReleases(
		IDiagnosticsCollector collector,
		string configPath,
		ChangelogBundleReleasesDto? yaml,
		Dictionary<string, BundleProfile>? profiles,
		HashSet<string> validProductIds
	)
	{
		if (yaml == null)
			return null;

		// Tags that map to no defined profile are reported by ChangelogConfigurationConstraints
		Dictionary<string, string>? github = yaml.Github is { Count: > 0 } ? new(yaml.Github) : null;

		Dictionary<string, string>? products = null;
		if (yaml.Products is { Count: > 0 })
		{
			products = [];
			foreach (var (product, profile) in yaml.Products)
			{
				if (string.IsNullOrWhiteSpace(profile))
				{
					collector.EmitError(configPath, $"bundle.releases.products['{product}']: must map to a non-empty profile name.");
					return null;
				}
				var normalizedProduct = ProductNames.Normalize(product);
				if (!validProductIds.Contains(normalizedProduct))
				{
					var available = string.Join(", ", validProductIds.OrderBy(p => p));
					collector.EmitError(
						configPath,
						$"bundle.releases.products key '{product}' is not in products.yml. Available products: {available}"
					);
					return null;
				}
				if (products.ContainsKey(normalizedProduct))
				{
					collector.EmitError(
						configPath,
						$"bundle.releases.products: duplicate normalized key '{normalizedProduct}' (from '{product}'). Use only one spelling per product."
					);
					return null;
				}
				if (profiles == null || !profiles.ContainsKey(profile))
				{
					collector.EmitError(
						configPath,
						$"bundle.releases.products['{product}']: profile '{profile}' is not defined in bundle.profiles."
					);
					return null;
				}
				products[normalizedProduct] = profile;
			}
		}

		if (github == null && products == null)
			return null;

		return new BundleReleases { Github = github, Products = products };
	}

	private async Task<ChangelogConfiguration?> LoadRequiredCore(IDiagnosticsCollector collector, string configPath, Cancel ctx)
	{
		if (!fileSystem.File.Exists(configPath))
		{
			collector.EmitError(
				configPath,
				$"Changelog configuration file not found at '{configPath}'. " + "Either run 'docs-builder changelog init' to create one, " +
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

	private async Task<ChangelogConfiguration?> LoadForProfileModeCore(IDiagnosticsCollector collector, Cancel ctx)
	{
		var cwd = fileSystem.Directory.GetCurrentDirectory();
		var candidates = new[] { fileSystem.Path.Join(cwd, "changelog.yml"), fileSystem.Path.Join(cwd, "docs", "changelog.yml") };

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
		ChangelogRulesDto? rulesYaml,
		string configPath,
		HashSet<string> validProductIds
	)
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
				collector.EmitError(configPath, $"rules.match: '{rulesYaml.Match}' is not valid. Use 'any', 'all', or 'conjunction'.");
				return null;
			}
			globalMatch = parsed.Value;
		}

		// Parse create rules
		var createRules = ParseCreateRules(collector, rulesYaml.Create, configPath, validProductIds, "rules.create", globalMatch);
		if (createRules == null && collector.Errors > 0)
			return null;

		// Parse bundle rules
		var bundleRules = ParseBundleRules(collector, rulesYaml.Bundle, configPath, validProductIds, globalMatch);
		if (bundleRules == null && collector.Errors > 0)
			return null;

		// Parse publish rules — emit deprecation warning when present
		if (rulesYaml.Publish != null)
			collector.EmitWarning(
				configPath,
				"rules.publish is deprecated and no longer used by the changelog render command. Move type/area filtering to rules.bundle, which applies at bundle time instead of render time."
			);

		// Note: rules.publish is no longer used by changelog render; set to null so it's never applied
		// The warning above alerts users they need to migrate to rules.bundle

		return new RulesConfiguration
		{
			Match = globalMatch,
			Create = createRules,
			Bundle = bundleRules,
			Publish =
				null // rules.publish is retired; filtering happens at bundle time via rules.bundle
		};
	}

	private BundleRules? ParseBundleRules(
		IDiagnosticsCollector collector,
		ChangelogBundleRulesDto? yaml,
		string configPath,
		HashSet<string> validProductIds,
		MatchMode inheritedMatch
	)
	{
		if (yaml == null)
			return null;

		// Parse and validate product lists; ChangelogConfigurationConstraints checks only one of them is set
		var excludeProducts = ParseAndValidateProductList(
			collector,
			yaml.ExcludeProducts,
			configPath,
			validProductIds,
			"rules.bundle.exclude_products"
		);
		if (excludeProducts == null && collector.Errors > 0)
			return null;

		var includeProducts = ParseAndValidateProductList(
			collector,
			yaml.IncludeProducts,
			configPath,
			validProductIds,
			"rules.bundle.include_products"
		);
		if (includeProducts == null && collector.Errors > 0)
			return null;

		// Parse match_products
		var matchProducts = inheritedMatch;
		if (!string.IsNullOrWhiteSpace(yaml.MatchProducts))
		{
			var parsed = ParseMatchMode(yaml.MatchProducts);
			if (parsed == null)
			{
				collector.EmitError(
					configPath,
					$"rules.bundle.match_products: '{yaml.MatchProducts}' is not valid. Use 'any', 'all', or 'conjunction'."
				);
				return null;
			}
			matchProducts = parsed.Value;
		}

		// Parse match_areas (inherited from globalMatch if omitted)
		var matchAreas = inheritedMatch;
		if (!string.IsNullOrWhiteSpace(yaml.MatchAreas))
		{
			var parsed = ParseMatchMode(yaml.MatchAreas);
			if (parsed == null)
			{
				collector.EmitError(
					configPath,
					$"rules.bundle.match_areas: '{yaml.MatchAreas}' is not valid. Use 'any', 'all', or 'conjunction'."
				);
				return null;
			}
			matchAreas = parsed.Value;
		}

		// Parse global type/area blocker (reusing ChangelogPublishRulesDto parsing logic)
		var blockerYaml = new ChangelogPublishRulesDto
		{
			MatchAreas = yaml.MatchAreas,
			ExcludeTypes = yaml.ExcludeTypes,
			IncludeTypes = yaml.IncludeTypes,
			ExcludeAreas = yaml.ExcludeAreas,
			IncludeAreas = yaml.IncludeAreas
		};
		var blocker = ParsePublishBlockerFromYaml(collector, blockerYaml, configPath, "rules.bundle", matchAreas);
		if (blocker == null && collector.Errors > 0)
			return null;

		// Parse per-product overrides
		Dictionary<string, BundlePerProductRule>? byProduct = null;
		if (yaml.Products is { Count: > 0 })
		{
			byProduct = [with(StringComparer.OrdinalIgnoreCase)];
			foreach (var (productKey, productYaml) in yaml.Products)
			{
				var productIds = productKey.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var productId in productIds)
				{
					var normalizedProductId = ProductNames.Normalize(productId);
					if (!validProductIds.Contains(normalizedProductId))
					{
						var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
						collector.EmitError(
							configPath,
							$"rules.bundle.products: '{productId}' not in available products. Available: {availableProducts}"
						);
						return null;
					}

					if (productYaml == null)
						continue;

					// Parse product lists for this context
					var contextExcludeProducts = ParseAndValidateProductList(
						collector,
						productYaml.ExcludeProducts,
						configPath,
						validProductIds,
						$"rules.bundle.products.{normalizedProductId}.exclude_products"
					);
					if (contextExcludeProducts == null && collector.Errors > 0)
						return null;

					var contextIncludeProducts = ParseAndValidateProductList(
						collector,
						productYaml.IncludeProducts,
						configPath,
						validProductIds,
						$"rules.bundle.products.{normalizedProductId}.include_products"
					);
					if (contextIncludeProducts == null && collector.Errors > 0)
						return null;

					// Mode 3: global rules.bundle product lists are not used for filtering — do not warn about
					// subset relationships between global and per-product include/exclude lists (would mislead authors).

					// Parse match_products for this context
					var contextMatchProducts = matchProducts;
					if (!string.IsNullOrWhiteSpace(productYaml.MatchProducts))
					{
						var parsed = ParseMatchMode(productYaml.MatchProducts);
						if (parsed == null)
						{
							collector.EmitError(
								configPath,
								$"rules.bundle.products.{normalizedProductId}.match_products: '{productYaml.MatchProducts}' is not valid. Use 'any', 'all', or 'conjunction'."
							);
							return null;
						}
						contextMatchProducts = parsed.Value;
					}

					// Parse type/area blocker
					var productMatchAreas = matchAreas;
					if (!string.IsNullOrWhiteSpace(productYaml.MatchAreas))
					{
						var parsedMode = ParseMatchMode(productYaml.MatchAreas);
						if (parsedMode == null)
						{
							collector.EmitError(
								configPath,
								$"rules.bundle.products.{normalizedProductId}.match_areas: '{productYaml.MatchAreas}' is not valid. Use 'any', 'all', or 'conjunction'."
							);
							return null;
						}
						productMatchAreas = parsedMode.Value;
					}

					var productBlockerYaml = new ChangelogPublishRulesDto
					{
						MatchAreas = productYaml.MatchAreas,
						ExcludeTypes = productYaml.ExcludeTypes,
						IncludeTypes = productYaml.IncludeTypes,
						ExcludeAreas = productYaml.ExcludeAreas,
						IncludeAreas = productYaml.IncludeAreas
					};
					var productBlocker = ParsePublishBlockerFromYaml(
						collector,
						productBlockerYaml,
						configPath,
						$"rules.bundle.products.{normalizedProductId}",
						productMatchAreas
					);
					if (productBlocker == null && collector.Errors > 0)
						return null;

					// Create per-product rule if any rules are defined (product filtering OR type/area blocking)
					if (productBlocker != null || contextIncludeProducts != null || contextExcludeProducts != null)
					{
						byProduct[normalizedProductId] = new BundlePerProductRule
						{
							Blocker = productBlocker,
							IncludeProducts = contextIncludeProducts,
							ExcludeProducts = contextExcludeProducts,
							MatchProducts = contextMatchProducts
						};
					}
				}
			}
		}

		if (yaml.Products is { Count: > 0 })
		{
			var hasGlobalProductFilters = (excludeProducts?.Count ?? 0) > 0 || (includeProducts?.Count ?? 0) > 0;
			if (hasGlobalProductFilters || blocker != null)
			{
				collector.EmitHint(
					configPath,
					"rules.bundle: When 'products' is present, global include_products, exclude_products, and type/area rules are not applied for filtering; configure filters under each product key or use global-only rules.bundle (no 'products' section). " +
						"See: https://github.com/elastic/docs-builder/blob/main/docs/contribute/configure-changelogs-ref.md#rules-bundle"
				);
			}
		}

		return new BundleRules
		{
			ExcludeProducts = excludeProducts,
			IncludeProducts = includeProducts,
			MatchProducts = matchProducts,
			Blocker = blocker,
			ByProduct = byProduct?.Count > 0 ? byProduct : null
		};
	}

	private static IReadOnlyList<string>? ParseAndValidateProductList(
		IDiagnosticsCollector collector,
		YamlLenientList? list,
		string configPath,
		HashSet<string> validProductIds,
		string fieldPath
	)
	{
		if (list?.Values is not { Count: > 0 } values)
			return null;

		var result = new List<string>();
		foreach (var rawId in values)
		{
			var normalizedId = ProductNames.Normalize(rawId);
			if (!validProductIds.Contains(normalizedId))
			{
				var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
				collector.EmitError(
					configPath,
					$"{fieldPath}: '{rawId}' is not in the list of available products. Available products: {availableProducts}"
				);
				return null;
			}
			result.Add(normalizedId);
		}
		return result;
	}

	private CreateRules? ParseCreateRules(
		IDiagnosticsCollector collector,
		ChangelogCreateRulesDto? yaml,
		string configPath,
		HashSet<string> validProductIds,
		string path,
		MatchMode inheritedMatch
	)
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
				collector.EmitError(configPath, $"{path}.match: '{yaml.Match}' is not valid. Use 'any', 'all', or 'conjunction'.");
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
			byProduct = [with(StringComparer.OrdinalIgnoreCase)];
			foreach (var (productKey, productYaml) in yaml.Products)
			{
				var productIds = productKey.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var productId in productIds)
				{
					var normalizedProductId = ProductNames.Normalize(productId);
					if (!validProductIds.Contains(normalizedProductId))
					{
						var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
						collector.EmitError(
							configPath,
							$"{path}.products: '{productId}' not in available products. Available: {availableProducts}"
						);
						return null;
					}

					var productRules = ParseCreateRules(
						collector,
						productYaml,
						configPath,
						validProductIds,
						$"{path}.products.{normalizedProductId}",
						match
					);
					if (productRules == null && collector.Errors > 0)
						return null;
					if (productRules != null)
						byProduct[normalizedProductId] = productRules;
				}
			}
		}

		return new CreateRules { Labels = labels, Mode = mode, Match = match, ByProduct = byProduct };
	}

	private PublishRules? ParsePublishRules(
		IDiagnosticsCollector collector,
		ChangelogPublishRulesDto? yaml,
		string configPath,
		HashSet<string> validProductIds,
		string path,
		MatchMode inheritedMatch
	)
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
				collector.EmitError(
					configPath,
					$"{path}.match_areas: '{yaml.MatchAreas}' is not valid. Use 'any', 'all', or 'conjunction'."
				);
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
			byProduct = [with(StringComparer.OrdinalIgnoreCase)];
			foreach (var (productKey, productYaml) in yaml.Products)
			{
				var productIds = productKey.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var productId in productIds)
				{
					var normalizedProductId = ProductNames.Normalize(productId);
					if (!validProductIds.Contains(normalizedProductId))
					{
						var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
						collector.EmitError(
							configPath,
							$"{path}.products: '{productId}' not in available products. Available: {availableProducts}"
						);
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
							collector.EmitError(
								configPath,
								$"{path}.products.{normalizedProductId}.match_areas: '{productYaml.MatchAreas}' is not valid. Use 'any', 'all', or 'conjunction'."
							);
							return null;
						}
						productMatchAreas = parsed.Value;
					}

					var productBlocker = ParsePublishBlockerFromYaml(
						collector,
						productYaml,
						configPath,
						$"{path}.products.{normalizedProductId}",
						productMatchAreas
					);
					if (productBlocker == null && collector.Errors > 0)
						return null;
					if (productBlocker != null)
						byProduct[normalizedProductId] = productBlocker;
				}
			}
		}

		return new PublishRules { Blocker = blocker, ByProduct = byProduct };
	}

	private static PublishBlocker? ParsePublishBlockerFromYaml(
		IDiagnosticsCollector collector,
		ChangelogPublishRulesDto yaml,
		string configPath,
		string path,
		MatchMode matchAreas
	)
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

	private static PublishBlocker? ParsePublishBlocker(ChangelogPublishRulesDto? yaml, MatchMode matchAreas)
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

	private static bool IsOwnerRepo(string entry)
	{
		var slash = entry.IndexOf('/');
		return slash > 0 && slash == entry.LastIndexOf('/') && slash < entry.Length - 1;
	}

	private static MatchMode? ParseMatchMode(string? value) => value?.ToLowerInvariant() switch
	{
		"any" => MatchMode.Any,
		"all" => MatchMode.All,
		"conjunction" => MatchMode.Conjunction,
		_ => string.IsNullOrWhiteSpace(value) ? null : null
	};

	/// <summary>
	/// Builds LabelToType mapping by inverting pivot.types entries.
	/// Each label in a type entry maps to that type name.
	/// </summary>
	private static Dictionary<string, string>? BuildLabelToTypeMapping(Dictionary<string, ChangelogTypeEntryDto?>? types)
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
	/// Each label in an area entry maps to that area name. The same label may appear under multiple areas; all area names are collected.
	/// </summary>
	private static Dictionary<string, List<string>>? BuildLabelToAreasMapping(Dictionary<string, YamlLenientList?>? areas)
	{
		if (areas == null || areas.Count == 0)
			return null;

		var labelToAreas = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

		foreach (var (areaName, labelList) in areas)
		{
			if (labelList?.Values == null)
				continue;

			foreach (var label in labelList.Values)
			{
				if (!labelToAreas.TryGetValue(label, out var areaNames))
				{
					areaNames = [];
					labelToAreas[label] = areaNames;
				}

				if (!areaNames.Exists(a => a.Equals(areaName, StringComparison.OrdinalIgnoreCase)))
					areaNames.Add(areaName);
			}
		}

		return labelToAreas.Count > 0 ? labelToAreas : null;
	}

	/// <summary>
	/// Builds LabelToProducts mapping by inverting pivot.products entries.
	/// Each label in a product entry maps to that product spec string.
	/// </summary>
	private static Dictionary<string, string>? BuildLabelToProductsMapping(Dictionary<string, YamlLenientList?>? products)
	{
		if (products == null || products.Count == 0)
			return null;

		var labelToProducts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var (productSpec, labelList) in products)
		{
			if (labelList?.Values == null)
				continue;

			foreach (var label in labelList.Values)
				labelToProducts[label] = productSpec;
		}

		return labelToProducts.Count > 0 ? labelToProducts : null;
	}

	/// <summary>
	/// Builds LabelToFeatures mapping by inverting pivot.features entries.
	/// Each label in a feature entry maps to that feature-id string.
	/// </summary>
	private static Dictionary<string, string>? BuildLabelToFeaturesMapping(Dictionary<string, YamlLenientList?>? features)
	{
		if (features == null || features.Count == 0)
			return null;

		var labelToFeatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var (featureId, labelList) in features)
		{
			if (labelList?.Values == null)
				continue;

			foreach (var label in labelList.Values)
				labelToFeatures[label] = featureId;
		}

		return labelToFeatures.Count > 0 ? labelToFeatures : null;
	}
}
