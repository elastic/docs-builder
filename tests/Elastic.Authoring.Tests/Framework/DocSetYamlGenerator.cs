// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using YamlDotNet.RepresentationModel;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Port of <c>Setup.GenerateDocSetYaml</c> and <c>RedirectMockTargets</c> from
/// <c>tests/authoring/Framework/Setup.fs</c>.
///
/// IMPORTANT: This generator reads real repo state from disk:
/// <list type="bullet">
///   <item><description><c>docs/_redirects.yml</c> — production redirect rules</description></item>
///   <item><description><c>docs-tests/redirects.yml</c> — test-specific complex redirect entries</description></item>
///   <item><description><c>docs-tests/redirects/{5th,second,third,first}-page.md</c> — redirect target stubs</description></item>
/// </list>
/// Pass/fail of cross-link and redirect tests depends on files that are not normally thought
/// of as test fixtures. When real <c>docs/_redirects.yml</c> changes, these tests may break.
///
/// The docset filename is randomised between <c>docset.yml</c> and <c>_docset.yml</c> on each
/// run — this is intentional coverage of the underscore-prefixed config path. If you see a
/// flaky test, this coin-flip is the first suspect. The chosen name is logged to the test output.
/// To fix the coin-flip, add <see cref="SetupOptions.ConfigurationFileName"/> and a dedicated
/// scenario per variant; do not remove the randomisation in this PR.
/// </summary>
public static class DocSetYamlGenerator
{
	private const string StubMarkdown = """
		---
		navigation_title: Stub
		---
		# Stub
		""";

