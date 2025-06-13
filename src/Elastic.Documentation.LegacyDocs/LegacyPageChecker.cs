// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;

namespace Elastic.Documentation.LegacyDocs;

public class LegacyPageChecker(IFileSystem fs)
{
	private BloomFilter? _bloomFilter;
	private readonly string _bloomFilterBinaryPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "src", "Elastic.Documentation.LegacyDocs", "legacy-pages.bloom.bin");


	public bool PathExists(string path)
	{
		_bloomFilter ??= LoadBloomFilter();
		return _bloomFilter.Check(path);
	}

	private BloomFilter LoadBloomFilter()
	{
		var bloomFilterBinaryInfo = fs.FileInfo.New(_bloomFilterBinaryPath);
		_bloomFilter ??= BloomFilter.Load(bloomFilterBinaryInfo.FullName);
		return _bloomFilter;
	}

	public void GenerateBloomFilterBinary(IPagesProvider pagesProvider)
	{
		var pages = pagesProvider.GetPages();
		var enumerable = pages as string[] ?? pages.ToArray();
		var paths = enumerable.ToHashSet();
		var bloomFilter = BloomFilter.FromCollection(enumerable, 0.001);
		Console.WriteLine(paths.Count);
		bloomFilter.Save(_bloomFilterBinaryPath);
	}
}
