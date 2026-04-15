// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// YAML type converter that handles both the old format (string) and new format (ApiConfiguration object)
/// for backward compatibility.
///
/// Old format: api: { elasticsearch: "elasticsearch-openapi.json" }
/// New format: api: { elasticsearch: { spec: "elasticsearch-openapi.json", template: "elasticsearch-overview.md" } }
/// </summary>
public class ApiConfigurationConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(ApiConfiguration);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.Current is Scalar scalar)
		{
			// Old format: simple string path
			_ = parser.MoveNext();
			return new ApiConfiguration { Spec = scalar.Value };
		}

		if (parser.Current is MappingStart)
		{
			// New format: object with spec/specs/template properties.
			// Do not call rootDeserializer(typeof(ApiConfiguration)) — that re-enters this converter and overflows the stack.
			return ReadMapping(parser);
		}

		throw new YamlException(parser.Current?.Start ?? Mark.Empty, parser.Current?.End ?? Mark.Empty,
			"API configuration must be either a string (spec path) or an object with spec/specs/template properties.");
	}

	private static ApiConfiguration ReadMapping(IParser parser)
	{
		_ = parser.MoveNext();
		var config = new ApiConfiguration();
		while (parser.Current is not MappingEnd)
		{
			if (parser.Current is not Scalar keyScalar)
				throw new YamlException(parser.Current?.Start ?? Mark.Empty, parser.Current?.End ?? Mark.Empty,
					"Expected a string key in api configuration mapping.");

			var key = keyScalar.Value;
			_ = parser.MoveNext();

			switch (key)
			{
				case "template":
				case "spec":
					if (parser.Current is Scalar valueScalar)
					{
						if (key == "template")
							config.Template = valueScalar.Value;
						else
							config.Spec = valueScalar.Value;
						_ = parser.MoveNext();
					}

					break;
				case "specs":
					ReadSpecsSequence(parser, config);
					break;
				default:
					SkipValue(parser);
					break;
			}
		}

		_ = parser.MoveNext();
		return config;
	}

	private static void ReadSpecsSequence(IParser parser, ApiConfiguration config)
	{
		if (parser.Current is not SequenceStart)
			return;

		_ = parser.MoveNext();
		while (parser.Current is not SequenceEnd)
		{
			if (parser.Current is Scalar item)
			{
				config.Specs.Add(item.Value);
				_ = parser.MoveNext();
			}
			else
				_ = parser.MoveNext();
		}

		_ = parser.MoveNext();
	}

	private static void SkipValue(IParser parser)
	{
		switch (parser.Current)
		{
			case Scalar:
				_ = parser.MoveNext();
				return;
			case MappingStart:
			case SequenceStart:
				SkipNested(parser);
				return;
			default:
				throw new YamlException(parser.Current?.Start ?? Mark.Empty, parser.Current?.End ?? Mark.Empty,
					"Unexpected YAML event in api configuration value.");
		}
	}

	private static void SkipNested(IParser parser)
	{
		var depth = 1;
		_ = parser.MoveNext();
		while (depth > 0 && parser.Current is not null)
		{
			switch (parser.Current)
			{
				case MappingStart:
				case SequenceStart:
					depth++;
					break;
				case MappingEnd:
				case SequenceEnd:
					depth--;
					break;
			}

			_ = parser.MoveNext();
		}
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value is not ApiConfiguration config)
		{
			emitter.Emit(new Scalar(string.Empty));
			return;
		}

		// If it's a simple configuration with just a spec, write as string for clean output
		if (config.Template == null && config.Specs.Count == 0 && !string.IsNullOrEmpty(config.Spec))
		{
			emitter.Emit(new Scalar(config.Spec));
			return;
		}

		// Do not call serializer(config, typeof(ApiConfiguration)) — that re-enters this converter.
		emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));
		if (!string.IsNullOrEmpty(config.Template))
		{
			emitter.Emit(new Scalar(null, null, "template", ScalarStyle.Plain, true, false));
			emitter.Emit(new Scalar(null, null, config.Template, ScalarStyle.Plain, true, false));
		}

		if (!string.IsNullOrEmpty(config.Spec))
		{
			emitter.Emit(new Scalar(null, null, "spec", ScalarStyle.Plain, true, false));
			emitter.Emit(new Scalar(null, null, config.Spec, ScalarStyle.Plain, true, false));
		}

		if (config.Specs.Count > 0)
		{
			emitter.Emit(new Scalar(null, null, "specs", ScalarStyle.Plain, true, false));
			emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
			foreach (var spec in config.Specs)
				emitter.Emit(new Scalar(null, null, spec, ScalarStyle.Plain, true, false));
			emitter.Emit(new SequenceEnd());
		}

		emitter.Emit(new MappingEnd());
	}
}
