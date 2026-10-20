// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.Serialization;
using Elastic.Documentation.Diagnostics;
using SharpYaml;
using SharpYaml.Model;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>The <c>config/changelog-entry-formats.yml</c> file.</summary>
public sealed record ChangelogEntryFormatsFileYaml
{
	public Dictionary<string, ChangelogEntryFormatYaml>? Formats { get; set; }
}

/// <summary>One entry format: how docs-builder entry fields map to the fields of a source file.</summary>
public sealed record ChangelogEntryFormatYaml
{
	public Dictionary<string, ChangelogEntryFieldYaml>? Fields { get; set; }
	public Dictionary<string, Dictionary<string, string>>? Values { get; set; }
	public YamlLenientList? Ignore { get; set; }
}

/// <summary>How one docs-builder entry field is read from a source file.</summary>
public sealed record ChangelogEntryFieldYaml
{
	public YamlLenientList? From { get; set; }
	public string? Split { get; set; }
	public string? Unless { get; set; }
	public string? Template { get; set; }
	public string? Present { get; set; }
}

/// <summary>
/// A changelog entry format other than the docs-builder one, defined as data in <c>config/changelog-entry-formats.yml</c>.
/// A repository selects it with <c>entry_format</c> in its changelog.yml, and <see cref="ToEntryYaml"/> converts its entry
/// files to the docs-builder format. Converted entries have no products: they take them from the configuration.
/// </summary>
public sealed partial class ChangelogEntryFormat
{
	private static readonly string[] StringTargets = ["title", "type", "subtype", "description", "impact", "action", "pr", "feature-id"];
	private static readonly string[] ListTargets = ["areas", "prs", "issues"];
	private const string HighlightTarget = "highlight";

	private readonly IReadOnlyDictionary<string, ChangelogEntryFieldYaml> _fields;
	private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _values;
	private readonly HashSet<string> _ignored;
	private readonly HashSet<string> _known;

	private ChangelogEntryFormat(string name, ChangelogEntryFormatYaml yaml)
	{
		Name = name;
		_fields = yaml.Fields ?? [];
		_values = (yaml.Values ?? []).ToDictionary(
			kv => kv.Key,
			kv => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(kv.Value, StringComparer.OrdinalIgnoreCase)
		);
		_ignored = new HashSet<string>(yaml.Ignore?.Values ?? [], StringComparer.Ordinal);

		foreach (var target in _fields.Keys.Concat(_values.Keys))
		{
			if (!StringTargets.Contains(target) && !ListTargets.Contains(target) && target != HighlightTarget)
				throw new InvalidOperationException(
					$"Changelog entry format '{name}' maps unknown field '{target}'. Fields: {string.Join(", ", StringTargets.Concat(ListTargets).Append(HighlightTarget))}."
				);
		}

		_known = [];
		foreach (var field in _fields.Values)
		{
			foreach (var path in field.From?.Values ?? [])
				_ = _known.Add(path);
			if (field.Unless is { } unless)
				_ = _known.Add(unless);
			if (field.Present is { } present)
				_ = _known.Add(present);
			if (field.Template is { } template)
			{
				foreach (Match m in PlaceholderRegex().Matches(template))
					_ = _known.Add(m.Groups[1].Value);
			}
		}
	}

	public string Name { get; }

	/// <summary>Converts an entry file in this format to an entry in the docs-builder format, as YAML.</summary>
	/// <exception cref="YamlException">The file is not a mapping or has keys that this format does not know.</exception>
	public string ToEntryYaml(string yaml, string? sourceName = null)
	{
		var stream = YamlStream.Load(new StringReader(yaml));
		if (stream.Count == 0 || stream[0].Contents is not YamlMapping root)
			throw new YamlException($"{sourceName ?? "Changelog entry"}: expected a mapping in the '{Name}' entry format.");

		var unknown = new List<string>();
		CollectUnknownKeys(root, "", unknown);
		if (unknown.Count > 0)
		{
			var valid = string.Join(", ", _known.Concat(_ignored).Order(StringComparer.Ordinal));
			throw new YamlException(
				$"{sourceName ?? "Changelog entry"}: {string.Join("; ", unknown.Select(k => $"Unknown key '{k}'"))} for the '{Name}' entry format. Valid keys: {valid}."
			);
		}

		var dto = new ChangelogEntryDto
		{
			Title = String(root, "title"),
			Type = String(root, "type"),
			Subtype = String(root, "subtype"),
			Description = String(root, "description"),
			Impact = String(root, "impact"),
			Action = String(root, "action"),
			Pr = String(root, "pr"),
			FeatureId = String(root, "feature-id"),
			Areas = List(root, "areas"),
			Prs = List(root, "prs"),
			Issues = List(root, "issues"),
			Highlight = Highlight(root)
		};
		return ReleaseNotesSerialization.SerializeEntryDto(dto);
	}

