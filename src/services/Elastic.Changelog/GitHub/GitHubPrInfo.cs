// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Information about a GitHub pull request
/// </summary>
public record GitHubPrInfo
{
	public string Title { get; set; } = "";
	public string Body { get; set; } = "";
	public IReadOnlyList<string> Labels { get; set; } = [];
}
