// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Configuration;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Changelog.Creation;

/// <summary>
/// Service for writing changelog files
/// </summary>
public class ChangelogFileWriter(IFileSystem fileSystem, ILogger logger)
{
	/// <summary>
	/// Writes a changelog file with the given data.
	/// </summary>
	public async Task<bool> WriteChangelogAsync(
		IDiagnosticsCollector collector,
		ChangelogInput input,
		ChangelogConfiguration config,
		string? prUrl,
		bool titleMissing,
		bool typeMissing,
		Cancel ctx)
	{
		// Build changelog data from input
		var changelogData = BuildChangelogData(input, prUrl);

		// Generate YAML file
		var yamlContent = GenerateYaml(changelogData, config, titleMissing, typeMissing);

		// Determine output path
		var outputDir = input.Output ?? fileSystem.Directory.GetCurrentDirectory();
		if (!fileSystem.Directory.Exists(outputDir))
			_ = fileSystem.Directory.CreateDirectory(outputDir);

		// Generate filename
		var filename = GenerateFilename(collector, input, prUrl);
		var filePath = fileSystem.Path.Combine(outputDir, filename);

		// Write file
		await fileSystem.File.WriteAllTextAsync(filePath, yamlContent, ctx);
		logger.LogInformation("Created changelog fragment: {FilePath}", filePath);

		return true;
	}

	private string GenerateFilename(IDiagnosticsCollector collector, ChangelogInput input, string? prUrl)
	{
		if (input.UsePrNumber && !string.IsNullOrWhiteSpace(prUrl))
		{
			// Use PR number as filename when --use-pr-number is specified
			var prNumber = ChangelogTextUtilities.ExtractPrNumber(prUrl, input.Owner, input.Repo);
			if (prNumber.HasValue)
				return $"{prNumber.Value}.yaml";

			// Fall back to timestamp-slug format if PR number extraction fails
			collector.EmitWarning(string.Empty, $"Failed to extract PR number from '{prUrl}'. Falling back to timestamp-based filename.");
		}

		// Default: timestamp-slug.yaml
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var slug = string.IsNullOrWhiteSpace(input.Title)
			? string.IsNullOrWhiteSpace(prUrl)
				? "changelog"
				: $"pr-{prUrl.Replace("/", "-").Replace(":", "-")}"
			: ChangelogTextUtilities.SanitizeFilename(input.Title);
		return $"{timestamp}-{slug}.yaml";
	}

	private static ChangelogData BuildChangelogData(ChangelogInput input, string? prUrl) =>
		new()
		{
			Title = input.Title ?? string.Empty,
			Type = input.Type ?? string.Empty,
			Subtype = input.Subtype,
			Description = input.Description,
			Impact = input.Impact,
			Action = input.Action,
			FeatureId = input.FeatureId,
			Highlight = input.Highlight,
			Pr = prUrl ?? (input.Prs != null && input.Prs.Length > 0 ? input.Prs[0] : null),
			Products = input.Products.Select(p => new ProductInfo { Product = p.Product, Target = p.Target, Lifecycle = p.Lifecycle }).ToList(),
			Areas = input.Areas.Length > 0 ? input.Areas.ToList() : null,
			Issues = input.Issues.Length > 0 ? input.Issues.ToList() : null
		};

	private static string GenerateYaml(ChangelogData data, ChangelogConfiguration config, bool titleMissing, bool typeMissing)
	{
		// Create a mutable copy for serialization adjustments
		var areas = data.Areas;
		var issues = data.Issues;

		// Temporarily remove title/type if they're missing so they don't appear in YAML
		var serializeData = data with
		{
			Title = titleMissing ? string.Empty : data.Title,
			Type = typeMissing ? string.Empty : data.Type,
			Areas = areas != null && areas.Count == 0 ? null : areas,
			Issues = issues != null && issues.Count == 0 ? null : issues
		};

		var serializer = new StaticSerializerBuilder(new ChangelogYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
			.Build();

		var yaml = serializer.Serialize(serializeData);

		// Comment out missing title/type fields - insert at the beginning of the YAML data
		if (titleMissing || typeMissing)
		{
			var lines = yaml.Split('\n').ToList();
			var commentedFields = new List<string>();

			if (titleMissing)
				commentedFields.Add("# title: # TODO: Add title");
			if (typeMissing)
				commentedFields.Add("# type: # TODO: Add type (e.g., feature, enhancement, bug-fix, breaking-change)");

			// Find the first non-empty, non-comment line (start of actual YAML data)
			var insertIndex = lines.FindIndex(line =>
				!string.IsNullOrWhiteSpace(line) &&
				!line.TrimStart().StartsWith('#') &&
				!line.TrimStart().StartsWith("---", StringComparison.Ordinal));

			lines.InsertRange(insertIndex >= 0 ? insertIndex : lines.Count, commentedFields);

			yaml = string.Join('\n', lines);
		}

		// Build types list
		var typesList = string.Join("\n", config.AvailableTypes.Select(t => $"#   - {t}"));

		// Build subtypes list
		var subtypesList = string.Join("\n", config.AvailableSubtypes.Select(s => $"#   - {s}"));

		// Build lifecycles list
		var lifecyclesList = string.Join("\n", config.AvailableLifecycles.Select(l => $"#       - {l}"));

		// Add schema comments using raw string literal
		var result = $"""
			##### Required fields #####

			# title:
			#   A required string that is a short, user-facing headline.
			#   (Max 80 characters)

			# type:
			#   A required string that contains the type of change
			#   It can be one of:
			{typesList}

			# products:
			#   A required array of objects that denote the affected products
			#   Each product object contains:
			#
			#   - product:
			#       A required string with a valid product ID.
			#       Valid values are defined in https://github.com/elastic/docs-builder/blob/main/config/products.yml
			#
			#     target:
			#       An optional string with the target version or date.
			#
			#     lifecycle:
			#       An optional string for new features or enhancements that have a specific availability.
			#       It can be one of:
			{lifecyclesList}

			##### Optional fields #####

			# action:
			#   An optional string that describes what users must do to mitigate
			#   the impact of a breaking change or known issue.

			# areas:
			#   An optional array of strings that denotes the parts/components/services
			#   of the product that are affected.

			# description:
			#   An optional string that provides additional information.
			#   (Max 600 characters).

			# feature-id:
			#   An optional string to associate a feature or enhanceent with a
			#   unique feature flag.

			# highlight:
			#   An optional boolean for items that should be included in release
			#   highlights or the UI to draw user attention.

			# impact:
			#   An optional string that describes how the user's environment is
			#   affected by a breaking change or known issue.

			# issues:
			#   An optional array of strings that contain the issues that are
			#   relevant to the PR.

			# pr:
			#   An optional string that contains the pull request number.

			# subtype:
			#   An optional string that applies only to breaking changes.
			#   It can be one of:
			{subtypesList}

			{yaml}
			""";

		return result;
	}
}