	/// <summary>
	/// Generates the <c>docset.yml</c> / <c>_docset.yml</c> and redirect files in the mock file system.
	/// Called once per test class, from inside <see cref="AuthoringGenerator.GenerateAsync"/>.
	/// </summary>
	public static void Generate(MockFileSystem fileSystem, IReadOnlyCollection<string>? docsetProducts)
	{
		var repoRoot = Paths.WorkingDirectoryRoot.FullName;
		var mockDocsRoot = fileSystem.DirectoryInfo.New(Path.Join(repoRoot, "docs"));

		var realRedirectsPath = Path.Join(mockDocsRoot.FullName, "_redirects.yml");
		var redirectYaml = File.ReadAllText(realRedirectsPath);

		// Merge the complex test-specific redirect entries from docs-tests/redirects.yml.
		// The real _redirects.yml has simple testing/* → index.md entries for CI validation;
		// the complex entries (anchors, many:, etc.) are needed by cross-link redirect tests.
		// Strip the simple entries that would conflict with the complex ones, then append.
		var testRedirectYaml = File.ReadAllText(Path.Join(repoRoot, "docs-tests", "redirects.yml"));
		var testEntries = testRedirectYaml
			.Replace("redirects:\r\n", "")
			.Replace("redirects:\n", "")
			.Replace("'redirects/", "'testing/redirects/")
			.Replace("\"redirects/", "\"testing/redirects/")
			.Replace("!redirects/", "!testing/redirects/");

		// Collect the test entry keys so we can remove their simple duplicates from the real redirects.
		var testKeys = testEntries
			.Split('\n')
			.Select(line => line.TrimStart())
			.Where(t => t.StartsWith("'testing/", StringComparison.Ordinal) || t.StartsWith("\"testing/", StringComparison.Ordinal))
			.Select(t => t.Split(':')[0].Trim())
			.ToArray();

		var filtered = redirectYaml;
		foreach (var key in testKeys)
		{
			filtered = filtered.Replace($"  {key}: 'index.md'\r\n", "").Replace($"  {key}: 'index.md'\n", "");
		}

		var mergedRedirectYaml = filtered.TrimEnd() + "\n" + testEntries;

		// Write both _redirects.yml and redirects.yml so the correct one is found regardless
		// of whether the randomised config name is _docset.yml or docset.yml.
		fileSystem.AddFile(Path.Join(mockDocsRoot.FullName, "_redirects.yml"), new MockFileData(mergedRedirectYaml));
		fileSystem.AddFile(Path.Join(mockDocsRoot.FullName, "redirects.yml"), new MockFileData(mergedRedirectYaml));

		// Stub out every local redirect target that exists under the real docs/ tree so that
		// redirect-validation passes without copying production page content.
		CopyRedirectTargetsFromRealDocsIntoMock(fileSystem, mergedRedirectYaml, mockDocsRoot.FullName);

		// Build the docset YAML from every *.md present in the mock filesystem.
		var yaml = new StringWriter();
		yaml.WriteLine("cross_links:");
		yaml.WriteLine("  - docs-content");
		yaml.WriteLine("  - elasticsearch");
		yaml.WriteLine("  - kibana");
		yaml.WriteLine("exclude:");
		yaml.WriteLine("  - '_*.md'");

		if (docsetProducts is { Count: > 0 })
		{
			yaml.WriteLine("products:");
			foreach (var p in docsetProducts)
				yaml.WriteLine($"  - id: {p}");
		}

		yaml.WriteLine("toc:");
		foreach (var markdownFile in fileSystem.Directory.EnumerateFiles(mockDocsRoot.FullName, "*.md", SearchOption.AllDirectories))
		{
			var relative = fileSystem.Path.GetRelativePath(mockDocsRoot.FullName, markdownFile);
			// Skip files whose path contains a segment starting with '_' (matches F# exclusion logic)
			var segments = relative.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
			if (segments.Any(s => s.StartsWith('_')))
				continue;
			yaml.WriteLine($" - file: {relative}");
		}

		// Add the docs-tests redirect target pages.
		var docsTestsRoot = Path.Join(repoRoot, "docs-tests");
		var redirectFiles = new[] { "5th-page", "second-page", "third-page", "first-page" };
		foreach (var file in redirectFiles)
		{
			var relative = $"testing/redirects/{file}.md";
			yaml.WriteLine($" - file: {relative}");
			var mockPath = Path.Join(mockDocsRoot.FullName, relative);
			var realPath = Path.Join(docsTestsRoot, $"redirects/{file}.md");
			var contents = File.ReadAllText(realPath);
			fileSystem.AddFile(mockPath, new MockFileData(contents));
		}

		// Randomise the config filename to cover both _docset.yml and docset.yml paths.
		// If you see a flaky failure, this is the first suspect — see XML doc on this class.
		var configName = Random.Shared.Next(0, 10) % 2 == 0 ? "_docset.yml" : "docset.yml";
		fileSystem.AddFile(Path.Join(mockDocsRoot.FullName, configName), new MockFileData(yaml.ToString()));

		// Keep redirects consistent with the config name.
		var redirectsName = configName.StartsWith('_') ? "_redirects.yml" : "redirects.yml";
		fileSystem.AddFile(Path.Join(mockDocsRoot.FullName, redirectsName), new MockFileData(mergedRedirectYaml));

		// Log chosen name so flakes are diagnosable.
		TestContext.Current?.Output.WriteLine($"[DocSetYamlGenerator] Using config file: {configName}");
	}

	/// <summary>
	/// For each local redirect target in the merged redirects YAML, if the target path exists
	/// under the real <c>docs/</c> tree, stubs it in the mock filesystem.
	/// Content is a minimal stub — not a copy — so redirect validation passes without requiring
	/// production page content.
	/// </summary>
	private static void CopyRedirectTargetsFromRealDocsIntoMock(MockFileSystem fileSystem, string redirectYaml, string mockDocsRoot)
	{
		var targets = CollectRedirectTargetPaths(redirectYaml);
		var repoRoot = Paths.WorkingDirectoryRoot.FullName;

		foreach (var rel in targets)
		{
			var normalized = rel.Replace('/', Path.DirectorySeparatorChar);
			var destPath = Path.Join(mockDocsRoot, normalized);

			// Tests supply their own minimal pages; do not replace them with production stubs.
			if (fileSystem.File.Exists(destPath))
				continue;

			var sourcePath = Path.Join(repoRoot, "docs", normalized);

			// For testing/* paths also check docs-tests/ (strip the testing/ prefix).
			var altSourcePath = rel.StartsWith("testing/", StringComparison.Ordinal)
				? Path.Join(repoRoot, "docs-tests", rel["testing/".Length..].Replace('/', Path.DirectorySeparatorChar))
				: string.Empty;

			if (File.Exists(sourcePath) || (altSourcePath.Length > 0 && File.Exists(altSourcePath)))
				fileSystem.AddFile(destPath, new MockFileData(StubMarkdown));
		}
	}

