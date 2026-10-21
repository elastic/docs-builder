// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Converters;

/// <summary>
/// YAML type converter for nullable <see cref="TimeSpan"/>.
/// Accepts duration strings of the form <c>&lt;integer&gt;s</c> (seconds) or <c>&lt;integer&gt;m</c> (minutes).
/// No other units are supported; values such as <c>1h</c>, <c>90</c>, or <c>0m</c> are rejected with a
/// descriptive <see cref="YamlException"/>. A null / absent mapping value deserializes as <c>null</c>.
/// </summary>
/// <remarks>
/// This converter is intentionally restrictive: minutes is the largest unit to keep operator intent
/// clear and to prevent accidentally setting very long timeouts.
/// </remarks>
public class DurationYamlConverter : YamlConverter<TimeSpan?>
{
	public override TimeSpan? Read(YamlReader reader)
	{
		if (reader.TokenType != YamlTokenType.Scalar)
		{
			reader.Skip();
			return null;
		}

		var value = reader.ScalarValue;
		var start = reader.Start;
		var end = reader.End;
		_ = reader.Read();

		if (string.IsNullOrWhiteSpace(value))
			return null;

		if (value.EndsWith('m') && int.TryParse(value.AsSpan(0, value.Length - 1), out var minutes) && minutes > 0)
			return TimeSpan.FromMinutes(minutes);

		if (value.EndsWith('s') && int.TryParse(value.AsSpan(0, value.Length - 1), out var seconds) && seconds > 0)
			return TimeSpan.FromSeconds(seconds);

		throw new YamlException(
			reader.SourceName,
			start,
			end,
			$"Invalid duration '{value}'. Expected a positive integer followed by 's' (seconds) or 'm' (minutes), e.g. '30s' or '15m'."
		);
	}

	public override void Write(YamlWriter writer, TimeSpan? value)
	{
		if (value is { } ts)
		{
			var text = ts.TotalMinutes >= 1 && ts.TotalMinutes % 1 == 0 ? $"{(int)ts.TotalMinutes}m" : $"{(int)ts.TotalSeconds}s";
			writer.WriteScalar(text);
		}
		else
			writer.WriteScalar("");
	}
}
