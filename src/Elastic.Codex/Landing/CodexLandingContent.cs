// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Codex.Landing;

/// <summary>A titled grid of documentation set cards on a Codex page. A group links to its own page; the ungrouped sets have none.</summary>
public sealed record CodexSection(
	string Title,
	string? Url,
	string? Description,
	int DocumentationSetCount,
	IReadOnlyList<CodexCardModel> Cards,
	string? GridId = null
);

/// <summary>What the Codex landing shows, apart from where it came from, so a real build and a placeholder preview render the same markup.</summary>
public sealed record CodexLandingContent(
	string Title,
	string McpUrl,
	int Groups,
	int DocumentationSets,
	int Pages,
	IReadOnlyList<CodexSection> Sections
);

/// <summary>What a Codex group page shows.</summary>
public sealed record CodexGroupContent(string Title, string? Description, IReadOnlyList<CodexCardModel> Cards);
