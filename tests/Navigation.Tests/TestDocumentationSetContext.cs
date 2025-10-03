// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated;

namespace Elastic.Documentation.Navigation.Tests;

public class TestDiagnosticsOutput(ITestOutputHelper output) : IDiagnosticsOutput
{
	public void Write(Diagnostic diagnostic)
	{
		if (diagnostic.Severity == Severity.Error)
			output.WriteLine($"Error: {diagnostic.Message} ({diagnostic.File}:{diagnostic.Line})");
		else
			output.WriteLine($"Warn : {diagnostic.Message} ({diagnostic.File}:{diagnostic.Line})");
	}
}

public class TestDiagnosticsCollector(ITestOutputHelper output)
	: DiagnosticsCollector([new TestDiagnosticsOutput(output)])
{
	private readonly List<Diagnostic> _diagnostics = [];

	public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

	protected override void HandleItem(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);
}

public class TestDocumentationSetContext : IDocumentationSetContext
{
	public TestDocumentationSetContext(
		IFileSystem fileSystem,
		IDirectoryInfo sourceDirectory,
		IDirectoryInfo outputDirectory,
		IFileInfo configPath,
		ITestOutputHelper output,
		string? repository = null
	)
	{
		ReadFileSystem = fileSystem;
		WriteFileSystem = fileSystem;
		DocumentationSourceDirectory = sourceDirectory;
		OutputDirectory = outputDirectory;
		ConfigurationPath = configPath;
		Collector = new TestDiagnosticsCollector(output);
		Git = repository is null ? GitCheckoutInformation.Unavailable : new GitCheckoutInformation
		{
			Branch = "main",
			Remote = $"elastic/{repository}",
			Ref = "main",
			RepositoryName = repository
		};

		// Start the diagnostics collector to process messages
		_ = Collector.StartAsync(CancellationToken.None);
	}

	public IDiagnosticsCollector Collector { get; }
	public IFileSystem ReadFileSystem { get; }
	public IFileSystem WriteFileSystem { get; }
	public IDirectoryInfo OutputDirectory { get; }
	public IDirectoryInfo DocumentationSourceDirectory { get; }
	public GitCheckoutInformation Git { get; }
	public IFileInfo ConfigurationPath { get; }

	public IReadOnlyCollection<Diagnostic> Diagnostics => ((TestDiagnosticsCollector)Collector).Diagnostics;
}

public class TestDocumentationFile : IDocumentationFile
{
	/// <inheritdoc />
	public string NavigationTitle { get; } = "Some navigation title";
}

public class TestDocumentationFileFactory : IDocumentationFileFactory<TestDocumentationFile>
{
	public static IDocumentationFileFactory<IDocumentationFile> Instance { get; } = new TestDocumentationFileFactory();

	/// <inheritdoc />
	public TestDocumentationFile? TryCreateDocumentationFile(IFileInfo path)
	{
		return new TestDocumentationFile();
	}
}
