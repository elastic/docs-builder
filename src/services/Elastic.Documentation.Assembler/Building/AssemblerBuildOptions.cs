// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Assembler.Building;

/// <summary>Options for an assembler build, bound from CLI flags via argh <c>[AsParameters]</c>.</summary>
public record AssemblerBuildOptions
{
	public bool? Strict { get; init; }
	public string? Environment { get; init; }
	public bool? MetadataOnly { get; init; }
	public bool? ShowHints { get; init; }
	public IReadOnlySet<Exporter>? Exporters { get; init; }
	public bool? AssumeBuild { get; init; }
}
