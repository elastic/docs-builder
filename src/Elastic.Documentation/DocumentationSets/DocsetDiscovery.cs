// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Constraints;
using Elastic.Documentation.Equality;
using Generator.Equals;

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
public static class DocsetDiscoveryConstraints
{
	public static IReadOnlyList<Constraint<DocsetDiscovery>> All { get; } =
	[
		new(
			"one-docset-per-registry",
			"A repository has one default docset and at most one docset per codex registry.",
			m => m
				.Candidates
				.GroupBy(c => c.EffectiveRegistry)
				.Where(g => g.Count() > 1)
				.Select(
					g => Finding.Warning(
						$"Found {g.Count()} documentation sets for the '{g.Key.ToStringFast(true)}' registry: {string.Join(", ", g.Select(c => c.File.FullName))}. A repository has one default docset and at most one per codex registry; builds use {g.First().File.FullName}."
					)
				)
		),
		new(
			"discovered-docset-is-default",
			"The docset discovery builds is the repository's default docset.",
			m => m.Candidates is [{ EffectiveRegistry: not DocSetRegistry.Public } first, ..]
				? [
					Finding.Warning(
						$"Discovery picked {first.File.FullName}, which belongs to the '{first.EffectiveRegistry.ToStringFast(true)}' codex registry. Isolated and assembler builds build the default docset; pass the docset's path explicitly."
					)
				]
				: []
		)
	];

	public static IReadOnlyList<ConstraintViolation> Check(DocsetDiscovery discovery) => All.CheckAll(discovery);
}
