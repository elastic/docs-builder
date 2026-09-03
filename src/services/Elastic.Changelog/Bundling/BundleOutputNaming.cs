// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>Inputs for conventional bundle file names (profile and option mode).</summary>
public readonly record struct BundleOutputNameRequest(
	string Product,
	string Version,
	string? CliRepo,
	string? ProfileRepo,
	string? BundleRepo,
	string? ConfigPath
);

/// <summary>
/// Bundle names: <c>{repo}-{product}-{version}.yaml</c> when an authoring repo
/// resolves, else <c>{product}-{version}.yaml</c> with a warning. When product or version
/// cannot be resolved, <see cref="FallbackFileName"/>.
/// </summary>
public static class BundleOutputNaming
{
	public const string UnprefixedConvention = "{product}-{version}.yaml";
	public const string PrefixedConvention = "{repo}-{product}-{version}.yaml";

	public const string FallbackFileName = "changelog-bundle.yaml";

	/// <summary>
	/// Resolves the conventional file name (basename only). Repo precedence:
	/// <c>--repo</c>, profile <c>repo</c>, <c>bundle.repo</c>, git <c>origin</c> on github.com.
	/// When <paramref name="product"/> or <paramref name="version"/> is missing, warns and returns
	/// <see cref="FallbackFileName"/>.
	/// </summary>
	public static string ResolveFileNameOrFallback(IDiagnosticsCollector collector, IFileSystem fileSystem, BundleOutputNameRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Product) || string.IsNullOrWhiteSpace(request.Version))
		{
			collector.EmitWarning(
				string.Empty,
				"Could not resolve a product and version for the bundle file (pass --output-products or --input-products with a concrete target). " +
					$"Using '{FallbackFileName}'."
			);
			return FallbackFileName;
		}

		return ResolveFileName(collector, fileSystem, request);
	}

	public static bool IsYamlFilePath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return false;

		return path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Concrete version for option-mode naming: first non-wildcard target on
	/// <c>--output-products</c>, then <c>--input-products</c> (kept as-is, including
	/// calendar dates like <c>2026-08-27</c>), then <c>--release-version</c> with a
	/// leading <c>v</c> and pre-release suffix stripped. <c>latest</c> is ignored.
	/// </summary>
	public static string? ResolveVersion(
		IReadOnlyList<ProductArgument>? outputProducts,
		IReadOnlyList<ProductArgument>? inputProducts,
		string? releaseVersion
	)
	{
		foreach (var list in new[] { outputProducts, inputProducts })
		{
			if (list is null)
				continue;
			foreach (var p in list)
			{
				if (!string.IsNullOrWhiteSpace(p.Target) && p.Target != "*")
					return p.Target;
			}
		}

		if (string.IsNullOrWhiteSpace(releaseVersion) || releaseVersion.Equals("latest", StringComparison.OrdinalIgnoreCase))
			return null;

		return ChangelogTextUtilities.ExtractBaseVersion(releaseVersion);
	}

	public static string ResolveFileName(IDiagnosticsCollector collector, IFileSystem fileSystem, BundleOutputNameRequest request)
	{
		var repo = ResolveAuthoringRepo(fileSystem, request);
		if (!string.IsNullOrWhiteSpace(repo))
			return $"{repo}-{request.Product}-{request.Version}.yaml";

		collector.EmitWarning(
			string.Empty,
			"Could not resolve a repository name for the bundle file (set bundle.repo, pass --repo, or run from a git checkout with a github.com origin). " +
				$"Using '{UnprefixedConvention}'; two repositories publishing the same product and version may overwrite each other."
		);
		return $"{request.Product}-{request.Version}.yaml";
	}

	internal static string? ResolveAuthoringRepo(IFileSystem fileSystem, BundleOutputNameRequest request)
	{
		var configured = FirstNonEmpty(request.CliRepo, request.ProfileRepo, request.BundleRepo);
		var normalized = ChangelogRepoOwnerResolver.NormalizeRepo(configured);
		return !string.IsNullOrWhiteSpace(normalized) ? normalized : TryGitOriginRepo(fileSystem, request.ConfigPath);
	}

	private static string? FirstNonEmpty(params string?[] values)
	{
		foreach (var value in values)
		{
			if (!string.IsNullOrWhiteSpace(value))
				return value;
		}

		return null;
	}

	private static string? TryGitOriginRepo(IFileSystem fileSystem, string? configPath)
	{
		string? start = null;
		if (!string.IsNullOrWhiteSpace(configPath))
			start = fileSystem.Path.GetDirectoryName(configPath);
		start ??= fileSystem.Directory.GetCurrentDirectory();
		if (string.IsNullOrWhiteSpace(start))
			return null;

		var current = fileSystem.DirectoryInfo.New(start);
		for (var depth = 0; depth < 16 && current != null; depth++)
		{
			if (
				GitRemoteConfigurationReader.TryReadOriginUrl(fileSystem, current.FullName, out var url)
				&& GitHubRemoteParser.TryParseGitHubComOwnerRepo(url, out _, out var repo)
			)
			{
				var normalized = ChangelogRepoOwnerResolver.NormalizeRepo(repo);
				if (!string.IsNullOrWhiteSpace(normalized))
					return normalized;
			}

			var parent = current.Parent;
			if (parent is null || string.Equals(parent.FullName, current.FullName, StringComparison.Ordinal))
				break;
			current = parent;
		}

		return null;
	}
}