	/// <summary>
	/// Walks the YAML redirect rules and collects every unique local redirect target path.
	/// Port of <c>RedirectMockTargets.collectRedirectTargetPaths</c> from Setup.fs.
	/// </summary>
	private static IReadOnlySet<string> CollectRedirectTargetPaths(string yamlText)
	{
		var stream = new YamlStream();
		using var reader = new StringReader(yamlText);
		stream.Load(reader);

		if (stream.Documents.Count == 0)
			return new HashSet<string>();

		if (stream.Documents[0].RootNode is not YamlMappingNode rootMap)
			return new HashSet<string>();

		var redirectsMap = rootMap
			.Children
			.OfType<KeyValuePair<YamlNode, YamlNode>>()
			.Select(kv => kv)
			.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == "redirects")
			.Value
			as YamlMappingNode;

		if (redirectsMap is null)
			return new HashSet<string>();

		var result = new HashSet<string>(StringComparer.Ordinal);
		foreach (var kv in redirectsMap.Children)
		{
			var fromKey = kv.Key is YamlScalarNode sk ? sk.Value ?? string.Empty : string.Empty;
			var value = kv.Value;

			switch (value)
			{
				case YamlScalarNode scalar:
					var scalarTarget = string.IsNullOrEmpty(scalar.Value) ? "index.md" : scalar.Value;
					AddLocal(scalarTarget, result);
					break;

				case YamlMappingNode mapping:
					string? toVal = null;
					YamlSequenceNode? manyNode = null;

					foreach (var child in mapping.Children)
					{
						if (child.Key is not YamlScalarNode childKey)
							continue;
						switch (childKey.Value)
						{
							case "to":
								if (child.Value is YamlScalarNode toScalar)
									toVal = toScalar.Value;
								break;
							case "many":
								if (child.Value is YamlSequenceNode seq)
									manyNode = seq;
								break;
						}
					}

					if (toVal is not null)
						AddLocal(toVal, result);
					else if (manyNode is null)
						AddLocal(fromKey, result);

					if (manyNode is not null)
					{
						foreach (var manyTarget in CollectManyTargets(fromKey, manyNode))
							AddLocal(manyTarget, result);
					}
					break;
			}
		}
		return result;
	}

	private static IEnumerable<string> CollectManyTargets(string outerFromKey, YamlSequenceNode seq)
	{
		foreach (var node in seq.Children)
		{
			if (node is not YamlMappingNode m)
				continue;

			string? toVal = null;
			var hasAnchors = false;

			foreach (var kv in m.Children)
			{
				if (kv.Key is not YamlScalarNode k)
					continue;
				switch (k.Value)
				{
					case "to":
						if (kv.Value is YamlScalarNode ts)
							toVal = ts.Value;
						break;
					case "anchors":
						hasAnchors = true;
						break;
				}
			}

			yield return toVal ?? (hasAnchors ? outerFromKey : outerFromKey);
		}
	}

	private static void AddLocal(string path, HashSet<string> set)
	{
		if (string.IsNullOrWhiteSpace(path))
			return;
		var t = path.TrimStart('!');
		// Skip URLs — only local paths
		if (!t.Contains("://", StringComparison.Ordinal))
			set.Add(t);
	}
}
