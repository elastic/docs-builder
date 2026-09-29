// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// A path relative to the documentation set's source directory, <c>/</c>-separated, e.g. <c>guides/setup.md</c> for a
/// file or <c>guides/advanced</c> for a folder or nested table of contents. Not an absolute path, and not relative to
/// the <c>toc.yml</c> that declared it (that is a <see cref="ContainerPath"/>).
/// </summary>
/// <remarks>
/// Built only through <see cref="From"/>, so a string cannot become one by accident; it reads as a string wherever
/// one is needed. The value is kept exactly as resolution produced it.
/// </remarks>
public readonly record struct DocsetPath
{
	private DocsetPath(string value) => Value = value;

	public string Value { get; }

	public static DocsetPath From(string docsetRelativePath) => new(docsetRelativePath);

	public static implicit operator string(DocsetPath path) => path.Value;

	public override string ToString() => Value;
}

/// <summary>
/// A path relative to the <c>docset.yml</c> or <c>toc.yml</c> that declared a table of contents entry: the entry's
/// <see cref="DocsetPath"/> with the declaring file's directory removed.
/// </summary>
public readonly record struct ContainerPath
{
	private ContainerPath(string value) => Value = value;

	public string Value { get; }

	public static ContainerPath From(string containerRelativePath) => new(containerRelativePath);

	public static implicit operator string(ContainerPath path) => path.Value;

	public override string ToString() => Value;
}
