// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Versions;
using SharpYaml.Serialization;

// ReSharper disable once CheckNamespace — intentionally preserving the original namespace so consumers need no using changes
#pragma warning disable IDE0130
namespace Elastic.Documentation.AppliesTo;

public class SemVersionConverter : YamlConverter<SemVersion>
{
	public override SemVersion? Read(YamlReader reader)
	{
		var value = reader.GetScalarValue();
		_ = reader.Read();
		if (string.IsNullOrWhiteSpace(value))
			return AllVersions.Instance;
		if (string.Equals(value.Trim(), "all", StringComparison.OrdinalIgnoreCase))
			return AllVersions.Instance;
		return (SemVersion)value;
	}

	public override void Write(YamlWriter writer, SemVersion value) => writer.WriteScalar(value.ToString());

	public static bool TryParse(string? value, out SemVersion? version)
	{
		version = value?.Trim().ToLowerInvariant() switch
		{
			null => AllVersions.Instance,
			"all" => AllVersions.Instance,
			"" => AllVersions.Instance,
			_ => SemVersion.TryParse(value, out var v) ? v : SemVersion.TryParse(value + ".0", out v) ? v : null
		};
		return version is not null;
	}
}
