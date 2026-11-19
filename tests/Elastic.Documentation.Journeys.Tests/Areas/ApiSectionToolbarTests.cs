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
			at rest: visible True, pinned False, current section Overview
			row top at 200px: visible True, pinned False, current section Overview
			row top at 60px: visible True, pinned True, current section Overview
			row top at 20px: visible True, pinned True, current section Overview
			at the last section: visible True, pinned True, current section Responses
			"""
		);
	}

	[Test]
	public async Task OpeningASectionFragment_PinsTheSectionLinksWithThatSectionCurrent()
	{
		var report = string.Empty;
		await WithPageAsync("#responses", async page => report = await DescribeAsync(page).ConfigureAwait(false)).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot("visible True, pinned True, current section Responses");
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
					return {
						scrollLeft: track.scrollLeft,
						lastLabel: last.textContent.trim(),
						lastReachable: l.left >= t.left - 1 && l.right <= t.right + 1,
						trackInsideRow: t.top >= row.top - 1 && t.bottom <= rowBottom + 1,
						linksInsideTrack: links.every(a => a.getBoundingClientRect().bottom <= t.bottom + 1),
					};
				})()
				"""
			).ConfigureAwait(false);
			report = string.Create(
				CultureInfo.InvariantCulture,
				$"links overflow {before.GetProperty("overflows").GetBoolean()}, wheel scrolled the track {after.GetProperty("scrollLeft").GetDouble() > before.GetProperty("scrollLeft").GetDouble()}, " +
					$"last link {after.GetProperty("lastLabel").GetString()} reachable {after.GetProperty("lastReachable").GetBoolean()}, " +
					$"track inside the row {after.GetProperty("trackInsideRow").GetBoolean()}, links inside the track {after.GetProperty("linksInsideTrack").GetBoolean()}"
			);
		}).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"links overflow True, wheel scrolled the track True, last link Responses reachable True, track inside the row True, links inside the track True"
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
				};
			})()
			"""
		).ConfigureAwait(false);

		return string.Create(
			CultureInfo.InvariantCulture,
			$"visible {state.GetProperty("visible").GetBoolean()}, pinned {state.GetProperty("pinned").GetBoolean()}, current section {state.GetProperty("current").GetString()}"
		);
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
