// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Equality;
using Generator.Equals;
using Nullean.Entities;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// The docset files a repository holds where isolated and assembler builds look for one, in the order they look, with
/// the <c>registry</c> each declares. Discovery builds the first.
/// </summary>
[Equatable]
public sealed partial record DocsetDiscovery([property: OrderedEquality] IReadOnlyList<DiscoveredDocset> Candidates);

[Equatable]
public sealed partial record DiscoveredDocset(
	[property: CustomEquality(typeof(FileSystemInfoLocationComparer))] IFileInfo File,
	string? Registry
)
{
	public DocSetRegistry EffectiveRegistry => DocumentationSetRules.ParseRegistry(Registry);
}

/// <summary>Every rule a <see cref="DocsetDiscovery"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class DocsetDiscoveryConstraints
{
	public static Constraint<DocsetDiscovery> OneDocsetPerRegistry { get; } = Constraints<DocsetDiscovery>
		.Unique(
			m => m.Candidates,
			c => c.EffectiveRegistry,
			describeGroup: (registry, found) =>
				$"Found {found.Count} documentation sets for the '{registry.ToStringFast(true)}' registry: {string.Join(", ", found.Select(c => c.File.FullName))}. A repository has one default docset and at most one per codex registry; builds use {found[0].File.FullName}."
		)
		.WithSeverity(Severity.Warning)
		.Describe("A repository has one default docset and at most one docset per codex registry.");

	public static Constraint<DocsetDiscovery> DiscoveredDocsetIsDefault { get; } = Constraints<DocsetDiscovery>
		.Each(
			m => m.Candidates.Take(1),
			first => first.EffectiveRegistry == DocSetRegistry.Public,
			first =>
				$"Discovery picked {first.File.FullName}, which belongs to the '{first.EffectiveRegistry.ToStringFast(true)}' codex registry. Isolated and assembler builds build the default docset; pass the docset's path explicitly."
		)
		.WithSeverity(Severity.Warning)
		.Describe("The docset discovery builds is the repository's default docset.");

	public static Constraint<DocsetDiscovery> DiscoveredDocsetNameExact { get; } = Constraints<DocsetDiscovery>
		.Each(
			m => m.Candidates,
			c => c.File.Name is "docset.yml" or "_docset.yml",
			c =>
				$"{c.File.FullName} matched the *docset.yml glob but is not named docset.yml or _docset.yml; rename it. Codex discovery matches exact names only."
		)
		.WithSeverity(Severity.Warning)
		.Describe("D2: a discovered docset file is named docset.yml or _docset.yml, not merely matched by the *docset.yml glob.");

	public static Constraint<DocsetDiscovery> DocsetDiscovered { get; } = Constraints<DocsetDiscovery>.Always(
		m => m.Candidates.Count > 0,
		"No documentation set (docset.yml) found"
	).Describe("D3: at least one docset file is discovered.");
}
