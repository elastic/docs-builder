// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Journeys.Tests.Targets;
using Nullean.Cdp;
using Nullean.Snapshots;

namespace Elastic.Documentation.Journeys.Tests.Areas;

/// <summary>
/// The tooltip reads its colours from <c>--color-emphasis</c> and falls back to the legacy theme's tokens when no design
/// system theme defines it. A tooltip with no background is unreadable, and only a real stylesheet cascade shows that:
/// a unit test sees no CSS, and a journey file can read computed styles but cannot assert on one. The isolated site loads
/// the legacy bundle unless the navigation preview flag is on, so this checks the fallback path by default.
/// </summary>
[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class TooltipColorTests(JourneyTarget target)
{
	[Test]
	public async Task Tooltip_OnTheIsolatedSite_HasAnOpaqueBackgroundAndReadableText()
	{
		if (target.Name != "isolated")
			Skip.Test($"The pages this test opens are part of the isolated site, not '{target.Name}'.");

		await using var browser = await Browser.LaunchAsync(new BrowserLaunchOptions()).ConfigureAwait(false);
		await using var context = await browser.NewContextAsync().ConfigureAwait(false);
		var page = await context.NewPageAsync().ConfigureAwait(false);
		await page.SetViewportAsync(1280, 800, 1).ConfigureAwait(false);
		await page.GotoAsync(new Uri(target.BaseUrl, "/syntax/code").ToString()).ConfigureAwait(false);
		await page.WaitForLoadStateAsync("networkidle").ConfigureAwait(false);

		// The copy button carries a data-tooltip; a real pointer over it opens the shared tooltip.
		var button = await page.EvaluateAsync(
			"""
			(() => {
				const box = document.querySelector('.copybtn').getBoundingClientRect();
				return { x: box.left + box.width / 2, y: box.top + box.height / 2 };
			})()
			"""
		).ConfigureAwait(false);
		await page.SendAsync("Input.dispatchMouseEvent", writer =>
		{
			writer.WriteString("type", "mouseMoved");
			writer.WriteNumber("x", button.GetProperty("x").GetDouble());
			writer.WriteNumber("y", button.GetProperty("y").GetDouble());
		}).ConfigureAwait(false);

		var colours = await WaitForPlacedTooltipAsync(page).ConfigureAwait(false);

		var report = string.Join(
			'\n',
			$"background is opaque: {colours.GetProperty("opaque").GetBoolean()}",
			$"text differs from background: {colours.GetProperty("background").GetString() != colours.GetProperty("text").GetString()}",
			$"arrow matches background: {colours.GetProperty("background").GetString() == colours.GetProperty("arrow").GetString()}"
		);
		_ = report.Should().MatchInlineSnapshot(
			"""
			background is opaque: True
			text differs from background: True
			arrow matches background: True
			"""
		);
	}

	private static async Task<JsonElement> WaitForPlacedTooltipAsync(Page page)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (true)
		{
			var result = await page.EvaluateAsync(
				"""
				(() => {
					const tip = document.querySelector('#site-tooltip');
					if (!tip || !tip.matches(':popover-open') || !tip.hasAttribute('data-placed')) return null;
					const style = getComputedStyle(tip);
					const arrow = getComputedStyle(tip, '::before');
					// Chrome serialises a computed colour as rgb(), rgba(), oklab(), color(srgb ...) and more; a canvas pixel
					// turns any of them into sRGB bytes with an exact alpha.
					const ctx = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
					const pixel = colour => {
						ctx.clearRect(0, 0, 1, 1);
						ctx.fillStyle = colour;
						ctx.fillRect(0, 0, 1, 1);
						return Array.from(ctx.getImageData(0, 0, 1, 1).data);
					};
					const background = pixel(style.backgroundColor);
					return {
						opaque: background[3] === 255,
						background: background.join(','),
						text: pixel(style.color).join(','),
						arrow: pixel(tip.dataset.side === 'bottom' ? arrow.borderBottomColor : arrow.borderTopColor).join(','),
					};
				})()
				"""
			).ConfigureAwait(false);
			if (result.ValueKind == JsonValueKind.Object)
				return result;
			if (DateTime.UtcNow > deadline)
				throw new TimeoutException("No tooltip was placed within 5 seconds of the pointer moving over the copy button.");
			await Task.Delay(50).ConfigureAwait(false);
		}
	}
}
