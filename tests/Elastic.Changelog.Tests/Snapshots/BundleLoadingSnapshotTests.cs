// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Snapshots;
using Nullean.Snapshots.Json;

namespace Elastic.Changelog.Tests.Snapshots;

/// <summary>
/// Snapshots what <see cref="BundleLoader"/> makes of a set of bundle files: loaded from a folder, loaded from content in the
/// order given, and merged by target. Amend sidecars, invalid entries and file orders are recorded as they behave today.
/// </summary>
public class BundleLoadingSnapshotTests
{
	private const string Folder = "/docs/releases";

	private static string Run(params (string Name, string Yaml)[] files)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.Directory.CreateDirectory(Folder);
		foreach (var (name, yaml) in files)
			fileSystem.File.WriteAllText($"{Folder}/{name}", yaml);

		var loader = new BundleLoader(fileSystem);

		var folderWarnings = new List<string>();
		var fromFolder = loader.LoadBundles(Folder, folderWarnings.Add);

		var contentWarnings = new List<string>();
		var fromContent = loader.LoadBundlesFromContent([.. files.Select(f => (f.Name, f.Yaml))], contentWarnings.Add);

		var root = new JsonObject
		{
			["fromFolder"] = Describe(fromFolder, folderWarnings),
			["fromContentInGivenOrder"] = Describe(fromContent, contentWarnings),
			["mergedByTarget"] = Describe(BundleLoader.MergeBundlesByTarget(fromFolder), [])
		};
		return root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
	}

	// The mock file system roots paths at the current drive on Windows and separates them with backslashes.
	private static string NormalisePath(string path)
	{
		var normalised = path.Replace('\\', '/');
		return normalised.Length > 2 && normalised[1] == ':' ? normalised[2..] : normalised;
	}

	private static JsonObject Describe(IReadOnlyList<LoadedBundle> bundles, List<string> warnings) =>
		new()
		{
			["warnings"] = new JsonArray([.. warnings.Select(w => (JsonNode?)JsonValue.Create(w))]),
			["bundles"] = new JsonArray([
				.. bundles.Select(
					b =>
						(JsonNode?)new JsonObject
						{
							["version"] = b.Version,
							["repo"] = b.Repo,
							["owner"] = b.Owner,
							["filePath"] = NormalisePath(b.FilePath),
							["description"] = b.Data.Description,
							["releaseDate"] = b.Data.ReleaseDate?.ToString("yyyy-MM-dd"),
							["gitRef"] = b.Data.GitRef,
							["hideFeatures"] = new JsonArray([.. b.HideFeatures.Select(h => (JsonNode?)JsonValue.Create(h))]),
							["products"] = System.Text.Json.Nodes.JsonNode.Parse(ChangelogConfigurationDump.ToJson(b.Data.Products)),
							["rawEntryFiles"] = new JsonArray([
								.. b.Data.Entries.Select(e => (JsonNode?)JsonValue.Create(e.File?.Name ?? e.Title))
							]),
							["entries"] = new JsonArray([
								.. b.Entries.Select(
									e =>
										(JsonNode?)JsonValue.Create(
											$"{e.Type}: {e.Title} prs=[{string.Join(',', e.Prs ?? [])}] subtype={e.Subtype}"
										)
								)
							])
						}
				)
			])
		};

	private static string Entry(string title, string type = "feature", string? file = null, string? checksum = null, string? pr = null) =>
		$"""
		  - type: {type}
		    title: {title}
		{(file is null ? "" : $"    file:\n      name: {file}\n" + (checksum is null ? "" : $"      checksum: {checksum}\n"))}{(pr is null ? "" : $"    prs:\n      - \"{pr}\"\n")}
		""";

	private static string Bundle(string product, string target, string? extra = null, params string[] entries) =>
		$"products:\n  - product: {product}\n    target: {target}\n{extra}entries:\n{string.Join("", entries)}";

	[Test]
	public void SingleProduct() =>
		Run(
			("elasticsearch-9.3.0.yaml", Bundle(
				"elasticsearch",
				"9.3.0",
				null,
				Entry("Add a thing", pr: "100"),
				Entry("Fix a thing", "bug-fix", pr: "101")
			))
		).Should().MatchJsonSnapshot();

	[Test]
	public void MultipleProductsInOneBundleTakesVersionAndRepoFromTheFirst() =>
		Run(
			("multi.yaml", """
				products:
				  - product: kibana
				    target: 9.3.0
				  - product: elasticsearch
				    target: 9.2.0
				entries:
				  - type: feature
				    title: Shared
				""")
		).Should().MatchJsonSnapshot();

	[Test]
	public void ProductWithExplicitRepoAndOwner() =>
		Run(
			("repo.yaml", """
				products:
				  - product: elasticsearch
				    target: 9.3.0
				    repo: example-repo
				    owner: example-owner
				entries:
				  - type: feature
				    title: Entry
				""")
		).Should().MatchJsonSnapshot();

	[Test]
	public void BundleWithoutProductsUsesTheFileNameAsVersion() =>
		Run(("9.9.9.yaml", "entries:\n  - type: feature\n    title: No products\n")).Should().MatchJsonSnapshot();

	[Test]
	public void EmptyBundleFile() => Run(("empty.yaml", "entries: []\n")).Should().MatchJsonSnapshot();

	[Test]
	public void YamlAndYmlExtensionsAreBothLoaded() =>
		Run(
			("a-9.1.0.yml", Bundle("elasticsearch", "9.1.0", null, Entry("From yml"))),
			("b-9.2.0.yaml", Bundle("elasticsearch", "9.2.0", null, Entry("From yaml")))
		).Should().MatchJsonSnapshot();

	[Test]
	public void ReleaseDateGitRefDescriptionAndHideFeatures() =>
		Run(
			("full.yaml", """
				products:
				  - product: cloud-serverless
				    target: 2025-06-02
				description: |
				  Intro line
				  Second line
				release-date: 2025-06-02
				git_ref: abc1234
				hide-features:
				  - flag-a
				  - flag-b
				entries:
				  - type: feature
				    title: Hidden thing
				    feature-id: flag-a
				""")
		).Should().MatchJsonSnapshot();

	[Test]
	public void SameTargetInDifferentRepos_FilesInAlphabeticalOrder() =>
		Run(
			("a-kibana-9.3.0.yaml", SameTarget("kibana", "example-repo-a", "First description", "2025-01-02", "From a")),
			("b-elasticsearch-9.3.0.yaml", SameTarget("elasticsearch", "example-repo-b", "Second description", "2025-01-03", "From b"))
		).Should().MatchJsonSnapshot();

	[Test]
	public void SameTargetInDifferentRepos_FilesInReverseOrder() =>
		Run(
			("b-elasticsearch-9.3.0.yaml", SameTarget("elasticsearch", "example-repo-b", "Second description", "2025-01-03", "From b")),
			("a-kibana-9.3.0.yaml", SameTarget("kibana", "example-repo-a", "First description", "2025-01-02", "From a"))
		).Should().MatchJsonSnapshot();

	[Test]
	public void SameTargetSameRepoThreeBundlesWithoutDescriptions() =>
		Run(
			("one.yaml", Bundle("elasticsearch", "9.3.0", null, Entry("One"))),
			("two.yaml", Bundle("elasticsearch", "9.3.0", null, Entry("Two"))),
			("three.yaml", Bundle("elasticsearch", "9.3.0", "release-date: 2025-02-02\n", Entry("Three")))
		).Should().MatchJsonSnapshot();

	private static string SameTarget(string product, string repo, string description, string releaseDate, string title) =>
		$"products:\n  - product: {product}\n    target: 9.3.0\n    repo: {repo}\ndescription: {description}\nrelease-date: {releaseDate}\nentries:\n  - type: feature\n    title: {title}\n";

	[Test]
	public void TargetsOfEveryKindSortNewestFirstWhenMerged() =>
		Run(
			("v-9.2.0.yaml", Bundle("elasticsearch", "9.2.0", null, Entry("v9.2.0"))),
			("v-9.10.0.yaml", Bundle("elasticsearch", "9.10.0", null, Entry("v9.10.0"))),
			("v-9.3.0-beta.1.yaml", Bundle("elasticsearch", "9.3.0-beta.1", null, Entry("v9.3.0-beta.1"))),
			("v-9.3.0.yaml", Bundle("elasticsearch", "9.3.0", null, Entry("v9.3.0"))),
			("v-2025-06-02.yaml", Bundle("cloud-serverless", "2025-06-02", null, Entry("serverless june"))),
			("v-2025-11-18.yaml", Bundle("cloud-serverless", "2025-11-18", null, Entry("serverless november"))),
			("v-not-a-version.yaml", Bundle("elasticsearch", "not-a-version", null, Entry("garbage target")))
		).Should().MatchJsonSnapshot();

	[Test]
	public void EntriesWithoutInlineContentAreSkippedWithAWarning() =>
		Run(
			("invalid.yaml", """
				products:
				  - product: elasticsearch
				    target: 9.3.0
				entries:
				  - type: feature
				    title: Valid
				  - title: Missing type
				  - type: bug-fix
				  - type: bug-fix
				    title: "   "
				    file:
				      name: whitespace-title.yaml
				      checksum: abc
				  - file:
				      name: file-only.yaml
				      checksum: def
				  - type: docs
				    title: Also valid
				""")
		).Should().MatchJsonSnapshot();

	[Test]
	public void UnparseableYamlIsSkippedWithAWarningAndTheRestStillLoads() =>
		Run(("broken.yaml", "products: [unclosed\n"), ("fine.yaml", Bundle("elasticsearch", "9.3.0", null, Entry("Fine"))))
			.Should()
			.MatchJsonSnapshot();

	[Test]
	public void BundleWithUnknownEntryType() =>
		Run(("odd.yaml", Bundle("elasticsearch", "9.3.0", null, Entry("Odd", "mystery")))).Should().MatchJsonSnapshot();

	private static string Parent() =>
		Bundle(
			"elasticsearch",
			"9.3.0",
			"description: Parent intro\n",
			Entry("Keep", file: "keep.yaml", checksum: "aaa"),
			Entry("Drop me", file: "drop.yaml", checksum: "bbb"),
			Entry("Also keep", "bug-fix", file: "other.yaml", checksum: "ccc")
		);

	private static string Amend(
		string? description = null,
		string? excludeFile = null,
		string? excludeChecksum = null,
		params string[] entries
	) =>
		$"products:\n  - product: elasticsearch\n    target: 9.3.0\n{(description is null ? "" : $"description: \"{description}\"\n")}"
			+ (excludeFile is null
				? ""
				: $"exclude-entries:\n  - file:\n      name: {excludeFile}\n{(excludeChecksum is null ? "" : $"      checksum: {excludeChecksum}\n")}")
			+ (entries.Length == 0 ? "" : $"entries:\n{string.Join("", entries)}");

	[Test]
	public void AmendExcludesAndAdds() =>
		Run(
			("elasticsearch-9.3.0.yaml", Parent()),
			("elasticsearch-9.3.0.amend-1.yaml", Amend(null, "drop.yaml", "bbb", Entry("Added later", file: "added.yaml", checksum: "ddd")))
		).Should().MatchJsonSnapshot();

	[Test]
	public void AmendsWrittenOutOfOrderApplyInNumericOrder() =>
		Run(
			("elasticsearch-9.3.0.yaml", Parent()),
			("elasticsearch-9.3.0.amend-10.yaml", Amend("Ten", null, null, Entry("From ten", file: "ten.yaml", checksum: "t"))),
			("elasticsearch-9.3.0.amend-2.yaml", Amend("Two", "keep.yaml")),
			("elasticsearch-9.3.0.amend-1.yaml", Amend("One", null, null, Entry("From one", file: "keep.yaml", checksum: "aaa")))
		).Should().MatchJsonSnapshot();

	[Test]
	public void RepeatedExclusionOfTheSameEntryByTwoAmends() =>
		Run(
			("elasticsearch-9.3.0.yaml", Parent()),
			("elasticsearch-9.3.0.amend-1.yaml", Amend(null, "drop.yaml", "bbb")),
			("elasticsearch-9.3.0.amend-2.yaml", Amend(null, "drop.yaml", "bbb"))
		).Should().MatchJsonSnapshot();

	[Test]
	public void AmendReAddsAnEntryAnEarlierAmendExcluded() =>
		Run(
			("elasticsearch-9.3.0.yaml", Parent()),
			("elasticsearch-9.3.0.amend-1.yaml", Amend(null, "drop.yaml", "bbb")),
			("elasticsearch-9.3.0.amend-2.yaml", Amend(null, null, null, Entry("Back again", file: "drop.yaml", checksum: "bbb")))
		).Should().MatchJsonSnapshot();

	[Test]
	public void ExclusionWithAChecksumThatDoesNotMatchKeepsTheEntry() =>
		Run(("elasticsearch-9.3.0.yaml", Parent()), ("elasticsearch-9.3.0.amend-1.yaml", Amend(null, "drop.yaml", "different")))
			.Should()
			.MatchJsonSnapshot();

	[Test]
	public void ExclusionWithoutAChecksumMatchesByFileNameOnly() =>
		Run(("elasticsearch-9.3.0.yaml", Parent()), ("elasticsearch-9.3.0.amend-1.yaml", Amend(null, "drop.yaml")))
			.Should()
			.MatchJsonSnapshot();

	[Test]
	public void AmendDescriptionPatches_EmptyStringClearsTheIntro() =>
		Run(("elasticsearch-9.3.0.yaml", Parent()), ("elasticsearch-9.3.0.amend-1.yaml", Amend(""))).Should().MatchJsonSnapshot();

	[Test]
	public void AmendDescriptionPatches_OmittedInheritsAndLaterReplaces() =>
		Run(
			("elasticsearch-9.3.0.yaml", Parent()),
			("elasticsearch-9.3.0.amend-1.yaml", Amend(null, null, null, Entry("No description patch", file: "n.yaml", checksum: "n"))),
			("elasticsearch-9.3.0.amend-2.yaml", Amend("Replaced intro"))
		).Should().MatchJsonSnapshot();

	[Test]
	public void NotesAmendAppliesAfterNumberedAmendsAndNeverPatchesTheDescription() =>
		Run(
			("elasticsearch-9.3.0.yaml", Parent()),
			("elasticsearch-9.3.0.amend-notes.yaml", Amend(
				"Ignored notes description",
				null,
				null,
				Entry("Notes entry", file: "notes.yaml", checksum: "n")
			)),
			("elasticsearch-9.3.0.amend-1.yaml", Amend("Numbered intro", "other.yaml"))
		).Should().MatchJsonSnapshot();

	[Test]
	public void AmendWithoutAParentStaysAsItsOwnBundle() =>
		Run(
			("elasticsearch-9.3.0.yaml", Parent()),
			("elasticsearch-9.4.0.amend-1.yaml", Amend(null, null, null, Entry("Orphan amend", file: "orphan.yaml", checksum: "o")))
		).Should().MatchJsonSnapshot();

	[Test]
	public void AmendsOfTwoDifferentParents() =>
		Run(
			("elasticsearch-9.3.0.yaml", Parent()),
			("kibana-9.3.0.yaml", Bundle("kibana", "9.3.0", null, Entry("Kibana", file: "kb.yaml", checksum: "k"))),
			("kibana-9.3.0.amend-1.yaml", Amend(null, "kb.yaml", "k")),
			("elasticsearch-9.3.0.amend-1.yaml", Amend(null, "drop.yaml", "bbb"))
		).Should().MatchJsonSnapshot();

	[Test]
	public void AmendMergerTable()
	{
		string[] paths =
		[
			"9.3.0.amend-1.yaml",
			"9.3.0.amend-12.yml",
			"9.3.0.AMEND-2.YAML",
			"9.3.0.amend-notes.yaml",
			"9.3.0.amend-0.yaml",
			"9.3.0.amend-1.yaml.bak",
			"9.3.0.yaml",
			"/releases/repo-9.3.0.amend-3.yaml",
			"C:\\releases\\repo-9.3.0.amend-4.yml",
			"x.amend-99999999999.yaml"
		];
		var lines = paths.Select(
			p =>
				$"{p} | amend={BundleAmendMerger.IsAmendFile(p)} notes={BundleAmendMerger.IsNotesAmendFile(p)} number={BundleAmendMerger.GetAmendFileNumber(p)} order={BundleAmendMerger.GetAmendMergeOrder(p)} parent={BundleAmendMerger.GetParentBundlePath(p) ?? "null"}"
		);
		string.Join('\n', lines).Should().MatchSnapshot();
	}
}
