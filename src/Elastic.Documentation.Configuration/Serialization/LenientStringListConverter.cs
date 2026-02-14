// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Serialization;

/// <summary>
/// YAML type converter for <c>List&lt;string&gt;</c> that accepts both comma-separated strings and YAML sequences.
/// Used by the minimal changelog DTO deserializer so that publish blocker fields like <c>types</c> and <c>areas</c>
/// can be specified as either <c>"deprecation, known-issue"</c> or a YAML list.
/// </summary>
public class LenientStringListConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(List<string>);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.TryConsume<Scalar>(out var scalar))
		{
			if (string.IsNullOrEmpty(scalar.Value) || scalar.Value == "~")
				return null;

			var items = scalar.Value
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToList();

			return items.Count > 0 ? items : null;
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

			return items.Count > 0 ? items : null;
		}

		return null;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value is not List<string> { Count: > 0 } items)
		{
			emitter.Emit(new Scalar(null, null, string.Empty, ScalarStyle.Plain, true, false));
			return;
		}

		emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));

		foreach (var item in items)
			emitter.Emit(new Scalar(null, null, item, ScalarStyle.Plain, true, false));

		emitter.Emit(new SequenceEnd());
	}
}
