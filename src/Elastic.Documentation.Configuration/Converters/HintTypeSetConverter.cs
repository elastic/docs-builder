// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Converters;

/// <summary>
/// YAML converter for deserializing a list of strings into a HashSet of HintType enums.
/// </summary>
public class HintTypeSetConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(HashSet<HintType>);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var result = new HashSet<HintType>();

		// Handle null/empty case
		if (parser.Current is not SequenceStart)
		{
			_ = parser.MoveNext();
			return result;
		}

		_ = parser.MoveNext(); // Skip SequenceStart

		while (parser.Current is not SequenceEnd)
		{
			if (parser.Current is Scalar scalar)
			{
				var value = scalar.Value;
				if (!string.IsNullOrWhiteSpace(value) &&
					Enum.TryParse<HintType>(value, ignoreCase: true, out var hintType))
				{
					_ = result.Add(hintType);
				}
			}
			_ = parser.MoveNext();
		}

		_ = parser.MoveNext(); // Skip SequenceEnd

		return result;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value is not HashSet<HintType> set)
		{
			emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
			emitter.Emit(new SequenceEnd());
			return;
		}

		emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
		foreach (var hint in set)
		{
			emitter.Emit(new Scalar(hint.ToString()));
		}
		emitter.Emit(new SequenceEnd());
	}
}
