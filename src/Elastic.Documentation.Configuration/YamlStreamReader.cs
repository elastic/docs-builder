// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Documentation.Diagnostics;
using SharpYaml;
using SharpYaml.Model;

namespace Elastic.Documentation.Configuration;

public record YamlToplevelKey
{
	public required string Key { get; init; }
	public required KeyValuePair<YamlElement, YamlElement?> Entry { get; init; }
}

public class YamlStreamReader(IFileInfo source, IDiagnosticsCollector collector)
{
	private IFileInfo Source { get; init; } = source;
	private IDiagnosticsCollector Collector { get; init; } = collector;

	public IEnumerable<YamlToplevelKey> Read()
	{
		// Load the stream
		using var textReader = Source.FileSystem.File.OpenText(Source.FullName);
		var yaml = YamlStream.Load(textReader);

		if (yaml.Count == 0)
		{
			Collector.EmitWarning(Source, "empty redirect file");
			yield break;
		}
		// Examine the stream
		var mapping = (YamlMapping)yaml[0].Contents!;

		foreach (var entry in mapping)
		{
			var key = (entry.Key as YamlValue)?.Value;
			if (key is null)
				continue;
			yield return new YamlToplevelKey { Key = key, Entry = entry };
		}
	}

	public string? ReadString(KeyValuePair<YamlElement, YamlElement?> entry)
	{
		if (entry.Value is YamlValue scalar)
			return scalar.Value;

		if (entry.Key is YamlValue scalarKey)
		{
			var key = scalarKey.Value;
			EmitError($"'{key}' is not a string", entry.Key);
			return null;
		}

		EmitError($"'{entry.Key}' is not a string", entry.Key);
		return null;
	}

	public static string[] ReadStringArray(KeyValuePair<YamlElement, YamlElement?> entry)
	{
		var values = new List<string>();
		if (entry.Value is not YamlSequence sequence)
			return [.. values];

		foreach (var entryValue in sequence.OfType<YamlValue>())
			values.Add(entryValue.Value);

		return [.. values];
	}

	public bool ReadObjectDictionary(KeyValuePair<YamlElement, YamlElement?> entry, [NotNullWhen(true)] out YamlMapping? mapping)
	{
		mapping = null;
		if (entry.Value is not YamlMapping m)
		{
			if (entry.Key is YamlValue scalarKey)
			{
				var key = scalarKey.Value;
				EmitWarning($"'{key}' is not a dictionary");
			}
			else
				EmitWarning($"'{entry.Key}' is not a dictionary");

			return false;
		}

		mapping = m;

		return true;
	}

	public Dictionary<string, string?> ReadDictionaryNullValue(KeyValuePair<YamlElement, YamlElement?> entry)
	{
		var dictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
		if (entry.Value is YamlValue shortSyntax)
		{
			if (shortSyntax.Value is "!")
				return new Dictionary<string, string?> { { "!", "!" } };
			EmitError($"'{shortSyntax.Value}' is not a valid redirect anchor value", entry.Key);
			return [];
		}
		if (entry.Value is not YamlMapping mapping)
		{
			if (entry.Key is YamlValue scalarKey)
			{
				var key = scalarKey.Value;
				EmitWarning($"'{key}' is not a dictionary");
			}
			else
				EmitWarning($"'{entry.Key}' is not a dictionary");

			return dictionary;
		}

		foreach (var entryValue in mapping)
		{
			if (entryValue.Key is not YamlValue scalar)
				continue;
			var key = scalar.Value;
			var value = ReadString(entryValue);
			if (value is "null" or "")
				dictionary.Add(key, null);
			else if (value is not null)
				dictionary.Add(key, value);
		}

		return dictionary;
	}

	public Dictionary<string, string> ReadDictionary(KeyValuePair<YamlElement, YamlElement?> entry)
	{
		var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (entry.Value is not YamlMapping mapping)
		{
			if (entry.Key is YamlValue scalarKey)
			{
				var key = scalarKey.Value;
				EmitWarning($"'{key}' is not a dictionary");
			}
			else
				EmitWarning($"'{entry.Key}' is not a dictionary");

			return dictionary;
		}

		foreach (var entryValue in mapping)
		{
			if (entryValue.Key is not YamlValue scalar)
				continue;
			var key = scalar.Value;
			var value = ReadString(entryValue);
			if (value is not null)
				dictionary.Add(key, value);
		}

		return dictionary;
	}

	public void EmitError(string message, YamlElement? node)
	{
		var (start, end) = GetMarks(node);
		EmitError(message, start, end, (node as YamlValue)?.Value.Length);
	}

	/// <summary>Reports an unknown key as an error at the key, with a suggestion or the list of valid keys.</summary>
	public void EmitUnknownKey(string path, YamlElement keyNode, IReadOnlyCollection<string> knownKeys)
	{
		var name = (keyNode as YamlValue)?.Value ?? string.Empty;
		var suggestion = YamlUnmappedMemberDiagnostics.Suggest(name, knownKeys);
		EmitError(YamlUnmappedMemberDiagnostics.FormatMessage(path, name, suggestion, knownKeys), keyNode);
	}

	public void EmitWarning(string message, YamlElement? node)
	{
		var (start, end) = GetMarks(node);
		EmitWarning(message, start, end, (node as YamlValue)?.Value.Length);
	}

	public void EmitError(string message, Exception e) => Collector.EmitError(Source.FullName, message, e);

	private static (Mark? Start, Mark? End) GetMarks(YamlElement? node) => node switch
	{
		YamlValue v => (v.Scalar.Start, v.Scalar.End),
		YamlMapping m => (m.MappingStart.Start, m.MappingStart.End),
		YamlSequence s => (s.SequenceStart.Start, s.SequenceStart.End),
		_ => (null, null)
	};

	private void EmitError(string message, Mark? start = null, Mark? end = null, int? length = null)
	{
		length ??= start.HasValue && end.HasValue ? start.Value.Column - end.Value.Column : null;
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = Source.FullName,
			Message = message,
			// SharpYaml's Mark.Line/Column are 0-based; +1 to match the 1-based convention
			// used everywhere else for user-facing diagnostics.
			Line = start.HasValue ? start.Value.Line + 1 : null,
			Column = start.HasValue ? start.Value.Column + 1 : null,
			Length = length
		};
		Collector.Write(d);
	}
	public void EmitWarning(string message, Mark? start = null, Mark? end = null, int? length = null)
	{
		length ??= start.HasValue && end.HasValue ? start.Value.Column - end.Value.Column : null;
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = Source.FullName,
			Message = message,
			// SharpYaml's Mark.Line/Column are 0-based; +1 to match the 1-based convention
			// used everywhere else for user-facing diagnostics.
			Line = start.HasValue ? start.Value.Line + 1 : null,
			Column = start.HasValue ? start.Value.Column + 1 : null,
			Length = length
		};
		Collector.Write(d);
	}
}
