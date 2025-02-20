// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Elastic.Markdown.IO.Configuration;

public record YamlToplevelKey
{
	public required string Key { get; init; }
	public required KeyValuePair<YamlNode, YamlNode> Entry { get; init; }
}

public class YamlStreamReader(IFileInfo source, BuildContext context)
{
	public IFileInfo Source { get; init; } = source;
	public BuildContext Context { get; init; } = context;

	public IEnumerable<YamlToplevelKey> Read()
	{
		// Load the stream
		var yaml = new YamlStream();
		var textReader = Source.FileSystem.File.OpenText(Source.FullName);
		yaml.Load(textReader);

		if (yaml.Documents.Count == 0)
		{
			Context.EmitWarning(Source, "empty redirect file");
			yield break;
		}
		// Examine the stream
		var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

		foreach (var entry in mapping.Children)
		{
			var key = (entry.Key as YamlScalarNode)?.Value;
			if (key is null)
				continue;
			yield return new YamlToplevelKey { Key = key, Entry = entry };
		}
	}

	public string? ReadString(KeyValuePair<YamlNode, YamlNode> entry)
	{
		if (entry.Value is YamlScalarNode scalar)
			return scalar.Value;

		if (entry.Key is YamlScalarNode scalarKey)
		{
			var key = scalarKey.Value;
			EmitError($"'{key}' is not a string", entry.Key);
			return null;
		}

		EmitError($"'{entry.Key}' is not a string", entry.Key);
		return null;
	}

	public static string[] ReadStringArray(KeyValuePair<YamlNode, YamlNode> entry)
	{
		var values = new List<string>();
		if (entry.Value is not YamlSequenceNode sequence)
			return [.. values];

		foreach (var entryValue in sequence.Children.OfType<YamlScalarNode>())
		{
			if (entryValue.Value is not null)
				values.Add(entryValue.Value);
		}

		return [.. values];
	}

	public bool ReadObjectDictionary(KeyValuePair<YamlNode, YamlNode> entry, [NotNullWhen(true)] out YamlMappingNode? mapping)
	{
		mapping = null;
		if (entry.Value is not YamlMappingNode m)
		{
			if (entry.Key is YamlScalarNode scalarKey)
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

	public Dictionary<string, string?> ReadDictionaryNullValue(KeyValuePair<YamlNode, YamlNode> entry)
	{
		var dictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
		if (entry.Value is YamlScalarNode shortSyntax && shortSyntax.Value is not null)
		{
			if (shortSyntax.Value is "!")
				return new Dictionary<string, string?> { { "!", "!" } };
			EmitError($"'{shortSyntax.Value}' is not a valid redirect anchor value", entry.Key);
			return [];
		}
		if (entry.Value is not YamlMappingNode mapping)
		{
			if (entry.Key is YamlScalarNode scalarKey)
			{
				var key = scalarKey.Value;
				EmitWarning($"'{key}' is not a dictionary");
			}
			else
				EmitWarning($"'{entry.Key}' is not a dictionary");

			return dictionary;
		}

		foreach (var entryValue in mapping.Children)
		{
			if (entryValue.Key is not YamlScalarNode scalar || scalar.Value is null)
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

	public Dictionary<string, string> ReadDictionary(KeyValuePair<YamlNode, YamlNode> entry)
	{
		var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (entry.Value is not YamlMappingNode mapping)
		{
			if (entry.Key is YamlScalarNode scalarKey)
			{
				var key = scalarKey.Value;
				EmitWarning($"'{key}' is not a dictionary");
			}
			else
				EmitWarning($"'{entry.Key}' is not a dictionary");

			return dictionary;
		}

		foreach (var entryValue in mapping.Children)
		{
			if (entryValue.Key is not YamlScalarNode scalar || scalar.Value is null)
				continue;
			var key = scalar.Value;
			var value = ReadString(entryValue);
			if (value is not null)
				dictionary.Add(key, value);
		}

		return dictionary;
	}

	public void EmitError(string message, YamlNode? node) =>
		EmitError(message, node?.Start, node?.End, (node as YamlScalarNode)?.Value?.Length);

	public void EmitWarning(string message, YamlNode? node) =>
		EmitWarning(message, node?.Start, node?.End, (node as YamlScalarNode)?.Value?.Length);

	public void EmitError(string message, Exception e) =>
		Context.Collector.EmitError(Source.FullName, message, e);

	private void EmitError(string message, Mark? start = null, Mark? end = null, int? length = null)
	{
		length ??= start.HasValue && end.HasValue ? (int)start.Value.Column - (int)end.Value.Column : null;
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = Source.FullName,
			Message = message,
			Line = start.HasValue ? (int)start.Value.Line : null,
			Column = start.HasValue ? (int)start.Value.Column : null,
			Length = length
		};
		Context.Collector.Channel.Write(d);
	}
	public void EmitWarning(string message, Mark? start = null, Mark? end = null, int? length = null)
	{
		length ??= start.HasValue && end.HasValue ? (int)start.Value.Column - (int)end.Value.Column : null;
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = Source.FullName,
			Message = message,
			Line = start.HasValue ? (int)start.Value.Line : null,
			Column = start.HasValue ? (int)start.Value.Column : null,
			Length = length
		};
		Context.Collector.Channel.Write(d);
	}
}
