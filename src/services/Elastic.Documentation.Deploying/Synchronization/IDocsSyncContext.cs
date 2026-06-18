// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Deploying.Synchronization;

/// <summary>
/// Minimal context required by the S3 sync strategies.
/// Implemented by both <c>AssembleContext</c> and <c>CodexContext</c>.
/// </summary>
public interface IDocsSyncContext
{
	ScopedFileSystem ReadFileSystem { get; }
	ScopedFileSystem WriteFileSystem { get; }
	IDirectoryInfo OutputDirectory { get; }
	IDiagnosticsCollector Collector { get; }

	/// <summary>Deployment environment name, used only for log messages.</summary>
	string EnvironmentName { get; }
}
