// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Slugify;
using Soenneker.Utils.AutoBogus;

namespace Documentation.Generator.Domain;

public record MarkdownFile
{
	public required string FileName { get; init; }
	public required Section[] Sections { get; init; }
	public required string Title { get; init; }

	public string RelativePath => $"{Directory}/{FileName}";

	public required bool IncludeInUpdate { get; init; } = true;

	public string Directory { get; set; } = string.Empty;
	public List<string> Links { get; set; } = [];

	public void RewriteLinksIntoSections()
	{
		var linksLength = Links.Count;
		var sectionsLength = Sections.Length;
		for (var i = 0; i < linksLength; i++)
		{
			var link = Links[i];
			var section = Sections[Determinism.Random.Contents.Number(0, sectionsLength - 1)];
			var words = section.Paragraphs.Split(" ");
			var w = Determinism.Random.Contents.Number(0, words.Length - 1);
			var word = words[w];
			words[w] = $"[{word}](/{link})";
			section.Paragraphs = string.Join(" ", words);
		}
	}


	public string GetRandomLink()
	{
		//TODO since updates rewrite section headers old docs might
		//no longer validate so we always link to whole files
		return RelativePath;
		/*
		var sectionLink = Determinism.Random.Contents.Bool(0.8f);
		if (!sectionLink) return RelativePath;
		var section = Sections[Determinism.Random.Contents.Number(0, Sections.Length - 1)];
		return $"{RelativePath}#{Generators.Slug.GenerateSlug(section.Header)}";
		*/

	}
}

