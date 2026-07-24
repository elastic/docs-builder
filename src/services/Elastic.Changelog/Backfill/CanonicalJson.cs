// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;

namespace Elastic.Changelog.Backfill;

/// <summary>
/// Rewrites a JSON document into one agreed-upon form — the "canonical" form — so the
/// same content always produces exactly the same text, no matter how the file was
/// formatted or in what order a producer happened to add keys. Hashing that text (see
/// <see cref="BackfillHash"/>) then gives every document a stable identity.
/// </summary>
/// <remarks>
/// The rules, in plain terms:
/// <list type="bullet">
/// <item>object keys are sorted by ordinal (byte-order) comparison;</item>
/// <item>all insignificant whitespace is removed;</item>
/// <item>Windows (<c>\r\n</c>) and old-Mac (<c>\r</c>) line endings inside strings become <c>\n</c>;</item>
/// <item>object properties whose value is null are dropped — absent and null mean the same thing;</item>
/// <item>array items keep their order (order is part of the meaning) and null items are kept;</item>
/// <item>numbers keep the exact text they were written with (all contract numbers are integers).</item>
/// </list>
/// Dictionaries in the contracts serialize as JSON objects, so the key sorting here is what
/// makes dictionary insertion order irrelevant to a document's hash.
/// </remarks>
public static class CanonicalJson
{
	/// <summary>
	/// Returns the canonical form of <paramref name="json"/>. Throws
	/// <see cref="BackfillDocumentException"/> when the text is not valid JSON or an
	/// object contains the same key twice (a duplicate key would make the content ambiguous).
	/// </summary>
	public static string Canonicalize(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		JsonDocument document;
		try
		{
			document = JsonDocument.Parse(json);
		}
		catch (JsonException e)
		{
			throw new BackfillDocumentException($"Cannot canonicalize: the text is not valid JSON. {e.Message}", e);
		}

		using (document)
		{
			using var stream = new MemoryStream();
			using (var writer = new Utf8JsonWriter(stream))
				WriteCanonical(writer, document.RootElement);
			return Encoding.UTF8.GetString(stream.ToArray());
		}
	}

	private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				WriteCanonicalObject(writer, element);
				break;
			case JsonValueKind.Array:
				writer.WriteStartArray();
				foreach (var item in element.EnumerateArray())
					WriteCanonical(writer, item);
				writer.WriteEndArray();
				break;
			case JsonValueKind.String:
				writer.WriteStringValue(NormalizeLineEndings(element.GetString()!));
				break;
			case JsonValueKind.Number:
				// Written verbatim: re-parsing a number could change its text (e.g. trailing zeros).
				writer.WriteRawValue(element.GetRawText());
				break;
			case JsonValueKind.True:
				writer.WriteBooleanValue(true);
				break;
			case JsonValueKind.False:
				writer.WriteBooleanValue(false);
				break;
			case JsonValueKind.Null:
				writer.WriteNullValue();
				break;
			default:
				throw new BackfillDocumentException($"Cannot canonicalize: unexpected JSON value kind '{element.ValueKind}'.");
		}
	}

	private static void WriteCanonicalObject(Utf8JsonWriter writer, JsonElement element)
	{
		var properties = element.EnumerateObject()
			.Where(p => p.Value.ValueKind != JsonValueKind.Null)
			.OrderBy(p => p.Name, StringComparer.Ordinal)
			.ToList();

		writer.WriteStartObject();
		string? previousName = null;
		foreach (var property in properties)
		{
			if (string.Equals(previousName, property.Name, StringComparison.Ordinal))
				throw new BackfillDocumentException($"Cannot canonicalize: the key '{property.Name}' appears more than once in the same object, which makes the content ambiguous.");

			previousName = property.Name;
			writer.WritePropertyName(NormalizeLineEndings(property.Name));
			WriteCanonical(writer, property.Value);
		}
		writer.WriteEndObject();
	}

	private static string NormalizeLineEndings(string value)
	{
		if (!value.Contains('\r', StringComparison.Ordinal))
			return value;

		return value
			.Replace("\r\n", "\n", StringComparison.Ordinal)
			.Replace("\r", "\n", StringComparison.Ordinal);
	}
}
