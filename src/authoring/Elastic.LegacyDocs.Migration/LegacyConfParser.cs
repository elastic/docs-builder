// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using SharpYaml;
using SharpYaml.Model;

namespace Elastic.LegacyDocs.Migration;

public static class LegacyConfParser
{
	private static readonly YamlSerializerOptions TypedOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		Converters = [new BranchRefListConverter(), new BranchRefConverter()]
	};

	public static LegacyConf Parse(string yaml)
	{
		// Materializes anchor/alias references into their resolved values before typed deserialization
		// (legacy conf.yaml files commonly share branch lists via YAML anchors).
		var resolved = YamlStream.Load(new StringReader(yaml)).ToString();
		var conf = YamlSerializer.Deserialize<LegacyConf>(resolved, TypedOptions) ?? new LegacyConf();
		return Flatten(conf);
	}

	private static LegacyConf Flatten(LegacyConf conf)
	{
		var flatCategories = conf.Contents.Select(c => c with { Sections = FlattenBooks(c.Sections, "") }).ToList();
		return conf with { Contents = flatCategories };
	}

	/// <summary>
	/// Recursively flattens nested sub-group entries (those with <c>sections</c> but no <c>sources</c>)
	/// into leaf <see cref="LegacyBook"/> records, accumulating the <c>base_dir</c> prefix as we descend.
	/// </summary>
	private static List<LegacyBook> FlattenBooks(List<LegacyBook> books, string parentBaseDir)
	{
		var result = new List<LegacyBook>();
		foreach (var book in books)
		{
			var dir = parentBaseDir.Length > 0 && book.BaseDir.Length > 0
				? $"{parentBaseDir}/{book.BaseDir}"
				: parentBaseDir.Length > 0 ? parentBaseDir : book.BaseDir;

			if (book.Sections.Count > 0)
			{
				result.AddRange(FlattenBooks(book.Sections, dir));
			}
			else
			{
				var prefix = dir.Length > 0 && !book.Prefix.StartsWith(dir, StringComparison.Ordinal)
					? $"{dir}/{book.Prefix}"
					: book.Prefix;
				result.Add(book with { Prefix = prefix });
			}
		}
		return result;
	}
}
