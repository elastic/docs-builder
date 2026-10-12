// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Actions.Core.Services;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Isolated;

/// <summary>Options for building the unified release notes site from a folder of bundles.</summary>
public record UnifiedReleaseNotesBuildOptions
{
	/// <summary>Root folder that holds one sub-folder of bundle files per product, as written by <c>release unified bundle</c>.</summary>
	public required DirectoryInfo BundlesDirectory { get; init; }

	/// <summary>Destination for the generated site. The folder is cleaned before the build.</summary>
	public required DirectoryInfo Output { get; init; }

	/// <summary>Versions that every product must have a bundle for. Empty requires at least one bundle per product.</summary>
	public IReadOnlyCollection<string> RequiredVersions { get; init; } = [];

	/// <summary>URL path prefix prepended to every generated link.</summary>
	public string? PathPrefix { get; init; }

	/// <summary>Base URL written into canonical link tags.</summary>
	public Uri? CanonicalBaseUrl { get; init; }

	/// <summary>Treat warnings as errors.</summary>
	public bool? Strict { get; init; }
}

/// <summary>
/// Builds the unified release notes site. The docset is synthesized in an in-memory file system from
/// <c>products.yml</c>, the bundles are copied into it, and the result is written to a real output folder.
/// </summary>
public class UnifiedReleaseNotesBuildService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<UnifiedReleaseNotesBuildService>();

	private sealed record ProductBundles(Product Product, IReadOnlyList<(string FileName, string Content)> Files);

	public async Task<bool> Build(IDiagnosticsCollector collector, UnifiedReleaseNotesBuildOptions options, Cancel ctx = default)
	{
		var products = UnifiedReleaseNotesDocset.SelectProducts(configurationContext.ProductsConfiguration.Products.Values);
		if (products.Count == 0)
		{
			collector.EmitError(string.Empty, "No products with 'release-notes: dra' found in products.yml. There is nothing to build.");
			return false;
		}

		var realFs = new FileSystem();
		var bundles = LoadBundles(collector, realFs, products, options);
		if (bundles is null)
			return false;

		var docsRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, "unified-release-notes", "docs");
		var mockFs = CreateVirtualDocset(docsRoot, products, bundles);
		PrepareOutput(realFs, options.Output.FullName);

		var docFs = DocumentationFileSystem.Resolve(
			mockFs.DirectoryInfo.New(docsRoot),
			new DocumentationScopeOptions
			{
				Inner = mockFs,
				InnerWrite = realFs,
				Output = options.Output.FullName,
				Git = GitCheckoutInformation.Unavailable
			}
		);

		var buildOptions = new IsolatedBuildOptions
		{
			Output = options.Output,
			PathPrefix = options.PathPrefix,
			CanonicalBaseUrl = options.CanonicalBaseUrl,
			Strict = options.Strict,
			Force = true,
			SkipApi = true,
			SkipCrossLinks = true
		};

		_logger.LogInformation(
			"Building unified release notes for {Count} product(s) into {Output}",
			products.Count,
			options.Output.FullName
		);
		var service = new IsolatedBuildService(logFactory, configurationContext, githubActionsService, environmentVariables);
		return await service.BuildFrom(collector, buildOptions, docFs, ctx);
	}

	private IReadOnlyList<ProductBundles>? LoadBundles(
		IDiagnosticsCollector collector,
		IFileSystem realFs,
		IReadOnlyList<Product> products,
		UnifiedReleaseNotesBuildOptions options
	)
	{
		var loader = new BundleLoader(realFs);
		var result = new List<ProductBundles>(products.Count);
		var failed = false;

		foreach (var product in products)
		{
			var folder = realFs.Path.Join(options.BundlesDirectory.FullName, product.Id);
			if (!realFs.Directory.Exists(folder))
			{
				collector.EmitError(folder, $"Product '{product.Id}' has no bundles folder. Run 'release unified bundle' first.");
				failed = true;
				continue;
			}

			var loaded = loader.LoadBundles(folder, warning => collector.EmitWarning(folder, warning));
			var missing = MissingVersions(loaded.Select(b => b.Version), options.RequiredVersions);
			if (loaded.Count == 0 || missing.Count > 0)
			{
				var detail = loaded.Count == 0 ? "no bundle files" : $"no bundle for version(s) {string.Join(", ", missing)}";
				collector.EmitError(folder, $"Product '{product.Id}' has {detail}.");
				failed = true;
				continue;
			}

			var files = loaded.Select(
				b => (FileName: realFs.Path.GetFileName(b.FilePath), Content: realFs.File.ReadAllText(b.FilePath))
			).ToArray();
			result.Add(new ProductBundles(product, files));
		}

		return failed ? null : result;
	}

	private static IReadOnlyList<string> MissingVersions(IEnumerable<string> bundleVersions, IReadOnlyCollection<string> required)
	{
		var present = new HashSet<string>(bundleVersions.Select(NormalizeVersion), StringComparer.OrdinalIgnoreCase);
		return [.. required.Where(v => !present.Contains(NormalizeVersion(v)))];
	}

	private static string NormalizeVersion(string version) => version.TrimStart('v');

	private static MockFileSystem CreateVirtualDocset(
		string docsRoot,
		IReadOnlyList<Product> products,
		IReadOnlyList<ProductBundles> bundles
	)
	{
		var mockFs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = docsRoot, CreateDefaultTempDir = false });

		foreach (var (relativePath, content) in UnifiedReleaseNotesDocset.Generate(products))
			mockFs.AddFile(mockFs.Path.Join(docsRoot, relativePath), new MockFileData(content));

		foreach (var (product, files) in bundles)
		{
			foreach (var (fileName, content) in files)
			{
				var path = mockFs.Path.Join(docsRoot, UnifiedReleaseNotesDocset.BundlesPath(product.Id), fileName);
				mockFs.AddFile(path, new MockFileData(content));
			}
		}

		return mockFs;
	}

	private static void PrepareOutput(IFileSystem realFs, string output)
	{
		// The output folder is created on the in-memory read file system during resolution,
		// so the real folder has to be cleaned here.
		if (realFs.Directory.Exists(output))
			realFs.Directory.Delete(output, true);
		_ = realFs.Directory.CreateDirectory(output);
	}
}
