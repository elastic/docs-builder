// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Applies <c>bundle.owner</c>, <c>bundle.repo</c>, and <c>bundle.link_allow_repos</c> seeding
/// to the changelog template placeholder. Pure string transformation with no I/O.
/// </summary>
public static class ChangelogTemplateSeeder
{
	internal const string Placeholder = "  # changelog-init-bundle-seed";

	/// <summary>
	/// Replaces or removes the <c># changelog-init-bundle-seed</c> placeholder in template content.
	/// CLI values take precedence over git-inferred values. When only repo is known, owner defaults to <c>elastic</c>.
	/// </summary>
	public static string ApplyBundleRepoSeed(string content, string? ownerCli, string? repoCli, string? gitOwner, string? gitRepo)
	{
		var gitMatched = gitOwner is not null && gitRepo is not null;

		var resolvedRepo = string.IsNullOrWhiteSpace(repoCli) ? gitRepo : repoCli.Trim();
		var resolvedOwner = string.IsNullOrWhiteSpace(ownerCli) ? gitOwner : ownerCli.Trim();
		if (!string.IsNullOrWhiteSpace(resolvedRepo) && string.IsNullOrWhiteSpace(resolvedOwner))
			resolvedOwner = "elastic";

		var shouldSeed = !string.IsNullOrWhiteSpace(resolvedOwner) && !string.IsNullOrWhiteSpace(resolvedRepo)
			&& (!string.IsNullOrWhiteSpace(ownerCli) || !string.IsNullOrWhiteSpace(repoCli) || gitMatched);

		var eol = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

		var block = shouldSeed
			? $"  owner: {QuoteForYaml(resolvedOwner!)}{eol}  repo: {QuoteForYaml(resolvedRepo!)}{eol}  link_allow_repos:{eol}    - {QuoteForYaml($"{resolvedOwner}/{resolvedRepo}")}{eol}"
			: "";

		var placeholderWithEol = Placeholder + eol;
		if (content.Contains(placeholderWithEol, StringComparison.Ordinal))
			return content.Replace(placeholderWithEol, block, StringComparison.Ordinal);

		return content.Replace(
			Placeholder,
			shouldSeed ? block.TrimEnd('\r', '\n') : string.Empty,
			StringComparison.Ordinal
		);
	}

	internal static string QuoteForYaml(string value) =>
		value.Contains(':') || value.Contains(' ') || value.Contains('#') || value.Contains('"')
			|| value.Contains('\\') || value.Contains('\n') || value.Contains('\r') || value.Contains('\t')
			? $"\"{value
				.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("\r", "\\r")
				.Replace("\n", "\\n")
				.Replace("\t", "\\t")}\""
			: value;
}
