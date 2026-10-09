// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.VectorSizing;

/// <summary>
/// The vector sizing calculator. The server renders the whole form with its defaults; script reads the inputs, runs the
/// calculation and writes the results, so there are no models to fill in.
/// </summary>
public sealed record VectorSizingModel
{
	/// <summary>Prefix for element ids, so two calculators on one page never share one.</summary>
	public string Id { get; init; } = "vector-sizing";
}

public static class VectorSizingCopy
{
	public const string Vectors =
		"The total number of vectors you plan to store in this index, not the number of documents. A single document can produce several vectors, such as one embedding for each product image and one for the description.";

	public const string Dimensions = "The number of values in each vector, set by your embedding model.";

	public const string ElementType =
		"The numeric format used to store each value in a vector. Use float32 in most cases, since Elasticsearch quantizes float vectors by default to reduce memory.";

	public const string IndexStructure =
		"How vectors are indexed, which drives memory use and search speed. HNSW keeps a graph in memory for fast approximate search. Flat compares every vector for exact results, best at small scale. DiskBBQ reads compressed clusters from disk and needs far less memory than HNSW.";

	public const string GraphConnections =
		"How many neighbors each node connects to in the HNSW graph. Higher values improve recall, but increase memory and build time.";

	public const string Quantization =
		"Compresses vectors to reduce memory, at some cost to accuracy. Applies to float and bfloat16 vectors only. Available options depend on your index structure.";

	public const string Replicas =
		"The number of replica copies, not counting the primary. Each replica holds a full copy of the vector data, so replicas multiply storage and memory. The default is 0.";

	public const string VectorsPerCluster =
		"The target number of vectors per cluster. Smaller values create more centroids in RAM and usually improve recall at the cost of performance. The default is 384.";

	public const string OffHeapRam =
		"The percentage of clustered vector data, called posting lists, held in off-heap RAM. Supported values are 0 to 10 percent. Lower values save memory. Higher values raise query throughput and lower latency. This estimate assumes all centroids stay in RAM, since performance drops sharply otherwise.";

	public const string ServerlessNote = "Does not apply to Elastic Cloud Serverless.";

	public const string Disclaimer =
		"These estimates are disk and RAM per copy for self-managed Elasticsearch and Elastic Cloud Hosted. This RAM is for fast search, not the Java heap, and is not a full node size. Actual needs still depend on data shape, indexing settings, and query patterns.";

	public const string DiskToRam =
		"This compares how much the field stores on disk with how much RAM it wants in the filesystem cache for fast search. A high number, typical of DiskBBQ, means most of the index can stay on disk. It is not a node type, not Java heap, and not a serverless capacity number.";
}
