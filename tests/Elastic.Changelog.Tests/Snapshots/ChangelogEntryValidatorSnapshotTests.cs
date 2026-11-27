// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Snapshots;

/// <summary>
/// One snapshot per validation context: each case's YAML, then the findings the validator returns for it, in order.
/// A finding the validator does not raise today is as much part of the record as one it does.
/// </summary>
public class ChangelogEntryValidatorSnapshotTests : ChangelogTestBase
{
	private const string FilePath = "docs/changelog/42.yaml";

	private static readonly string LongTitle = new('t', 81);
	private static readonly string LongDescription = new('d', 601);

	private static readonly (string Name, string Yaml)[] Cases =
	[
		("valid minimal", "type: feature\ntitle: A title\nproducts:\n  - product: elasticsearch"),
		("valid full", "type: breaking-change\nsubtype: api\ntitle: A title\ndescription: A description\nimpact: Impact\naction: Action\nproducts:\n  - product: elasticsearch\n    lifecycle: ga\nareas:\n  - Search\nprs:\n  - \"42\""),
		("empty document", ""),
		("missing title", "type: feature\nproducts:\n  - product: elasticsearch"),
		("whitespace title", "type: feature\ntitle: \"   \"\nproducts:\n  - product: elasticsearch"),
		("title of 80 characters", $"type: feature\ntitle: {new string('t', 80)}\nproducts:\n  - product: elasticsearch"),
		($"title of 81 characters", $"type: feature\ntitle: {LongTitle}\nproducts:\n  - product: elasticsearch"),
		("description of 601 characters", $"type: feature\ntitle: T\ndescription: {LongDescription}\nproducts:\n  - product: elasticsearch"),
		("missing products", "type: feature\ntitle: T"),
		("empty products list", "type: feature\ntitle: T\nproducts: []"),
		("product without an id", "type: feature\ntitle: T\nproducts:\n  - lifecycle: ga"),
		("unknown product", "type: feature\ntitle: T\nproducts:\n  - product: example-missing-product"),
		("product with underscore", "type: feature\ntitle: T\nproducts:\n  - product: cloud_hosted"),
		("product in upper case", "type: feature\ntitle: T\nproducts:\n  - product: Elasticsearch"),
		("two products, one unknown", "type: feature\ntitle: T\nproducts:\n  - product: elasticsearch\n  - product: example-missing-product"),
		("obsolete product target", "type: feature\ntitle: T\nproducts:\n  - product: elasticsearch\n    target: 9.3.0"),
		("product versions on an entry", "type: feature\ntitle: T\nproducts:\n  - product: elasticsearch\n    versions: [9.3.0]"),
		("lifecycle in upper case", "type: feature\ntitle: T\nproducts:\n  - product: elasticsearch\n    lifecycle: GA"),
		("invalid lifecycle", "type: feature\ntitle: T\nproducts:\n  - product: elasticsearch\n    lifecycle: stable"),
		("missing type", "title: T\nproducts:\n  - product: elasticsearch"),
		("unknown type", "type: chore\ntitle: T\nproducts:\n  - product: elasticsearch"),
		("type in upper case", "type: FEATURE\ntitle: T\nproducts:\n  - product: elasticsearch"),
		("subtype on a breaking change", "type: breaking-change\nsubtype: api\ntitle: T\nproducts:\n  - product: elasticsearch"),
		("subtype on a feature", "type: feature\nsubtype: api\ntitle: T\nproducts:\n  - product: elasticsearch"),
		("subtype that does not exist", "type: breaking-change\nsubtype: cosmetic\ntitle: T\nproducts:\n  - product: elasticsearch"),
		("subtype without a type", "subtype: api\ntitle: T\nproducts:\n  - product: elasticsearch"),
		("pr that differs from the file name, no hash", "type: feature\ntitle: T\npr: \"7\"\nproducts:\n  - product: elasticsearch"),
		("pr that is not a number", "type: feature\ntitle: T\npr: seven\nproducts:\n  - product: elasticsearch"),
		("pr with a hash", "type: feature\ntitle: T\npr: \"#42\"\nproducts:\n  - product: elasticsearch"),
		("pr that differs from the file name", "type: feature\ntitle: T\npr: \"43\"\nproducts:\n  - product: elasticsearch"),
		("area that does not exist", "type: feature\ntitle: T\nareas:\n  - Nowhere\nproducts:\n  - product: elasticsearch"),
		("marker alone", "link: elastic/example-repo#42"),
		("marker with other fields", "link: elastic/example-repo#42\ntitle: T\ntype: feature\nproducts:\n  - product: elasticsearch\nprs:\n  - \"42\"\nissues:\n  - \"1\""),
		("marker carrying source-redirect", "link: elastic/example-repo#42\nsource-redirect: true"),
		("source-redirect on an entry", "type: feature\ntitle: T\nsource-redirect: true\nproducts:\n  - product: elasticsearch"),
		("everything wrong at once", "type: chore\ntitle: \"\"\nsubtype: cosmetic\nsource-redirect: true\npr: x\nproducts:\n  - lifecycle: stable\n  - product: example-missing-product\n    target: 1.0.0\nareas: [Nowhere]")
	];

