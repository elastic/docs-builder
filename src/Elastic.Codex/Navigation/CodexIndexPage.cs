// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;

namespace Elastic.Codex.Navigation;

/// <summary>
/// Represents the codex's index page that shows all documentation sets.
/// </summary>
public record CodexIndexPage(string NavigationTitle) : IDocumentationFile
{
	/// <inheritdoc />
	public string Title => NavigationTitle;
}

/// <summary>
/// Contains information about a documentation set for display on the codex index page.
/// </summary>
public record CodexDocumentationSetInfo
{
	/// <summary>
	/// The name of the documentation set (used in URL).
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// The display title of the documentation set.
	/// </summary>
	public required string Title { get; init; }

	/// <summary>
	/// The URL to the documentation set's root page.
	/// </summary>
	public required string Url { get; init; }

	/// <summary>
	/// The group id this documentation set belongs to, if any.
	/// </summary>
	public string? Group { get; init; }

	/// <summary>
	/// The total number of pages in the documentation set.
	/// </summary>
	public int PageCount { get; init; }

	/// <summary>
	/// Optional short description for display on the codex card.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Optional icon identifier for display on the codex card.
	/// </summary>
	public string? Icon { get; init; }
}
