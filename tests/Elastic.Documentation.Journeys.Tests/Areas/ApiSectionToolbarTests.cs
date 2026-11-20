// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Journeys.Tests.Targets;
using Nullean.Cdp;
using Nullean.Snapshots;

namespace Elastic.Documentation.Journeys.Tests.Areas;

/// <summary>
/// An operation page has a row of links to its sections above the description, and the row stays under the page header
/// as the reader scrolls. Whether it sticks, and where, only real layout can tell: a unit test that supplies the offsets
/// would pass even if the row lost <c>position: sticky</c>. The Cloud Connect spec renders in under a second.
/// </summary>
[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class ApiSectionToolbarTests(JourneyTarget target)
{
	private const string Operation = "api/cloud-connect/operation/operation-patchcluster";
	private static readonly int[] int32Array = new[] { 200, 60, 20 };

	[Test]
	public async Task ScrollingThePage_PinsTheSectionLinksUnderTheHeader()
	{
		var report = string.Empty;
		await WithPageAsync(string.Empty, async page =>
		{
			var lines = new List<string> { $"at rest: {await DescribeAsync(page).ConfigureAwait(false)}" };

			// Where the row's top would be without sticking, in pixels below the top of the window. It pins at the header's bottom edge.
			foreach (var edge in int32Array)
			{
				await ScrollBarTopToAsync(page, edge).ConfigureAwait(false);
				lines.Add($"row top at {edge}px: {await DescribeAsync(page).ConfigureAwait(false)}");
			}

			_ = await page.EvaluateAsync("document.getElementById('responses').scrollIntoView(); true").ConfigureAwait(false);
			lines.Add($"at the last section: {await DescribeAsync(page).ConfigureAwait(false)}");
			report = string.Join('\n', lines);
		}).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"""
			at rest: visible True, pinned False, current section Overview, title shown False
			row top at 200px: visible True, pinned False, current section Overview, title shown False
			row top at 60px: visible True, pinned True, current section Overview, title shown True
			row top at 20px: visible True, pinned True, current section Overview, title shown True
			at the last section: visible True, pinned True, current section Responses, title shown True
			"""
		);
	}

	[Test]
	public async Task OpeningASectionFragment_PinsTheSectionLinksWithThatSectionCurrent()
	{
		var report = string.Empty;
		await WithPageAsync("#responses", async page => report = await DescribeAsync(page).ConfigureAwait(false)).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot("visible True, pinned True, current section Responses, title shown True");
	}

	[Test]
	public async Task ForcedColors_TheCurrentSectionKeepsAnUnderline()
	{
		var report = string.Empty;
		await WithPageAsync("#responses", async page =>
		{
			// Forced colors drop box-shadow, which draws the underline otherwise, so the current section needs a border there.
			await page.SendAsync("Emulation.setEmulatedMedia", writer =>
			{
				writer.WriteStartArray("features");
				writer.WriteStartObject();
				writer.WriteString("name", "forced-colors");
				writer.WriteString("value", "active");
				writer.WriteEndObject();
				writer.WriteEndArray();
			}).ConfigureAwait(false);
			await Task.Delay(300).ConfigureAwait(false);

			var state = await page.EvaluateAsync(
				"""
				(() => {
					const current = document.querySelector('.api-section-links-track a[aria-current]');
					const style = getComputedStyle(current);
					return {
						forced: matchMedia('(forced-colors: active)').matches,
						label: current.textContent.trim(),
						border: style.borderBottomStyle !== 'none' ? parseFloat(style.borderBottomWidth) : 0,
					};
				})()
				"""
			).ConfigureAwait(false);
			report = string.Create(
				CultureInfo.InvariantCulture,
				$"forced colors {state.GetProperty("forced").GetBoolean()}, current section {state.GetProperty("label").GetString()}, bottom border {state.GetProperty("border").GetDouble()}px"
			);
		}).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot("forced colors True, current section Responses, bottom border 2px");
	}

	[Test]
	public async Task OpeningAPropertyFragment_StopsItBelowTheToolbar()
	{
		var report = string.Empty;
		await WithPageAsync(string.Empty, async page =>
		{
			// A property well down the page, with room below it to scroll to its margin.
			var id = (await page.EvaluateAsync(
				"""
					(() => {
						const all = [...document.querySelectorAll('.property-item dt[id]')];
						const room = document.documentElement.scrollHeight - innerHeight;
						const at = (e) => e.getBoundingClientRect().top + scrollY;
						return (all.find(e => at(e) > 700 && at(e) < room) ?? all[all.length - 1]).id;
					})()
					"""
			).ConfigureAwait(false)).GetString();
			_ = await page.EvaluateAsync($"location.hash = {JsonSerializer.Serialize(id)}; true").ConfigureAwait(false);
			await Task.Delay(800).ConfigureAwait(false);

			var state = await page.EvaluateAsync(
				$$"""
				(() => {
					const property = document.getElementById({{JsonSerializer.Serialize(id)}});
					const toolbar = document.querySelector('.api-section-links');
					const top = property.getBoundingClientRect().top;
					const state = {
						toolbarVisible: getComputedStyle(toolbar).visibility === 'visible',
						atItsScrollMargin: Math.abs(top - parseFloat(getComputedStyle(property).scrollMarginTop)) < 2,
						clearOfToolbar: top >= toolbar.getBoundingClientRect().bottom,
					};
					const global = getComputedStyle(document.body).scrollMarginTop;
					// Another element with an id in the same section, measured while the toolbar class is on: a rule written too widely would reach it.
					const description = document.getElementById('description');
					state.descriptionKeepsGlobal = description !== null && getComputedStyle(description).scrollMarginTop === global;
					// Without the class the page has no toolbar, and a property keeps the margin every element has.
					document.getElementById('elastic-api-v3').classList.remove('api-has-toolbar');
					state.globalWithoutToolbar = getComputedStyle(property).scrollMarginTop === global;
					return state;
				})()
				"""
			).ConfigureAwait(false);
			report = string.Create(
				CultureInfo.InvariantCulture,
				$"toolbar visible {state.GetProperty("toolbarVisible").GetBoolean()}, property at its scroll margin {state.GetProperty("atItsScrollMargin").GetBoolean()}, " +
					$"property clear of the toolbar {state.GetProperty("clearOfToolbar").GetBoolean()}, " +
					$"global margin without the toolbar {state.GetProperty("globalWithoutToolbar").GetBoolean()}, " +
					$"description keeps the global margin {state.GetProperty("descriptionKeepsGlobal").GetBoolean()}"
			);
		}).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"toolbar visible True, property at its scroll margin True, property clear of the toolbar True, global margin without the toolbar True, description keeps the global margin True"
		);
	}

	[Test]
	public async Task PageShorterThanTheWindow_OpensWithOverviewCurrentAndExpandAllHidden()
	{
		var report = string.Empty;
		await WithPageAsync(string.Empty, async page =>
		{
			// A window far taller than the page. The layout still overflows it (the site header, the banner and the footer
			// surround a min-height of the window), so the page is never "at its bottom" at the top and the scroll-spy does
			// not jump to the last section.
			await page.SetViewportAsync(1280, 6000, 1).ConfigureAwait(false);
			await Task.Delay(800).ConfigureAwait(false);

			var state = await page.EvaluateAsync(
				"""
				(() => {
					const bar = document.querySelector('.api-section-links');
					return {
						current: [...bar.querySelectorAll('[aria-current]')].map(a => a.textContent.trim()).join(',') || 'none',
						expandAll: getComputedStyle(bar.querySelector('.api-expand-toggle')).visibility,
					};
				})()
				"""
			).ConfigureAwait(false);
			report = string.Create(
				CultureInfo.InvariantCulture,
				$"current section {state.GetProperty("current").GetString()}, row Expand all {state.GetProperty("expandAll").GetString()}"
			);
		}).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot("current section Overview, row Expand all hidden");
	}

	[Test]
	public async Task OpeningASupplementalHeadingFragment_StopsItBelowTheRow()
	{
		var report = string.Empty;
		await WithPageAsync(string.Empty, async page =>
		{
			// The Cloud Connect spec has no supplemental sections, so add one as the page renders it: a plain
			// h2.section-header after the operation's own sections, with room below it to scroll to its margin.
			_ = await page.EvaluateAsync(
				"""
				(() => {
					const section = document.getElementById('elastic-api-v3');
					const heading = document.createElement('h2');
					heading.className = 'section-header';
					heading.id = 'supplemental-guide';
					heading.innerHTML = '<a class="headerlink" href="#supplemental-guide">Guide</a>';
					const room = document.createElement('div');
					room.style.minHeight = '1500px';
					section.append(heading, room);
					return true;
				})()
				"""
			).ConfigureAwait(false);
			_ = await page.EvaluateAsync("location.hash = 'supplemental-guide'; true").ConfigureAwait(false);
			await Task.Delay(800).ConfigureAwait(false);

			var state = await page.EvaluateAsync(
				"""
				(() => {
					const heading = document.getElementById('supplemental-guide');
					const bar = document.querySelector('.api-section-links').getBoundingClientRect();
					const top = heading.getBoundingClientRect().top;
					const state = {
						rowOnScreen: bar.bottom > 0 && bar.top < innerHeight,
						clearOfRow: top >= bar.bottom,
						atItsScrollMargin: Math.abs(top - parseFloat(getComputedStyle(heading).scrollMarginTop)) < 2,
					};
					// Without the class the page has no row, as a single-section operation with supplemental sections does, and
					// the heading keeps the margin every element has.
					document.getElementById('elastic-api-v3').classList.remove('api-has-toolbar');
					state.globalWithoutRow = getComputedStyle(heading).scrollMarginTop === getComputedStyle(document.body).scrollMarginTop;
					return state;
				})()
				"""
			).ConfigureAwait(false);
			report = string.Create(
				CultureInfo.InvariantCulture,
				$"row on screen {state.GetProperty("rowOnScreen").GetBoolean()}, heading clear of the row {state.GetProperty("clearOfRow").GetBoolean()}, " +
					$"heading at its scroll margin {state.GetProperty("atItsScrollMargin").GetBoolean()}, " +
					$"global margin without the row {state.GetProperty("globalWithoutRow").GetBoolean()}"
			);
		}).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"row on screen True, heading clear of the row True, heading at its scroll margin True, global margin without the row True"
		);
	}

	[Test]
	public async Task AnyWindowWidth_TheRowsRuleMeetsTheSidebar()
	{
		var report = string.Empty;
		await WithPageAsync(string.Empty, async page =>
		{
			var lines = new List<string>();
			// 1100px has a 42px nav gap. Past 1920px the content is centred beside the sidebar, also when the reader widens it.
			foreach (var (width, sidebar) in new (int, string?)[] { (1100, null), (1600, null), (2560, null), (2560, "400px") })
			{
				await page.SetViewportAsync(width, 600, 1).ConfigureAwait(false);
				if (sidebar is not null)
					_ = await page.EvaluateAsync(
						$"document.querySelector('.api-page-shell').style.setProperty('--max-sidebar-width', '{sidebar}'); true"
					).ConfigureAwait(false);
				_ = await page.EvaluateAsync("window.scrollTo(0, 0); true").ConfigureAwait(false);
				var atRest = await SidebarGapAsync(page).ConfigureAwait(false);
				await ScrollBarTopToAsync(page, 20).ConfigureAwait(false);
				var pinned = await SidebarGapAsync(page).ConfigureAwait(false);
				// The gap only counts once the row is on screen and stuck under the header.
				var stuck = (await page.EvaluateAsync(
					"(() => { const bar = document.querySelector('.api-section-links'); const r = bar.getBoundingClientRect(); return r.bottom > 0 && r.top < innerHeight && Math.abs(r.top - parseFloat(getComputedStyle(bar).top)) < 1; })()"
				).ConfigureAwait(false)).GetBoolean();
				// Measured, not the width asked for: the resize script drops an override it did not save when it reconciles the layout.
				var sidebarWidth = await page.EvaluateAsync(
					"Math.round(document.getElementById('pages-nav').getBoundingClientRect().width)"
				).ConfigureAwait(false);
				lines.Add(
					string.Create(
						CultureInfo.InvariantCulture,
						$"{width}px window, {sidebarWidth.GetDouble()}px sidebar: gap at rest {atRest}px, pinned {stuck} with gap {pinned}px"
					)
				);
			}
			report = string.Join('\n', lines);
		}).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"""
			1100px window, 259px sidebar: gap at rest 1px, pinned True with gap 1px
			1600px window, 259px sidebar: gap at rest 1px, pinned True with gap 1px
			2560px window, 259px sidebar: gap at rest 1px, pinned True with gap 1px
			2560px window, 399px sidebar: gap at rest 1px, pinned True with gap 1px
			"""
		);
	}

	[Test]
	public async Task PageShell_IsAContainerOnlyFrom1280px()
	{
		var report = string.Empty;
		await WithPageAsync(string.Empty, async page =>
		{
			var lines = new List<string>();
			// Below 1280px the mobile nav drawer is available. Older browsers make a size container the containing block of
			// fixed boxes, which would anchor the drawer and its backdrop to the shell instead of the window.
			foreach (var width in new[] { 800, 1279, 1280, 1600 })
			{
				await page.SetViewportAsync(width, 600, 1).ConfigureAwait(false);
				await Task.Delay(300).ConfigureAwait(false);
				var containerType = await page.EvaluateAsync(
					"getComputedStyle(document.querySelector('.api-page-shell')).containerType"
				).ConfigureAwait(false);
				lines.Add($"{width}px: container-type {containerType.GetString()}");
			}
			report = string.Join('\n', lines);
		}).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"""
			800px: container-type normal
			1279px: container-type normal
			1280px: container-type inline-size
			1600px: container-type inline-size
			"""
		);
	}

	[Test]
	public async Task LinksThatDoNotFit_ScrollInsideTheRowAndTheLastOneIsReachable()
	{
		var report = string.Empty;
		await WithPageAsync(string.Empty, async page =>
		{
			// Too narrow for the links, so the track has to scroll sideways.
			await page.SetViewportAsync(260, 700, 1).ConfigureAwait(false);
			await Task.Delay(500).ConfigureAwait(false);

			var before = await page.EvaluateAsync(
				"""
				(() => {
					const track = document.querySelector('.api-section-links-track');
					// A scrollbar that takes layout room, as on a system without overlay scrollbars. Headless Chrome may draw
					// none, so where it does not, reserve the same room with padding: the track must stay inside the row either way.
					if (track.offsetHeight - track.clientHeight === 0) track.style.paddingBottom = '12px';
					track.scrollIntoView({ block: 'center' });
					const t = track.getBoundingClientRect();
					return {
						overflows: track.scrollWidth > track.clientWidth + 1,
						scrollLeft: track.scrollLeft,
						centerX: t.left + t.width / 2,
						centerY: t.top + t.height / 2,
					};
				})()
				"""
			).ConfigureAwait(false);

			// A sideways wheel over the links, as a trackpad swipe or shift+wheel sends it: a track a reader cannot scroll this
			// way would pass a test that sets scrollLeft from a script.
			await page.SendAsync("Input.dispatchMouseEvent", writer =>
			{
				writer.WriteString("type", "mouseWheel");
				writer.WriteNumber("x", before.GetProperty("centerX").GetDouble());
				writer.WriteNumber("y", before.GetProperty("centerY").GetDouble());
				writer.WriteNumber("deltaX", 600);
				writer.WriteNumber("deltaY", 0);
			}).ConfigureAwait(false);
			await Task.Delay(500).ConfigureAwait(false);

			var after = await page.EvaluateAsync(
				"""
				(() => {
					const bar = document.querySelector('.api-section-links');
					const track = bar.querySelector('.api-section-links-track');
					const links = [...track.querySelectorAll('a')];
					const last = links[links.length - 1];
					const row = bar.getBoundingClientRect();
					const rowBottom = row.bottom - parseFloat(getComputedStyle(bar).borderBottomWidth);
					const t = track.getBoundingClientRect();
					const l = last.getBoundingClientRect();
					// The visible area above the scrollbar: clientHeight leaves out a real scrollbar, and the padding stands in
					// for one where the browser draws none, so it comes off too.
					const visibleBottom = t.top + track.clientHeight - parseFloat(getComputedStyle(track).paddingBottom);
					return {
						scrollLeft: track.scrollLeft,
						lastLabel: last.textContent.trim(),
						lastReachable: l.left >= t.left - 1 && l.right <= t.right + 1,
						trackInsideRow: t.top >= row.top - 1 && t.bottom <= rowBottom + 1,
						linksAboveScrollbar: links.every(a => a.getBoundingClientRect().bottom <= visibleBottom + 1),
					};
				})()
				"""
			).ConfigureAwait(false);
			report = string.Create(
				CultureInfo.InvariantCulture,
				$"links overflow {before.GetProperty("overflows").GetBoolean()}, wheel scrolled the track {after.GetProperty("scrollLeft").GetDouble() > before.GetProperty("scrollLeft").GetDouble()}, " +
					$"last link {after.GetProperty("lastLabel").GetString()} reachable {after.GetProperty("lastReachable").GetBoolean()}, " +
					$"track inside the row {after.GetProperty("trackInsideRow").GetBoolean()}, links above the scrollbar {after.GetProperty("linksAboveScrollbar").GetBoolean()}"
			);
		}).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"links overflow True, wheel scrolled the track True, last link Responses reachable True, track inside the row True, links above the scrollbar True"
		);
	}

	// The row's state after a scroll settles: the scroll-spy runs on a frame.
	private static async Task<string> DescribeAsync(Page page)
	{
		await Task.Delay(300).ConfigureAwait(false);
		var state = await page.EvaluateAsync(
			"""
			(() => {
				const bar = document.querySelector('.api-section-links');
				const pinnedAt = parseFloat(getComputedStyle(bar).top);
				const rect = bar.getBoundingClientRect();
				return {
					visible: getComputedStyle(bar).visibility === 'visible' && rect.bottom > 0 && rect.top < innerHeight,
					pinned: Math.abs(rect.top - pinnedAt) < 1,
					current: [...bar.querySelectorAll('[aria-current]')].map(a => a.textContent.trim()).join(',') || 'none',
					title: getComputedStyle(bar.querySelector('.api-section-links-title')).visibility === 'visible',
				};
			})()
			"""
		).ConfigureAwait(false);

		return string.Create(
			CultureInfo.InvariantCulture,
			$"visible {state.GetProperty("visible").GetBoolean()}, pinned {state.GetProperty("pinned").GetBoolean()}, current section {state.GetProperty("current").GetString()}, title shown {state.GetProperty("title").GetBoolean()}"
		);
	}

	// Pixels between the sidebar's right edge and the start of the row's bottom rule, after the layout settles.
	private static async Task<string> SidebarGapAsync(Page page)
	{
		await Task.Delay(300).ConfigureAwait(false);
		var gap = await page.EvaluateAsync(
			"Math.round(document.querySelector('.api-section-links').getBoundingClientRect().left - document.getElementById('pages-nav').getBoundingClientRect().right)"
		).ConfigureAwait(false);
		return gap.GetDouble().ToString(CultureInfo.InvariantCulture);
	}

	// Scrolls so the row's own place in the page, the top of its section plus its top margin, sits at the given edge.
	private static async Task ScrollBarTopToAsync(Page page, int edge) =>
		_ = await page.EvaluateAsync(
			string.Create(
				CultureInfo.InvariantCulture,
				$"(() => {{ const bar = document.querySelector('.api-section-links'); const at = document.getElementById('elastic-api-v3').getBoundingClientRect().top + parseFloat(getComputedStyle(bar).marginTop); window.scrollBy(0, at - {edge}); return true; }})()"
			)
		).ConfigureAwait(false);

	private async Task WithPageAsync(string fragment, Func<Page, Task> body, string path = Operation)
	{
		if (target.Name != "isolated")
			Skip.Test($"The API pages these tests open are part of the isolated site, not '{target.Name}'.");

		var url = new Uri(target.BaseUrl, path);
		await WaitForPageAsync(url).ConfigureAwait(false);

		await using var browser = await Browser.LaunchAsync(new BrowserLaunchOptions()).ConfigureAwait(false);
		await using var context = await browser.NewContextAsync().ConfigureAwait(false);
		var page = await context.NewPageAsync().ConfigureAwait(false);
		// Short enough that the page always has room to scroll past the strip.
		await page.SetViewportAsync(1280, 600, 1).ConfigureAwait(false);
		await page.GotoAsync(url + fragment).ConfigureAwait(false);
		await page.WaitForLoadStateAsync("networkidle").ConfigureAwait(false);
		await body(page).ConfigureAwait(false);
	}

	// The site serves an API's pages once its spec is rendered; on the isolated site that waits for every spec, Elasticsearch's too.
	private static async Task WaitForPageAsync(Uri url)
	{
		using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
		var deadline = DateTime.UtcNow.AddMinutes(5);
		while (DateTime.UtcNow < deadline)
		{
			using var response = await http.GetAsync(url).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
				return;
			await Task.Delay(500).ConfigureAwait(false);
		}
		throw new TimeoutException(string.Create(CultureInfo.InvariantCulture, $"{url} did not render within 5 minutes."));
	}
}
