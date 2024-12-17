// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

public class AllVersions() : SemVersion(9999, 9999, 9999)
{
	public static AllVersions Instance { get; } = new ();
}

public class SemVersionConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(SemVersion);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var value = parser.Consume<Scalar>();
		if (string.IsNullOrWhiteSpace(value.Value))
			return AllVersions.Instance;
		if (string.Equals(value.Value.Trim(), "all", StringComparison.InvariantCultureIgnoreCase))
			return AllVersions.Instance;
		return (SemVersion)value.Value;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value == null)
			return;
		emitter.Emit(new Scalar(value.ToString()!));
	}

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

