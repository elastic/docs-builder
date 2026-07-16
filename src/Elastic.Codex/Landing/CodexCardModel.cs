// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Codex.Navigation;

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

	/// <summary>
	/// The repo path (e.g. "/r/beacon") shown on docset cards so it is findable via browser ctrl+F. Null for group cards.
	/// </summary>
	public string? RepoPath { get; init; }

	/// <summary>
	/// Builds the card model for a documentation set, so every docset card renders the same fields.
	/// </summary>
	public static CodexCardModel FromDocumentationSet(CodexDocumentationSetInfo docSet) => new()
	{
		Url = docSet.Url,
		Title = docSet.Title ?? docSet.Name,
		Description = docSet.Description,
		Icon = docSet.Icon,
		PageCount = docSet.PageCount,
		RepoPath = docSet.RepoPath,
	};
}
