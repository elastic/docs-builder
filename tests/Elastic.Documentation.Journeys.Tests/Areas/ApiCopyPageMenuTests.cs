// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using AwesomeAssertions;
using Elastic.Documentation.Journeys.Tests.Targets;
using Nullean.Cdp;
using Nullean.Snapshots;

namespace Elastic.Documentation.Journeys.Tests.Areas;

/// <summary>
/// The Copy page button ends the breadcrumb row, or wraps under the breadcrumbs when the two do not fit on one line.
/// Its menu is a popover placed by floating.ts, so where it opens, and whether it stays inside the row rather than over
/// the sidebar, only real layout can tell. The journey DSL cannot assert a box is inside another, hence this test.
/// </summary>
[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class ApiCopyPageMenuTests(JourneyTarget target)
{
	private const string Operation = "api/cloud-connect/operation/operation-patchcluster";

	[Test]
	public async Task CopyPageMenu_OpensInsideTheBreadcrumbRowAtEveryWidth()
	{
		var lines = new List<string>();
		await WithPageAsync(async page =>
		{
			// A phone, where Copy page wraps under the breadcrumbs at the left; a laptop, where it can sit beside the
			// sidebar; and a wide screen, where it ends the breadcrumb row at the right.
			foreach (var width in new[] { 375, 1280, 1680 })
			{
				await page.SetViewportAsync(width, 900, 1).ConfigureAwait(false);
				await Task.Delay(500).ConfigureAwait(false);
				var state = await page.EvaluateAsync(
					"""
					(async () => {
						window.scrollTo(0, 0);
						const chevron = document.querySelector('.api-page-toolbar .api-page-actions-chevron');
						const menu = document.querySelector('#api-page-actions-menu');
						chevron.click();
						await new Promise(r => setTimeout(r, 600));
						const m = menu.getBoundingClientRect();
						const row = document.querySelector('.api-page-toolbar').getBoundingClientRect();
						const state = {
							open: menu.matches(':popover-open'),
							onScreen: m.left >= 0 && m.right <= document.documentElement.clientWidth,
							insideTheRow: m.left >= row.left - 1 && m.right <= row.right + 1,
						};
						chevron.click();
						await new Promise(r => setTimeout(r, 200));
						return state;
					})()
					"""
				).ConfigureAwait(false);
				lines.Add(
					string.Create(
						CultureInfo.InvariantCulture,
						$"{width}px: menu open {state.GetProperty("open").GetBoolean()}, on screen {state.GetProperty("onScreen").GetBoolean()}, inside the row {state.GetProperty("insideTheRow").GetBoolean()}"
					)
				);
			}
		}).ConfigureAwait(false);

		_ = string
			.Join('\n', lines)
			.Should()
			.MatchInlineSnapshot(
				"""
			375px: menu open True, on screen True, inside the row True
			1280px: menu open True, on screen True, inside the row True
			1680px: menu open True, on screen True, inside the row True
			"""
			);
	}

	[Test]
	public async Task CopyPageMenu_StaysInsideTheRowWhenTheRowReflowsWhileItIsOpen()
	{
		var lines = new List<string>();
		await WithPageAsync(async page =>
		{
			// A rotation or a window resize can move Copy page from under the breadcrumbs to the end of their row, or back,
			// with the menu open: it follows its button and keeps to the row.
			foreach (var (from, to) in new[] { (375, 1680), (1680, 375) })
			{
				await page.SetViewportAsync(from, 900, 1).ConfigureAwait(false);
				await Task.Delay(500).ConfigureAwait(false);
				_ = await page.EvaluateAsync(
					"""
					(async () => {
						window.scrollTo(0, 0);
						document.querySelector('.api-page-toolbar .api-page-actions-chevron').click();
						await new Promise(r => setTimeout(r, 600));
						return true;
					})()
					"""
				).ConfigureAwait(false);

				await page.SetViewportAsync(to, 900, 1).ConfigureAwait(false);
				await Task.Delay(800).ConfigureAwait(false);
				var state = await page.EvaluateAsync(
					"""
					(async () => {
						const menu = document.querySelector('#api-page-actions-menu');
						const m = menu.getBoundingClientRect();
						const row = document.querySelector('.api-page-toolbar').getBoundingClientRect();
						const button = document.querySelector('.api-page-toolbar .api-page-actions').getBoundingClientRect();
						const state = {
							open: menu.matches(':popover-open'),
							insideTheRow: m.left >= row.left - 1 && m.right <= row.right + 1,
							belowTheButton: m.top >= button.bottom - 1,
						};
						document.querySelector('.api-page-toolbar .api-page-actions-chevron').click();
						await new Promise(r => setTimeout(r, 200));
						return state;
					})()
					"""
				).ConfigureAwait(false);
				lines.Add(
					string.Create(
						CultureInfo.InvariantCulture,
						$"opened at {from}px, resized to {to}px: menu open {state.GetProperty("open").GetBoolean()}, inside the row {state.GetProperty("insideTheRow").GetBoolean()}, below its button {state.GetProperty("belowTheButton").GetBoolean()}"
					)
				);
			}
		}).ConfigureAwait(false);

		_ = string
			.Join('\n', lines)
			.Should()
			.MatchInlineSnapshot(
				"""
			opened at 375px, resized to 1680px: menu open True, inside the row True, below its button True
			opened at 1680px, resized to 375px: menu open True, inside the row True, below its button True
			"""
			);
	}

	private async Task WithPageAsync(Func<Page, Task> body)
	{
		if (target.Name != "isolated")
			Skip.Test($"The API pages this test opens are part of the isolated site, not '{target.Name}'.");

		var url = new Uri(target.BaseUrl, Operation);
		await WaitForPageAsync(url).ConfigureAwait(false);

		await using var browser = await Browser.LaunchAsync(new BrowserLaunchOptions()).ConfigureAwait(false);
		await using var context = await browser.NewContextAsync().ConfigureAwait(false);
		var page = await context.NewPageAsync().ConfigureAwait(false);
		await page.SetViewportAsync(1280, 900, 1).ConfigureAwait(false);
		await page.GotoAsync(url.ToString()).ConfigureAwait(false);
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
