// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Nullean.Entities;
using Nullean.ScopedFileSystem;

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

	// Found by generated data. The expected text of each of these is what origin/main reports for the same file.
	[Test]
	public void Configure_ApiEntryWithANulCharacterAfterAFinding_ReportsTheFindingThenFails() =>
		Configure(
			"""
			api:
			  a:
			    - spec: x
			  b:
			    - product: elasticsearch
			      spec: "a\0.json"
			  c:
			    - spec: y
			storybook:
			  registry: ${A}

			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml:6 API 'a' is missing required 'product:'. It must match a product id defined in products.yml.
			error /docs/docset.yml Could not load docset.yml: Null character in path. (Parameter 'path')
			throws ArgumentException: (message omitted)
			"""
			);

	[Test]
	public void Configure_ApiChildWithANulCharacterAfterAFinding_ReportsTheChildFindingThenFails() =>
		Configure(
			"""
			api:
			  b:
			    - product: elasticsearch
			      spec: s.json
			      children:
			        - file: missing.md
			        - file: "a\0.md"

			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml Child page 'missing.md' for API 'b' does not exist under 'api/b/'.
			error /docs/docset.yml Could not load docset.yml: Null character in path. (Parameter 'path')
			throws ArgumentException: (message omitted)
			"""
			);

	[Test]
	public void Configure_BrandingImageWithANulCharacterAfterAFinding_ReportsTheFindingThenFailsBeforeTheTemplates() =>
		Configure(
			"""
			branding:
			  icon: x.exe
			  og-image: "a\0.svg"
			cta:
			  b:
			    button:
			      label: L

			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml 'branding.icon' has unsupported extension '.exe'. Allowed: .svg, .png, .jpg, .jpeg, .gif, .webp, .ico
			error /docs/docset.yml Could not load docset.yml: Null character in path. (Parameter 'path')
			throws ArgumentException: (message omitted)
			"""
			);

	[Test]
	public void Configure_CtaBenefitsWithNoValueAfterAFinding_ReportsTheFindingThenFails() =>
		Configure(
			"""
			cta:
			  b:
			    button:
			      label: L
			  a:
			    button:
			      label: L
			      url: http://example.test
			    benefits:
			  c:
			    button:
			      label: L
			default_cta: q

			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml 'cta.b' must define both 'button.label' and 'button.url'.
			error /docs/docset.yml Could not load docset.yml: Object reference not set to an instance of an object.
			throws NullReferenceException: Object reference not set to an instance of an object.
			"""
			);

	[Test]
	public void Configure_CtaBenefitsWithNoValueAndAnUnusableButton_ReportsTheButtonAndGoesOn() =>
		Configure(
			"""
			cta:
			  a:
			    button:
			      label: L
			    benefits:
			  b:
			    button:
			      label: L
			      url: ftp://example.test
			    benefits:

			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml 'cta.a' must define both 'button.label' and 'button.url'.
			error /docs/docset.yml 'cta.b.button.url' must use http/https or a relative URL.
			model <ConfigurationFile>:
			"""
			);

	[Test]
	public void Configure_CrossLinksWithNoValueAndALaterFinding_FailsBeforeReportingIt() =>
		Configure("cross_links:\nrelease_notes:\n  - product: nope\napi:\n  p:\n    - spec: a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /docs/docset.yml Could not load docset.yml: Value cannot be null. (Parameter 'source')
			throws ArgumentNullException: (message omitted)
			"""
			);

	// the stage reports the resolutions it is given; it does not resolve the entries a second time
	[Test]
	public void Apis_GivenAResolution_ReportsThatResolution()
	{
		var docset = new DocumentationSetDefinition { SourceFile = null!, SourceDirectory = null! };
		ApiResolution rejected = new ApiRejected([Violation.Error("first"), Violation.Error("second")]);

		var findings = new DocumentationSetApis(docset, [rejected]).Check("docset.yml").Findings;

		findings.Select(f => f.Message).Should().Equal("first", "second");
	}

	// the resolver probes the files of an api entry once: its result is what the stage reports and what the configuration is built from
	[Test]
	public void Resolve_ApiEntry_IsResolvedOnce()
	{
		var yaml = "project: p\napi:\n  elasticsearch:\n    - spec: spec.json\n      product: elasticsearch\ntoc:\n  - file: index.md\n";
		var mock = DocsetGolden.CreateFileSystem(yaml, new DocFile("index.md"));
		var fileSystem = new MockCountingFileSystem(mock);
		var context = DocsetGolden.CreateContext(new DiagnosticsCollector([]), fileSystem);
		var definition = DocumentationSetLoader.Load(
			new DiagnosticsCollector([]),
			mock.FileInfo.New($"{DocsetGolden.Root}/docset.yml"),
			new ScopedFileSystem(mock, DocsetGolden.Root)
		).Value;

		_ = DocumentationSetResolver.Resolve(definition, context, GoldenConfig.LoadVersions(), GoldenConfig.LoadProducts());

		fileSystem.FileInfoPaths.Count(p => p.EndsWith("spec.json", StringComparison.Ordinal)).Should().Be(1);
	}

	/// <summary>A file system that records the path of every file info asked of it. "Mock" in the name keeps the checkout resolver from asking for a .git folder.</summary>
	private sealed class MockCountingFileSystem(IFileSystem inner) : IFileSystem
	{
		private readonly List<string> _paths = [];

		public IReadOnlyList<string> FileInfoPaths => _paths;

		public IFileInfoFactory FileInfo => new CountingFileInfoFactory(inner.FileInfo, _paths);
		public IFile File => inner.File;
		public IDirectory Directory => inner.Directory;
		public IFileStreamFactory FileStream => inner.FileStream;
		public IPath Path => inner.Path;
		public IDirectoryInfoFactory DirectoryInfo => inner.DirectoryInfo;
		public IDriveInfoFactory DriveInfo => inner.DriveInfo;
		public IFileSystemWatcherFactory FileSystemWatcher => inner.FileSystemWatcher;
		public IFileVersionInfoFactory FileVersionInfo => inner.FileVersionInfo;
		public IRandomAccess RandomAccess => inner.RandomAccess;
	}

	private sealed class CountingFileInfoFactory(IFileInfoFactory inner, List<string> paths) : IFileInfoFactory
	{
		public IFileSystem FileSystem => inner.FileSystem;

		public IFileInfo New(string fileName)
		{
			paths.Add(fileName);
			return inner.New(fileName);
		}

		public IFileInfo? Wrap(System.IO.FileInfo? fileInfo) => inner.Wrap(fileInfo);
	}

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
