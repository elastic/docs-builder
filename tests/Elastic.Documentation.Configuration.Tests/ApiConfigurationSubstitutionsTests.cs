// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Tests;

public class ApiConfigurationSubstitutionsTests
{
	private const string MinimalOpenApiJson =
							 /*lang=json,strict*/
							 """
		{
		  "openapi": "3.0.0",
		  "info": {
		    "title": "Test API for substitutions",
		    "version": "1.2.3",
		    "description": "Description line",
		    "license": {
		      "name": "Apache 2.0",
		      "url": "https://www.apache.org/licenses/LICENSE-2.0"
		    },
		    "contact": {
		      "name": "Support Team",
		      "email": "support@example.com",
		      "url": "https://example.com/support"
		    }
		  },
		  "paths": {}
		}
		""";

	[Fact]
	public void ConfigurationFile_AddsApiMetadataSubstitutions_FromPrimarySpec()
	{
		var collector = new TestDiagnosticsCollector();
		var root = Paths.WorkingDirectoryRoot.FullName;
		var docsDir = Path.Join(root, "docs");
		var configFilePath = Path.Join(docsDir, "_docset.yml");
		var specPath = Path.Join(docsDir, "minimal-openapi.json");

		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configFilePath, new MockFileData("") },
			{ specPath, new MockFileData(MinimalOpenApiJson) }
		}, root);

		var docSet = new DocumentationSetFile
		{
			Project = "test",
			TableOfContents = [],
			Api = new Dictionary<string, ApiConfiguration>
			{
				["testapi"] = new ApiConfiguration { Spec = "minimal-openapi.json" }
			}
		};

		var configPath = fileSystem.FileInfo.New(configFilePath);
		var docsDirInfo = fileSystem.DirectoryInfo.New(docsDir);

		var context = new MockDocumentationSetContext(collector, fileSystem, configPath, docsDirInfo);
		var versionsConfig = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>()
		};
		var productsConfig = new ProductsConfiguration
		{
			Products = new Dictionary<string, Product>().ToFrozenDictionary(),
			PublicReferenceProducts = new Dictionary<string, Product>().ToFrozenDictionary(),
			ProductDisplayNames = new Dictionary<string, string>().ToFrozenDictionary()
		};

		var config = new ConfigurationFile(docSet, context, versionsConfig, productsConfig);

		config.ApiConfigurations.Should().NotBeNull().And.ContainKey("testapi");

		config.Substitutions.Keys.Should().Contain("api.testapi.title");
		config.Substitutions["api.testapi.title"].Should().Be("Test API for substitutions");
		config.Substitutions["api.testapi.version"].Should().Be("1.2.3");
	}

	[Fact]
	public void ConfigurationFile_AddsLicenseAndContactSubstitutions_WhenPresent()
	{
		var collector = new TestDiagnosticsCollector();
		var root = Paths.WorkingDirectoryRoot.FullName;
		var docsDir = Path.Join(root, "docs");
		var configFilePath = Path.Join(docsDir, "_docset.yml");
		var specPath = Path.Join(docsDir, "minimal-openapi.json");

		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configFilePath, new MockFileData("") },
			{ specPath, new MockFileData(MinimalOpenApiJson) }
		}, root);

		var docSet = new DocumentationSetFile
		{
			Project = "test",
			TableOfContents = [],
			Api = new Dictionary<string, ApiConfiguration>
			{
				["testapi"] = new ApiConfiguration { Spec = "minimal-openapi.json" }
			}
		};

		var configPath = fileSystem.FileInfo.New(configFilePath);
		var docsDirInfo = fileSystem.DirectoryInfo.New(docsDir);

		var context = new MockDocumentationSetContext(collector, fileSystem, configPath, docsDirInfo);
		var versionsConfig = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>()
		};
		var productsConfig = new ProductsConfiguration
		{
			Products = new Dictionary<string, Product>().ToFrozenDictionary(),
			PublicReferenceProducts = new Dictionary<string, Product>().ToFrozenDictionary(),
			ProductDisplayNames = new Dictionary<string, string>().ToFrozenDictionary()
		};

		var config = new ConfigurationFile(docSet, context, versionsConfig, productsConfig);

		// License substitutions
		config.Substitutions["api.testapi.license.name"].Should().Be("Apache 2.0");
		config.Substitutions["api.testapi.license.url"].Should().Be("https://www.apache.org/licenses/LICENSE-2.0");

		// Contact substitutions
		config.Substitutions["api.testapi.contact.name"].Should().Be("Support Team");
		config.Substitutions["api.testapi.contact.email"].Should().Be("support@example.com");
		config.Substitutions["api.testapi.contact.url"].Should().Be("https://example.com/support");
	}


	[Fact]
	public void ConfigurationFile_EmitsWarning_WhenSpecFileCannotBeLoaded()
	{
		var collector = new TestDiagnosticsCollector();
		var root = Paths.WorkingDirectoryRoot.FullName;
		var docsDir = Path.Join(root, "docs");
		var configFilePath = Path.Join(docsDir, "_docset.yml");
		var specPath = Path.Join(docsDir, "invalid-openapi.json");

		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configFilePath, new MockFileData("") },
			{ specPath, new MockFileData("invalid json content") }
		}, root);

		var docSet = new DocumentationSetFile
		{
			Project = "test",
			TableOfContents = [],
			Api = new Dictionary<string, ApiConfiguration>
			{
				["testapi"] = new ApiConfiguration { Spec = "invalid-openapi.json" }
			}
		};

		var configPath = fileSystem.FileInfo.New(configFilePath);
		var docsDirInfo = fileSystem.DirectoryInfo.New(docsDir);

		var context = new MockDocumentationSetContext(collector, fileSystem, configPath, docsDirInfo);
		var versionsConfig = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>()
		};
		var productsConfig = new ProductsConfiguration
		{
			Products = new Dictionary<string, Product>().ToFrozenDictionary(),
			PublicReferenceProducts = new Dictionary<string, Product>().ToFrozenDictionary(),
			ProductDisplayNames = new Dictionary<string, string>().ToFrozenDictionary()
		};

		var config = new ConfigurationFile(docSet, context, versionsConfig, productsConfig);

		// Should have emitted a warning
		collector.Diagnostics.Should().NotBeEmpty();
		collector.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Warning);

		// Should not have API substitutions for the failed spec
		config.Substitutions.Keys.Should().NotContain(k => k.StartsWith("api.testapi."));
	}

	/// <summary>
	/// Test-specific diagnostics collector that exposes collected diagnostics for assertions.
	/// </summary>
	private sealed class TestDiagnosticsCollector : IDiagnosticsCollector
	{
		private readonly List<Diagnostic> _diagnostics = [];
		private int _errors;
		private int _warnings;
		private int _hints;

		public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

		public DiagnosticsChannel Channel => throw new NotImplementedException();
		public HashSet<string> OffendingFiles { get; } = [];
		public ConcurrentBag<string> CrossLinks { get; } = [];
		public ConcurrentDictionary<string, bool> InUseSubstitutionKeys { get; } = [];
		public bool NoHints { get; set; }
		public int Warnings => _warnings;
		public int Errors => _errors;
		public int Hints => _hints;

		public void Emit(Severity severity, string file, string message)
		{
			var diagnostic = new Diagnostic
			{
				Severity = severity,
				File = file,
				Message = message
			};
			_diagnostics.Add(diagnostic);

			switch (severity)
			{
				case Severity.Error:
					Interlocked.Increment(ref _errors);
					break;
				case Severity.Warning:
					Interlocked.Increment(ref _warnings);
					break;
				case Severity.Hint:
					Interlocked.Increment(ref _hints);
					break;
			}
		}

		public void EmitError(string file, string message, Exception? e = null) => Emit(Severity.Error, file, message);
		public void EmitError(string file, string message, string specificErrorMessage) => Emit(Severity.Error, file, message);
		public void EmitWarning(string file, string message) => Emit(Severity.Warning, file, message);
		public void EmitHint(string file, string message) => Emit(Severity.Hint, file, message);
		public void Write(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);
		public void CollectUsedSubstitutionKey(ReadOnlySpan<char> key) { }
		public void EmitCrossLink(string link) => CrossLinks.Add(link);

		public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	/// <summary>
	/// Mock documentation set context for testing configuration file processing.
	/// </summary>
	private sealed class MockDocumentationSetContext(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		IFileInfo configurationPath,
		IDirectoryInfo documentationSourceDirectory)
		: IDocumentationSetContext
	{
		public IDiagnosticsCollector Collector => collector;
		public ScopedFileSystem ReadFileSystem => WriteFileSystem;
		public ScopedFileSystem WriteFileSystem { get; } = FileSystemFactory.ScopeCurrentWorkingDirectoryForWrite(fileSystem);
		public IDirectoryInfo OutputDirectory => fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts"));
		public IFileInfo ConfigurationPath => configurationPath;
		public BuildType BuildType => BuildType.Isolated;
		public IDirectoryInfo DocumentationSourceDirectory => documentationSourceDirectory;
		public GitCheckoutInformation Git => GitCheckoutInformation.Create(documentationSourceDirectory, fileSystem);
	}
}
