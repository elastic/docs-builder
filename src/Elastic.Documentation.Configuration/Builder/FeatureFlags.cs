// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Builder;

/// <summary>
/// Feature toggles from configuration, overridable per process with <c>FEATURE_&lt;KEY&gt;</c> environment variables.
/// The variables are read through <see cref="IEnvironmentVariables"/> so tests can supply their own without touching the
/// process environment.
/// </summary>
/// <summary>
/// The feature flags a build runs with: <c>docset.yml</c> toggles and per-environment <c>feature_flags</c>, each
/// overridable by a <c>FEATURE_*</c> environment variable. Immutable; runtime overrides build a new instance with
/// <see cref="With(string, bool)"/>.
/// </summary>
public sealed class FeatureFlags
{
	private readonly Dictionary<string, bool> _featureFlags;
	private readonly IEnvironmentVariables _environment;

	public FeatureFlags(Dictionary<string, bool> initFeatureFlags, IEnvironmentVariables? environment = null)
	{
		_featureFlags = [with(initFeatureFlags)];
		_environment = environment ?? SystemEnvironmentVariables.Instance;
	}

	private FeatureFlags(
		Dictionary<string, bool> featureFlags,
		IEnvironmentVariables environment,
		string? websiteSearchScriptUrl,
		string? wafIntegrationUrl
	)
	{
		_featureFlags = featureFlags;
		_environment = environment;
		WebsiteSearchScriptUrl = websiteSearchScriptUrl;
		WafIntegrationUrl = wafIntegrationUrl;
	}

	/// <summary>A copy with <paramref name="key"/> set; the key normalizes to kebab-case (<c>API_NAV_GROUPING</c> is <c>api-nav-grouping</c>).</summary>
	public FeatureFlags With(string key, bool value)
	{
		var flags = new Dictionary<string, bool>(_featureFlags, _featureFlags.Comparer)
		{
			[key.ToLowerInvariant().Replace('_', '-')] = value
		};
		return new FeatureFlags(flags, _environment, WebsiteSearchScriptUrl, WafIntegrationUrl);
	}

	/// <summary>A copy with every flag in <paramref name="flags"/> set, in order.</summary>
	public FeatureFlags With(IEnumerable<KeyValuePair<string, bool>> flags) =>
		flags.Aggregate(this, (current, flag) => current.With(flag.Key, flag.Value));

	public FeatureFlags WithWebsiteSearchScriptUrl(string? url) => new(_featureFlags, _environment, url, WafIntegrationUrl);

	public FeatureFlags WithWafIntegrationUrl(string? url) => new(_featureFlags, _environment, WebsiteSearchScriptUrl, url);

	public string? WebsiteSearchScriptUrl { get; }

	public string? WafIntegrationUrl { get; }

	public bool PrimaryNavEnabled => IsEnabled("primary-nav");

	public bool DisableGitHubEditLink => IsEnabled("disable-github-edit-link");

	public bool StagingElasticNavEnabled => IsEnabled("staging-elastic-nav");

	public bool WebsiteSearchEnabled => IsEnabled("website-search");

	public bool AirGappedEnabled => IsEnabled("air-gapped");

	public bool DiagnosticsPanelEnabled => IsEnabled("diagnostics-panel");

	public bool AssemblerApiExplorerEnabled => IsEnabled("assembler-api-explorer");

	public bool GuideNavEnabled => IsEnabled("guide-nav");

	public bool NavigationPreviewEnabled => IsEnabled("navigation-preview");

	public bool PageFeedbackEnabled => IsEnabled("page-feedback");

	/// <summary>Keys of the typed flags, in kebab-case. Keep in step with the properties above: a test enforces it.</summary>
	public static IReadOnlyList<string> KnownKeys { get; } =
	[
		"air-gapped",
		"assembler-api-explorer",
		"diagnostics-panel",
		"disable-github-edit-link",
		"guide-nav",
		"navigation-preview",
		"page-feedback",
		"primary-nav",
		"staging-elastic-nav",
		"website-search"
	];

	/// <summary>The enabled typed flags, sorted, with environment overrides applied.</summary>
	public IReadOnlyList<string> EnabledKeys() => [.. KnownKeys.Where(IsEnabled)];

	private bool IsEnabled(string key)
	{
		var envKey = $"FEATURE_{key.ToUpperInvariant().Replace('-', '_')}";
		if (_environment.GetEnvironmentVariable(envKey) is { } envValue)
		{
			if (bool.TryParse(envValue, out var envBool))
				return envBool;
			// if the env var is set but not a bool, we treat it as enabled
			return true;
		}

		return _featureFlags.TryGetValue(key, out var value) && value;
	}
}
