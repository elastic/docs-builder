// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Configuration for bundle operations
/// </summary>
public record BundleConfiguration
{
	/// <summary>
	/// Input directory containing changelog YAML files.
	/// Defaults to "docs/changelog"
	/// </summary>
	public string? Directory { get; init; }

	/// <summary>
	/// Output directory for bundled changelog files.
	/// Defaults to "docs/releases"
	/// </summary>
	public string? OutputDirectory { get; init; }

	/// <summary>
	/// When true, the individual changelog entries that make up a bundle are sourced from the local
	/// <see cref="Directory"/>. When false (the default), they are fetched from the public changelog
	/// CDN, scoped to the bundle's products. An explicit <c>--directory</c> on the CLI always forces
	/// local sourcing regardless of this setting.
	/// </summary>
	public bool UseLocalChangelogs { get; init; }

	/// <summary>
	/// Default bundle description used when no profile-specific description is provided.
	/// Supports {version}, {lifecycle}, {owner}, and {repo} placeholders.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Default GitHub repository name applied to all profiles that do not specify their own.
	/// <para>
	/// <b>Deprecated.</b> The repository is now derived automatically from the <c>GITHUB_REPOSITORY</c>
	/// environment variable or the git remote origin, so this field is redundant in almost every case.
	/// Remove it from <c>changelog.yml</c> unless the repo name genuinely differs from the derived value.
	/// Setting it to a <em>different</em> repository than where the command runs is a hard error, because
	/// it would silently repoint the S3 upload pool and GitHub API calls.
	/// </para>
	/// </summary>
	[Obsolete("Derived automatically. Remove bundle.repo from changelog.yml; a mismatch with the running repo is a hard error.")]
	public string? Repo { get; init; }

	/// <summary>
	/// Default GitHub repository owner applied to all profiles that do not specify their own.
	/// <para><b>Deprecated.</b> Derived automatically alongside <c>bundle.repo</c>. Remove from <c>changelog.yml</c>.</para>
	/// </summary>
	[Obsolete("Derived automatically. Remove bundle.owner from changelog.yml.")]
	public string? Owner { get; init; }

	/// <summary>
	/// Branch whose CDN changelog pool (<c>changelog/{org}/{repo}/{branch}/…</c>) entries are sourced from
	/// when bundling from the CDN. Applied to all profiles that do not specify their own. Defaults to
	/// <c>main</c> when unset.
	/// </summary>
	public string? Branch { get; init; }

	/// <summary>
	/// Obsolete — no longer read. Link sanitization is handled exclusively by the scrubber Lambda.
	/// Remove <c>link_allow_repos</c> from <c>changelog.yml</c>.
	/// </summary>
	[Obsolete("link_allow_repos is no longer read. Remove it from changelog.yml.", error: false)]
	public IReadOnlyList<string>? LinkAllowRepos { get; init; }

	/// <summary>
	/// When true, auto-populate release date in bundle output. Defaults to true when omitted.
	/// </summary>
	public bool? ReleaseDates { get; init; }

	/// <summary>
	/// Named bundle profiles for different release scenarios.
	/// </summary>
	public IReadOnlyDictionary<string, BundleProfile>? Profiles { get; init; }

	/// <summary>
	/// Release trigger to profile mappings. <c>github</c> maps tag globs to profiles;
	/// <c>products</c> maps product IDs to profiles for any product-scoped release (versioned stack or date-based).
	/// </summary>
	public BundleReleases? Releases { get; init; }
}

/// <summary>
/// A named bundle profile configuration.
/// Profiles can be invoked with a version number or promotion report URL.
/// </summary>
public record BundleProfile
{
	/// <summary>
	/// Target product ID for this profile. Validated against products.yml. Replaces output_products.
	/// </summary>
	public string? Product { get; init; }

	/// <summary>
	/// Product filter pattern for input changelogs.
	/// Format: "product {version} {lifecycle}" where placeholders are substituted at runtime.
	/// Examples:
	/// - "elasticsearch {version} {lifecycle}"
	/// - "cloud-serverless {version} *"
	/// </summary>
	public string? Products { get; init; }

	/// <summary>
	/// Legacy output filename pattern. No longer supported: bundle output names are derived by
	/// convention as <c>{repo}-{product}-{version}.yaml</c> from the authoring repo and the profile's
	/// primary output product (elastic/docs-builder#3774). Any profile setting this is a hard error
	/// at bundle time; the field remains parseable for one release cycle so authors get an actionable
	/// error rather than a YAML parse failure.
	/// </summary>
	[Obsolete("No longer supported: bundle output names are derived by convention as '{repo}-{product}-{version}.yaml' from the authoring repo and the profile's output_products. Setting 'output' is a hard error at bundle time.")]
	public string? Output { get; init; }

	/// <summary>
	/// Profile-specific output directory. Deprecated: derived automatically as bundle.output_directory/{product}.
	/// </summary>
	[Obsolete("Profile output_directory is derived automatically as bundle.output_directory/{product}. Remove this field.")]
	public string? OutputDirectory { get; init; }

	/// <summary>
	/// Output products pattern. Deprecated: use 'product' instead.
	/// </summary>
	[Obsolete("Use 'product' instead. 'output_products' will be removed in a future version.")]
	public string? OutputProducts { get; init; }

	/// <summary>
	/// Profile-specific bundle description. When provided, overrides the bundle.description default.
	/// Supports {version}, {lifecycle}, {owner}, and {repo} placeholders.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// GitHub repository name for link and file name generation.
	/// <para>
	/// <b>Deprecated.</b> Derived from <c>GITHUB_REPOSITORY</c> or git <c>origin</c> when omitted.
	/// <c>--repo</c> and <c>bundle.repo</c> still override when set.
	/// </para>
	/// </summary>
	[Obsolete("Derived from GITHUB_REPOSITORY or git origin when omitted. Remove from profile config.")]
	public string? Repo { get; init; }

	/// <summary>
	/// GitHub repository owner stored on each product in the bundle output.
	/// <para><b>Deprecated.</b> Derived automatically alongside <c>repo</c>. Remove from profile config.</para>
	/// </summary>
	[Obsolete("Derived automatically. Remove from profile config.")]
	public string? Owner { get; init; }

	/// <summary>
	/// Branch whose CDN changelog pool entries this profile sources from. Overrides
	/// <see cref="BundleConfiguration.Branch"/> when set.
	/// </summary>
	public string? Branch { get; init; }

	/// <summary>
	/// Feature IDs to mark as hidden in the bundle output.
	/// When the bundle is rendered, entries with matching feature-id values will be commented out.
	/// </summary>
	public IReadOnlyList<string>? HideFeatures { get; init; }

	/// <summary>
	/// When true, auto-populate release date in bundle output. Defaults to true when omitted.
	/// </summary>
	public bool? ReleaseDates { get; init; }

	/// <summary>
	/// Profile source type. Removed — use bundle.releases.github to map release tags to profiles.
	/// </summary>
	[Obsolete("'source: github_release' is removed. Use bundle.releases.github to map release tags to profiles.")]
	public string? Source { get; init; }
}

/// <summary>
/// Release trigger to profile mappings.
/// </summary>
public record BundleReleases
{
	/// <summary>
	/// Maps GitHub release tag glob patterns to profiles. Key is the tag glob; value is the profile name.
	/// </summary>
	public IReadOnlyDictionary<string, string>? Github { get; init; }

	/// <summary>
	/// Maps product IDs to profiles for any product-scoped release (versioned stack or date-based).
	/// Key is the product ID; value is the profile name.
	/// </summary>
	public IReadOnlyDictionary<string, string>? Products { get; init; }
}
