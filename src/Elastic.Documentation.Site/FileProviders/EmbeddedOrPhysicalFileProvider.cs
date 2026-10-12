// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Elastic.Documentation.Site.FileProviders;

public sealed class EmbeddedOrPhysicalFileProvider : IFileProvider, IDisposable
{
	private readonly EmbeddedFileProvider _embeddedProvider = new(
		typeof(EmbeddedOrPhysicalFileProvider).Assembly,
		"Elastic.Documentation.Site._static"
	);
	private readonly PhysicalFileProvider? _staticFilesInDocsFolder;

	private readonly PhysicalFileProvider? _staticWebFilesDuringDebug;

	// Most specific first: the checkout during debug, then the doc set's own `_static`, then the embedded resources.
	// The first provider that has a file wins; one that only answers "not found" does not shadow the next.
	private readonly CompositeFileProvider _providers;

	public EmbeddedOrPhysicalFileProvider(IDocumentationSetContext context)
	{
		var documentationStaticFiles = Path.Join(context.DocumentationSourceDirectory.FullName, "_static");
#if DEBUG
		// this attempts to serve files directly from their source rather than the embedded resources during development.
		// this allows us to change js/css files without restarting the webserver
		var solutionRoot = Paths.GetSolutionDirectory();
		if (solutionRoot != null)
		{

			var debugWebFiles = Path.Join(solutionRoot.FullName, "src", "Elastic.Documentation.Site", "_static");
			// No exclusions: the default hides dot-prefixed paths, and Vite writes `.vite/manifest.json`.
			_staticWebFilesDuringDebug = new PhysicalFileProvider(debugWebFiles, ExclusionFilters.None);
		}
#else
		_staticWebFilesDuringDebug = null;
#endif
		if (context.ReadFileSystem.Directory.Exists(documentationStaticFiles))
			_staticFilesInDocsFolder = new PhysicalFileProvider(documentationStaticFiles);

		IFileProvider?[] ordered = [_staticWebFilesDuringDebug, _staticFilesInDocsFolder, _embeddedProvider];
		_providers = new CompositeFileProvider(ordered.OfType<IFileProvider>());
	}

	public IDirectoryContents GetDirectoryContents(string subpath) => _providers.GetDirectoryContents(subpath.TrimStart('/'));

	public IFileInfo GetFileInfo(string subpath)
	{
		var path = subpath.Replace($"{Path.DirectorySeparatorChar}_static", "").TrimStart('/');
		return _providers.GetFileInfo(path);
	}

	public IChangeToken Watch(string filter) => _providers.Watch(filter.TrimStart('/'));

	public void Dispose()
	{
		_staticFilesInDocsFolder?.Dispose();
		_staticWebFilesDuringDebug?.Dispose();
	}
}
