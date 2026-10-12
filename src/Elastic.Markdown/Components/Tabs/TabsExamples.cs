// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.Tabs;

public static class TabsExamples
{
	private static async Task<HtmlString> ItemsAsync()
	{
		var items = new[]
		{
			new TabItemModel
			{
				SetIndex = 900,
				Index = 0,
				Title = "curl",
				SyncGroup = "lang",
				SyncKey = "curl",
				Body = new HtmlString("<p>Send the request with curl.</p>")
			},
			new TabItemModel
			{
				SetIndex = 900,
				Index = 1,
				Title = "Python",
				SyncGroup = "lang",
				SyncKey = "python",
				Body = new HtmlString("<p>Use the Python client.</p>")
			},
			new TabItemModel
			{
				SetIndex = 900,
				Index = 2,
				Title = "JavaScript",
				SyncGroup = "lang",
				SyncKey = "js",
				Body = new HtmlString("<p>Use the JavaScript client.</p>")
			}
		};
		var html = new System.Text.StringBuilder();
		foreach (var item in items)
		{
			using var writer = new StringWriter();
			await _TabItem.Create(item).RenderAsync(writer);
			_ = html.Append(writer);
		}
		return new HtmlString(html.ToString());
	}

	public static ComponentSection Tabs { get; } = new(
		"Tabs",
		"CSS-only tabs built on radio inputs, so they work without JavaScript. When tabs share a sync group, the enhancement keeps them in step across the page and across page loads.",
		"tab-set",
		[ComponentExample.Of("Tab set", _TabSet.Create, new TabSetModel { Items = ItemsAsync().GetAwaiter().GetResult() })]
	);
}
