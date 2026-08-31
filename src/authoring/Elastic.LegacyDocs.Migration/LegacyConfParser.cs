// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.LegacyDocs.Migration;

public static class LegacyConfParser
{
	private static readonly IDeserializer RawDeserializer = new DeserializerBuilder().Build();

	private static readonly ISerializer RoundTripSerializer = new SerializerBuilder().DisableAliases().Build();

	private static readonly IDeserializer TypedDeserializer = new DeserializerBuilder()
		.WithNamingConvention(UnderscoredNamingConvention.Instance)
		.WithTypeConverter(new BranchRefListConverter())
		.WithTypeConverter(new BranchRefConverter())
		.IgnoreUnmatchedProperties()
		.Build();

	public static LegacyConf Parse(string yaml)
	{
		var raw = RawDeserializer.Deserialize<object>(yaml);
		var resolved = RoundTripSerializer.Serialize(raw);
		var conf = TypedDeserializer.Deserialize<LegacyConf>(resolved) ?? new LegacyConf();
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
