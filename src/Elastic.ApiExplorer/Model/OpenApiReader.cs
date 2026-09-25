// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using Elastic.Documentation.Diagnostics;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Elastic.ApiExplorer.Model;

public sealed class OpenApiReader : IOpenApiSpecificationReader
{
	private const string JsonFormat = "json";

	public static OpenApiReader Instance { get; } = new OpenApiReader();

	private OpenApiReader() { }

	private static bool SupportsSpecFileName(string specFileName) =>
		Path.GetExtension(specFileName).ToLowerInvariant() is ".json" or ".yaml" or ".yml";

	private static bool IsJsonFileName(string specFileName) => Path.GetExtension(specFileName).ToLowerInvariant() is ".json";

	public async Task<OpenApiDocument?> ReadAsync(IFileInfo openApiSpecification, IDiagnosticsCollector? collector = null)
	{
		if (!openApiSpecification.Exists)
			return null;

		if (!SupportsSpecFileName(openApiSpecification.Name))
			return null;

		await using var fs = openApiSpecification.OpenRead();
		return await ReadAsync(fs, openApiSpecification.Name, collector);
	}

	/// <summary>
	/// Parses an OpenAPI document from an already-open stream, e.g. one fetched remotely through
	/// <see cref="VersionIndexClient.FetchSpecStreamAsync"/>. Closes <paramref name="stream"/> when done.
	/// </summary>
	/// <remarks>
	/// JSON specs are passed directly to Microsoft.OpenApi. YAML specs (.yaml / .yml) are first parsed
	/// by YamlDotNet and re-serialized to JSON because Microsoft.OpenApi accepts JSON only.
	/// Microsoft.OpenApi auto-detects the spec version from the root <c>openapi</c> / <c>swagger</c>
	/// property, so both OpenAPI 3.x and Swagger 2.0 documents are supported.
	/// </remarks>
	public async Task<OpenApiDocument?> ReadAsync(Stream stream, string specFileName, IDiagnosticsCollector? collector = null)
	{
		if (!SupportsSpecFileName(specFileName))
			return null;

		var settings = new OpenApiReaderSettings { LeaveStreamOpen = false, RuleSet = ValidationRuleSet.GetEmptyRuleSet() };

		ReadResult result;
		bool hasTemplatePlaceholderHost;

		if (IsJsonFileName(specFileName))
		{
			// Buffer so the host value can be inspected before loading.
			var buffered = new MemoryStream();
			await stream.CopyToAsync(buffered).ConfigureAwait(false);
			await stream.DisposeAsync().ConfigureAwait(false);
			hasTemplatePlaceholderHost = HasTemplatePlaceholderHost(buffered);
			buffered.Position = 0;
			result = await OpenApiDocument.LoadAsync(buffered, settings: settings);
		}
		else
		{
			var jsonStream = await ParseYamlToJsonStreamAsync(stream).ConfigureAwait(false);
			hasTemplatePlaceholderHost = HasTemplatePlaceholderHost(jsonStream);
			result = await OpenApiDocument.LoadAsync(jsonStream, JsonFormat, settings: settings);
		}

		if (collector is not null && result.Diagnostic?.Errors is { Count: > 0 } errors)
		{
			foreach (var error in errors)
			{
				// Swagger 2.0 specs (e.g. the ECE API) may use template placeholders such as
				// {{hostname}} in the host field. Microsoft.OpenApi rejects that as an invalid
				// URI host but still returns a complete document. When the spec itself contains
				// a template placeholder host, downgrade the "Invalid host" diagnostic to a
				// warning. Any other invalid-host value remains a hard error.
				if (hasTemplatePlaceholderHost && error.Message.Contains("Invalid host", StringComparison.OrdinalIgnoreCase))
					collector.EmitGlobalWarning(error.Message);
				else
					collector.EmitGlobalError(error.Message);
			}
		}

		return result.Document;
	}

	// Reads the "host" field from a JSON representation of a spec and returns true when the
	// value contains a template placeholder (e.g. {{hostname}}). Position is restored on exit.
	private static bool HasTemplatePlaceholderHost(MemoryStream jsonStream)
	{
		var savedPosition = jsonStream.Position;
		jsonStream.Position = 0;
		try
		{
			using var doc = JsonDocument.Parse(jsonStream);
			return doc.RootElement.TryGetProperty("host", out var hostElement)
				&& hostElement.ValueKind == JsonValueKind.String
				&& hostElement.GetString()?.Contains("{{") == true;
		}
		catch
		{
			return false;
		}
		finally
		{
			jsonStream.Position = savedPosition;
		}
	}

	private static async Task<MemoryStream> ParseYamlToJsonStreamAsync(Stream specStream)
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

		// Only plain (unquoted) scalars carry implicit YAML typing. A quoted scalar is always a string
		// and must not be coerced — "2.0" quoted would otherwise become the JSON number 2, breaking
		// Swagger 2.0 version detection in Microsoft.OpenApi's ParsingContext.
		if (scalar.Style is not ScalarStyle.Plain)
		{
			writer.WriteStringValue(value);
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

		// The IsFinite guard exists for YAML specs that contain Infinity or NaN (e.g. enum regression
		// tests). Those must round-trip as strings to avoid JSON serialization failures downstream.
		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
		{
			writer.WriteNumberValue(number);
			return;
		}

		writer.WriteStringValue(value);
	}
}
