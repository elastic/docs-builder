// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Navigation.Floating;
using Elastic.Documentation.Testing;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

/// <summary>
/// The site navigation of the frozen shipped config shape (<c>Baseline/</c>) over stub docsets. Each test builds the whole
/// site once and snapshots one view of it. Locks today's behaviour ahead of the domain model refactor.
/// </summary>
public class BaselineSiteNavigationTests
{
	private static readonly BaselineOptions Prod = new();
	private static readonly BaselineOptions SkipPrivate = new() { SkipPrivateRepositories = true };
	private static readonly BaselineOptions Preview = new() { NavigationPreview = true };
	private static readonly BaselineOptions PreviewSkipPrivate = new() { NavigationPreview = true, SkipPrivateRepositories = true };

	[Test]
	public void Tree_NavigationFlagOff_MatchesSnapshot() =>
		NavigationDump.ToText(BaselineSite.Build(Prod).Navigation).Should().MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void Diagnostics_NavigationFlagOff_MatchesSnapshot() =>
		BaselineSite.Build(Prod).SiteContext.Diagnostics.ToSnapshotText().Should().MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void Tree_SkipPrivateRepositories_MatchesSnapshot() =>
		NavigationDump.ToText(BaselineSite.Build(SkipPrivate).Navigation).Should().MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void Diagnostics_SkipPrivateRepositories_MatchesSnapshot() =>
		BaselineSite.Build(SkipPrivate).SiteContext.Diagnostics.ToSnapshotText().Should().MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void Tree_NavigationPreview_MatchesSnapshot() =>
		NavigationDump.ToText(BaselineSite.Build(Preview).Navigation).Should().MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void Diagnostics_NavigationPreview_MatchesSnapshot() =>
		BaselineSite.Build(Preview).SiteContext.Diagnostics.ToSnapshotText().Should().MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void TopNav_NavigationPreview_MatchesSnapshot() =>
		BaselineDumps.TopNavToText(BaselineSite.Build(Preview).Navigation.TopNav).Should().MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void Tree_NavigationPreviewSkipPrivate_MatchesSnapshot() =>
		NavigationDump.ToText(BaselineSite.Build(PreviewSkipPrivate).Navigation).Should().MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void TopNav_NavigationPreviewSkipPrivate_MatchesSnapshot() =>
		BaselineDumps
			.TopNavToText(BaselineSite.Build(PreviewSkipPrivate).Navigation.TopNav)
			.Should()
			.MatchSnapshot(options: BaselineDumps.Scrubbed());

	[Test]
	public void Tree_NavigationPreviewWithAnApiSection_MountsTheSectionUnderTheHub() =>
		NavigationDump
			.ToText(BaselineSite.Build(PreviewWithApi()).Navigation)
			.Split('\n')
			.SkipWhile(l => !l.Contains("\"APIs\"", StringComparison.Ordinal))
			.Take(4)
			.Should()
			.BeEquivalentTo(
				[
					"  SectionNavigation  /docs/api  \"APIs\" [island]",
					"    index: SectionIndexLeaf  /docs/api  \"APIs\"",
					"    DemoRoot  /api/doc/demo  \"Demo API\" [island]",
					"      index: DemoLandingLeaf  /api/doc/demo  \"Demo API\""
				],
				o => o.WithStrictOrdering()
			);

	[Test]
	public void TopNav_NavigationPreviewWithAnApiSection_MatchesSnapshot() =>
		BaselineDumps
			.TopNavToText(BaselineSite.Build(PreviewWithApi()).Navigation.TopNav)
			.Should()
			.MatchSnapshot(options: BaselineDumps.Scrubbed());

	private static BaselineOptions PreviewWithApi() =>
		new() { NavigationPreview = true, FloatingSections = new FloatingSectionRegistry([new DemoProvider()]) };

	[Test]
	public void TopNav_NavigationFlagOff_IsAbsent() => BaselineSite.Build(Prod).Navigation.TopNav.Should().BeNull();
}
