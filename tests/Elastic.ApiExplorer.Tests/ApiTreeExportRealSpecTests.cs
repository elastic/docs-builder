// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Measures the tree export on real Elasticsearch specs. Download <c>main</c>, <c>9.6</c> and <c>8.19</c> from the spec index into one
/// folder as <c>es-main.json</c>, <c>es-9.6.json</c> and <c>es-8.19.json</c>, set <c>API_TREE_SPECS</c> to it, and run this test by name.
/// </summary>
public class ApiTreeExportRealSpecTests
{
	[Test]
	[Explicit]
	public async Task Collapse_RealElasticsearchSpecs_PrintsDocumentCounts()
	{
		var folder = Environment.GetEnvironmentVariable("API_TREE_SPECS");
		if (string.IsNullOrEmpty(folder))
			Skip.Test("API_TREE_SPECS is not set");

		(string Moniker, string File)[] files = [("main", "es-main.json"), ("9", "es-9.6.json"), ("8", "es-8.19.json")];
		var trees = new List<IndexedTree>();
		foreach (var (moniker, file) in files)
		{
			var spec = await OpenApiReader.Instance.ReadAsync(new FileSystem().FileInfo.New(Path.Join(folder, file)));
			trees.Add(new IndexedTree("elasticsearch", "elasticsearch", moniker, spec!));
		}
		var exporter = new OpenApiDocumentExporter(
			TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9, currentMinor: 5, patch: 6)
		);

		var timer = Stopwatch.StartNew();
		var perTree = trees.Select(t => (t.Moniker, Count: exporter.ConvertToDocuments(t).Count())).ToList();
		var converted = timer.Elapsed;
		timer.Restart();
		var docs = ApiTreeCollapse.Collapse(
			trees.SelectMany(exporter.ConvertToDocuments),
			ApiVersions.DefaultTree(files.Select(f => f.Moniker))!
		).ToList();
		var exported = timer.Elapsed;

		Console.WriteLine(
			$"per tree: {string.Join(", ", perTree.Select(t => $"{t.Moniker}={t.Count}"))} (converted once in {converted.TotalSeconds:0.0}s)"
		);
		Console.WriteLine($"total: {docs.Count}, visible: {docs.Count(d => !d.Hidden)}, hidden: {docs.Count(d => d.Hidden)}");
		Console.WriteLine(
			$"listing 9: {docs.Count(d => d.Api!.Trees.Any(t => t.Tree == "9"))}, visible without 9: {docs.Count(d => !d.Hidden && d.Api!.Trees.All(t => t.Tree != "9"))}"
		);
		Console.WriteLine(
			$"tree sets: {string.Join(", ", docs.GroupBy(d => string.Join("+", d.Api!.Trees.Select(t => t.Tree))).Select(g => $"{g.Key}={g.Count()}"))}"
		);
		Console.WriteLine($"convert and collapse: {exported.TotalSeconds:0.0}s");
	}
}
