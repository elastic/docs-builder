// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Changelog.Serialization;

/// <summary>
/// YAML type converter for TypeEntryYaml that handles both string and object forms.
/// String form: "label1, label2"
/// Object form: { labels: "label1", subtypes: { api: "label" } }
/// </summary>
public class TypeEntryYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(TypeEntryYaml);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		// Handle null value
		if (parser.TryConsume<Scalar>(out var scalar))
		{
			// Handle empty scalar (null/empty value)
			if (string.IsNullOrEmpty(scalar.Value) || scalar.Value == "~")
				return new TypeEntryYaml();

			// Handle string form: just labels
			return TypeEntryYaml.FromLabels(scalar.Value);
		}

		// Handle object form: { labels: "...", subtypes: {...} }
		if (parser.TryConsume<MappingStart>(out _))
		{
			var entry = new TypeEntryYaml();

			while (!parser.TryConsume<MappingEnd>(out _))
			{
				var key = parser.Consume<Scalar>();

				switch (key.Value)
				{
					case "labels":
						var labelsScalar = parser.Consume<Scalar>();
						entry = entry with { Labels = string.IsNullOrEmpty(labelsScalar.Value) ? null : labelsScalar.Value };
						break;
					case "subtypes":
						entry = entry with { Subtypes = ParseSubtypes(parser) };
						break;
					default:
						// Skip unknown keys - consume the value
						SkipValue(parser);
						break;
				}
			}

			return entry;
		}

		// Unknown format
		return new TypeEntryYaml();
	}

	private static Dictionary<string, string?>? ParseSubtypes(IParser parser)
	{
		if (!parser.TryConsume<MappingStart>(out _))
		{
			// Handle null value
			if (parser.TryConsume<Scalar>(out _))
				return null;
			return null;
		}

		var subtypes = new Dictionary<string, string?>();

		while (!parser.TryConsume<MappingEnd>(out _))
		{
			var key = parser.Consume<Scalar>();
			var value = parser.Consume<Scalar>();
			subtypes[key.Value] = string.IsNullOrEmpty(value.Value) ? null : value.Value;
		}

		return subtypes;
	}

	private static void SkipValue(IParser parser)
	{
		if (parser.TryConsume<Scalar>(out _))
			return;

		if (parser.TryConsume<MappingStart>(out _))
		{
			var depth = 1;
			while (depth > 0)
			{
				if (parser.TryConsume<MappingStart>(out _))
					depth++;
				else if (parser.TryConsume<MappingEnd>(out _))
					depth--;
				else
					_ = parser.MoveNext();
			}
		}
		else if (parser.TryConsume<SequenceStart>(out _))
		{
			var depth = 1;
			while (depth > 0)
			{
				if (parser.TryConsume<SequenceStart>(out _))
					depth++;
				else if (parser.TryConsume<SequenceEnd>(out _))
					depth--;
				else
					_ = parser.MoveNext();
			}
		}
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value is not TypeEntryYaml entry)
		{
			emitter.Emit(new Scalar(null, null, string.Empty, ScalarStyle.Plain, true, false));
			return;
		}

		// If only labels and no subtypes, emit as simple string
		if (entry.Subtypes == null || entry.Subtypes.Count == 0)
		{
			emitter.Emit(new Scalar(null, null, entry.Labels ?? string.Empty, ScalarStyle.Plain, true, false));
			return;
		}

		// Otherwise emit as object
		emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));

		if (!string.IsNullOrEmpty(entry.Labels))
		{
			emitter.Emit(new Scalar(null, null, "labels", ScalarStyle.Plain, true, false));
			emitter.Emit(new Scalar(null, null, entry.Labels, ScalarStyle.DoubleQuoted, false, true));
		}

		if (entry.Subtypes is { Count: > 0 })
		{
			emitter.Emit(new Scalar(null, null, "subtypes", ScalarStyle.Plain, true, false));
			emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));

			foreach (var (subKey, subValue) in entry.Subtypes)
			{
				emitter.Emit(new Scalar(null, null, subKey, ScalarStyle.Plain, true, false));
				emitter.Emit(new Scalar(null, null, subValue ?? string.Empty, ScalarStyle.Plain, true, false));
			}

			emitter.Emit(new MappingEnd());
		}

		emitter.Emit(new MappingEnd());
	}
}
