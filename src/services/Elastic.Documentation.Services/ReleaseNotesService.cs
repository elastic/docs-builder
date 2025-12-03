// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services.ReleaseNotes;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Documentation.Services;

public class ReleaseNotesService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ReleaseNotesService>();
	private readonly IFileSystem _fileSystem = new FileSystem();

	public async Task<bool> CreateReleaseNotes(
		IDiagnosticsCollector collector,
		ReleaseNotesInput input,
		Cancel ctx
	)
	{
		try
		{
			// Load changelog configuration
			var config = await LoadChangelogConfiguration(collector, input.Config, ctx);
			if (config == null)
			{
				collector.EmitError(string.Empty, "Failed to load changelog configuration");
				return false;
			}

			// Validate required fields
			if (string.IsNullOrWhiteSpace(input.Title))
			{
				collector.EmitError(string.Empty, "Title is required");
				return false;
			}

			if (string.IsNullOrWhiteSpace(input.Type))
			{
				collector.EmitError(string.Empty, "Type is required");
				return false;
			}

			if (input.Products.Count == 0)
			{
				collector.EmitError(string.Empty, "At least one product is required");
				return false;
			}

			// Validate type is in allowed list
			if (!config.AvailableTypes.Contains(input.Type))
			{
				collector.EmitError(string.Empty, $"Type '{input.Type}' is not in the list of available types. Available types: {string.Join(", ", config.AvailableTypes)}");
				return false;
			}

			// Validate subtype if provided
			if (!string.IsNullOrWhiteSpace(input.Subtype))
			{
				if (!config.AvailableSubtypes.Contains(input.Subtype))
				{
					collector.EmitError(string.Empty, $"Subtype '{input.Subtype}' is not in the list of available subtypes. Available subtypes: {string.Join(", ", config.AvailableSubtypes)}");
					return false;
				}
			}

			// Validate areas if configuration provides available areas
			if (config.AvailableAreas != null && config.AvailableAreas.Count > 0)
			{
				foreach (var area in input.Areas)
				{
					if (!config.AvailableAreas.Contains(area))
					{
						collector.EmitError(string.Empty, $"Area '{area}' is not in the list of available areas. Available areas: {string.Join(", ", config.AvailableAreas)}");
						return false;
					}
				}
			}

			// Validate products if configuration provides available products
			if (config.AvailableProducts != null && config.AvailableProducts.Count > 0)
			{
				foreach (var product in input.Products)
				{
					if (!config.AvailableProducts.Contains(product.Product))
					{
						collector.EmitError(string.Empty, $"Product '{product.Product}' is not in the list of available products. Available products: {string.Join(", ", config.AvailableProducts)}");
						return false;
					}
				}
			}

			// Validate lifecycle values in products
			foreach (var product in input.Products)
			{
				if (!string.IsNullOrWhiteSpace(product.Lifecycle))
				{
					if (!config.AvailableLifecycles.Contains(product.Lifecycle))
					{
						collector.EmitError(string.Empty, $"Lifecycle '{product.Lifecycle}' for product '{product.Product}' is not in the list of available lifecycles. Available lifecycles: {string.Join(", ", config.AvailableLifecycles)}");
						return false;
					}
				}
			}

			// Build release notes data from input
			var releaseNotesData = BuildReleaseNotesData(input);

			// Generate YAML file
			var yamlContent = GenerateYaml(releaseNotesData, config);

			// Determine output path
			var outputDir = input.Output ?? Directory.GetCurrentDirectory();
			if (!_fileSystem.Directory.Exists(outputDir))
			{
				_ = _fileSystem.Directory.CreateDirectory(outputDir);
			}

			// Generate filename (timestamp-slug.yaml)
			var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			var slug = SanitizeFilename(input.Title);
			var filename = $"{timestamp}-{slug}.yaml";
			var filePath = _fileSystem.Path.Combine(outputDir, filename);

			// Write file
			await _fileSystem.File.WriteAllTextAsync(filePath, yamlContent, ctx);
			_logger.LogInformation("Created changelog fragment: {FilePath}", filePath);

			return true;
		}
		catch (OperationCanceledException)
		{
			// If cancelled, don't emit error; propagate cancellation signal.
			throw;
		}
		catch (IOException ioEx)
		{
			collector.EmitError(string.Empty, $"IO error creating release notes: {ioEx.Message}", ioEx);
			return false;
		}
		catch (UnauthorizedAccessException uaEx)
		{
			collector.EmitError(string.Empty, $"Access denied creating release notes: {uaEx.Message}", uaEx);
			return false;
		}
	}

	private async Task<ChangelogConfiguration?> LoadChangelogConfiguration(
		IDiagnosticsCollector collector,
		string? configPath,
		Cancel ctx
	)
	{
		// Determine config file path
		_ = configurationContext; // Suppress unused warning - kept for future extensibility
		var finalConfigPath = configPath ?? _fileSystem.Path.Combine(Directory.GetCurrentDirectory(), "docs", "changelog.yml");

		if (!_fileSystem.File.Exists(finalConfigPath))
		{
			// Use default configuration if file doesn't exist
			_logger.LogWarning("Changelog configuration not found at {ConfigPath}, using defaults", finalConfigPath);
			return ChangelogConfiguration.Default;
		}

		try
		{
			var yamlContent = await _fileSystem.File.ReadAllTextAsync(finalConfigPath, ctx);
			var deserializer = new StaticDeserializerBuilder(new ReleaseNotesYamlStaticContext())
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			var config = deserializer.Deserialize<ChangelogConfiguration>(yamlContent);
			return config;
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

	private static ReleaseNotesData BuildReleaseNotesData(ReleaseNotesInput input)
	{
		var data = new ReleaseNotesData
		{
			Title = input.Title,
			Type = input.Type,
			Subtype = input.Subtype,
			Description = input.Description,
			Impact = input.Impact,
			Action = input.Action,
			FeatureId = input.FeatureId,
			Highlight = input.Highlight,
			Pr = input.Pr,
			Products = input.Products
		};

		if (input.Areas.Length > 0)
		{
			data.Areas = input.Areas.ToList();
		}

		if (input.Issues.Length > 0)
		{
			data.Issues = input.Issues.ToList();
		}

		return data;
	}

	private string GenerateYaml(ReleaseNotesData data, ChangelogConfiguration config)
	{
		// Ensure areas is null if empty to omit it from YAML
		if (data.Areas != null && data.Areas.Count == 0)
			data.Areas = null;

		// Ensure issues is null if empty to omit it from YAML
		if (data.Issues != null && data.Issues.Count == 0)
			data.Issues = null;

		var serializer = new StaticSerializerBuilder(new ReleaseNotesYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
			.Build();

		var yaml = serializer.Serialize(data);

		// Add schema comments
		var sb = new StringBuilder();
		_ = sb.AppendLine("##### Automated fields #####");
		_ = sb.AppendLine();
		_ = sb.AppendLine("# These fields are likely generated when the changelog is created and unlikely to require edits");
		_ = sb.AppendLine();
		_ = sb.AppendLine("# pr: An optional string that contains the pull request number");
		_ = sb.AppendLine("# issues: An optional array of strings that contain URLs for issues that are relevant to the PR");
		_ = sb.AppendLine("# type: A required string that contains the type of change");
		_ = sb.AppendLine("#   It can be one of:");
		foreach (var type in config.AvailableTypes)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"#   - {type}");
		}
		_ = sb.AppendLine("# subtype: An optional string that applies only to breaking changes");
		if (config.AvailableSubtypes.Count > 0)
		{
			_ = sb.AppendLine("#   It can be one of:");
			foreach (var subtype in config.AvailableSubtypes)
			{
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"#   - {subtype}");
			}
		}
		_ = sb.AppendLine("# products: A required array of objects that denote the affected products");
		_ = sb.AppendLine("#   Each product object contains:");
		_ = sb.AppendLine("#     - product: A required string with a predefined product ID");
		_ = sb.AppendLine("#     - target: An optional string with the target version or date");
		_ = sb.AppendLine("#     - lifecycle: An optional string (preview, beta, ga)");
		_ = sb.AppendLine("# areas: An optional array of strings that denotes the parts/components/services affected");
		_ = sb.AppendLine();
		_ = sb.AppendLine("##### Non-automated fields #####");
		_ = sb.AppendLine();
		_ = sb.AppendLine("# These fields might be generated when the changelog is created but are likely to require edits");
		_ = sb.AppendLine();
		_ = sb.AppendLine("# title: A required string that is a short, user-facing headline (Max 80 characters)");
		_ = sb.AppendLine("# description: An optional string that provides additional information (Max 600 characters)");
		_ = sb.AppendLine("# impact: An optional string that describes how the user's environment is affected");
		_ = sb.AppendLine("# action: An optional string that describes what users must do to mitigate");
		_ = sb.AppendLine("# feature-id: An optional string to associate with a unique feature flag");
		_ = sb.AppendLine("# highlight: An optional boolean for items that should be included in release highlights");
		_ = sb.AppendLine();
		_ = sb.Append(yaml);

		return sb.ToString();
	}

	private static string SanitizeFilename(string input)
	{
		var sanitized = input.ToLowerInvariant()
			.Replace(" ", "-")
			.Replace("/", "-")
			.Replace("\\", "-")
			.Replace(":", "")
			.Replace("'", "")
			.Replace("\"", "");

		// Limit length
		if (sanitized.Length > 50)
			sanitized = sanitized[..50];

		return sanitized;
	}
}
