// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
﻿// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// ReSharper disable RedundantLambdaParameterType

using System.Security.Cryptography.X509Certificates;
using Bogus;
using ConsoleAppFramework;
using Documentation.Generator.Domain;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

await ConsoleApp.RunAsync(args, async Task<int> (
	int? seedFileSystem = null,
	int? seedContent = null,
	string? output = null,
	bool? clear = null
) =>
{
	var cleanOutputDirectory = clear ?? true;
	var outputFolder = !string.IsNullOrWhiteSpace(output)
		? new DirectoryInfo(output)
		: new DirectoryInfo(Path.Combine(Paths.Root.FullName, ".artifacts/docs/markdown"));
	var stateFile = new FileInfo(Path.Combine(outputFolder.FullName, "generator.state"));

	LoadStateFromFile(stateFile, clear, ref seedFileSystem, ref cleanOutputDirectory);

	Determinism.Random = new Determinism(seedFileSystem, seedContent);

	Console.WriteLine($"Running generator with file seed: {Determinism.Random.SeedFileSystem} and content seed: {Determinism.Random.SeedContent}");

	Generators.FolderName.UseSeed(Determinism.Random.SeedFileSystem);
	Generators.File.UseSeed(Determinism.Random.SeedFileSystem);
	Generators.Section.UseSeed(Determinism.Random.SeedContent);

	Generators.FolderNames = Generators.FolderName
		.Generate(Determinism.Random.FileSystem.Number(3, 15))
		.SelectMany(p => Generators.CreateSubPaths(p.Folder, Determinism.Random.FileSystem.Number(0, 3), 0))
		.Distinct()
		.ToArray();

	var folders = new List<Folder>();
	foreach (var folder in Generators.FolderNames)
	{
		var mdFolder = new Folder
		{
			Path = folder,
			Files = Generators.File
				.Generate(Determinism.Random.FileSystem.Number(1, 4))
				.Select(f =>
				{
					f.Directory = folder;
					return f;
				})
				.ToArray()
		};
		folders.Add(mdFolder);
	}

	var files = folders.SelectMany(f => f.Files).ToArray();
	foreach (var folder in folders)
	{
		foreach (var file in folder.Files)
		{
			var length = Determinism.Random.Contents.Number(1, 10);
			file.Links = Enumerable.Range(0, length)
				.Select(i => files[Determinism.Random.Contents.Number(0, files.Length - 1)])
				.Select(f => f.GetRandomLink())
				.ToList();
			file.RewriteLinksIntoSections();
		}
	}

	Console.WriteLine($"Writing to {outputFolder.FullName}");

	if (outputFolder.Exists && cleanOutputDirectory)
		Directory.Delete(outputFolder.FullName, true);

	var updateFiles = files
		.Where(f => cleanOutputDirectory || f.IncludeInUpdate)
		.ToArray();
	foreach (var file in updateFiles)
	{
		var directory = Path.Combine(outputFolder.FullName, file.Directory);
		Console.WriteLine($"Writing to {directory}");
		Directory.CreateDirectory(directory);

		WriteMarkdownFile(outputFolder, file);
	}

	var name = $"random-docset-{seedContent}-{seedFileSystem}";
	WriteIndexMarkdownFile(name, outputFolder);

	var docset = Path.Combine(outputFolder.FullName, "docset.yml");
	File.WriteAllText(docset, $"project: {name}{Environment.NewLine}");
	File.AppendAllText(docset, $"toc:{Environment.NewLine}");
	foreach (var folder in folders)
		File.AppendAllText(docset, $"  - folder: {folder.Path}{Environment.NewLine}");

	File.AppendAllText(docset, $"  - file: index.md{Environment.NewLine}");
	File.AppendAllText(docset, Environment.NewLine);

	File.WriteAllText(stateFile.FullName, $"{Determinism.Random.SeedFileSystem}|{Determinism.Random.SeedContent}");

	return await Task.FromResult(0);
});

void WriteIndexMarkdownFile(string name, DirectoryInfo directoryInfo)
{
	var filePath = Path.Combine(directoryInfo.FullName, "index.md");
	File.WriteAllText(filePath,
		$"""
		 ---
		 title: {name} Documentation Set
		 ---

		 """);
	File.AppendAllText(filePath, "This docset is generated using docs-generator");
	File.AppendAllText(filePath, Environment.NewLine);
}

void WriteMarkdownFile(DirectoryInfo directoryInfo, MarkdownFile markdownFile)
{
	var filePath = Path.Combine(directoryInfo.FullName, markdownFile.RelativePath);
	File.WriteAllText(filePath,
		$"""
		 ---
		 title: {markdownFile.Title}
		 ---

		 """);
	foreach (var section in markdownFile.Sections)
	{
		File.AppendAllText(filePath, Environment.NewLine);
		var header = new string('#', section.Level);
		File.AppendAllText(filePath, $"{header} {section.Header}{Environment.NewLine}");
		File.AppendAllText(filePath, Environment.NewLine);

		File.AppendAllText(filePath, section.Paragraphs);
		File.AppendAllText(filePath, Environment.NewLine);
	}
}

void LoadStateFromFile(FileInfo fileInfo, bool? clear, ref int? seedFs, ref bool cleanOutput)
{
	if (!fileInfo.Exists) return;
	var state = File.ReadAllText(fileInfo.FullName).Split("|");
	if (state.Length != 2) return;
	seedFs ??= int.TryParse(state[0], out var seed) ? seed : seedFs;
	Console.WriteLine($"Seeding with {seedFs} from previous run {fileInfo.FullName}");
	cleanOutput = clear ?? false;
}