	private void CollectUnknownKeys(YamlMapping mapping, string prefix, List<string> unknown)
	{
		foreach (var (keyElement, value) in mapping)
		{
			if (keyElement is not YamlValue { Value: { } key })
				continue;
			var path = prefix + key;
			if (_ignored.Contains(path))
				continue;
			if (
				value is YamlMapping child && (_known.Contains(path) || _known.Any(k => k.StartsWith(path + ".", StringComparison.Ordinal)))
			)
				CollectUnknownKeys(child, path + ".", unknown);
			else if (!_known.Contains(path))
				unknown.Add(path);
		}
	}

	private string? String(YamlMapping root, string target)
	{
		var values = Resolve(root, target);
		return values is { Count: > 0 } ? string.Join(", ", values) : null;
	}

	private List<string>? List(YamlMapping root, string target)
	{
		var values = Resolve(root, target);
		return values is { Count: > 0 } ? values : null;
	}

	private bool? Highlight(YamlMapping root)
	{
		if (!_fields.TryGetValue(HighlightTarget, out var field))
			return null;
		var value = Resolve(root, HighlightTarget);
		if (value is { Count: > 0 } && bool.TryParse(value[0], out var parsed))
			return parsed;
		return field.Present is { } present && Find(root, present) is not null ? true : null;
	}

	private List<string>? Resolve(YamlMapping root, string target)
	{
		if (!_fields.TryGetValue(target, out var field))
			return null;
		if (field.Unless is { } unless && Scalars(Find(root, unless)) is { Count: > 0 })
			return null;

		List<string>? values = null;
		if (field.Template is { } template)
		{
			var complete = true;
			var text = PlaceholderRegex().Replace(template, m =>
			{
				var value = Scalars(Find(root, m.Groups[1].Value));
				complete &= value is { Count: > 0 };
				return value is { Count: > 0 } ? value[0] : "";
			});
			values = complete ? [text] : null;
		}
		else
		{
			foreach (var path in field.From?.Values ?? [])
			{
				values = Scalars(Find(root, path));
				if (values is { Count: > 0 })
					break;
			}
		}

		if (values is not { Count: > 0 })
			return null;
		if (field.Split is { Length: > 0 } separator)
			values =
			[
				.. values.SelectMany(v => v.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			];
		if (_values.TryGetValue(target, out var map))
			values = [.. values.Select(v => map.TryGetValue(v.Trim(), out var mapped) ? mapped : v)];
		return values;
	}

	private static YamlElement? Find(YamlMapping root, string path)
	{
		YamlElement? current = root;
		foreach (var segment in path.Split('.'))
		{
			if (current is not YamlMapping mapping || !mapping.TryGetValue(segment, out var next))
				return null;
			current = next;
		}
		return current;
	}

	private static List<string>? Scalars(YamlElement? element) => element switch
	{
		YamlValue { Value: { } value } when !string.IsNullOrWhiteSpace(value) && value != "~" && value != "null" => [value],
		YamlSequence sequence => [.. sequence.OfType<YamlValue>().Select(v => v.Value).Where(v => !string.IsNullOrWhiteSpace(v))],
		_ => null
	};

	[GeneratedRegex(@"\{([A-Za-z0-9_.\-]+)\}")]
	private static partial Regex PlaceholderRegex();

	private static readonly Lazy<IReadOnlyDictionary<string, ChangelogEntryFormat>> BuiltIn = new(LoadBuiltIn);

	/// <summary>The formats defined in <c>config/changelog-entry-formats.yml</c>, by name.</summary>
	public static IReadOnlyDictionary<string, ChangelogEntryFormat> All => BuiltIn.Value;

	private static IReadOnlyDictionary<string, ChangelogEntryFormat> LoadBuiltIn()
	{
		using var stream = typeof(ChangelogEntryFormat).Assembly.GetManifestResourceStream("changelog-entry-formats.yml")
			?? throw new InvalidOperationException("The embedded changelog-entry-formats.yml is missing.");
		using var reader = new StreamReader(stream);
		return Parse(reader.ReadToEnd());
	}

	/// <summary>Parses a formats file in the shape of <c>config/changelog-entry-formats.yml</c>.</summary>
	public static IReadOnlyDictionary<string, ChangelogEntryFormat> Parse(string yaml)
	{
		var options = YamlStaticContext.Default.CreateOptions(
			o => (o with { Converters = [new YamlLenientListConverter()] }).WithUnmappedKeysThrowing()
		);
		var file = YamlSerializer.Deserialize<ChangelogEntryFormatsFileYaml>(yaml, options) ?? new ChangelogEntryFormatsFileYaml();
		return (file.Formats ?? []).ToDictionary(
			kv => kv.Key,
			kv => new ChangelogEntryFormat(kv.Key, kv.Value),
			StringComparer.OrdinalIgnoreCase
		);
	}
}
