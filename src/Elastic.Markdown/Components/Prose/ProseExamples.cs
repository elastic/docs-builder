// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.Prose;

public static class ProseExamples
{
	private static HtmlString Article =>
		new(
			"""
			<h2>Index a document</h2>
			<p>Send a <code>PUT</code> request to add a document. The <a href="#">index API</a> creates the index if it does not exist, and <a href="https://www.elastic.co" target="_blank">elastic.co</a> has more.</p>
			<ul><li>Documents are JSON.</li><li>Each document has an <strong>id</strong>.<ul><li>You can set it yourself.</li></ul></li></ul>
			<ol><li>Create the index.</li><li>Add documents.</li><li>Search.</li></ol>
			<blockquote><p>Mappings define how fields are stored and indexed.</p></blockquote>
			<h3>Settings</h3>
			<table><thead><tr><th>Setting</th><th>Default</th></tr></thead><tbody><tr><td><code>number_of_shards</code></td><td>1</td></tr><tr><td><code>number_of_replicas</code></td><td>1</td></tr></tbody></table>
			<dl><dt>Shard</dt><dd>A unit of storage and search.</dd></dl>
			<hr>
			<p>Press <kbd class="kbd">Ctrl</kbd> to continue.</p>
			"""
		);

	public static ComponentSection Articles { get; } = new(
		"Prose",
		"Article typography for plain Markdown output. Elements are styled by tag inside the prose container, so authors never write classes.",
		"prose",
		[ComponentExample.Of("Article", _Prose.Create, new ProseModel { Body = Article })]
	);
}