	private static ChangelogEntryDto Parse(string yaml) =>
		SharpYaml.YamlSerializer.Deserialize<ChangelogEntryDto>(
			ReleaseNotesSerialization.NormalizeYaml(yaml),
			ReleaseNotesSerialization.GetEntryDeserializerOptions()
		)
			?? new ChangelogEntryDto();

	private static string Render(
		ChangelogConfiguration config,
		ChangelogEntryType? labelDerivedType = null,
		IReadOnlySet<string>? knownProducts = null,
		int? filenamePrNumber = 42
	)
	{
		var sb = new StringBuilder();
		foreach (var (name, yaml) in Cases)
		{
			var findings = ChangelogEntryValidator.Validate(
				FilePath,
				Parse(yaml),
				config,
				labelDerivedType,
				knownProducts,
				filenamePrNumber
			);
			_ = sb.Append("## ").Append(name).Append('\n');
			if (findings.Count == 0)
				_ = sb.Append("(no findings)\n");
			foreach (var f in findings)
				_ = sb
					.Append(f.Severity.ToString().ToLowerInvariant())
					.Append(' ')
					.Append(f.File)
					.Append(' ')
					.Append(f.Message)
					.Append('\n');
		}
		return sb.ToString();
	}

	private ChangelogConfiguration Restricted() =>
		new()
		{
			Products =
			[
				ConfigurationContext.Catalog.Products.Products["elasticsearch"],
				ConfigurationContext.Catalog.Products.Products["kibana"]
			],
			Areas = ["Search", "Security"],
			Lifecycles = [Lifecycle.Beta, Lifecycle.Ga]
		};

	private static readonly HashSet<string> Known = new(["elasticsearch", "kibana", "cloud-hosted"], StringComparer.OrdinalIgnoreCase);

	[Test]
	public void DefaultConfigurationWithoutProductList() => Render(ChangelogConfiguration.Default).Should().MatchSnapshot();

	[Test]
	public void DefaultConfigurationWithKnownProducts() => Render(ChangelogConfiguration.Default, null, Known).Should().MatchSnapshot();

	[Test]
	public void RestrictedConfigurationWithProductsAreasAndLifecycles() => Render(Restricted(), null, Known).Should().MatchSnapshot();

	[Test]
	public void LabelDerivedTypeIsBug_fix() =>
		Render(ChangelogConfiguration.Default, ChangelogEntryType.BugFix, Known).Should().MatchSnapshot();

	[Test]
	public void WithoutFilenamePrNumber() => Render(ChangelogConfiguration.Default, null, Known, null).Should().MatchSnapshot();

	[Test]
	public void FilenameTable()
	{
		string[] paths =
		[
			"docs/changelog/42.yaml",
			"docs/changelog/42-my-feature.yaml",
			"docs/changelog/42.yml",
			"docs/changelog/042.yaml",
			"docs/changelog/my-feature.yaml",
			"docs/changelog/42my.yaml",
			"docs/changelog/.yaml",
			"docs/changelog/42-.yaml",
			"docs/changelog/-42.yaml",
			"docs/changelog/99999999999.yaml",
			"docs/changelog/42.yaml.bak",
			"docs/changelog/42"
		];
		var sb = new StringBuilder();
		foreach (var p in paths)
		{
			var ok = ChangelogEntryValidator.TryParseFilenameAsPrNumber(p, out var number);
			var findings = ChangelogEntryValidator.ValidateFilename(p);
			_ = sb
				.Append(p)
				.Append(" | parsed=")
				.Append(ok)
				.Append(" number=")
				.Append(number)
				.Append(" | ")
				.Append(findings.Count == 0 ? "ok" : string.Join("; ", findings.Select(f => $"{f.Severity}: {f.Message}")))
				.Append('\n');
		}
		sb.ToString().Should().MatchSnapshot();
	}
}
