// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ConsoleAppFramework;
using Zx;
using static Zx.Env;

var app = ConsoleApp.Create();

app.Add("", async Task (Cancel _) =>
{
	await "dotnet tool restore";
	await "dotnet build -c Release --verbosity minimal";
	await "dotnet test --configuration Release --logger GitHubActions -- RunConfiguration.CollectSourceInformation=true";
});

app.Add("publish", async (Cancel _) =>
{
	var source = "src/docs-builder/docs-builder.csproj";
	await $"""
		dotnet publish {source}
		""";

	var generatorSource = "src/docs-generator/docs-generator.csproj";
	await $"""
		dotnet publish {generatorSource}
		""";
});

// this is manual for now and quite hacky.
// this ensures we download the actual LICENSE files in the repositories.
// NOT the SPDX html from licenses.nuget.org

app.Add("notices", async Task<int> (Cancel ctx) =>
{
	var packages = await "dotnet thirdlicense --project src/docs-builder/docs-builder.csproj --output NOTICE.txt";
	var packageLines = packages.Split(Environment.NewLine).Where(l => l.StartsWith("+"));

	await File.WriteAllTextAsync("NOTICE.txt",
		$"""
		 Elastic Documentation Tooling
		 Copyright 2024-{DateTime.UtcNow.Year} Elasticsearch B.V.


		 """, ctx);


	Console.WriteLine("Package lines:");
	foreach (var line in packageLines)
	{
		var package = line.Split('+', '(')[1].ToLowerInvariant().Trim();
		var version = line.Split('(', ')')[1].TrimStart('v').Trim();
		if (package.StartsWith("microsoft.") || package.StartsWith("system"))
			continue;

		var text = await fetchText($"https://api.nuget.org/v3-flatcontainer/{package}/{version}/{package}.nuspec");
		var xml = XDocument.Load(new StringReader(text));
		var projectUrl = xml.XPathSelectElement("//*[local-name()='projectUrl']")?.Value;
		var id = xml.XPathSelectElement("//*[local-name()='id']")?.Value;
		projectUrl = projectUrl?.Replace("/wiki", string.Empty);

		if (projectUrl is null || projectUrl.Contains(".github.com"))
			throw new Exception($"Can not download license for {id}: {projectUrl}");

		var rawUrl = projectUrl.Replace("github.com", "raw.githubusercontent.com");
		string[] targets =
		[
			rawUrl + $"/refs/heads/master/" + "LICENSE.txt",
			rawUrl + $"/refs/heads/master/" + "license.txt",
			rawUrl + $"/refs/heads/master/" + "LICENSE",
			rawUrl + $"/refs/heads/master/" + "LICENSE.md",
			rawUrl + $"/refs/heads/main/" + "LICENSE.txt",
			rawUrl + $"/refs/heads/main/" + "license.txt",
			rawUrl + $"/refs/heads/main/" + "LICENSE",
			rawUrl + $"/refs/heads/main/" + "LICENSE.md",
		];
		var license = string.Empty;
		foreach (var target in targets)
		{
			Console.WriteLine($"Downloading license for {id}: {target}");
			try
			{
				license = await fetchText(target);
			}
			catch { }
			if (license.Length > 0)
				break;
		}

		if (string.IsNullOrWhiteSpace(license))
			throw new Exception($"Can not download license for {id}: {projectUrl}");

		await File.AppendAllTextAsync("NOTICE.txt",
			$"""
			 License notice for {id} (v{version})
			 ------------------------------------
			 {license}


			 """, ctx);
	}

	try
	{
		await "git status --porcelain";
	}
	catch (Exception ex)
	{
		Console.WriteLine(ex.ToString());
		Console.WriteLine("The build left unchecked artifacts in the source folder");
		await "git diff NOTICE.txt";
		return 1;
	}

	return 0;
});

await app.RunAsync(args);
