// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Compression;
using System.Text;

namespace Elastic.Markdown.Myst.Directives.Diagram;

/// <summary>
/// Utility class for encoding diagrams for use with Kroki service
/// </summary>
public static class DiagramEncoder
{
	/// <summary>
	/// Supported diagram types for Kroki service
	/// </summary>
	private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"mermaid", "d2", "graphviz", "plantuml", "ditaa", "erd", "excalidraw",
		"nomnoml", "pikchr", "structurizr", "svgbob", "vega", "vegalite", "wavedrom"
	};

	/// <summary>
	/// Generates a Kroki URL for the given diagram type and content
	/// </summary>
	/// <param name="diagramType">The type of diagram (e.g., "mermaid", "d2")</param>
	/// <param name="content">The diagram content</param>
	/// <returns>The complete Kroki URL for rendering the diagram as SVG</returns>
	/// <exception cref="ArgumentException">Thrown when diagram type is not supported</exception>
	/// <exception cref="ArgumentNullException">Thrown when content is null or empty</exception>
	public static string GenerateKrokiUrl(string diagramType, string content)
	{
		if (string.IsNullOrWhiteSpace(diagramType))
			throw new ArgumentException("Diagram type cannot be null or empty", nameof(diagramType));

		if (string.IsNullOrWhiteSpace(content))
			throw new ArgumentException("Diagram content cannot be null or empty", nameof(content));

		var normalizedType = diagramType.ToLowerInvariant();
		if (!SupportedTypes.Contains(normalizedType))
			throw new ArgumentException($"Unsupported diagram type: {diagramType}. Supported types: {string.Join(", ", SupportedTypes)}", nameof(diagramType));

		var compressedBytes = Deflate(Encoding.UTF8.GetBytes(content));
		var encodedOutput = EncodeBase64Url(compressedBytes);

		return $"https://kroki.io/{normalizedType}/svg/{encodedOutput}";
	}

	/// <summary>
	/// Compresses data using Deflate compression with zlib headers
	/// </summary>
	/// <param name="data">The data to compress</param>
	/// <param name="level">The compression level</param>
	/// <returns>The compressed data with zlib headers</returns>
	private static byte[] Deflate(byte[] data, CompressionLevel? level = null)
	{
		using var memStream = new MemoryStream();

#if NET6_0_OR_GREATER
		using (var zlibStream = level.HasValue ? new ZLibStream(memStream, level.Value, true) : new ZLibStream(memStream, CompressionMode.Compress, true))
			zlibStream.Write(data);
#else
		// Reference: https://yal.cc/cs-deflatestream-zlib/#code

		// write header:
		memStream.WriteByte(0x78);
		memStream.WriteByte(level switch
		{
			CompressionLevel.NoCompression or CompressionLevel.Fastest => 0x01,
			CompressionLevel.Optimal => 0x0A,
			_ => 0x9C,
		});

		// write compressed data (with Deflate headers):
		using (var dflStream = level.HasValue ? new DeflateStream(memStream, level.Value, true) : new DeflateStream(memStream, CompressionMode.Compress, true))
		{
			dflStream.Write(data, 0, data.Length);
		}

		// compute Adler-32:
		uint a1 = 1, a2 = 0;
		foreach (byte b in data)
		{
			a1 = (a1 + b) % 65521;
			a2 = (a2 + a1) % 65521;
		}

		memStream.WriteByte((byte)(a2 >> 8));
		memStream.WriteByte((byte)a2);
		memStream.WriteByte((byte)(a1 >> 8));
		memStream.WriteByte((byte)a1);
#endif

		return memStream.ToArray();
	}

	/// <summary>
	/// Encodes bytes to Base64URL format
	/// </summary>
	/// <param name="bytes">The bytes to encode</param>
	/// <returns>The Base64URL encoded string</returns>
	private static string EncodeBase64Url(byte[] bytes) =>
#if NET9_0_OR_GREATER
		// You can use this in previous version of .NET with Microsoft.Bcl.Memory package
		System.Buffers.Text.Base64Url.EncodeToString(bytes);
#else
		Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');
#endif
}
