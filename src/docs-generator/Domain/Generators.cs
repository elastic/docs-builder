// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Soenneker.Utils.AutoBogus;

namespace Documentation.Generator.Domain;

public static class Generators
{
	public static AutoFaker<FolderName> FolderName { get; } = new();
	public static AutoFaker<Section> Section { get; } = new();
	public static AutoFaker<MarkdownFile> File { get; } = new();

	static Generators()
	{
		FolderName
			.RuleFor(p => p.Folder, f => f.Lorem.Slug(1));

		Section
			.RuleFor(p => p.Paragraphs, f => f.Lorem.Paragraphs(f.Random.Number(1, 10)))
			.RuleFor(p => p.Level, f => f.Random.Number(2, 4));

		File
			.Ignore(p => p.Links)
			.Ignore(p => p.Directory)
			.RuleFor(p => p.FileName, f => f.System.FileName("md"))
			.RuleFor(p => p.IncludeInUpdate, f => Determinism.Random.Contents.Float() <= Determinism.Random.ContentProbability)
			.RuleFor(p => p.Sections, f => Section.Generate(Determinism.Random.Contents.Number(1, 12)).ToArray());
	}

	public static IEnumerable<string> CreateSubPaths(string parent, int maxDepth, int currentDepth)
	{
		yield return parent;
		if (currentDepth == maxDepth)
			yield break;
		var subFolders = FolderName.Generate(Determinism.Random.FileSystem.Number(0, 4));
		foreach (var subFolder in subFolders)
		{
			var path = $"{parent}/{subFolder.Folder}";
			yield return path;
			var subPaths = CreateSubPaths(path, maxDepth, currentDepth + 1);
			foreach (var p in subPaths)
				yield return p;
		}
	}

	public static string[] FolderNames { get; set; } = [];
}
