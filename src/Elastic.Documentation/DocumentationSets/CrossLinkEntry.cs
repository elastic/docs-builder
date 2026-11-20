// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.DocumentationSets;

/// <summary>A parsed cross-link entry from docset.yml, with the target registry for lookup.</summary>
/// <remarks>
/// Constructed through <c>ResolvedDocumentationSet</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
/// <param name="Repository">Repository name (e.g. elasticsearch, docs-eng-team).</param>
/// <param name="Registry">Registry to use for lookup (public S3 or codex environment).</param>
public record CrossLinkEntry
{
	internal CrossLinkEntry(string repository, DocSetRegistry registry)
	{
		Repository = repository;
		Registry = registry;
	}

	public string Repository { get; internal init; }

	public DocSetRegistry Registry { get; internal init; }

	public void Deconstruct(out string repository, out DocSetRegistry registry)
	{
		repository = Repository;
		registry = Registry;
	}
}
