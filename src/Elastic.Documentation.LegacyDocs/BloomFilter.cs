// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace Elastic.Documentation.LegacyDocs;

internal sealed class BloomFilter
{
	/// <summary>
	/// The bit array for the filter.
	/// </summary>
	private readonly BitArray _bitArray;

	/// <summary>
	/// The size of the bit array.
	/// </summary>
	private int Size => _bitArray.Length;

	/// <summary>
	/// The number of hash functions used.
	/// </summary>
	private int HashCount { get; }

	/// <summary>
	/// Private constructor to be used by factory methods.
	/// </summary>
	private BloomFilter(int size, int hashCount)
	{
		if (size <= 0)
			throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");
		if (hashCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(hashCount), "Hash count must be greater than zero.");

		_bitArray = new BitArray(size);
		HashCount = hashCount;
	}

	/// <summary>
	/// Initializes a new BloomFilter with optimal parameters based on expected items and false positive probability.
	/// </summary>
	/// <param name="expectedItems">The expected number of items to be stored.</param>
	/// <param name="falsePositiveProbability">The desired false positive probability (e.g., 0.01 for 1%).</param>
	private BloomFilter(int expectedItems, double falsePositiveProbability)
	{
		if (expectedItems <= 0)
			throw new ArgumentOutOfRangeException(nameof(expectedItems), "Expected items must be greater than zero.");
		if (falsePositiveProbability is <= 0.0 or >= 1.0)
			throw new ArgumentOutOfRangeException(nameof(falsePositiveProbability), "False positive probability must be between 0 and 1.");

		var size = GetOptimalSize(expectedItems, falsePositiveProbability);
		var hashCount = GetOptimalHashCount(size, expectedItems);

		_bitArray = new BitArray(size);
		HashCount = hashCount;
	}

	/// <summary>
	/// Adds an item to the Bloom Filter.
	/// </summary>
	/// <param name="item">The item to add. The string will be UTF-8 encoded for hashing.</param>
	private void Add(string item)
	{
		var itemBytes = Encoding.UTF8.GetBytes(item);
		for (var i = 0; i < HashCount; i++)
		{
			var hash = GetHash(itemBytes, i);
			_bitArray[hash] = true;
		}
	}

	/// <summary>
	/// Checks if an item is possibly in the set.
	/// </summary>
	/// <param name="item">The item to check.</param>
	/// <returns>False if the item is definitely not in the set, True if it might be.</returns>
	public bool Check(string item)
	{
		var itemBytes = Encoding.UTF8.GetBytes(item.Replace("/guide/en/", ""));
		for (var i = 0; i < HashCount; i++)
		{
			var hash = GetHash(itemBytes, i);
			if (!_bitArray[hash])
				return false;
		}
		return true;
	}

	/// <summary>
	/// Hashes the input data using SHA256 with a given seed.
	/// </summary>
	private int GetHash(byte[] data, int seed)
	{
		var seedBytes = BitConverter.GetBytes(seed);
		var combinedBytes = new byte[data.Length + seedBytes.Length];
		Buffer.BlockCopy(data, 0, combinedBytes, 0, data.Length);
		Buffer.BlockCopy(seedBytes, 0, combinedBytes, data.Length, seedBytes.Length);
		var hashBytes = SHA256.HashData(combinedBytes);
		var hashInt = BitConverter.ToInt32(hashBytes, 0);
		return Math.Abs(hashInt % _bitArray.Length);
	}

	/// <summary>
	/// Creates a new BloomFilter from a collection of items.
	/// </summary>
	/// <param name="items">The collection of string items to add.</param>
	/// <param name="falsePositiveProbability">The desired false positive probability.</param>
	/// <returns>A new BloomFilter instance populated with the items.</returns>
	public static BloomFilter FromCollection(IEnumerable<string> items, double falsePositiveProbability)
	{
		var itemList = new List<string>(items);
		var filter = new BloomFilter(itemList.Count, falsePositiveProbability);
		foreach (var item in itemList)
		{
			filter.Add(item);
		}

		return filter;
	}

	// --- Persistence Methods ---

	/// <summary>
	/// Saves the Bloom Filter's state to a binary file.
	/// The format is: [4-byte Size int][4-byte HashCount int][bit array bytes]
	/// </summary>
	/// <param name="filePath">The path to the file.</param>
	public void Save(string filePath)
	{
		using var stream = File.Open(filePath, FileMode.Create);
		using var writer = new BinaryWriter(stream);
		// 1. Write the Size and HashCount as integers
		writer.Write(Size);
		writer.Write(HashCount);

		// 2. Write the bit array
		var bitArrayBytes = new byte[(Size + 7) / 8];
		_bitArray.CopyTo(bitArrayBytes, 0);
		writer.Write(bitArrayBytes);
	}

	/// <summary>
	/// Loads a Bloom Filter from a binary file.
	/// </summary>
	/// <param name="filePath">The path to the file containing the filter data.</param>
	/// <returns>A new BloomFilter instance.</returns>
	public static BloomFilter Load(string filePath)
	{
		using var stream = File.OpenRead(filePath);
		using var reader = new BinaryReader(stream);
		// 1. Read metadata (Size and HashCount)
		var size = reader.ReadInt32();
		var hashCount = reader.ReadInt32();

		// 2. Create a new filter with the loaded parameters
		var filter = new BloomFilter(size, hashCount);

		// 3. Read the bit array data
		var byteCount = (size + 7) / 8;
		var bitArrayBytes = reader.ReadBytes(byteCount);

		// Re-initialize the internal BitArray with the loaded data
		for (var i = 0; i < size; i++)
		{
			if ((bitArrayBytes[i / 8] & (1 << (i % 8))) != 0)
			{
				filter._bitArray[i] = true;
			}
		}

		return filter;
	}


	// --- Optimal Parameter Calculation ---

	/// <summary>
	/// Calculates the optimal size of the bit array (m).
	/// Formula: m = - (n * log(p)) / (log(2)^2)
	/// </summary>
	private static int GetOptimalSize(int n, double p) => (int)Math.Ceiling(-1 * (n * Math.Log(p)) / Math.Pow(Math.Log(2), 2));

	/// <summary>
	/// Calculates the optimal number of hash functions (k).
	/// Formula: k = (m/n) * log(2)
	/// </summary>
	private static int GetOptimalHashCount(int m, int n) => (int)Math.Ceiling((double)m / n * Math.Log(2));
}
