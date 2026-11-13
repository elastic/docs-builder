// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Journeys.Tests.Targets;
using Nullean.Cdp;
using Nullean.Snapshots;

namespace Elastic.Documentation.Journeys.Tests.Areas;

/// <summary>
/// Long lines in the API examples rail scroll sideways under a scrollbar a mouse can drag, while the line numbers and the
/// "Generated from the API spec" note stay in place. Only real layout shows this, so it runs in Chrome: a journey file has
/// no way to drag a scrollbar or compare two positions. The Cloud Connect spec renders in under a second, and this
/// operation has a wide generated sample in both its request and its response.
/// </summary>
[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class ApiExampleScrollingTests(JourneyTarget target)
{
	private const string Operation = "api/doc/cloud-connect/operation/operation-patchcluster";
	private const string Scenario = ".api-examples-scenario-panel:not([hidden])";

	[Test]
	public async Task Request_DraggingTheScrollbar_ScrollsTheCodeAndKeepsLineNumbersAndNoteInPlace()
	{
		var report = await DragScrollbarAsync($"{Scenario} .api-code-carousel-card.is-active .api-code-carousel-card-body").ConfigureAwait(
			false
		);

		_ = report.Should().MatchInlineSnapshot(
			"""
			overflows sideways: True
			scrollbar slot: 14px
			scrolled after the drag: True
			language before and after: curl, curl
			line numbers from the left edge: 0px
			note from the right edge: 0px
			note from the bottom edge: 0px
			"""
		);
	}

	[Test]
	public async Task Response_DraggingTheScrollbar_ScrollsTheCodeAndKeepsLineNumbersAndNoteInPlace()
	{
		var report = await DragScrollbarAsync($"{Scenario} .example-block--response .example-block-body").ConfigureAwait(false);

		_ = report.Should().MatchInlineSnapshot(
			"""
			overflows sideways: True
			scrollbar slot: 14px
			scrolled after the drag: True
			language before and after: curl, curl
			line numbers from the left edge: 0px
			note from the right edge: 0px
			note from the bottom edge: 0px
			"""
		);
	}

	private async Task<string> DragScrollbarAsync(string body)
	{
		if (target.Name != "isolated")
			Skip.Test($"The API pages these tests open are part of the isolated site, not '{target.Name}'.");

		var url = new Uri(target.BaseUrl, Operation);
		await WaitForPageAsync(url).ConfigureAwait(false);

		await using var chrome = await ChromeWithScrollbars.StartAsync().ConfigureAwait(false);
		await using var context = await chrome.Browser.NewContextAsync().ConfigureAwait(false);
		var page = await context.NewPageAsync().ConfigureAwait(false);
		await page.SetViewportAsync(1280, 900, 1).ConfigureAwait(false);
		await page.GotoAsync(url.ToString()).ConfigureAwait(false);
		await page.WaitForLoadStateAsync("networkidle").ConfigureAwait(false);

		var before = await MeasureAsync(page, body).ConfigureAwait(false);
		// Grab the thumb near the left end of the bar, inside the bottom scrollbar slot, and pull it right.
		var x = before.GetProperty("left").GetDouble() + 30;
		var y = before.GetProperty("bottom").GetDouble() - (before.GetProperty("slot").GetDouble() / 2);
		await MouseAsync(page, "mouseMoved", (x, y), pressed: false).ConfigureAwait(false);
		await MouseAsync(page, "mousePressed", (x, y), pressed: true).ConfigureAwait(false);
		for (var step = 1; step <= 10; step++)
			await MouseAsync(page, "mouseMoved", (x + (step * 15), y), pressed: true).ConfigureAwait(false);
		await MouseAsync(page, "mouseReleased", (x + 150, y), pressed: false).ConfigureAwait(false);
		var after = await MeasureAsync(page, body).ConfigureAwait(false);

		return string.Join(
			'\n',
			$"overflows sideways: {before.GetProperty("overflows").GetBoolean()}",
			$"scrollbar slot: {before.GetProperty("slot").GetInt32()}px",
			$"scrolled after the drag: {after.GetProperty("scrollLeft").GetInt32() > 0}",
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
					slot: body.offsetHeight - body.clientHeight,
					scrollLeft: body.scrollLeft,
					left: box.left,
					bottom: box.bottom,
					gutter: Math.round(gutter.left - left),
					noteRight: Math.round(left + body.clientWidth - note.right),
					noteBottom: Math.round(top + body.clientHeight - note.bottom),
				};
			})()
			"""
		).ConfigureAwait(false);

	private static Task MouseAsync(Page page, string type, (double X, double Y) at, bool pressed) =>
		page.SendAsync("Input.dispatchMouseEvent", writer =>
		{
			writer.WriteString("type", type);
			writer.WriteNumber("x", at.X);
			writer.WriteNumber("y", at.Y);
			writer.WriteString("button", type == "mouseMoved" && !pressed ? "none" : "left");
			writer.WriteNumber("buttons", pressed ? 1 : 0);
			writer.WriteNumber("clickCount", 1);
		});

	// The site serves an API's pages once its spec is rendered, which can finish after the landing page is up.
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

/// <summary>
/// Headless Chrome as <see cref="Browser.LaunchAsync"/> starts it, but without <c>--hide-scrollbars</c>: that flag hides
/// the scrollbar these tests drag, and extra arguments cannot take it back off.
/// </summary>
internal sealed class ChromeWithScrollbars : IAsyncDisposable
{
	private readonly Process _process;
	private readonly string _profile;

	private ChromeWithScrollbars(Process process, string profile, Browser browser)
	{
		_process = process;
		_profile = profile;
		Browser = browser;
	}

	public Browser Browser { get; }

	public static async Task<ChromeWithScrollbars> StartAsync()
	{
		var executable = new ChromeLocator().Locate().Path ?? throw new InvalidOperationException("Chrome was not found.");
		var profile = Directory.CreateTempSubdirectory("api-scrolling-").FullName;
		var info = new ProcessStartInfo(executable)
		{
			ArgumentList = {
				"--headless=new",
				"--remote-debugging-port=0",
				$"--user-data-dir={profile}",
				"--no-first-run",
				"--no-default-browser-check"
			},
			RedirectStandardError = true
		};
		var process = Process.Start(info) ?? throw new InvalidOperationException($"{executable} did not start.");
		var socket = await DevToolsSocketAsync(profile).ConfigureAwait(false);
		var browser = await Browser.ConnectAsync(socket).ConfigureAwait(false);
		return new ChromeWithScrollbars(process, profile, browser);
	}

	public async ValueTask DisposeAsync()
	{
		await Browser.DisposeAsync().ConfigureAwait(false);
		if (!_process.HasExited)
		{
			_process.Kill(entireProcessTree: true);
			await _process.WaitForExitAsync().ConfigureAwait(false);
		}
		_process.Dispose();
		try
		{
			Directory.Delete(_profile, recursive: true);
		}
		catch (IOException)
		{
			// Chrome can hold a file in the profile for a moment after it exits; the temp folder is cleaned up later.
		}
	}

	// Chrome writes the port on the first line of DevToolsActivePort and the browser's socket path on the second.
	private static async Task<Uri> DevToolsSocketAsync(string profile)
	{
		var file = Path.Combine(profile, "DevToolsActivePort");
		var deadline = DateTime.UtcNow.AddSeconds(30);
		while (DateTime.UtcNow < deadline)
		{
			var lines = File.Exists(file) ? await File.ReadAllLinesAsync(file).ConfigureAwait(false) : [];
			if (lines is [var port, var path, ..] && path.Length > 0)
				return new Uri($"ws://127.0.0.1:{port}{path}");
			await Task.Delay(100).ConfigureAwait(false);
		}
		throw new TimeoutException("Chrome did not open its DevTools port within 30 seconds.");
	}
}
