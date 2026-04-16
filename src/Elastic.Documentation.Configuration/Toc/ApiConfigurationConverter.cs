// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// YAML converter that provides backward compatibility for API configuration.
/// Supports both old string format and new object format.
/// 
/// Old format: api: { elasticsearch: "elasticsearch-openapi.json" }
/// New format: api: { elasticsearch: { spec: "elasticsearch-openapi.json", template: "elasticsearch-api-overview.md" } }
/// </summary>
public class ApiConfigurationConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(ApiConfiguration);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.Current is Scalar scalar)
		{
			// Handle old string format: "elasticsearch-openapi.json"
			_ = parser.MoveNext();
			return new ApiConfiguration { Spec = scalar.Value };
		}

		if (parser.Current is MappingStart)
		{
			// Handle new object format: { spec: "...", template: "...", specs: [...] }
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
						break;
					case "template":
						if (parser.Current is Scalar templateValue)
						{
							config.Template = templateValue.Value;
							_ = parser.MoveNext();
						}
						break;
					case "specs":
						if (parser.Current is SequenceStart)
						{
							_ = parser.MoveNext();
							var specs = new List<string>();
							while (parser.Current is not SequenceEnd)
							{
								if (parser.Current is Scalar specItem)
								{
									specs.Add(specItem.Value ?? "");
									_ = parser.MoveNext();
								}
							}
							config.Specs = specs;
							_ = parser.MoveNext(); // consume SequenceEnd
						}
						break;
					default:
						// Skip unknown properties
						_ = parser.MoveNext();
						break;
				}
			}

			_ = parser.MoveNext(); // consume MappingEnd
			return config;
		}

		throw new YamlException(parser.Current?.Start ?? Mark.Empty, parser.Current?.End ?? Mark.Empty,
			"API configuration must be either a string (spec path) or an object with spec/template fields");
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value is ApiConfiguration config)
		{
			// Always write as object format for consistency
			serializer(config, typeof(ApiConfiguration));
		}
	}
}
