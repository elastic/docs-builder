// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// The rules of a documentation set are reported in the order resolution reaches them, and an entry written with no value fails after the
/// findings that come before it. The expected text is what <c>origin/main</c> reports for the same file.
/// </summary>
public class DocumentationSetStagesTests
{
	private const string Toc = "project: p\ntoc:\n  - file: index.md\n";

	private static string Configure(string extra) => DocsetGolden.Configure(Toc + extra, true, [], new DocFile("index.md"));

	[Test]
	public void Configure_EveryKindOfFinding_ReportsInTheOrderResolutionReachesThem() =>
		Configure(
			"""
			cross_links: [foo://x]
			release_notes:
			  - product: nope
			api:
			  p:
			    - spec: a
			storybook:
			  registry: ${A}
			branding:
			  icon: x.exe
			cta:
			  a:
			    button:
			      label: L
			default_cta: q
			features:
			  primary-nav: true

			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml Cross-link 'foo://x' uses unknown registry 'foo'. Use 'public' or 'internal'.
				error /docs/docset.yml Unknown 'release_notes' product 'nope'. It must be a product id defined in products.yml.
				error /docs/docset.yml:9 API 'p' is missing required 'product:'. It must match a product id defined in products.yml.
				warning /docs/docset.yml 'storybook.registry' references environment variable 'A' which is not allow-listed for interpolation and is left literal. Allowed: KIBANA_STORYBOOK_REGISTRY.
				error /docs/docset.yml 'branding.icon' has unsupported extension '.exe'. Allowed: .svg, .png, .jpg, .jpeg, .gif, .webp, .ico
				error /docs/docset.yml 'cta.a' must define both 'button.label' and 'button.url'.
				error /docs/docset.yml 'default_cta: q' on page 'index.md' does not match any 'cta' template in docset.yml.
				error /docs/docset.yml 'features.primary-nav' cannot be used together with 'branding': the primary nav requires Elastic global navigation.
				model <ConfigurationFile>:
				"""
			);

	[Test]
	public void Configure_ReleaseNotesNullAfterAFinding_ReportsTheFindingThenFails() =>
		Configure("release_notes:\n  - product: nope\n  - \n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml Unknown 'release_notes' product 'nope'. It must be a product id defined in products.yml.
				error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
				throws NullReferenceException: Object reference not set to an instance of an object.
				"""
			);

	[Test]
	public void Configure_CtaNullAfterAFinding_ReportsTheFindingThenFailsBeforeLaterRules() =>
		Configure("cta:\n  a:\n    button:\n      label: L\n  b:\n\ndefault_cta: q\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml 'cta.a' must define both 'button.label' and 'button.url'.
				error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
				throws NullReferenceException: Object reference not set to an instance of an object.
				"""
			);

	[Test]
	public void Configure_CrossLinkFindingThenReleaseNotesWithNoValue_ReportsTheCrossLinkFirst() =>
		Configure("cross_links: [foo://x]\nrelease_notes:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml Cross-link 'foo://x' uses unknown registry 'foo'. Use 'public' or 'internal'.
			error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
			throws NullReferenceException: Object reference not set to an instance of an object.
			"""
			);

	[Test]
	public void Configure_ReleaseNotesWithNoValue_FailsWithANullReference() =>
		Configure("release_notes:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
		throws NullReferenceException: Object reference not set to an instance of an object.
		"""
			);

	[Test]
	public void Configure_NullReleaseNotesBeforeABadOne_FailsBeforeReportingLaterEntries() =>
		Configure("release_notes:\n  - \n  - product: nope\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
			throws NullReferenceException: Object reference not set to an instance of an object.
			"""
			);

	[Test]
	public void Configure_BadReleaseNotesNullBadReleaseNotes_ReportsOnlyTheEntryBeforeTheNull() =>
		Configure("release_notes:\n  - product: nope\n  - \n  - product: nada\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml Unknown 'release_notes' product 'nope'. It must be a product id defined in products.yml.
			error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
			throws NullReferenceException: Object reference not set to an instance of an object.
			"""
			);

	[Test]
	public void Configure_NullCtaBeforeABadOne_FailsBeforeReportingLaterTemplates() =>
		Configure("cta:\n  a:\n  b:\n    button:\n      label: L\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
			throws NullReferenceException: Object reference not set to an instance of an object.
			"""
			);

	[Test]
	public void Configure_ApiWithNoValue_FailsWithANullReferenceAfterEarlierFindings() =>
		Configure("release_notes:\n  - product: nope\napi:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml Unknown 'release_notes' product 'nope'. It must be a product id defined in products.yml.
			error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
			throws NullReferenceException: Object reference not set to an instance of an object.
			"""
			);

	[Test]
	public void Configure_ApiWithNoValueAndStorybook_FailsAtApiBeforeStorybookIsReported() =>
		Configure("storybook:\n  registry: ${A}\napi:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
			throws NullReferenceException: Object reference not set to an instance of an object.
			"""
			);

	[Test]
	public void Configure_ExtensionsWithNoValue_FailsBeforeApiAndLaterRules() =>
		Configure("extensions:\ncross_links: [foo://x]\napi:\n  p:\n    - spec: a\nbranding:\n  icon: x.exe\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /docs/docset.yml Cross-link 'foo://x' uses unknown registry 'foo'. Use 'public' or 'internal'.
				error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
				throws NullReferenceException: Object reference not set to an instance of an object.
				"""
			);

	[Test]
	public void Configure_FeaturesWithNoValueAndUnknownDefaultCta_ReportsTheDefaultCtaThenFails() =>
		Configure("default_cta: q\nbranding:\n  icon: x.svg\nfeatures:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml 'branding.icon' file 'x.svg' does not exist.
			error /docs/docset.yml 'default_cta: q' on page 'index.md' does not match any 'cta' template in docset.yml.
			error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
			throws NullReferenceException: Object reference not set to an instance of an object.
			"""
			);

	[Test]
	public void ResolveCrossLink_PublicSetLinkingToInternal_IsRejected()
	{
		var resolution = DocumentationSetRules.ResolveCrossLink("internal://x", DocSetRegistry.Public);

		resolution.Value.Should().BeOfType<CrossLinkRejected>();
	}

	[Test]
	public void ResolveCrossLink_InternalSetLinkingToInternal_IsAccepted()
	{
		var resolution = DocumentationSetRules.ResolveCrossLink("internal://x", DocSetRegistry.Internal);

		resolution.Value.Should().BeOfType<CrossLinkAccepted>().Which.Entry.Repository.Should().Be("x");
	}
}
