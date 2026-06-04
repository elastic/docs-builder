// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;
using Nullean.ScopedFileSystem;

namespace Elastic.Codex;

/// <summary>
/// Context for codex operations containing configuration, file systems, and directories.
/// </summary>
public class CodexContext
{
	public ScopedFileSystem ReadFileSystem { get; }
	public ScopedFileSystem WriteFileSystem { get; }
	public IDiagnosticsCollector Collector { get; }
	public CodexConfiguration Configuration { get; }
	public IFileInfo ConfigurationPath { get; }
	public IDirectoryInfo CheckoutDirectory { get; }
	public IDirectoryInfo OutputDirectory { get; }

	/// <summary>
	/// The Elasticsearch index namespace for this codex, derived from the environment name.
	/// Falls back to "codex" when no environment is specified.
	/// </summary>
	public string IndexNamespace => string.IsNullOrEmpty(Configuration.Environment)
		? "codex"
		: $"codex-{Configuration.Environment}";

	public CodexContext(
		CodexConfiguration configuration,
		IFileInfo configurationPath,
		IDiagnosticsCollector collector,
		ScopedFileSystem readFileSystem,
		ScopedFileSystem writeFileSystem,
		string? checkoutDirectory,
		string? outputDirectory)
	{
		Configuration = configuration;
		ConfigurationPath = configurationPath;
		Collector = collector;
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;

		var defaultCheckoutDirectory = Path.Join(Paths.ApplicationData.FullName, "codex", "clone");
		CheckoutDirectory = checkoutDirectory is null
			? FileSystemFactory.AppData.DirectoryInfo.New(defaultCheckoutDirectory)
			: ReadFileSystem.DirectoryInfo.New(checkoutDirectory);

		var defaultOutputDirectory = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "codex", "docs");
		OutputDirectory = WriteFileSystem.DirectoryInfo.New(outputDirectory ?? defaultOutputDirectory);
	}
}
