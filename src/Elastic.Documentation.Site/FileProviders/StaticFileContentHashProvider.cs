// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Text.Json;

namespace Elastic.Documentation.Site.FileProviders;

public class StaticFileContentHashProvider(EmbeddedOrPhysicalFileProvider fileProvider)
{
	private const string ManifestFile = "vite-manifest.json";
	private static readonly Dictionary<string, string> NoEntries = [];

	private readonly ConcurrentDictionary<string, string> _contentHashes = [];
	private Manifest? _manifest;

	/// <summary>
	/// Builds the URL for a file under <c>_static</c>. Vite writes the entry bundle as <c>main-[hash].js</c> and its
	/// chunks import that exact name, so the page must reference it verbatim: a <c>?v=</c> query on the entry would
	/// make the browser evaluate the module a second time. Every other file keeps the content hash query.
	/// </summary>
	public string GetUrl(string path, string pathPrefix)
	{
		var file = path.TrimStart('/');
		if (ReadManifest().TryGetValue(file, out var emitted) && emitted != file)
			return $"{pathPrefix}/_static/{emitted}";

		var contentHash = GetContentHash(file);
		var url = $"{pathPrefix}/_static/{file}";
		return string.IsNullOrEmpty(contentHash) ? url : $"{url}?v={contentHash}";
	}

	/// <summary>Maps each Vite entry's logical name (<c>main.js</c>) to the file it emitted (<c>main-[hash].js</c>).</summary>
	public static Dictionary<string, string> ParseManifest(Stream json)
	{
		using var document = JsonDocument.Parse(json);
		var entries = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var chunk in document.RootElement.EnumerateObject())
		{
			var value = chunk.Value;
			if (value.ValueKind != JsonValueKind.Object)
				continue;
			if (!value.TryGetProperty("isEntry", out var isEntry) || !isEntry.GetBoolean())
				continue;
			if (!value.TryGetProperty("name", out var name) || !value.TryGetProperty("file", out var file))
				continue;
			var emitted = file.GetString()!;
			entries[$"{name.GetString()}{Path.GetExtension(emitted)}"] = emitted;
		}
		return entries;
	}

	private string GetContentHash(string path)
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

	// Re-read when the file on disk changes, which is what `npm run watch` does; the embedded copy never changes.
	private Dictionary<string, string> ReadManifest()
	{
		var info = fileProvider.GetFileInfo(ManifestFile);
		if (!info.Exists)
			return NoEntries;
		if (_manifest is { } cached && cached.Stamp == info.LastModified)
			return cached.Entries;

		using var stream = info.CreateReadStream();
		var manifest = new Manifest(info.LastModified, ParseManifest(stream));
		_manifest = manifest;
		return manifest.Entries;
	}

	private sealed record Manifest(DateTimeOffset Stamp, Dictionary<string, string> Entries);
}
