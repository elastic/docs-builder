// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>The shape the table of contents reader chose for a <c>toc:</c> entry mapping.</summary>
public enum TocEntryKind
{
	/// <summary>No key of the mapping selected a shape: the entry is dropped.</summary>
	Dropped,
	Listing,
	CliReference,
	/// <summary><c>folder:</c> together with <c>file:</c>.</summary>
	FolderWithFile,
	DetectionRules,
	File,
	CrossLink,
	/// <summary><c>hidden:</c> naming a file.</summary>
	HiddenFile,
	Folder,
	NestedToc
}

/// <summary>
/// One <c>toc:</c> entry mapping as written, next to the <see cref="TocItem"/> the reader made of it. The reader picks one
/// shape by a fixed order of keys and drops every other key, coerces text into booleans and enums, and drops a mapping
/// that selects no shape; this keeps what it hid (see <c>Constraints-Conventions.md</c>). Not part of equality.
/// </summary>
/// <remarks>
/// Constructed with <c>TocItemYamlConverter</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record TocEntryAuthoring
{
	internal TocEntryAuthoring(TocEntryKind kind, string entry)
	{
		Kind = kind;
		Entry = entry;
	}

	public TocEntryKind Kind { get; internal init; }

	public string Entry { get; internal init; }

	public void Deconstruct(out TocEntryKind kind, out string entry)
	{
		kind = Kind;
		entry = Entry;
	}

	/// <summary>Every key of the mapping, in the order written.</summary>
	[OrderedEquality]
	public IReadOnlyList<string> WrittenKeys { get; internal init; } = [];

	/// <summary>Keys that no entry shape reads at all.</summary>
	[OrderedEquality]
	public IReadOnlyList<string> UnknownKeys { get; internal init; } = [];

	/// <summary>Keys that other shapes read but the chosen shape does not (for example <c>toc:</c> next to <c>file:</c>).</summary>
	[OrderedEquality]
	public IReadOnlyList<string> IgnoredKeys { get; internal init; } = [];

	/// <summary>Whether the resolver takes the entry's children as written, so that auto-discovery (<c>sort</c>, <c>exclude</c>) is not used.</summary>
	public bool ExplicitChildren { get; internal init; }

	/// <summary>The <c>toc.yml</c> or <c>docset.yml</c> the entry is written in.</summary>
	public string? File { get; internal init; }

	public SourceLocation? Location { get; internal init; }

	/// <summary><c>island</c> as written (a listing or a nested toc); null when absent.</summary>
	public string? IslandRaw { get; internal init; }

	/// <summary><c>hidden</c> as written on a <c>crosslink:</c> entry; null when absent.</summary>
	public string? HiddenRaw { get; internal init; }

	/// <summary><c>visual</c> as written on a listing; null when absent.</summary>
	public string? VisualRaw { get; internal init; }

	/// <summary><c>sort</c> as written on a listing; null when absent.</summary>
	public string? SortRaw { get; internal init; }
}
