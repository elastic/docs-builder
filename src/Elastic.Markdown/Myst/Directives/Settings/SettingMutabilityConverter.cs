// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Markdown.Myst.Directives.Settings;

/// <summary>
/// Converts <see cref="SettingMutability"/> using its historical lower-case alias names
/// (<c>static</c> / <c>dynamic</c>), replacing YamlDotNet's hyphenated-enum naming convention for
/// this one enum. SharpYaml's built-in enum converter has no per-member naming hook.
/// </summary>
public class SettingMutabilityConverter : YamlConverter<SettingMutability>
{
	public override SettingMutability Read(YamlReader reader)
	{
		var value = reader.GetScalarValue();
		var start = reader.Start;
		var end = reader.End;
		_ = reader.Read();

		return value switch
		{
			"static" => SettingMutability.Static,
			"dynamic" => SettingMutability.Dynamic,
			_ =>
				throw new YamlException(
					reader.SourceName,
					start,
					end,
					$"Invalid setting mutability '{value}'. Expected 'static' or 'dynamic'."
				)
		};
	}

	public override void Write(YamlWriter writer, SettingMutability value) =>
		writer.WriteScalar(value switch
		{
			SettingMutability.Static => "static",
			SettingMutability.Dynamic => "dynamic",
			_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
		});
}
