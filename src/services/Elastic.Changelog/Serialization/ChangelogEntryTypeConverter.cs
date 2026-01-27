// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Changelog.Serialization;

/// <summary>
/// YAML type converter for ChangelogEntryType that handles string serialization/deserialization.
/// Reads/writes the Display attribute value (e.g., "bug-fix" instead of "BugFix").
/// </summary>
public class ChangelogEntryTypeConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(ChangelogEntryType);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var scalar = parser.Consume<Scalar>();

		if (string.IsNullOrEmpty(scalar.Value))
			return ChangelogEntryType.Invalid;

		// Try to parse using the extension method that supports Display attribute matching
		if (ChangelogEntryTypeExtensions.TryParse(scalar.Value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true))
			return result;

		// Return Invalid for unrecognized type strings - will be caught by validation
		return ChangelogEntryType.Invalid;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value is not ChangelogEntryType entryType)
		{
			emitter.Emit(new Scalar(null, null, string.Empty, ScalarStyle.Plain, true, false));
			return;
		}

		// Write the Display attribute value (e.g., "bug-fix" instead of "BugFix")
		var stringValue = entryType.ToStringFast(true);
		emitter.Emit(new Scalar(null, null, stringValue, ScalarStyle.Plain, true, false));
	}
}
