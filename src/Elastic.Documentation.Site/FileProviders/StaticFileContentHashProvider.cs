// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;

namespace Elastic.Documentation.Site.FileProviders;

public class StaticFileContentHashProvider(EmbeddedOrPhysicalFileProvider fileProvider)
{
	private readonly ConcurrentDictionary<string, (string Hash, DateTimeOffset LastModified)> _contentHashes = [];

	public string GetContentHash(string path)
	{
		var fileInfo = fileProvider.GetFileInfo(path);

		if (!fileInfo.Exists)
			return string.Empty;

		if (_contentHashes.TryGetValue(path, out var cached) && cached.LastModified == fileInfo.LastModified)
			return cached.Hash;

		using var stream = fileInfo.CreateReadStream();
		using var sha = System.Security.Cryptography.SHA256.Create();
		var fullHash = sha.ComputeHash(stream);
		var hash = Convert.ToHexString(fullHash).ToLowerInvariant()[..16];
		_contentHashes[path] = (hash, fileInfo.LastModified);
		return hash;
	}
}
