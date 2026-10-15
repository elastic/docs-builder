// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogEntryValidatorTests
{
	private static readonly ChangelogConfiguration Config = ChangelogConfiguration.Default;

	private static ChangelogEntryDto ParseYaml(string yaml)
	{
		var normalized = ReleaseNotesSerialization.NormalizeYaml(yaml);
		return ReleaseNotesSerialization.GetEntryDeserializer().Deserialize<ChangelogEntryDto>(normalized) ?? new ChangelogEntryDto();
	}

	private static IReadOnlyList<EntryFileFinding> Validate(
		ChangelogEntryDto entry,
		ChangelogEntryType? labelDerivedType = null,
		IReadOnlySet<string>? knownProducts = null
	) => ChangelogEntryValidator.Validate("docs/changelog/42.yaml", entry, Config, labelDerivedType, knownProducts);

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// Title rules
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void Validate_MissingTitle_ProducesError()
	{
		var entry = ParseYaml("type: feature\nproducts:\n  - product: elasticsearch");
		var findings = Validate(entry);
		findings.Should().ContainSingle(f => f.Severity == FindingSeverity.Error && f.Message.Contains("title is required"));
	}

	[Test]
	public void Validate_TitleOver80Chars_ProducesWarning()
	{
		var longTitle = new string('x', 81);
		var entry = ParseYaml($"type: feature\ntitle: {longTitle}\nproducts:\n  - product: elasticsearch");
		var findings = Validate(entry);
		findings.Should().ContainSingle(f => f.Severity == FindingSeverity.Warning && f.Message.Contains("title exceeds 80 characters"));
	}

	[Test]
	public void Validate_TitleExactly80Chars_NoWarning()
	{
		var title = new string('x', 80);
		var entry = ParseYaml($"type: feature\ntitle: {title}\nproducts:\n  - product: elasticsearch");
		var findings = Validate(entry);
		findings.Should().NotContain(f => f.Message.Contains("title exceeds"));
	}

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// Products rules
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void Validate_MissingProducts_ProducesError()
	{
		var entry = ParseYaml("type: feature\ntitle: Test");
		var findings = Validate(entry);
		findings.Should().ContainSingle(f => f.Severity == FindingSeverity.Error && f.Message.Contains("products is required"));
	}

	[Test]
	public void Validate_UnknownProduct_ProducesError()
	{
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: no-such-product");
		var known = new HashSet<string>(["elasticsearch", "kibana"], StringComparer.OrdinalIgnoreCase);
		var findings = Validate(entry, knownProducts: known);
		findings.Should().ContainSingle(
			f => f.Severity == FindingSeverity.Error && f.Message.Contains("not in the list of available products")
		);
	}

	[Test]
	public void Validate_KnownProduct_NoProductError()
	{
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch");
		var known = new HashSet<string>(["elasticsearch", "kibana"], StringComparer.OrdinalIgnoreCase);
		var findings = Validate(entry, knownProducts: known);
		findings.Should().NotContain(f => f.Message.Contains("not in the list of available products"));
	}

	[Test]
	public void Validate_ProductVersionsSet_ProducesError()
	{
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch\n    versions: [\"9.2.0\"]");
		var findings = Validate(entry);
		findings.Should().ContainSingle(
			f => f.Severity == FindingSeverity.Error && f.Message.Contains("versions is only valid in changelog note files")
		);
	}

	[Test]
	public void Validate_InvalidLifecycle_ProducesError()
	{
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch\n    lifecycle: nonexistent-lifecycle");
		var findings = Validate(entry);
		findings.Should().ContainSingle(
			f => f.Severity == FindingSeverity.Error && f.Message.Contains("lifecycle") && f.Message.Contains("not valid")
		);
	}

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// Type rules
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void Validate_MissingType_ProducesError()
	{
		var entry = ParseYaml("title: Test\nproducts:\n  - product: elasticsearch");
		var findings = Validate(entry);
		findings.Should().ContainSingle(f => f.Severity == FindingSeverity.Error && f.Message.Contains("type is required"));
	}

	[Test]
	public void Validate_MissingType_WithLabelDerived_ProducesLabelAwareError()
	{
		var entry = ParseYaml("title: Test\nproducts:\n  - product: elasticsearch");
		var findings = Validate(entry, labelDerivedType: ChangelogEntryType.Feature);
		findings.Should().ContainSingle(f => f.Severity == FindingSeverity.Error && f.Message.Contains("type: feature"));
	}

	[Test]
	public void Validate_UnrecognisedType_ProducesError()
	{
		var entry = ParseYaml("type: not-a-type\ntitle: Test\nproducts:\n  - product: elasticsearch");
		var findings = Validate(entry);
		findings.Should().ContainSingle(f => f.Severity == FindingSeverity.Error && f.Message.Contains("not recognised"));
	}

	[Test]
	public void Validate_TypeMismatchesLabel_ProducesError()
	{
		var entry = ParseYaml("type: bug-fix\ntitle: Test\nproducts:\n  - product: elasticsearch");
		var findings = Validate(entry, labelDerivedType: ChangelogEntryType.Feature);
		findings.Should().ContainSingle(
			f => f.Severity == FindingSeverity.Error && f.Message.Contains("does not match label-derived type")
		);
	}

	[Test]
	public void Validate_TypeMatchesLabel_NoTypeMismatch()
	{
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch");
		var findings = Validate(entry, labelDerivedType: ChangelogEntryType.Feature);
		findings.Should().NotContain(f => f.Message.Contains("does not match"));
	}

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// Subtype rules
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void Validate_InvalidSubtype_ProducesError()
	{
		var entry = ParseYaml("type: breaking-change\ntitle: Test\nproducts:\n  - product: elasticsearch\nsubtype: not-a-subtype");
		var findings = Validate(entry);
		findings.Should().ContainSingle(
			f => f.Severity == FindingSeverity.Error && f.Message.Contains("subtype") && f.Message.Contains("not valid")
		);
	}

	[Test]
	public void Validate_SubtypeOnNonBreakingChange_ProducesWarning()
	{
		// Get a valid subtype from defaults
		var validSubtype = ChangelogConfiguration.DefaultSubtypes[0];
		var entry = ParseYaml($"type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch\nsubtype: {validSubtype}");
		var findings = Validate(entry);
		findings.Should().ContainSingle(
			f => f.Severity == FindingSeverity.Warning && f.Message.Contains("subtype is only expected on breaking-change")
		);
	}

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// Areas rules
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void Validate_InvalidArea_ProducesError()
	{
		var configWithAreas = Config with { Areas = ["query-dsl", "mappings"] };
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch\nareas:\n  - not-an-area");
		var findings = ChangelogEntryValidator.Validate("docs/changelog/42.yaml", entry, configWithAreas, null, null);
		findings.Should().ContainSingle(
			f => f.Severity == FindingSeverity.Error && f.Message.Contains("area") && f.Message.Contains("not valid")
		);
	}

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// Marker / link: rules
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void Validate_MarkerWithOtherFields_ProducesError()
	{
		var entry = ParseYaml("link: https://example.com\ntitle: Should not be here");
		var findings = Validate(entry);
		findings.Should().ContainSingle(f => f.Severity == FindingSeverity.Error && f.Message.Contains("marker entries"));
	}

	[Test]
	public void Validate_MarkerOnly_NoErrors()
	{
		var entry = ParseYaml("link: https://example.com");
		var findings = Validate(entry);
		findings.Should().NotContain(f => f.Severity == FindingSeverity.Error);
	}

	[Test]
	public void Validate_SourceRedirectInEntry_ProducesError()
	{
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch\nsource-redirect: true");
		var findings = Validate(entry);
		findings.Should().ContainSingle(f => f.Severity == FindingSeverity.Error && f.Message.Contains("source-redirect"));
	}

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// Description length
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void Validate_DescriptionOver600Chars_ProducesWarning()
	{
		var longDesc = new string('x', 601);
		var yaml = $"type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch\ndescription: |\n  {longDesc}";
		var entry = ParseYaml(yaml);
		var findings = Validate(entry);
		findings.Should().ContainSingle(
			f => f.Severity == FindingSeverity.Warning && f.Message.Contains("description exceeds 600 characters")
		);
	}

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// Valid entry
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void Validate_ValidMinimalEntry_NoFindings()
	{
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch");
		var findings = Validate(entry);
		findings.Should().BeEmpty();
	}

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// Allowed products (per-repo restriction)
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Fact]
	public void Validate_AllowedProducts_ProductInAllowedSet_NoError()
	{
		var knownProducts = new HashSet<string>(["elasticsearch", "kibana"], StringComparer.OrdinalIgnoreCase);
		var allowedProducts = new HashSet<string>(["elasticsearch"], StringComparer.OrdinalIgnoreCase);
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch");
		var findings = ChangelogEntryValidator.Validate(
			"docs/changelog/42.yaml",
			entry,
			Config,
			null,
			knownProducts,
			allowedProducts: allowedProducts
		);
		findings.Should().BeEmpty();
	}

	[Fact]
	public void Validate_AllowedProducts_ProductNotInAllowedSet_ProducesError()
	{
		var knownProducts = new HashSet<string>(["elasticsearch", "kibana"], StringComparer.OrdinalIgnoreCase);
		var allowedProducts = new HashSet<string>(["elasticsearch"], StringComparer.OrdinalIgnoreCase);
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: kibana");
		var findings = ChangelogEntryValidator.Validate(
			"docs/changelog/42.yaml",
			entry,
			Config,
			null,
			knownProducts,
			allowedProducts: allowedProducts
		);
		findings.Should().ContainSingle(f => f.Severity == FindingSeverity.Error && f.Message.Contains("not allowed for this repository"));
	}

	[Fact]
	public void Validate_AllowedProducts_Null_SkipsRestrictionCheck()
	{
		var knownProducts = new HashSet<string>(["elasticsearch", "kibana"], StringComparer.OrdinalIgnoreCase);
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: kibana");
		var findings = ChangelogEntryValidator.Validate(
			"docs/changelog/42.yaml",
			entry,
			Config,
			null,
			knownProducts,
			allowedProducts: null
		);
		findings.Should().BeEmpty();
	}

	[Fact]
	public void Validate_AllowedProducts_UnknownProductDoesNotAlsoFireAllowedError()
	{
		// When a product is unknown (not in knownProducts), only the "not in products.yml" error fires,
		// not the allowed-products error — the else-if ensures mutual exclusion.
		var knownProducts = new HashSet<string>(["elasticsearch"], StringComparer.OrdinalIgnoreCase);
		var allowedProducts = new HashSet<string>(["elasticsearch"], StringComparer.OrdinalIgnoreCase);
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: totally-unknown");
		var findings = ChangelogEntryValidator.Validate(
			"docs/changelog/42.yaml",
			entry,
			Config,
			null,
			knownProducts,
			allowedProducts: allowedProducts
		);
		findings.Should().ContainSingle(f => f.Message.Contains("not in the list of available products"));
		findings.Should().NotContain(f => f.Message.Contains("not allowed for this repository"));
	}

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// Filename validation
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	[Arguments("docs/changelog/42.yaml")]
	[Arguments("docs/changelog/42.yml")]
	[Arguments("docs/changelog/42-my-feature.yaml")]
	[Arguments("docs/changelog/1234-fix-something-long.yaml")]
	public void ValidateFilename_ValidConvention_NoFindings(string filePath)
	{
		var findings = ChangelogEntryValidator.ValidateFilename(filePath);
		findings.Should().BeEmpty();
	}

	[Test]
	[Arguments("docs/changelog/my-feature.yaml")]
	[Arguments("docs/changelog/changelog.yaml")]
	[Arguments("docs/changelog/fix42.yaml")]
	public void ValidateFilename_InvalidConvention_ProducesError(string filePath)
	{
		var findings = ChangelogEntryValidator.ValidateFilename(filePath);
		findings.Should().ContainSingle(f => f.Severity == FindingSeverity.Error && f.Message.Contains("must start with a PR number"));
	}

	[Test]
	[Arguments("docs/changelog/42.yaml", 42)]
	[Arguments("docs/changelog/100-feature.yaml", 100)]
	[Arguments("docs/changelog/9999-x.yml", 9999)]
	public void TryParseFilenameAsPrNumber_ValidFilename_ReturnsNumber(string filePath, int expected)
	{
		ChangelogEntryValidator.TryParseFilenameAsPrNumber(filePath, out var prNumber).Should().BeTrue();
		prNumber.Should().Be(expected);
	}

	[Test]
	[Arguments("docs/changelog/feature.yaml")]
	[Arguments("docs/changelog/changelog.yml")]
	public void TryParseFilenameAsPrNumber_InvalidFilename_ReturnsFalse(string filePath) =>
		ChangelogEntryValidator.TryParseFilenameAsPrNumber(filePath, out _).Should().BeFalse();

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	// pr: field cross-check against filename
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void Validate_PrFieldMatchesFilename_NoError()
	{
		var entry = ParseYaml("pr: 42\ntype: feature\ntitle: Test\nproducts:\n  - product: elasticsearch");
		var findings = ChangelogEntryValidator.Validate("docs/changelog/42.yaml", entry, Config, null, null, filenamePrNumber: 42);
		findings.Should().NotContain(f => f.Message.Contains("does not match"));
	}

	[Test]
	public void Validate_PrFieldMismatchesFilename_ProducesError()
	{
		var entry = ParseYaml("pr: 99\ntype: feature\ntitle: Test\nproducts:\n  - product: elasticsearch");
		var findings = ChangelogEntryValidator.Validate("docs/changelog/42.yaml", entry, Config, null, null, filenamePrNumber: 42);
		findings.Should().ContainSingle(
			f => f.Severity == FindingSeverity.Error && f.Message.Contains("does not match") && f.Message.Contains(
				"99"
			) && f.Message.Contains("42")
		);
	}

	[Test]
	public void Validate_PrFieldAbsent_NoFilenameError()
	{
		var entry = ParseYaml("type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch");
		var findings = ChangelogEntryValidator.Validate("docs/changelog/42.yaml", entry, Config, null, null, filenamePrNumber: 42);
		findings.Should().NotContain(f => f.Message.Contains("does not match"));
	}
}
