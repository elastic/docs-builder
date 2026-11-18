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
/// An operation page opens with a strip of its sections, and a toolbar with the same links takes over once the strip has
/// scrolled away. The toolbar hangs from a zero-height sticky wrapper under the strip, so whether it shows, and where it
/// sits, only real layout can tell: a unit test that supplies the offsets would pass even if the wrapper lost
/// <c>position: sticky</c>. The Cloud Connect spec renders in under a second.
/// </summary>
[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class ApiSectionToolbarTests(JourneyTarget target)
{
	private const string Operation = "api/doc/cloud-connect/operation/operation-patchcluster";

	[Test]
	public async Task ScrollingPastTheStrip_ShowsTheToolbarAtOnceAndKeepsItPinned()
	{
		var report = string.Empty;
		await WithPageAsync(string.Empty, async page =>
		{
			var lines = new List<string> { $"at rest: {await DescribeAsync(page).ConfigureAwait(false)}" };

			// The strip's bottom edge, in pixels below the top of the window: the header is about 48px, so 60 is still behind it.
			foreach (var edge in new[] { 200, 60, 40 })
			{
				await ScrollStripBottomToAsync(page, edge).ConfigureAwait(false);
				lines.Add($"strip bottom at {edge}px: {await DescribeAsync(page).ConfigureAwait(false)}");
			}

			_ = await page.EvaluateAsync("document.getElementById('responses').scrollIntoView(); true").ConfigureAwait(false);
			lines.Add($"at the last section: {await DescribeAsync(page).ConfigureAwait(false)}");
			report = string.Join('\n', lines);
		}).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"""
			at rest: strip on screen True, toolbar visible False, at the pinned offset False, current section none
			strip bottom at 200px: strip on screen True, toolbar visible False, at the pinned offset False, current section none
			strip bottom at 60px: strip on screen True, toolbar visible False, at the pinned offset False, current section none
			strip bottom at 40px: strip on screen False, toolbar visible True, at the pinned offset True, current section none
			at the last section: strip on screen False, toolbar visible True, at the pinned offset True, current section Responses
			"""
		);
	}

	[Test]
	public async Task OpeningASectionFragment_ShowsTheToolbarWithThatSectionCurrent()
	{
		var report = string.Empty;
		await WithPageAsync("#responses", async page => report = await DescribeAsync(page).ConfigureAwait(false)).ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"strip on screen False, toolbar visible True, at the pinned offset True, current section Responses"
		);
	}

	// The toolbar's state after a scroll settles: the transition takes 150ms and the scroll handler runs on a frame.
	private static async Task<string> DescribeAsync(Page page)
	{
		await Task.Delay(500).ConfigureAwait(false);
		var state = await page.EvaluateAsync(
			"""
			(() => {
				const pin = document.querySelector('[data-section-pin]');
				const toolbar = document.querySelector('.api-section-links');
				const strip = document.querySelector('.api-section-strip').getBoundingClientRect();
				const visible = getComputedStyle(toolbar).visibility === 'visible';
				const pinnedAt = parseFloat(getComputedStyle(pin).top);
				return {
					stripOnScreen: strip.bottom > pinnedAt,
					toolbarVisible: visible,
					toolbarAtPinnedOffset: visible && Math.abs(toolbar.getBoundingClientRect().top - pinnedAt) < 1,
					current: [...toolbar.querySelectorAll('[aria-current]')].map(a => a.textContent.trim()).join(',') || 'none',
				};
			})()
			"""
		).ConfigureAwait(false);

		return string.Create(
			CultureInfo.InvariantCulture,
			$"strip on screen {state.GetProperty("stripOnScreen").GetBoolean()}, toolbar visible {state.GetProperty("toolbarVisible").GetBoolean()}, " +
				$"at the pinned offset {state.GetProperty("toolbarAtPinnedOffset").GetBoolean()}, current section {state.GetProperty("current").GetString()}"
		);
	}

	private static async Task ScrollStripBottomToAsync(Page page, int edge) =>
		_ = await page.EvaluateAsync(
			string.Create(
				CultureInfo.InvariantCulture,
				$"window.scrollBy(0, document.querySelector('.api-section-strip').getBoundingClientRect().bottom - {edge}); true"
			)
		).ConfigureAwait(false);

	private async Task WithPageAsync(string fragment, Func<Page, Task> body)
	{
		if (target.Name != "isolated")
			Skip.Test($"The API pages these tests open are part of the isolated site, not '{target.Name}'.");

		var url = new Uri(target.BaseUrl, Operation);
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
