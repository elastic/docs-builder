// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Portal;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Portal;

/// <summary>
/// Context for portal operations containing configuration, file systems, and directories.
/// </summary>
public class PortalContext
{
	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }
	public IDiagnosticsCollector Collector { get; }
	public PortalConfiguration Configuration { get; }
	public IFileInfo ConfigurationPath { get; }
	public IDirectoryInfo CheckoutDirectory { get; }
	public IDirectoryInfo OutputDirectory { get; }

	public PortalContext(
		PortalConfiguration configuration,
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

		var defaultCheckoutDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "portal-checkouts");
		CheckoutDirectory = ReadFileSystem.DirectoryInfo.New(checkoutDirectory ?? defaultCheckoutDirectory);

		var defaultOutputDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "portal");
		OutputDirectory = ReadFileSystem.DirectoryInfo.New(outputDirectory ?? defaultOutputDirectory);
	}
}
