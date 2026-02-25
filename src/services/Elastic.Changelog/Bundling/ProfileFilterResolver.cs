// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// The resolved filter derived from a bundle profile — either a product list or a set of PR URLs.
/// Exactly one of <see cref="Products"/> and <see cref="Prs"/> will be non-null on a successful result.
/// </summary>
public record ProfileFilterResult
{
	/// <summary>Product/target/lifecycle filters parsed from the profile's products pattern.</summary>
	public IReadOnlyList<ProductArgument>? Products { get; init; }

	/// <summary>PR URLs extracted from a promotion report supplied as the profile argument.</summary>
	public string[]? Prs { get; init; }

	/// <summary>
	/// The resolved version string used for placeholder substitution.
	/// This is the profile argument itself for version-based invocations, or <c>"unknown"</c> when
	/// a promotion report was parsed (because the actual version is not available from the report).
	/// </summary>
	public string Version { get; init; } = "unknown";
}

/// <summary>
/// Resolves a <see cref="BundleProfile"/> and a profile argument (version string or promotion
/// report URL/path) into a concrete filter that can be used by both
/// <see cref="ChangelogBundlingService"/> and <see cref="ChangelogRemoveService"/>.
/// </summary>
public static class ProfileFilterResolver
{
	/// <summary>
	/// Resolves the profile into a <see cref="ProfileFilterResult"/>, or returns <c>null</c> and
	/// emits diagnostics errors on failure.
	/// </summary>
	public static async Task<ProfileFilterResult?> ResolveAsync(
		IDiagnosticsCollector collector,
		string profileName,
		string? profileArgument,
		ChangelogConfiguration? config,
		IFileSystem fileSystem,
		ILogger? logger,
		Cancel ctx)
	{
		if (config?.Bundle?.Profiles == null || !config.Bundle.Profiles.TryGetValue(profileName, out var profile))
		{
			collector.EmitError(string.Empty, $"Profile '{profileName}' not found in bundle.profiles configuration");
			return null;
		}

		if (string.IsNullOrWhiteSpace(profileArgument))
		{
			collector.EmitError(string.Empty, $"Profile '{profileName}' requires a version number or promotion report URL as the second argument");
			return null;
		}

		// Auto-detect argument type
		var argType = PromotionReportParser.DetectArgumentType(profileArgument);

		// Treat an existing local file path as a promotion report
		if (argType == ProfileArgumentType.Version && fileSystem.File.Exists(profileArgument))
			argType = ProfileArgumentType.PromotionReportFile;

		string version;
		string[]? prsFromReport = null;

		if (argType is ProfileArgumentType.PromotionReportUrl or ProfileArgumentType.PromotionReportFile)
		{
			var parser = new PromotionReportParser(NullLoggerFactory.Instance, fileSystem);
			var reportResult = await parser.ParsePromotionReportAsync(profileArgument, ctx);

			if (!reportResult.IsValid)
			{
				collector.EmitError(string.Empty, reportResult.ErrorMessage ?? "Failed to parse promotion report");
				return null;
			}

			prsFromReport = reportResult.PrUrls.ToArray();
			logger?.LogInformation("Extracted {Count} PRs from promotion report", prsFromReport.Length);

			version = "unknown";
		}
		else
		{
			version = profileArgument;
		}

		// Substitute {version} and {lifecycle} in the products pattern
		var lifecycle = VersionLifecycleInference.InferLifecycle(version);
		var productsPattern = profile.Products?
			.Replace("{version}", version)
			.Replace("{lifecycle}", lifecycle);

		// If we have PRs from a report, return those directly
		if (prsFromReport != null)
			return new ProfileFilterResult { Prs = prsFromReport, Version = version };

		// Without a promotion report we need a products pattern to filter by
		if (string.IsNullOrWhiteSpace(productsPattern))
		{
			collector.EmitError(
				string.Empty,
				$"Profile '{profileName}' has no 'products' pattern configured and no promotion report was provided — cannot determine filter"
			);
			return null;
		}

		var products = ParseProfileProducts(productsPattern);
		return new ProfileFilterResult { Products = products, Version = version };
	}

	/// <summary>
	/// Parses a products pattern like <c>"elasticsearch 9.2.0 ga"</c> or
	/// <c>"cloud-serverless {version} *"</c> (after placeholder substitution) into a
	/// <see cref="ProductArgument"/> list.
	/// </summary>
	internal static List<ProductArgument> ParseProfileProducts(string pattern)
	{
		var parts = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 1)
			return [];

		var productId = parts[0];
		var target = parts.Length > 1 ? parts[1] : "*";
		var lifecycle = parts.Length > 2 ? parts[2] : "*";

		return
		[
			new ProductArgument
			{
				Product = productId == "*" ? "*" : productId,
				Target = target == "*" ? "*" : target,
				Lifecycle = lifecycle == "*" ? "*" : lifecycle
			}
		];
	}
}
