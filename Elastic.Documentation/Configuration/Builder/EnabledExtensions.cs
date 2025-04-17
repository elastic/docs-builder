// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Builder;

public class EnabledExtensions(IReadOnlyCollection<string> extensions)
{
	private readonly HashSet<string> _extensionsSet = [
		..extensions
	];

	private bool IsEnabled(string key) => _extensionsSet.Contains(key);

	public bool IsDetectionRulesEnabled => IsEnabled("detection-rules");

	public IEnumerable<string> Enabled => _extensionsSet;
}
