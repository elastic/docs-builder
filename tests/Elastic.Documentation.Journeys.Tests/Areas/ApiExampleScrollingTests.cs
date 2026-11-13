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
/// Long lines in the API examples rail scroll sideways while the line numbers and the "Generated from the API spec" note
/// stay in place. Only real layout shows this, so it runs in Chrome: a journey file has no way to compare two positions.
/// The Cloud Connect spec renders in under a second, and this operation has a wide generated sample in both its request
/// and its response.
/// </summary>
[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class ApiExampleScrollingTests(JourneyTarget target)
{
	private const string Operation = "api/doc/cloud-connect/operation/operation-patchcluster";
	private const string Scenario = ".api-examples-scenario-panel:not([hidden])";

	[Test]
	public async Task Request_ScrollingSideways_KeepsLineNumbersAndNoteInPlace()
	{
		var report = await ScrollSidewaysAsync($"{Scenario} .api-code-carousel-card.is-active .api-code-carousel-card-body").ConfigureAwait(
			false
		);

		_ = report.Should().MatchInlineSnapshot(
			"""
			overflows sideways: True
			scrolled sideways: True
			language before and after: curl, curl
			line numbers from the left edge: 0px
			note from the right edge: 0px
			note from the bottom edge: 0px
			"""
		);
	}

	[Test]
	public async Task Response_ScrollingSideways_KeepsLineNumbersAndNoteInPlace()
	{
		var report = await ScrollSidewaysAsync($"{Scenario} .example-block--response .example-block-body").ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"""
			overflows sideways: True
			scrolled sideways: True
			language before and after: curl, curl
			line numbers from the left edge: 0px
			note from the right edge: 0px
			note from the bottom edge: 0px
			"""
		);
	}

	private async Task<string> ScrollSidewaysAsync(string body)
	{
		if (target.Name != "isolated")
			Skip.Test($"The API pages these tests open are part of the isolated site, not '{target.Name}'.");

		var url = new Uri(target.BaseUrl, Operation);
		await WaitForPageAsync(url).ConfigureAwait(false);

		await using var browser = await Browser.LaunchAsync(new BrowserLaunchOptions()).ConfigureAwait(false);
		await using var context = await browser.NewContextAsync().ConfigureAwait(false);
		var page = await context.NewPageAsync().ConfigureAwait(false);
		await page.SetViewportAsync(1280, 900, 1).ConfigureAwait(false);
		await page.GotoAsync(url.ToString()).ConfigureAwait(false);
		await page.WaitForLoadStateAsync("networkidle").ConfigureAwait(false);

		var before = await MeasureAsync(page, body).ConfigureAwait(false);
		// A sideways wheel over the code, as a trackpad swipe or shift+wheel sends it.
		await page.SendAsync("Input.dispatchMouseEvent", writer =>
		{
			writer.WriteString("type", "mouseWheel");
			writer.WriteNumber("x", before.GetProperty("centerX").GetDouble());
			writer.WriteNumber("y", before.GetProperty("centerY").GetDouble());
			writer.WriteNumber("deltaX", 200);
			writer.WriteNumber("deltaY", 0);
		}).ConfigureAwait(false);
		await Task.Delay(300).ConfigureAwait(false);
		var after = await MeasureAsync(page, body).ConfigureAwait(false);

		return string.Join(
			'\n',
			$"overflows sideways: {before.GetProperty("overflows").GetBoolean()}",
			$"scrolled sideways: {after.GetProperty("scrollLeft").GetInt32() > 0}",
			$"language before and after: {before.GetProperty("language").GetString()}, {after.GetProperty("language").GetString()}",
			$"line numbers from the left edge: {after.GetProperty("gutter").GetInt32()}px",
			$"note from the right edge: {after.GetProperty("noteRight").GetInt32()}px",
			$"note from the bottom edge: {after.GetProperty("noteBottom").GetInt32()}px"
		);
	}

	// Edges are the scroll body's visible area: inside its border, short of its scrollbars.
	private static async Task<JsonElement> MeasureAsync(Page page, string body) =>
		await page.EvaluateAsync(
			$$"""
			(() => {
				const body = document.querySelector({{JsonSerializer.Serialize(body)}});
				const box = body.getBoundingClientRect();
				const left = box.left + body.clientLeft, top = box.top + body.clientTop;
				const gutter = body.querySelector('.code-line-gutter').getBoundingClientRect();
				const note = body.querySelector('.api-code-carousel-card-note').getBoundingClientRect();
				return {
					language: document.querySelector('{{Scenario}} .api-code-carousel-card.is-active').dataset.lang,
					overflows: body.scrollWidth > body.clientWidth,
					scrollLeft: body.scrollLeft,
					centerX: left + body.clientWidth / 2,
					centerY: top + body.clientHeight / 2,
					gutter: Math.round(gutter.left - left),
					noteRight: Math.round(left + body.clientWidth - note.right),
					noteBottom: Math.round(top + body.clientHeight - note.bottom),
				};
			})()
			"""
		).ConfigureAwait(false);

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
