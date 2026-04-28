// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Nullean.Argh;

namespace Elastic.Documentation.Isolated;

/// <summary>Options for an isolated documentation set build, bound from CLI flags via argh <c>[AsParameters]</c>.</summary>
public record IsolatedBuildOptions
{
	/// <summary>-p, Root directory of the documentation source. Defaults to <c>cwd/docs</c>.</summary>
	public DirectoryInfo? Path { get; init; }

	/// <summary>-o, Destination for generated HTML. Defaults to <c>.artifacts/html</c>.</summary>
	public DirectoryInfo? Output { get; init; }

	/// <summary>URL path prefix prepended to every generated link.</summary>
	public string? PathPrefix { get; init; }

	/// <summary>Delete and rebuild the output folder even if nothing changed.</summary>
	public bool? Force { get; init; }

	/// <summary>Treat warnings as errors.</summary>
	public bool? Strict { get; init; }

	/// <summary>Emit meta robots tags that allow search engine indexing.</summary>
	public bool? AllowIndexing { get; init; }

	/// <summary>Write only metadata files; skip HTML generation. Ignored when <c>--exporters</c> is also set.</summary>
	public bool? MetadataOnly { get; init; }

	/// <summary>
	/// Comma-separated list of exporters to run.
	/// Default: html, configuration, linkmetadata, documentationState, dedirects.
	/// </summary>
	// TODO: add [CollectionSyntax(Separator=",")] once argh fixes [AsParameters] + [CollectionSyntax] interaction
	public IReadOnlySet<Exporter>? Exporters { get; init; }

	/// <summary>Base URL written into <c>&lt;link rel=canonical&gt;</c> tags.</summary>
	[Url]
	public Uri? CanonicalBaseUrl { get; init; }

	/// <summary>Skip OpenAPI spec generation for faster builds.</summary>
	public bool SkipApi { get; init; }

	/// <summary>Skip fetching cross-doc-set link indexes.</summary>
	public bool SkipCrossLinks { get; init; }
}
