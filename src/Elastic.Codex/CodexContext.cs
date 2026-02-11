// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Codex;

/// <summary>
/// Context for codex operations containing configuration, file systems, and directories.
/// </summary>
public class CodexContext
{
	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }
	public IDiagnosticsCollector Collector { get; }
	public CodexConfiguration Configuration { get; }
	public IFileInfo ConfigurationPath { get; }
	public IDirectoryInfo CheckoutDirectory { get; }
	public IDirectoryInfo OutputDirectory { get; }

	/// <summary>
	/// The Elasticsearch index namespace for this codex, derived from the namespace field.
	/// Falls back to "codex" when no namespace is specified.
	/// </summary>
	public string IndexNamespace => string.IsNullOrEmpty(Configuration.Namespace)
		? "codex"
		: $"codex-{Configuration.Namespace}";

	public CodexContext(
		CodexConfiguration configuration,
		IFileInfo configurationPath,
		IDiagnosticsCollector collector,
		IFileSystem readFileSystem,
		IFileSystem writeFileSystem,
		string? checkoutDirectory,
		string? outputDirectory)
	{
		Configuration = configuration;
		ConfigurationPath = configurationPath;
		Collector = collector;
		ReadFileSystem = readFileSystem;
		WriteFileSystem = writeFileSystem;

		var defaultCheckoutDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "codex", "clone");
		CheckoutDirectory = ReadFileSystem.DirectoryInfo.New(checkoutDirectory ?? defaultCheckoutDirectory);

		var defaultOutputDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "codex", "docs");
		OutputDirectory = ReadFileSystem.DirectoryInfo.New(outputDirectory ?? defaultOutputDirectory);
	}
}
