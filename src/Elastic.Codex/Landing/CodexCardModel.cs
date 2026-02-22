// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Codex.Landing;

/// <summary>
/// Model for a codex landing page card (used for both group cards and docset cards).
/// </summary>
public record CodexCardModel
{
	public required string Url { get; init; }
	public required string Title { get; init; }
	public string? Description { get; init; }
	public string? Icon { get; init; }
	public int PageCount { get; init; }
}
