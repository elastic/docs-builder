// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
namespace Elastic.Markdown.IO;

public class ContentHashProvider(EmbeddedOrPhysicalFileProvider fileProvider)
{
	private readonly Dictionary<string, string> _contentHashes = [];

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
}
