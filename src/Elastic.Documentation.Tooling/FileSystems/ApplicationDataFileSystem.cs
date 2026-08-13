// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// A scoped filesystem rooted at the per-user <c>elastic/docs-builder</c> application data folder.
/// Use for components that access caches or state and have no need for workspace files
/// (e.g. <c>CrossLinkFetcher</c>, <c>CheckForUpdatesFilter</c>, <c>GitLinkIndexReader</c>).
/// </summary>
public class ApplicationDataFileSystem(IFileSystem? inner = null)
	: ScopedFileSystem(
		inner ?? new FileSystem(),
		new ScopedFileSystemOptions([Paths.ApplicationData.FullName])
		{
			// .git needed for codex-link-index clone directory inside ApplicationData
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
		}),
	IAppDataFileSystem
{
}
