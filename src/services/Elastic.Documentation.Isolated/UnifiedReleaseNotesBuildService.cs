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
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Isolated;

/// <summary>Options for building the unified release notes site from a folder of bundles.</summary>
public record UnifiedReleaseNotesBuildOptions
{
	/// <summary>Root folder that holds one sub-folder of bundle files per product, as written by <c>release unified bundle</c>.</summary>
	public required DirectoryInfo BundlesDirectory { get; init; }

	/// <summary>Destination for the generated site. The folder is cleaned before the build.</summary>
	public required DirectoryInfo Output { get; init; }

	/// <summary>
	/// Versions the site is built for. A product without a bundle for one of them is reported, not rejected:
	/// a product that did not change in a release has no bundle for it.
	/// </summary>
	public IReadOnlyCollection<string> ExpectedVersions { get; init; } = [];

	/// <summary>
	/// Products without <c>release-notes: dra</c> to put on the site, by id, such as the serverless products. Their releases are
	/// labels, so <see cref="ExpectedVersions"/> does not apply to them.
	/// </summary>
	public IReadOnlyCollection<string> AdditionalProductIds { get; init; } = [];

	/// <summary>
	/// Rewrites the content of a bundle file as it is copied into the site: product id, file name and content in, content out.
	/// <c>release preview build</c> uses it to scrub references to private repositories, so the preview shows what publication
	/// would. A failure to scrub fails the build. Not applied by default.
	/// </summary>
	public Func<string, string, string, Cancel, Task<string>>? ScrubBundle { get; init; }

	/// <summary>
	/// Add the diagnostics page when the bundles folder has the problems file of <c>release preview bundle</c>. Off by default: the
	/// file names pull requests of private repositories, and a folder reused by another command must not put them on its site.
	/// </summary>
	public bool ShowDiagnostics { get; init; }

	/// <summary>
	/// The day the site is built, for the preview releases of the serverless products. They have no version number: a release is named
	/// by the day it ships, so the page says what the name would be if it shipped today. Left out when <c>null</c>.
	/// </summary>
	public DateOnly? PreviewDate { get; init; }

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

	private sealed record ProductBundles(ProductVersions Versions, IReadOnlyList<(string FileName, string Content)> Files)
	{
		public Product Product => Versions.Product;
	}

	public async Task<bool> Build(IDiagnosticsCollector collector, UnifiedReleaseNotesBuildOptions options, Cancel ctx = default)
	{
		var products = UnifiedReleaseNotesDocset.SelectProducts(
			configurationContext.ProductsConfiguration.Products.Values,
			options.AdditionalProductIds
		);
		if (products.Count == 0)
		{
			collector.EmitError(string.Empty, "No products with 'release-notes: dra' found in products.yml. There is nothing to build.");
			return false;
		}

		var realFs = new FileSystem();
		var bundles = await LoadBundlesAsync(collector, realFs, products, options, ctx);
		if (bundles.Count == 0)
		{
			collector.EmitError(
				options.BundlesDirectory.FullName,
				$"No product has bundles under '{options.BundlesDirectory.FullName}'. Run 'release unified bundle' first."
			);
			return false;
		}

		products = [.. bundles.Select(b => b.Product)];

		var docsRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, "unified-release-notes", "docs");
		IReadOnlyList<PreviewProblem> problems = [];
		var checkedForProblems = options.ShowDiagnostics
			&& PreviewProblemsFile.TryRead(realFs, options.BundlesDirectory.FullName, out problems);
		var mockFs = CreateVirtualDocset(docsRoot, bundles, checkedForProblems ? problems : null, options.PreviewDate);
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

	private async Task<IReadOnlyList<ProductBundles>> LoadBundlesAsync(
		IDiagnosticsCollector collector,
		IFileSystem realFs,
		IReadOnlyList<Product> products,
		UnifiedReleaseNotesBuildOptions options,
		Cancel ctx
	)
	{
		var loader = new BundleLoader(realFs);
		var result = new List<ProductBundles>(products.Count);

		foreach (var product in products)
		{
			var folder = realFs.Path.Join(options.BundlesDirectory.FullName, product.Id);
			if (!realFs.Directory.Exists(folder))
			{
				_logger.LogInformation("'{Product}' has no bundles folder and is left out of the site.", product.Id);
				continue;
			}

			var loaded = loader.LoadBundles(folder, warning => collector.EmitWarning(folder, warning));
			if (loaded.Count == 0)
			{
				_logger.LogInformation("'{Product}' has no bundles and is left out of the site.", product.Id);
				continue;
			}

			var isDra = product.Features.ReleaseNotes == ReleaseNotesPath.DailyReleasableArtifacts;
			var missing = isDra ? MissingVersions(loaded.Select(b => b.Version), options.ExpectedVersions) : [];
			if (missing.Count > 0)
				_logger.LogInformation(
					"'{Product}' has no bundle for {Versions}. It did not change in them.",
					product.Id,
					string.Join(", ", missing)
				);

			// Copy every YAML file, not only the loaded bundles: LoadBundles folds amend sidecars into their
			// parent and drops them from its result, and the directive merges them again from the docset folder.
			var files = new List<(string FileName, string Content)>();
			var paths = realFs
				.Directory
				.EnumerateFiles(folder, "*.yaml")
				.Concat(realFs.Directory.EnumerateFiles(folder, "*.yml"))
				.Order(StringComparer.Ordinal);
			foreach (var file in paths)
			{
				var fileName = realFs.Path.GetFileName(file);
				var content = realFs.File.ReadAllText(file);
				if (options.ScrubBundle is { } scrub)
					content = await scrub(product.Id, fileName, content, ctx);
				files.Add((fileName, content));
			}

			result.Add(
				new ProductBundles(new ProductVersions(product, NewestFirst(loaded.Select(b => NormalizeVersion(b.Version)))), files)
			);
		}

		return result;
	}

	private static IReadOnlyList<string> MissingVersions(IEnumerable<string> bundleVersions, IReadOnlyCollection<string> required)
	{
		var present = new HashSet<string>(bundleVersions.Select(NormalizeVersion), StringComparer.OrdinalIgnoreCase);
		return [.. required.Where(v => !present.Contains(NormalizeVersion(v)))];
	}

	private static string NormalizeVersion(string version) => version.TrimStart('v');

	private static IReadOnlyList<string> NewestFirst(IEnumerable<string> versions) =>
		[.. versions.Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(VersionOrDate.Parse)];

	private static MockFileSystem CreateVirtualDocset(
		string docsRoot,
		IReadOnlyList<ProductBundles> bundles,
		IReadOnlyList<PreviewProblem>? problems,
		DateOnly? previewDate
	)
	{
		var mockFs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = docsRoot, CreateDefaultTempDir = false });

		foreach (var (relativePath, content) in UnifiedReleaseNotesDocset.Generate(
			[.. bundles.Select(b => b.Versions)],
			problems,
			previewDate
		))
			mockFs.AddFile(mockFs.Path.Join(docsRoot, relativePath), new MockFileData(content));

		foreach (var (productVersions, files) in bundles)
		{
			foreach (var (fileName, content) in files)
			{
				var path = mockFs.Path.Join(docsRoot, UnifiedReleaseNotesDocset.BundlesPath(productVersions.Product.Id), fileName);
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
