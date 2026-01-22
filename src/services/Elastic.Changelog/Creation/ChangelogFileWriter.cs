// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.Serialization;
using Elastic.Documentation.Changelog;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

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
		CreateChangelogArguments input,
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

	private string GenerateFilename(IDiagnosticsCollector collector, CreateChangelogArguments input, string? prUrl)
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

	private static ChangelogEntry BuildChangelogData(CreateChangelogArguments input, string? prUrl)
	{
		var entryType = ChangelogEntryTypeExtensions.TryParse(input.Type, out var parsed, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? parsed
			: ChangelogEntryType.Other;

		var subtype = !string.IsNullOrWhiteSpace(input.Subtype)
			? (ChangelogEntrySubtypeExtensions.TryParse(input.Subtype, out var subtypeParsed, ignoreCase: true, allowMatchingMetadataAttribute: true)
				? subtypeParsed
				: (ChangelogEntrySubtype?)null)
			: null;

		return new()
		{
			Title = input.Title ?? string.Empty,
			Type = entryType,
			Subtype = subtype,
			Description = input.Description,
			Impact = input.Impact,
			Action = input.Action,
			FeatureId = input.FeatureId,
			Highlight = input.Highlight,
			Pr = prUrl ?? (input.Prs != null && input.Prs.Length > 0 ? input.Prs[0] : null),
			Products = input.Products.Select(ChangelogMapper.ToProductReference).ToList(),
			Areas = input.Areas is { Length: > 0 } ? input.Areas.ToList() : null,
			Issues = input.Issues is { Length: > 0 } ? input.Issues.ToList() : null
		};
	}

	private static string GenerateYaml(ChangelogEntry data, ChangelogConfiguration config, bool titleMissing, bool typeMissing)
	{
		// Create a mutable copy for serialization adjustments
		var areas = data.Areas;
		var issues = data.Issues;

		// Temporarily remove title if missing so it doesn't appear in YAML
		// Type is an enum so we keep it and add a comment if missing
		var serializeData = data with
		{
			Title = titleMissing ? string.Empty : data.Title,
			Areas = areas != null && areas.Count == 0 ? null : areas,
			Issues = issues != null && issues.Count == 0 ? null : issues
		};

		// Use centralized serialization which handles DTO conversion
		var yaml = ChangelogYamlSerialization.SerializeEntry(serializeData);

		// Comment out missing title/type fields - insert at the beginning of the YAML data
		if (titleMissing || typeMissing)
		{
			var lines = yaml.Split('\n').ToList();
			var commentedFields = new List<string>();

			if (titleMissing)
				commentedFields.Add("# title: # TODO: Add title");
			if (typeMissing)
			{
				commentedFields.Add("# type: # TODO: Add type (e.g., feature, enhancement, bug-fix, breaking-change)");
				// Remove the serialized default type value when type is missing
				_ = lines.RemoveAll(line => line.TrimStart().StartsWith("type:", StringComparison.Ordinal));
			}

			// Find the first non-empty, non-comment line (start of actual YAML data)
			var insertIndex = lines.FindIndex(line =>
				!string.IsNullOrWhiteSpace(line) &&
				!line.TrimStart().StartsWith('#') &&
				!line.TrimStart().StartsWith("---", StringComparison.Ordinal));

			lines.InsertRange(insertIndex >= 0 ? insertIndex : lines.Count, commentedFields);

			yaml = string.Join('\n', lines);
		}

		// Build types list
		var typesList = string.Join("\n", config.Types.Select(t => $"#   - {t}"));

		// Build subtypes list
		var subtypesList = string.Join("\n", config.SubTypes.Select(s => $"#   - {s}"));

		// Build lifecycles list
		var lifecyclesList = string.Join("\n", config.Lifecycles.Select(l => $"#       - {l.ToStringFast(true)}"));

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
