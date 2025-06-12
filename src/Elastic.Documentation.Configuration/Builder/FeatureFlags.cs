// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Builder;

public class FeatureFlags(Dictionary<string, bool> featureFlags)
{
	public bool IsPrimaryNavEnabled => IsEnabled("primary-nav");
	public bool IsVersionDropdownEnabled => IsEnabled("version-dropdown");
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
		return featureFlags.TryGetValue(key, out var value) && value;
	}
}
