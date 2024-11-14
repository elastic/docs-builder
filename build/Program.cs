// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using ProcNet;
using Zx;
//using static Zx.Env;

var app = ConsoleApp.Create();

app.Add("", async Task<int> (Cancel ctx) =>
{
	await "dotnet tool restore";
	await "dotnet build -c Release --verbosity minimal";
	await File.WriteAllTextAsync("NOTICE.txt",
		$"""
		Elastic Documentation Tooling
		Copyright 2024-{DateTime.UtcNow.Year} Elasticsearch B.V.


		""", ctx);
	await "dotnet thirdlicense --project src/docs-builder/docs-builder.csproj --output .artifacts/NOTICE_temp.txt";
	await File.AppendAllTextAsync("NOTICE.txt", File.ReadAllText(".artifacts/NOTICE_temp.txt"), ctx);
	try
	{
		await "git diff-index --quiet HEAD --";
	}
	catch
	{
		Console.WriteLine("The build left unchecked artifacts in the source folder");
		await "git status --porcelain";
		return 1;
	}

	return 0;
});

app.Add("publish", async (Cancel _) =>
{
	var source = "src/docs-builder/docs-builder.csproj";
	await $"""
		dotnet publish {source} -c Release -o .artifacts/publish \
			--self-contained true /p:PublishTrimmed=true /p:PublishSingleFile=false /p:PublishAot=true
		""";
});
await app.RunAsync(args);
