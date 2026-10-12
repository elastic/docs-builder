// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;

namespace Elastic.Documentation.Site.FileProviders;

public class StaticFileContentHashProvider(EmbeddedOrPhysicalFileProvider fileProvider)
{
	private readonly ConcurrentDictionary<string, string> _contentHashes = [];
	private readonly ConcurrentDictionary<string, (string Path, long ResolvedAt)> _resolvedPaths = [];

	public string GetContentHash(string path)
	{
		if (_contentHashes.TryGetValue(path, out var contentHash))
			return contentHash;

		var fileInfo = fileProvider.GetFileInfo(path);

		if (!fileInfo.Exists)
			return string.Empty;

		using var stream = fileInfo.CreateReadStream();
		using var sha = System.Security.Cryptography.SHA256.Create();
		var fullHash = sha.ComputeHash(stream);
		_contentHashes[path] = Convert.ToHexString(fullHash).ToLowerInvariant()[..16];
		return _contentHashes[path];
	}

	/// <summary>
	/// Builds the URL for a file under <c>_static</c>. Vite writes the entry bundle as <c>main-[hash].js</c> and its
	/// chunks import that exact name, so the page must reference it verbatim: a <c>?v=</c> query on the entry would
	/// make the browser evaluate the module a second time. Every other file keeps the content hash query.
	/// </summary>
	public string GetUrl(string path, string pathPrefix)
	{
		var file = path.TrimStart('/');
		var resolved = ResolvePath(file);
		var url = $"{pathPrefix}/_static/{resolved}";
		if (resolved != file)
			return url;

		var contentHash = GetContentHash(file);
		return string.IsNullOrEmpty(contentHash) ? url : $"{url}?v={contentHash}";
	}

	// `npm run watch` writes a new hashed entry on every change, so the name is re-checked after a second
	// instead of cached for good. A directory listing once a second costs nothing in a build.
	private string ResolvePath(string path)
	{
		var now = Environment.TickCount64;
		if (_resolvedPaths.TryGetValue(path, out var cached) && now - cached.ResolvedAt < 1000)
			return cached.Path;

		var resolved = FindHashedFile(path);
		_resolvedPaths[path] = (resolved, now);
		return resolved;
	}

	// The newest match wins: `vite build --watch` leaves the entries of earlier builds behind.
	private string FindHashedFile(string path)
	{
		if (fileProvider.GetFileInfo(path).Exists)
			return path;

		var directory = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty;
		var prefix = Path.GetFileNameWithoutExtension(path) + "-";
		var extension = Path.GetExtension(path);
		var match = fileProvider
			.GetDirectoryContents(directory)
			.Where(
				f => !f.IsDirectory && f.Name.StartsWith(prefix, StringComparison.Ordinal) && f.Name.EndsWith(
					extension,
					StringComparison.Ordinal
				)
			)
			.MaxBy(f => f.LastModified);
		if (match is null)
			return path;
		return directory.Length == 0 ? match.Name : $"{directory}/{match.Name}";
	}
}
