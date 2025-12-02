// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services.ReleaseNotes;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static Elastic.Documentation.Configuration.ConfigurationFileProvider;

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
			// Load release notes configuration
			var config = await LoadReleaseNotesConfiguration(collector, ctx);
			if (config == null)
			{
				collector.EmitError(string.Empty, "Failed to load release notes configuration");
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

			if (input.Products.Length == 0)
			{
				collector.EmitError(string.Empty, "At least one product is required");
				return false;
			}

			// Validate type is in allowed list
			if (!config.AvailableTypes.Contains(input.Type))
			{
				collector.EmitWarning(string.Empty, $"Type '{input.Type}' is not in the list of available types. Available types: {string.Join(", ", config.AvailableTypes)}");
			}

			// Generate unique ID if not provided
			var id = input.Id ?? GenerateUniqueId(input.Title, input.Pr ?? string.Empty);

			// Build release notes data from input
			var releaseNotesData = BuildReleaseNotesData(input, id);

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
			var filePath = Path.Combine(outputDir, filename);

			// Write file
			await _fileSystem.File.WriteAllTextAsync(filePath, yamlContent, ctx);
			_logger.LogInformation("Created release notes fragment: {FilePath}", filePath);

			return true;
		}
		catch (Exception ex)
		{
			collector.EmitError(string.Empty, $"Error creating release notes: {ex.Message}", ex);
			return false;
		}
	}

	private async Task<ReleaseNotesConfiguration?> LoadReleaseNotesConfiguration(
		IDiagnosticsCollector collector,
		Cancel ctx
	)
	{
		// Try to load from config directory
		_ = configurationContext; // Suppress unused warning - kept for future extensibility
		var configPath = Path.Combine(LocalConfigurationDirectory, "release-notes.yml");

		if (!_fileSystem.File.Exists(configPath))
		{
			// Use default configuration if file doesn't exist
			_logger.LogWarning("Release notes configuration not found at {ConfigPath}, using defaults", configPath);
			return ReleaseNotesConfiguration.Default;
		}

		try
		{
			var yamlContent = await _fileSystem.File.ReadAllTextAsync(configPath, ctx);
			var deserializer = new StaticDeserializerBuilder(new ReleaseNotesYamlStaticContext())
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			var config = deserializer.Deserialize<ReleaseNotesConfiguration>(yamlContent);
			return config;
		}
		catch (Exception ex)
		{
			collector.EmitError(configPath, $"Failed to load release notes configuration: {ex.Message}", ex);
			return null;
		}
	}

	private static int GenerateUniqueId(string title, string prUrl)
	{
		// Generate a unique ID based on title and PR URL hash
		var input = $"{title}-{prUrl}";
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
		// Take first 4 bytes and convert to positive integer
		var id = Math.Abs(BitConverter.ToInt32(hash, 0));
		return id;
	}

	private static ReleaseNotesData BuildReleaseNotesData(ReleaseNotesInput input, int id)
	{
		var data = new ReleaseNotesData
		{
			Id = id,
			Title = input.Title,
			Type = input.Type,
			Subtype = input.Subtype,
			Description = input.Description,
			Impact = input.Impact,
			Action = input.Action,
			FeatureId = input.FeatureId,
			Highlight = input.Highlight,
			Pr = input.Pr,
			Products = input.Products.Select(p => new ProductInfo
			{
				Product = p,
				Target = input.Target,
				Lifecycle = input.Lifecycle
			}).ToList()
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

	private string GenerateYaml(ReleaseNotesData data, ReleaseNotesConfiguration config)
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
		_ = sb.AppendLine("# id: A required number that is a unique identifier for this changelog");
		_ = sb.AppendLine("# pr: An optional string that contains the pull request URL");
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
