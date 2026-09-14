// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Bootstrap-only filesystem rooted at the invocation path. Used only for the docset scan step of
/// <see cref="DocumentationPathsResolver"/>; discarded immediately after the anchor is found.
/// <para>
/// Rooted at (and below) the invocation path because both scan strategies read at-or-below their root:
/// the known-location heuristic checks the path itself and its <c>docs/</c> subdirectory, and the
/// recursive fallback enumerates downward. Nothing about the docset scan needs a parent directory.
/// </para>
/// </summary>
internal sealed class DocsetScanFileSystem(IDirectoryInfo path, IFileSystem? inner = null) : ScopedFileSystem(
	inner ?? new FileSystem(),
	new ScopedFileSystemOptions([path.FullName])
)
{
}
