// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.Disclosure;

public static class DisclosureExamples
{
	public static ComponentSection Disclosures { get; } = new(
		"Dropdown",
		"A native details element, so it opens and closes without JavaScript and is keyboard accessible. The icon rotation is a small, always-on cue.",
		"disclosure",
		[
			ComponentExample.Of(
				"Closed",
				_Disclosure.Create,
				new DisclosureModel
				{
					Title = "What is a shard?",
					Body = new HtmlString("<p>A shard is a unit of storage and search that holds part of an index.</p>")
				}
			),
			ComponentExample.Of(
				"Open",
				_Disclosure.Create,
				new DisclosureModel
				{
					Title = "How many shards do I need?",
					Open = true,
					Body = new HtmlString("<p>Start with one primary shard per 10 to 50 GB of data.</p>")
				}
			)
		]
	);
}
