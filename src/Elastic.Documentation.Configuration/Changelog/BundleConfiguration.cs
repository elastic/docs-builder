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
	/// When set (including an empty list), PR/issue references whose resolved <c>owner/repo</c> is not listed
	/// are rewritten to <c># PRIVATE:</c> sentinels at bundle time. When absent, no link filtering is applied.
	/// </summary>
	public IReadOnlyList<string>? LinkAllowRepos { get; init; }

	/// <summary>
	/// When true, auto-populate release date in bundle output. Defaults to true when omitted.
	/// </summary>
	public bool? ReleaseDates { get; init; }

	/// <summary>
	/// Named bundle profiles for different release scenarios.
	/// </summary>
	public IReadOnlyDictionary<string, BundleProfile>? Profiles { get; init; }
}

/// <summary>
/// A named bundle profile configuration.
/// Profiles can be invoked with a version number or promotion report URL.
/// </summary>
public record BundleProfile
{
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
	/// Profile-specific output directory. Replaces <see cref="BundleConfiguration.OutputDirectory"/>
	/// for this profile the same way option-mode <c>--output</c> as a directory replaces it. The
	/// conventional <c>{repo}-{product}-{version}.yaml</c> name is joined onto this path. A
	/// <c>.yml</c>/<c>.yaml</c> value is a hard error (use of free-form filenames is what
	/// <see cref="Output"/> used to allow).
	/// </summary>
	public string? OutputDirectory { get; init; }

	/// <summary>
	/// Output products pattern. When set, overrides the products array derived from matched changelogs.
	/// Supports {version} and {lifecycle} placeholders.
	/// </summary>
	public string? OutputProducts { get; init; }

	/// <summary>
	/// Profile-specific bundle description. When provided, overrides the bundle.description default.
	/// Supports {version}, {lifecycle}, {owner}, and {repo} placeholders.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// GitHub repository name stored on each product in the bundle output.
	/// <para>
	/// <b>Deprecated.</b> Per-product repo is now resolved from <c>products.yml</c> via the
	/// product's <c>repository:</c> field, making the bundle-level override redundant.
	/// Remove from profile config; a mismatch with the running repository is a hard error.
	/// </para>
	/// </summary>
	[Obsolete("Derived from products.yml repository field. Remove from profile config.")]
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
	/// Profile source type. When set to <c>"github_release"</c>, the profile fetches
	/// PR references directly from a GitHub release and uses them as the bundle filter.
	/// Mutually exclusive with <see cref="Products"/>.
	/// </summary>
	public string? Source { get; init; }
}
