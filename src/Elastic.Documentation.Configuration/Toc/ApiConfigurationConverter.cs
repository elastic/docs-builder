// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using static YamlDotNet.Core.ParserExtensions;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// YAML converter that provides backward compatibility for API configuration.
/// Supports legacy string/object formats and new sequence format.
/// 
/// Legacy string format: api: { elasticsearch: "elasticsearch-openapi.json" }
/// Legacy object format: api: { elasticsearch: { spec: "elasticsearch-openapi.json" } }
/// New sequence format: api: { kibana: [{ file: "intro.md" }, { spec: "kibana-openapi.json" }, { file: "outro.md" }] }
/// </summary>
public class ApiConfigurationConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(ApiConfiguration) || type == typeof(ApiProductSequence);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (type == typeof(ApiProductSequence))
		{
			return ReadApiProductSequence(parser, rootDeserializer);
		}

		// Legacy ApiConfiguration handling
		if (parser.Current is Scalar scalar)
		{
			// Handle old string format: "elasticsearch-openapi.json"
			_ = parser.MoveNext();
			return new ApiConfiguration { Spec = scalar.Value };
		}

		if (parser.Current is MappingStart)
		{
			// Handle legacy object format: { spec: "...", template: "...", specs: [...] }
			_ = parser.MoveNext();
			var config = new ApiConfiguration();

			while (parser.Current is not MappingEnd)
			{
				var key = parser.Consume<Scalar>();
				switch (key.Value)
				{
					case "spec":
						if (parser.Current is Scalar specValue)
						{
							config.Spec = specValue.Value;
							_ = parser.MoveNext();
						}
						else
						{
							// Wrong token type - skip safely
							parser.SkipThisAndNestedEvents();
						}
						break;
					case "template":
						// Legacy template support - skip for ApiConfiguration parsing
						parser.SkipThisAndNestedEvents();
						break;
					default:
						// Safely consume unknown values (including nested mappings/sequences)
						parser.SkipThisAndNestedEvents();
						break;
				}
			}

			_ = parser.MoveNext(); // consume MappingEnd
			return config;
		}

		throw new YamlException(parser.Current?.Start ?? Mark.Empty, parser.Current?.End ?? Mark.Empty,
			"API configuration must be either a string (spec path) or an object with spec field");
	}

	private ApiProductSequence ReadApiProductSequence(IParser parser, ObjectDeserializer rootDeserializer)
	{
		if (parser.Current is Scalar scalar)
		{
			// Convert legacy string format to sequence: "elasticsearch-openapi.json"
			_ = parser.MoveNext();
			return new ApiProductSequence
			{
				Entries = [new ApiProductEntry { Spec = scalar.Value }]
			};
		}

		if (parser.Current is MappingStart)
		{
			// Convert legacy object format to sequence: { spec: "..." }
			_ = parser.MoveNext();
			var entries = new List<ApiProductEntry>();
			string? specPath = null;

			while (parser.Current is not MappingEnd)
			{
				var key = parser.Consume<Scalar>();
				switch (key.Value)
				{
					case "spec":
						if (parser.Current is Scalar specValue)
						{
							specPath = specValue.Value;
							_ = parser.MoveNext();
						}
						else
						{
							parser.SkipThisAndNestedEvents();
						}
						break;
					case "template":
						// Legacy template support removed - skip entirely
						parser.SkipThisAndNestedEvents();
						break;
					default:
						parser.SkipThisAndNestedEvents();
						break;
				}
			}

			_ = parser.MoveNext(); // consume MappingEnd

			// Build sequence with spec only
			if (!string.IsNullOrWhiteSpace(specPath))
			{
				entries.Add(new ApiProductEntry { Spec = specPath });
			}

			return new ApiProductSequence { Entries = entries };
		}

		if (parser.Current is SequenceStart)
		{
			// Handle new sequence format: [{ file: "intro.md" }, { spec: "kibana-openapi.json" }, { file: "outro.md" }]
			_ = parser.MoveNext(); // consume SequenceStart
			var entries = new List<ApiProductEntry>();

			while (parser.Current is not SequenceEnd)
			{
				if (parser.Current is MappingStart)
				{
					var entry = (ApiProductEntry)rootDeserializer(typeof(ApiProductEntry))!;
					entries.Add(entry);
				}
				else
				{
					// Skip unexpected tokens
					parser.SkipThisAndNestedEvents();
				}
			}

			_ = parser.MoveNext(); // consume SequenceEnd
			return new ApiProductSequence { Entries = entries };
		}

		throw new YamlException(parser.Current?.Start ?? Mark.Empty, parser.Current?.End ?? Mark.Empty,
			"API configuration must be either a string (spec path), an object with spec field, or a sequence of file/spec entries");
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value is ApiConfiguration config)
		{
			// Always write as object format for consistency
			serializer(config, typeof(ApiConfiguration));
		}
		else if (value is ApiProductSequence sequence)
		{
			// Write as sequence format
			serializer(sequence, typeof(ApiProductSequence));
		}
	}
}
