// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.IO.Configuration;

public class FeatureFlags(Dictionary<string, bool> featureFlags)
{
	private bool IsEnabled(string key) => featureFlags.TryGetValue(key, out var value) && value;

	public bool IsPrimaryNavEnabled => IsEnabled("primary-nav");
}
