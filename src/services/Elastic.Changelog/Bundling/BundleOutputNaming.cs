// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Bundling;

/// <summary>Inputs for conventional profile-mode bundle file names.</summary>
public readonly record struct BundleOutputNameRequest(
	string Product,
	string Version,
	string? CliRepo,
	string? ProfileRepo,
	string? BundleRepo,
	string? ConfigPath
);

/// <summary>
/// Profile-mode bundle names: <c>{repo}-{product}-{version}.yaml</c> when an authoring repo
/// resolves, else <c>{product}-{version}.yaml</c> with a warning.
/// </summary>
public static class BundleOutputNaming
{
	public const string UnprefixedConvention = "{product}-{version}.yaml";
	public const string PrefixedConvention = "{repo}-{product}-{version}.yaml";

	/// <summary>
	/// Resolves the conventional file name (basename only). Repo precedence:
	/// <c>--repo</c>, profile <c>repo</c>, <c>bundle.repo</c>, git <c>origin</c> on github.com.
	/// </summary>
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
