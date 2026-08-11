// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// A scoped filesystem covering the docs configuration tree (<c>&lt;cwd&gt;/config</c>) and
/// per-user application data. Used by <c>ConfigurationFileProvider</c>, which reads
/// <c>config/*.yml</c> and writes runtime artefacts under <c>AppData/config-runtime</c>.
/// </summary>
public class ConfigurationFileSystem(IFileSystem? inner = null)
	: ScopedFileSystem(
		// ScopedFileSystem cannot wrap another ScopedFileSystem. When a ScopedFileSystem is
		// supplied (e.g. a DocumentationFileSystem from a test context) use the physical FS instead,
		// since config files live outside a docset scope anyway.
		inner is ScopedFileSystem ? new FileSystem() : (inner ?? new FileSystem()),
		new ScopedFileSystemOptions([
			System.IO.Path.Join(Paths.WorkingDirectoryRoot.FullName, "config"),
			Paths.ApplicationData.FullName
		])
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
		}),
	IAppDataFileSystem
{
}
