// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using YamlDotNet.RepresentationModel;

namespace Elastic.ApiExplorer.Model;

public sealed class OpenApiReader : IOpenApiSpecificationReader
{
	private const string JsonFormat = "json";

	public static OpenApiReader Instance { get; } = new OpenApiReader();

	private OpenApiReader() { }

	private static bool SupportsSpecFileName(string specFileName) =>
		Path.GetExtension(specFileName).ToLowerInvariant() is ".json" or ".yaml" or ".yml";

	public async Task<OpenApiDocument?> ReadAsync(IFileInfo openApiSpecification)
	{
		if (!openApiSpecification.Exists)
			return null;

		if (!SupportsSpecFileName(openApiSpecification.Name))
			return null;

		await using var fs = openApiSpecification.OpenRead();
		return await ReadAsync(fs, openApiSpecification.Name);
	}

	/// <summary>
	/// Parses an OpenAPI document from an already-open stream, e.g. one fetched remotely through
	/// <see cref="VersionIndexClient.FetchSpecStreamAsync"/>. Closes <paramref name="stream"/> when done.
	/// </summary>
	/// <remarks>
	/// All supported spec formats (.json, .yaml, .yml) are parsed with YamlDotNet. JSON is a subset of
	/// YAML 1.2, so one parser covers both. Microsoft.OpenApi accepts JSON input only, so the parsed
	/// tree is serialized to JSON before <see cref="OpenApiDocument.LoadAsync"/> runs.
	/// </remarks>
	public async Task<OpenApiDocument?> ReadAsync(Stream stream, string specFileName)
	{
		if (!SupportsSpecFileName(specFileName))
			return null;

		await using var jsonStream = await ParseSpecToJsonStreamAsync(stream).ConfigureAwait(false);

		var settings = new OpenApiReaderSettings { LeaveStreamOpen = false, RuleSet = ValidationRuleSet.GetEmptyRuleSet() };
		var openApiDocument = await OpenApiDocument.LoadAsync(jsonStream, JsonFormat, settings: settings);
		return openApiDocument.Document;
	}

	private static async Task<MemoryStream> ParseSpecToJsonStreamAsync(Stream specStream)
	{
		using var reader = new StreamReader(specStream, leaveOpen: false);
		var yaml = new YamlStream();
		yaml.Load(reader);

		var root = yaml.Documents[0].RootNode ?? throw new InvalidOperationException("OpenAPI spec document is empty.");

		var jsonStream = new MemoryStream();
		await using (var jsonWriter = new Utf8JsonWriter(jsonStream))
			WriteYamlNode(jsonWriter, root);

		jsonStream.Position = 0;
		return jsonStream;
	}

	private static void WriteYamlNode(Utf8JsonWriter writer, YamlNode node)
	{
		switch (node)
		{
			case YamlScalarNode scalar:
				WriteScalar(writer, scalar);
				break;
			case YamlSequenceNode sequence:
				writer.WriteStartArray();
				foreach (var child in sequence.Children)
					WriteYamlNode(writer, child);
				writer.WriteEndArray();
				break;
			case YamlMappingNode mapping:
				writer.WriteStartObject();
				foreach (var (keyNode, valueNode) in mapping.Children)
				{
					writer.WritePropertyName(keyNode.ToString());
					WriteYamlNode(writer, valueNode);
				}
				writer.WriteEndObject();
				break;
			default:
				writer.WriteNullValue();
				break;
		}
	}

	private static void WriteScalar(Utf8JsonWriter writer, YamlScalarNode scalar)
	{
		var value = scalar.Value;
		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		if (bool.TryParse(value, out var boolean))
		{
			writer.WriteBooleanValue(boolean);
			return;
		}

		if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
		{
			writer.WriteNumberValue(integer);
			return;
		}

		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
		{
			writer.WriteNumberValue(number);
			return;
		}

		writer.WriteStringValue(value);
	}
}
