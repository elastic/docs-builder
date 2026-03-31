// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Integrations.S3;

public interface IS3EtagCalculator
{
	Task<string> CalculateS3ETag(string filePath, Cancel ctx = default);
}

public class S3EtagCalculator(ILoggerFactory logFactory, IFileSystem readFileSystem) : IS3EtagCalculator
{
	private readonly ILogger _logger = logFactory.CreateLogger<S3EtagCalculator>();

	private static readonly ConcurrentDictionary<string, string> EtagCache = new();

	public const long PartSize = 5 * 1024 * 1024; // 5MB — matches TransferUtility default

	[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
	public async Task<string> CalculateS3ETag(string filePath, Cancel ctx = default)
	{
		if (EtagCache.TryGetValue(filePath, out var cachedEtag))
		{
			_logger.LogDebug("Using cached ETag for {Path}", filePath);
			return cachedEtag;
		}

		var fileInfo = readFileSystem.FileInfo.New(filePath);
		var fileSize = fileInfo.Length;

		if (fileSize <= PartSize)
		{
			await using var stream = readFileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var smallBuffer = new byte[fileSize];
			var bytesRead = await stream.ReadAsync(smallBuffer.AsMemory(0, (int)fileSize), ctx);
			var hash = MD5.HashData(smallBuffer.AsSpan(0, bytesRead));
			var etag = Convert.ToHexStringLower(hash);
			EtagCache[filePath] = etag;
			return etag;
		}

		var parts = (int)Math.Ceiling((double)fileSize / PartSize);
		await using var fileStream = readFileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		var partBuffer = new byte[PartSize];
		var partHashes = new List<byte[]>();

		for (var i = 0; i < parts; i++)
		{
			var bytesRead = await fileStream.ReadAsync(partBuffer.AsMemory(0, partBuffer.Length), ctx);
			var partHash = MD5.HashData(partBuffer.AsSpan(0, bytesRead));
			partHashes.Add(partHash);
		}

		var concatenatedHashes = partHashes.SelectMany(h => h).ToArray();
		var finalHash = MD5.HashData(concatenatedHashes);
		var multipartEtag = $"{Convert.ToHexStringLower(finalHash)}-{parts}";
		EtagCache[filePath] = multipartEtag;
		return multipartEtag;
	}
}
