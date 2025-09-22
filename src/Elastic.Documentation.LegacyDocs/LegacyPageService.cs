// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.LegacyDocs;

public class LegacyPageService(ILoggerFactory logFactory) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<LegacyPageService>();
	private BloomFilter? _bloomFilter;
	private const string RootNamespace = "Elastic.Documentation.LegacyDocs";
	private const string FileName = "legacy-pages.bloom.bin";
	private const string ResourceName = $"{RootNamespace}.{FileName}";
	private readonly string _bloomFilterBinaryPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "src", RootNamespace, FileName);

	public bool PathExists(string path)
	{
		_bloomFilter ??= LoadBloomFilter();
		var exists = _bloomFilter.Check(path);
		_logger.LogInformation("Path {Path} {Exists} in bloom filter", path, exists ? "exists" : "not exists");
		return exists;
	}

	private static BloomFilter LoadBloomFilter()
	{
		var assembly = typeof(LegacyPageService).Assembly;
		using var stream = assembly.GetManifestResourceStream(ResourceName) ?? throw new FileNotFoundException(
			$"Embedded resource '{ResourceName}' not found in assembly '{assembly.FullName}'. " +
			"Ensure the Build Action for 'legacy-pages.bloom.bin' is 'Embedded Resource' and the path/name is correct.");
		return BloomFilter.Load(stream);
	}

	public bool GenerateBloomFilterBinary(IPagesProvider pagesProvider)
	{
		var pages = pagesProvider.GetPages();
		var enumerable = pages as string[] ?? pages.ToArray();
		var paths = enumerable.ToHashSet();
		var bloomFilter = BloomFilter.FromCollection(enumerable, 0.001);
		Console.WriteLine(paths.Count);
		bloomFilter.Save(_bloomFilterBinaryPath);
		_logger.LogInformation("Bloom filter generated to {Path}", _bloomFilterBinaryPath);
		return true;
	}
}
