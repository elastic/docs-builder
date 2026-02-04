// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Codex.Group;
using Elastic.Codex.Landing;
using Elastic.Codex.Navigation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using RazorSlices;

namespace Elastic.Codex;

/// <summary>
/// Interface for codex models that can render themselves.
/// </summary>
public interface ICodexModel : INavigationModel, ICodexPageRenderer;

/// <summary>
/// Interface for rendering codex pages.
/// </summary>
public interface ICodexPageRenderer
{
	/// <summary>
	/// Renders the page to the given stream.
	/// </summary>
	Task RenderAsync(FileSystemStream stream, CodexRenderContext context, CancellationToken ctx = default);
}

/// <summary>
/// Generator for codex HTML pages.
/// </summary>
public class CodexGenerator(ILoggerFactory logFactory, BuildContext context, IDirectoryInfo outputDirectory)
{
	private readonly ILogger _logger = logFactory.CreateLogger<CodexGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));
	private readonly IDirectoryInfo _outputDirectory = outputDirectory;

	/// <summary>
	/// Generates all codex pages (landing and category pages).
	/// </summary>
	public async Task Generate(CodexNavigation codexNavigation, Cancel ctx = default)
	{
		_logger.LogInformation("Generating codex pages for {Title}", codexNavigation.NavigationTitle);

		// Extract static files to the codex's _static directory
		await ExtractEmbeddedStaticResources(ctx);

		var navigationRenderer = new CodexNavigationHtmlWriter(context, codexNavigation);

		var renderContext = new CodexRenderContext(context, codexNavigation, _contentHashProvider)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = codexNavigation.Index
		};

		// Render the codex landing page
		await RenderLandingPage(codexNavigation, renderContext, navigationRenderer, ctx);

		// Render group landing pages (/g/slug)
		foreach (var groupNav in codexNavigation.GroupNavigations)
			await RenderGroupLandingPage(groupNav, renderContext, navigationRenderer, ctx);
	}

	private async Task ExtractEmbeddedStaticResources(Cancel ctx)
	{
		_logger.LogInformation("Copying static files to codex output directory");
		var assembly = typeof(EmbeddedOrPhysicalFileProvider).Assembly;
		var embeddedStaticFiles = assembly
			.GetManifestResourceNames()
			.ToList();

		foreach (var resourceName in embeddedStaticFiles)
		{
			await using var resourceStream = assembly.GetManifestResourceStream(resourceName);
			if (resourceStream == null)
				continue;

			// Convert resource name to file path: Elastic.Documentation.Site._static.file.ext -> _static/file.ext
			var path = resourceName
				.Replace("Elastic.Documentation.Site.", "")
				.Replace("_static.", $"_static{Path.DirectorySeparatorChar}");

			// Output to codex's URL prefix directory (e.g., internal-docs/_static/)
			var outputPath = Path.Combine(
				_outputDirectory.FullName,
				context.UrlPathPrefix?.Trim('/') ?? string.Empty,
				path);

			var outputFile = _writeFileSystem.FileInfo.New(outputPath);
			if (outputFile.Directory is { Exists: false })
				outputFile.Directory.Create();

			await using var stream = outputFile.OpenWrite();
			await resourceStream.CopyToAsync(stream, ctx);
			_logger.LogDebug("Copied static embedded resource {Path}", path);
		}
	}

	private async Task RenderLandingPage(
		CodexNavigation codexNavigation,
		CodexRenderContext renderContext,
		CodexNavigationHtmlWriter navigationRenderer,
		CancellationToken ctx)
	{
		var navigationRenderResult = await navigationRenderer.RenderNavigation(
			codexNavigation,
			codexNavigation.Index,
			ctx);

		renderContext = renderContext with
		{
			CurrentNavigation = codexNavigation.Index,
			NavigationHtml = navigationRenderResult.Html
		};

		var viewModel = new LandingViewModel(renderContext)
		{
			IndexPage = (CodexIndexPage)codexNavigation.Index.Model
		};

		var outputFile = GetOutputFile(codexNavigation.Url);
		if (!outputFile.Directory!.Exists)
			outputFile.Directory.Create();

		var slice = LandingView.Create(viewModel);

		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.Create);
		await slice.RenderAsync(stream, cancellationToken: ctx);
		_logger.LogDebug("Generated codex landing page: {Path}", outputFile.FullName);
	}

	private async Task RenderGroupLandingPage(
		GroupNavigation groupNav,
		CodexRenderContext renderContext,
		CodexNavigationHtmlWriter navigationRenderer,
		CancellationToken ctx)
	{
		var navigationRenderResult = await navigationRenderer.RenderNavigation(
			groupNav,
			groupNav.Index,
			ctx);

		renderContext = renderContext with
		{
			CurrentNavigation = groupNav.Index,
			NavigationHtml = navigationRenderResult.Html
		};

		var viewModel = new GroupLandingViewModel(renderContext)
		{
			Group = groupNav
		};

		var outputFile = GetOutputFile(groupNav.Url);
		if (!outputFile.Directory!.Exists)
			outputFile.Directory.Create();

		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.Create);
		var slice = GroupLandingView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);

		_logger.LogDebug("Generated group landing page: {Path}", outputFile.FullName);
	}

	private IFileInfo GetOutputFile(string url)
	{
		const string indexHtml = "index.html";
		// Keep the full URL path so file structure matches URLs
		var fileName = url.TrimStart('/') + "/" + indexHtml;
		return _writeFileSystem.FileInfo.New(Path.Combine(_outputDirectory.FullName, fileName.Trim('/')));
	}
}

/// <summary>
/// Navigation HTML writer for codex builds.
/// </summary>
internal sealed class CodexNavigationHtmlWriter(BuildContext context, CodexNavigation codexNavigation)
	: IsolatedBuildNavigationHtmlWriter(context, codexNavigation);
