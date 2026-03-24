// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Changelog.Serialization;

/// <summary>
/// YAML type converter for <see cref="YamlLenientList"/> that accepts both string and list forms.
/// <list type="bullet">
/// <item>Scalar: splits by comma, trims entries, removes empties.</item>
/// <item>Sequence: reads each scalar item into a list.</item>
/// <item>Null/empty: returns <c>YamlLenientList(null)</c>.</item>
/// </list>
/// </summary>
public class YamlLenientListConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(YamlLenientList);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.TryConsume<Scalar>(out var scalar))
		{
			if (string.IsNullOrEmpty(scalar.Value) || scalar.Value == "~")
				return new YamlLenientList(null);

			var items = scalar.Value
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToList();

			return new YamlLenientList(items.Count > 0 ? items : null);
		}

		if (parser.TryConsume<SequenceStart>(out _))
		{
			var items = new List<string>();

			while (!parser.TryConsume<SequenceEnd>(out _))
			{
				var item = parser.Consume<Scalar>();
				if (!string.IsNullOrWhiteSpace(item.Value))
					items.Add(item.Value.Trim());
			}

			return new YamlLenientList(items.Count > 0 ? items : null);
		}

		return new YamlLenientList(null);
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value is not YamlLenientList { Values: { Count: > 0 } values })
		{
			emitter.Emit(new Scalar(null, null, string.Empty, ScalarStyle.Plain, true, false));
			return;
		}

		emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));

		foreach (var item in values)
			emitter.Emit(new Scalar(null, null, item, ScalarStyle.Plain, true, false));

		emitter.Emit(new SequenceEnd());
	}

	/// <summary>
	/// Reads a scalar-or-sequence value from the parser and returns a joined comma-separated string.
	/// Useful for fields that stay as <c>string?</c> in the domain model (e.g., TypeEntryYaml.Labels).
	/// </summary>
	internal static string? ReadAsString(IParser parser)
	{
		if (parser.TryConsume<Scalar>(out var scalar))
			return string.IsNullOrEmpty(scalar.Value) || scalar.Value == "~" ? null : scalar.Value;

		if (parser.TryConsume<SequenceStart>(out _))
		{
			var items = new List<string>();

			while (!parser.TryConsume<SequenceEnd>(out _))
			{
				var item = parser.Consume<Scalar>();
				if (!string.IsNullOrWhiteSpace(item.Value))
					items.Add(item.Value.Trim());
			}

			return items.Count > 0 ? string.Join(", ", items) : null;
		}

		return null;
	}
}
