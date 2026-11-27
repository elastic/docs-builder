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
/// The version notice of an older or unreleased API page starts below the title at every width, in both looks. The HTML
/// tests would still pass if a stylesheet put the notice beside a short title again, so only real layout can tell.
/// The real API pages use the legacy look. The style-guide previews of the same operation use the design-system look.
/// </summary>
[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class ApiVersionNoticeTests(JourneyTarget target)
{
	private static readonly (string Name, string Path)[] Pages =
	[
		("legacy v8 group", "api/elasticsearch/v8/group/endpoint-document"),
		("legacy next operation", "api/elasticsearch/next/operation/operation-search"),
		("legacy v8 operation", "api/elasticsearch/v8/operation/operation-search"),
		("design-system next operation", "style-guide/preview/api-operation-next"),
		("design-system v8 operation", "style-guide/preview/api-operation-older-major")
	];

	[Test]
	public async Task Notice_StartsBelowTheTitleAtEveryWidth()
	{
		if (target.Name != "isolated")
			Skip.Test($"The API pages this test opens are part of the isolated site, not '{target.Name}'.");

		var lines = new List<string>();
		await using var browser = await Browser.LaunchAsync(new BrowserLaunchOptions()).ConfigureAwait(false);
		await using var context = await browser.NewContextAsync().ConfigureAwait(false);
		var page = await context.NewPageAsync().ConfigureAwait(false);

		foreach (var (name, path) in Pages)
		{
			var url = new Uri(target.BaseUrl, path);
			await WaitForPageAsync(url).ConfigureAwait(false);
			await page.GotoAsync(url.ToString()).ConfigureAwait(false);
			await page.WaitForLoadStateAsync("networkidle").ConfigureAwait(false);

			// A phone; a laptop, where the article column is narrow; and a wide screen, where a short title leaves room beside it.
			foreach (var width in new[] { 375, 1280, 1680 })
			{
				await page.SetViewportAsync(width, 900, 1).ConfigureAwait(false);
				await Task.Delay(500).ConfigureAwait(false);
				var state = await page.EvaluateAsync(
					"""
					(() => {
						const title = document.querySelector('.api-page-heading h1').getBoundingClientRect();
						const notice = document.querySelector('.api-version-notice');
						const badges = document.querySelector('.api-title-badges');
						return {
							notice: notice ? notice.getBoundingClientRect().top >= title.bottom - 1 : null,
							versionBadge: badges ? badges.innerText.includes('Version') : false,
						};
					})()
					"""
				).ConfigureAwait(false);
				var below = state.GetProperty("notice");
				var result = below.ValueKind == System.Text.Json.JsonValueKind.Null
					? $"no notice under the title, version in the badges row {state.GetProperty("versionBadge").GetBoolean()}"
					: $"notice below the title {below.GetBoolean()}";
				lines.Add(string.Create(CultureInfo.InvariantCulture, $"{name} at {width}px: {result}"));
			}
		}

		_ = string
			.Join('\n', lines)
			.Should()
			.MatchInlineSnapshot(
				"""
			legacy v8 group at 375px: notice below the title True
			legacy v8 group at 1280px: notice below the title True
			legacy v8 group at 1680px: notice below the title True
			legacy next operation at 375px: notice below the title True
			legacy next operation at 1280px: notice below the title True
			legacy next operation at 1680px: notice below the title True
			legacy v8 operation at 375px: no notice under the title, version in the badges row True
			legacy v8 operation at 1280px: no notice under the title, version in the badges row True
			legacy v8 operation at 1680px: no notice under the title, version in the badges row True
			design-system next operation at 375px: notice below the title True
			design-system next operation at 1280px: notice below the title True
			design-system next operation at 1680px: notice below the title True
			design-system v8 operation at 375px: no notice under the title, version in the badges row True
			design-system v8 operation at 1280px: no notice under the title, version in the badges row True
			design-system v8 operation at 1680px: no notice under the title, version in the badges row True
			"""
			);
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
