// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;

namespace Elastic.Documentation.LegacyDocs;

public class LegacyPageChecker
{
	private BloomFilter? _bloomFilter;
	private const string RootNamespace = "Elastic.Documentation.LegacyDocs";
	private const string FileName = "legacy-pages.bloom.bin";
	private const string ResourceName = $"{RootNamespace}.{FileName}";
	private readonly string _bloomFilterBinaryPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "src", RootNamespace, FileName);


	public bool PathExists(string path)
	{
		_bloomFilter ??= LoadBloomFilter();
		return _bloomFilter.Check(path);
	}

	private static BloomFilter LoadBloomFilter()
	{
		var assembly = typeof(LegacyPageChecker).Assembly;
		using var stream = assembly.GetManifestResourceStream(ResourceName) ?? throw new FileNotFoundException(
			$"Embedded resource '{ResourceName}' not found in assembly '{assembly.FullName}'. " +
			"Ensure the Build Action for 'legacy-pages.bloom.bin' is 'Embedded Resource' and the path/name is correct.");
		return BloomFilter.Load(stream);
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
