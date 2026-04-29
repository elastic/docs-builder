// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Nullean.Argh;

namespace Elastic.Documentation.Assembler.Building;

/// <summary>Options for an assembler build, bound from CLI flags via argh <c>[AsParameters]</c>.</summary>
public record AssemblerBuildOptions
{
	/// <summary>Treat warnings as errors.</summary>
	public bool? Strict { get; init; }

	/// <summary>Named deployment target, e.g. <c>dev</c>, <c>staging</c>, <c>production</c>. Determines which configuration branch and index names are used.</summary>
	public string? Environment { get; init; }

	/// <summary>Write only metadata files; skip HTML generation. Ignored when <c>--exporters</c> is also set.</summary>
	public bool? MetadataOnly { get; init; }

	/// <summary>Print documentation hints emitted during the build.</summary>
	public bool? ShowHints { get; init; }

	/// <summary>
	/// Comma-separated list of exporters to run.
	/// Values: Html, Elasticsearch, Configuration, LinkMetadata, DocumentationState, LLMText, Redirects.
	/// Default: Html, Configuration, LinkMetadata, DocumentationState, Redirects.
	/// </summary>
	// [CollectionSyntax(Separator=",")] omitted: 0.12.2 generator bug — null-return for empty [AsParameters]
	// collection declared with non-nullable type (CS8600). Re-enable when fixed upstream.
	public IReadOnlySet<Exporter>? Exporters { get; init; }

	/// <summary>Skip the build step when <c>.artifacts/docs/index.html</c> already exists. Intended for test scenarios only.</summary>
	public bool? AssumeBuild { get; init; }
}
