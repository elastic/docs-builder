// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>A file of a test docset: its path under <c>/docs</c> and its text.</summary>
internal readonly record struct DocFile(string Path, string Content = "# Page\n");

/// <summary>
/// Loads a docset the way the builder does, <c>DocumentationSetFile.LoadAndResolve</c> and then <c>ConfigurationFile</c>, from a
/// mock file system that holds <c>/docs/docset.yml</c> and the given files, and writes what came out as text.
/// </summary>
internal static partial class DocsetGolden
{
	// rooted on the current drive, so the scoped file system recognises it on Windows too
	public static readonly string Root = Path.GetFullPath("/docs");

	/// <summary>The diagnostics, the resolved table of contents, then the whole resolved model.</summary>
	public static string Resolve(string docsetYaml, params DocFile[] files)
	{
		var collector = new CapturingDiagnosticsCollector();
		var sb = new StringBuilder();
		DocumentationSetFile? docSet = null;
		try
		{
			docSet = LoadAndResolve(collector, docsetYaml, files);
		}
		catch (Exception e)
		{
			_ = sb.AppendLine("diagnostics:").Append(collector.Captured.ToSnapshotText(firstLineOnly: true));
			AppendThrows(sb, e);
			return Normalise(sb.ToString());
		}

		_ = sb.AppendLine("diagnostics:").Append(collector.Captured.ToSnapshotText(firstLineOnly: true));
		_ = sb.AppendLine("toc:").Append(TocDump.ToText(docSet.TableOfContents));
		_ = sb.AppendLine("model:").Append(ModelDump.ToText(docSet, Summarize));
		return Normalise(sb.ToString());
	}

	/// <summary>The diagnostics, then the resolved table of contents only. For tables of small inputs.</summary>
	public static string Toc(string docsetYaml, params DocFile[] files)
	{
		var collector = new CapturingDiagnosticsCollector();
		var sb = new StringBuilder();
		try
		{
			var docSet = LoadAndResolve(collector, docsetYaml, files);
			_ = sb.AppendLine("diagnostics:").Append(collector.Captured.ToSnapshotText(firstLineOnly: true));
			_ = sb.Append(TocDump.ToText(docSet.TableOfContents));
		}
		catch (Exception e)
		{
			_ = sb.AppendLine("diagnostics:").Append(collector.Captured.ToSnapshotText(firstLineOnly: true));
			AppendThrows(sb, e);
		}
		return Normalise(sb.ToString());
	}

	/// <summary>The diagnostics of resolving and then building <c>ConfigurationFile</c>, and its members.</summary>
	public static string Configure(string docsetYaml, bool withGlobalConfig = false, params DocFile[] files) =>
		Configure(docsetYaml, withGlobalConfig, null, files);

	/// <summary>
	/// The same, with only the named members of <c>ConfigurationFile</c> written after the diagnostics, for tables where the
	/// diagnostics are the point. <c>null</c> writes every member.
	/// </summary>
	public static string Configure(string docsetYaml, bool withGlobalConfig, string[]? only, params DocFile[] files)
	{
		var collector = new CapturingDiagnosticsCollector();
		var sb = new StringBuilder();
		try
		{
			var docSet = LoadAndResolve(collector, docsetYaml, files);
			var fileSystem = CreateFileSystem(docsetYaml, files);
			var config = CreateConfiguration(docSet, collector, fileSystem, withGlobalConfig);
			_ = sb.AppendLine("diagnostics:").Append(collector.Captured.ToSnapshotText(firstLineOnly: true));
			if (only is not null)
			{
				var skip = typeof(ConfigurationFile).GetProperties().Select(p => p.Name).Except(only).ToArray();
				_ = sb.Append(ModelDump.ToText(config, Summarize, skip));
				return Normalise(sb.ToString());
			}

			_ = sb.Append(ModelDump.ToText(config, Summarize, "Substitutions"));
			_ = sb.AppendLine("substitutions:");
			foreach (var (key, value) in config
				.Substitutions
				.Where(kvp => !IsGenerated(kvp.Key))
				.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
				_ = sb.Append("  ").Append(key).Append(": ").AppendLine(value);
			_ = sb.Append("generated substitutions (version.* and product.* keys): ").AppendLine(
				config.Substitutions.Count(kvp => IsGenerated(kvp.Key)).ToString()
			);
		}
		catch (Exception e)
		{
			_ = sb.AppendLine("diagnostics:").Append(collector.Captured.ToSnapshotText(firstLineOnly: true));
			AppendThrows(sb, e);
		}
		return Normalise(sb.ToString());
	}

	private static bool IsGenerated(string key) =>
		key.StartsWith("version.", StringComparison.Ordinal) || key.StartsWith("product.", StringComparison.Ordinal) || key.StartsWith('.');

	/// <summary>Every substitution <c>ConfigurationFile</c> adds from the versions and products configuration, sorted.</summary>
	public static string GeneratedSubstitutions(string docsetYaml)
	{
		var collector = new CapturingDiagnosticsCollector();
		var docSet = LoadAndResolve(collector, docsetYaml);
		var config = CreateConfiguration(docSet, collector, CreateFileSystem(docsetYaml), withGlobalConfig: true);
		var sb = new StringBuilder();
		foreach (var (key, value) in config
			.Substitutions
			.Where(kvp => IsGenerated(kvp.Key))
			.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
			_ = sb.Append(key).Append(": ").AppendLine(value);
		return sb.ToString();
	}

	/// <summary>
	/// The frozen copy of this repository's <c>docs/</c> docset: its <c>_docset.yml</c>, <c>_redirects.yml</c> and <c>toc.yml</c> files
	/// as they were (see <c>Golden/Baseline/README.md</c>), and an empty page for every other file the folder held then.
	/// </summary>
	public static MockFileSystem CreateBaselineDocsFileSystem()
	{
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), Root);
		var baseline = Path.Join(AppContext.BaseDirectory, "Golden", "Baseline", "docs");
		foreach (var path in File.ReadAllLines(Path.Join(baseline, "files.txt")).Where(l => l.Length > 0))
			fileSystem.AddFile($"{Root}/{path}", new MockFileData(path.EndsWith(".json", StringComparison.Ordinal) ? "{}" : "# Page\n"));
		foreach (var real in new[] { "_docset.yml", "_redirects.yml", "development/toc.yml", "development/link-validation/toc.yml" })
			fileSystem.AddFile($"{Root}/{real}", new MockFileData(File.ReadAllText(Path.Join(baseline, real))));
		return fileSystem;
	}

