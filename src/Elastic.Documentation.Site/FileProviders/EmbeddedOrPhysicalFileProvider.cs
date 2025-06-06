// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Elastic.Documentation.Site.FileProviders;

public sealed class EmbeddedOrPhysicalFileProvider : IFileProvider, IDisposable
{
	private readonly EmbeddedFileProvider _embeddedProvider = new(typeof(IDocumentationContext).Assembly, "Elastic.Documentation.Site._static");
	private readonly PhysicalFileProvider? _staticFilesInDocsFolder;

	private readonly PhysicalFileProvider? _staticWebFilesDuringDebug;

	public EmbeddedOrPhysicalFileProvider(IDocumentationContext context)
	{
		var documentationStaticFiles = Path.Combine(context.DocumentationSourceDirectory.FullName, "_static");
#if DEBUG
		// this attempts to serve files directly from their source rather than the embedded resources during development.
		// this allows us to change js/css files without restarting the webserver
		var solutionRoot = Paths.GetSolutionDirectory();
		if (solutionRoot != null)
		{

			var debugWebFiles = Path.Combine(solutionRoot.FullName, "src", "Elastic.Documentation.Site", "_static");
			_staticWebFilesDuringDebug = new PhysicalFileProvider(debugWebFiles);
		}
#else
		_staticWebFilesDuringDebug = null;
#endif
		if (context.ReadFileSystem.Directory.Exists(documentationStaticFiles))
			_staticFilesInDocsFolder = new PhysicalFileProvider(documentationStaticFiles);
	}

	private T? FirstYielding<T>(string arg, Func<string, PhysicalFileProvider, T?> predicate) =>
		Yield(arg, predicate, _staticWebFilesDuringDebug) ?? Yield(arg, predicate, _staticFilesInDocsFolder);

	private static T? Yield<T>(string arg, Func<string, PhysicalFileProvider, T?> predicate, PhysicalFileProvider? provider)
	{
		if (provider is null)
			return default;
		var result = predicate(arg, provider);
		return result ?? default;
	}

	public IDirectoryContents GetDirectoryContents(string subpath)
	{
		var contents = FirstYielding(subpath, static (a, p) => p.GetDirectoryContents(a));
		if (contents is null || !contents.Exists)
			contents = _embeddedProvider.GetDirectoryContents(subpath);
		return contents;
	}

	public IFileInfo GetFileInfo(string subpath)
	{
		var path = subpath.Replace($"{Path.DirectorySeparatorChar}_static", "");
		var fileInfo = FirstYielding(path, static (a, p) => p.GetFileInfo(a));
		if (fileInfo is null || !fileInfo.Exists)
			fileInfo = _embeddedProvider.GetFileInfo(subpath);
		return fileInfo;
	}

	public IChangeToken Watch(string filter)
	{
		var changeToken = FirstYielding(filter, static (f, p) => p.Watch(f));
		if (changeToken is null or NullChangeToken)
			changeToken = _embeddedProvider.Watch(filter);
		return changeToken;
	}

	public void Dispose()
	{
		_staticFilesInDocsFolder?.Dispose();
		_staticWebFilesDuringDebug?.Dispose();
	}
}
