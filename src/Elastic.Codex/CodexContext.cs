// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Deploying.Synchronization;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;

namespace Elastic.Codex;

/// <summary>
/// Context for codex operations containing configuration, file systems, and directories.
/// </summary>
public class CodexContext : IDocsSyncContext
{
	public CheckoutsFileSystem ReadFileSystem { get; }
	public DocumentationWriteFileSystem WriteFileSystem { get; }
	public IDiagnosticsCollector Collector { get; }
	public CodexConfiguration Configuration { get; }
	public IFileInfo ConfigurationPath { get; }
	public IDirectoryInfo CheckoutDirectory { get; }
	public IDirectoryInfo OutputDirectory { get; }

	/// <summary>
	/// The Elasticsearch index namespace for this codex, derived from the environment name.
	/// Falls back to "codex" when no environment is specified.
	/// </summary>
	public string IndexNamespace => string.IsNullOrEmpty(Configuration.Environment) ? "codex" : $"codex-{EnvironmentName}";

	/// <inheritdoc cref="IDocsSyncContext.EnvironmentName"/>
	public string EnvironmentName { get; }

	public CodexContext(
		CodexConfiguration configuration,
		IFileInfo configurationPath,
		IDiagnosticsCollector collector,
		CheckoutsFileSystem fileSystem,
		string? checkoutDirectory = null,
		string? outputDirectory = null
	)
	{
		Configuration = configuration;
		ConfigurationPath = configurationPath;
		Collector = collector;
		ReadFileSystem = fileSystem;
		WriteFileSystem = fileSystem.Write;

		EnvironmentName = string.IsNullOrEmpty(configuration.Environment) ? "codex" : configuration.Environment;

		var defaultCheckoutDirectory = Path.Join(Paths.ApplicationData.FullName, "codex", "clone");
		CheckoutDirectory = checkoutDirectory is null
			? fileSystem.DirectoryInfo.New(defaultCheckoutDirectory)
			: fileSystem.DirectoryInfo.New(checkoutDirectory);

		var defaultOutputDirectory = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "codex", "docs");
		OutputDirectory = WriteFileSystem.DirectoryInfo.New(outputDirectory ?? defaultOutputDirectory);
	}
}