	public static DocumentationSetFile LoadAndResolveBaselineDocs(IDiagnosticsCollector collector, MockFileSystem fileSystem) =>
		DocumentationSetFile.LoadAndResolve(
			collector,
			fileSystem.FileInfo.New($"{Root}/_docset.yml"),
			new ScopedFileSystem(fileSystem, Root)
		);

	// paths read the same on every operating system: forward slashes and no drive letter
	public static string Normalise(string text) => DriveLetter().Replace(text.Replace('\\', '/'), string.Empty);

	[GeneratedRegex("(?<![A-Za-z0-9])[A-Za-z]:(?=/)", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex DriveLetter();

	public static DocumentationSetFile LoadAndResolve(IDiagnosticsCollector collector, string docsetYaml, params DocFile[] files)
	{
		var fileSystem = CreateFileSystem(docsetYaml, files);
		var docsetPath = fileSystem.FileInfo.New($"{Root}/docset.yml");
		return DocumentationSetFile.LoadAndResolve(collector, docsetPath, new ScopedFileSystem(fileSystem, Root));
	}

	public static MockFileSystem CreateFileSystem(string docsetYaml, params DocFile[] files)
	{
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), Root);
		fileSystem.AddFile($"{Root}/docset.yml", new MockFileData(docsetYaml));
		foreach (var file in files)
			fileSystem.AddFile($"{Root}/{file.Path}", new MockFileData(file.Content));
		return fileSystem;
	}

	public static IDocumentationSetContext CreateContext(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		string docsetName = "docset.yml"
	) =>
		new TestDocumentationSetContext(
			collector,
			fileSystem,
			fileSystem.FileInfo.New($"{Root}/{docsetName}"),
			fileSystem.DirectoryInfo.New(Root)
		);

	public static ConfigurationFile CreateConfiguration(
		DocumentationSetFile docSet,
		IDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		bool withGlobalConfig,
		string docsetName = "docset.yml"
	)
	{
		var configPath = fileSystem.FileInfo.New($"{Root}/{docsetName}");
		var docsDir = fileSystem.DirectoryInfo.New(Root);
		var context = new TestDocumentationSetContext(collector, fileSystem, configPath, docsDir);
		var versions = withGlobalConfig
			? GoldenConfig.LoadVersions()
			: new VersioningSystemCatalog { VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>() };
		var products = withGlobalConfig
			? GoldenConfig.LoadProducts()
			: new ProductCatalog { Products = new Dictionary<string, Product>().ToFrozenDictionary() };
		return new ConfigurationFile(DocumentationSetLoader.ToDefinition(docSet, configPath, docsDir), context, versions, products);
	}

	/// <summary>Names a file system entry by its path, so a dump does not walk the file system behind it.</summary>
	public static string? Summarize(object value) => value switch
	{
		IFileSystemInfo info => $"<path {info.FullName.Replace('\\', '/')}>",
		Product product => $"<Product {product.Id}>",
		IFileSystem => "<file system>",
		_ => null
	};

	private static void AppendThrows(StringBuilder sb, Exception e)
	{
		for (var current = e; current is not null; current = current.InnerException)
		{
			// the text of an argument exception comes from the runtime and differs between operating systems
			var message = current is ArgumentException ? "(message omitted)" : current.Message.Replace('\\', '/').Split('\n')[0].TrimEnd();
			_ = sb.Append(current == e ? "throws " : "inner ").Append(current.GetType().Name).Append(": ").AppendLine(message);
		}
	}

	private sealed class TestDocumentationSetContext(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		IFileInfo configurationPath,
		IDirectoryInfo documentationSourceDirectory
	) : IDocumentationSetContext
	{
		public IDiagnosticsCollector Collector => collector;
		public IDocumentationFileSystem ReadFileSystem { get; } = DocumentationFileSystem.Resolve(
			documentationSourceDirectory,
			new DocumentationScopeOptions { Inner = fileSystem, ConfigurationFile = configurationPath.FullName }
		);
		public DocumentationWriteFileSystem WriteFileSystem { get; } = new(
			fileSystem.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName),
			inner: fileSystem
		);
		public IDirectoryInfo OutputDirectory => fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts"));
		public IFileInfo ConfigurationPath => configurationPath;
		public BuildType BuildType => BuildType.Isolated;
		public IDirectoryInfo DocumentationSourceDirectory => documentationSourceDirectory;
		public GitCheckoutInformation Git => GitCheckoutInformationFactory.Create(documentationSourceDirectory, fileSystem);
		public IEnvironmentVariables Environment => SystemEnvironmentVariables.Instance;
	}
}
