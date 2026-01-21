// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Elastic.Changelog.Bundling;
using NetEscapades.EnumGenerators;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Input data for rendering changelog bundle to markdown or asciidoc
/// </summary>
public class ChangelogRenderInput
{
	public required IReadOnlyCollection<BundleInput> Bundles { get; init; }
	public string? Output { get; init; }
	public string? Title { get; init; }
	public bool Subsections { get; init; }
	public string[]? HideFeatures { get; init; }
	public string? Config { get; init; }
	public ChangelogFileType FileType { get; init; } = ChangelogFileType.Markdown;
}

[EnumExtensions]
public enum ChangelogFileType
{
	[Display(Name = "markdown")]
	[JsonStringEnumMemberName("markdown")]
	Markdown,
	[Display(Name = "asciidoc")]
	[JsonStringEnumMemberName("asciidoc")]
	Asciidoc
}
