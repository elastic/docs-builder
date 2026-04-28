// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Argh;

namespace Elastic.Documentation.Isolated;

/// <summary>Options for an isolated documentation set build, bound from CLI flags via argh <c>[AsParameters]</c>.</summary>
public record IsolatedBuildOptions
{
	public string? Path { get; init; }
	public string? Output { get; init; }
	public string? PathPrefix { get; init; }
	public bool? Force { get; init; }
	public bool? Strict { get; init; }
	public bool? AllowIndexing { get; init; }
	public bool? MetadataOnly { get; init; }
	[CollectionSyntax(Separator = ",")]
	public IReadOnlySet<Exporter>? Exporters { get; init; }
	public string? CanonicalBaseUrl { get; init; }
	public bool SkipApi { get; init; }
	public bool SkipCrossLinks { get; init; }
}
