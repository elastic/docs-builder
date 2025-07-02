// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Builder;

public class FeatureFlags(Dictionary<string, bool> initFeatureFlags)
{
	private readonly Dictionary<string, bool> _featureFlags = new(initFeatureFlags);

	public void Set(string key, bool value)
	{
		var normalizedKey = key.ToLowerInvariant().Replace('_', '-');
		_featureFlags[normalizedKey] = value;
	}

	public bool PrimaryNavEnabled
	{
		get => IsEnabled("primary-nav");
		set => _featureFlags["primary-nav"] = value;
	}

	public bool LazyLoadNavigation
	{
		get => IsEnabled("LAZY_LOAD_NAVIGATION");
		set => _featureFlags["LAZY_LOAD_NAVIGATION"] = value;
	}

	private bool IsEnabled(string key)
	{
		var envKey = $"FEATURE_{key.ToUpperInvariant().Replace('-', '_')}";
		if (Environment.GetEnvironmentVariable(envKey) is { } envValue)
		{
			if (bool.TryParse(envValue, out var envBool))
				return envBool;
			// if the env var is set but not a bool, we treat it as enabled
			return true;
		}

		return _featureFlags.TryGetValue(key, out var value) && value;
	}
}
