// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Layout;

/// <summary>The archived documentation sets, linked from the archive page.</summary>
public static class ArchiveLinks
{
	public static IReadOnlyList<(string Url, string Title)> All { get; } =
	[
		("https://elastic.co/guide/en/elasticsearch/guide/current/index.html", "Elasticsearch - The Definitive Guide"),
		("https://elastic.co/guide/en/elasticsearch/client/elasticsearch-js/current/index.html", "Elasticsearch.js for 5.6-7.6"),
		("https://elastic.co/guide/en/elastic-stack-gke/current/index.html", "Elastic Stack and Google Cloud's Anthos"),
		("https://elastic.co/guide/en/elasticsearch/resiliency/current/index.html", "Elasticsearch Resiliency Status"),
		("https://elastic.co/guide/en/beats/functionbeat/current/index.html", "Functionbeat Reference"),
		("https://elastic.co/guide/en/elastic-stack-get-started/index.html", "Getting Started"),
		("https://elastic.co/guide/en/graph/current/index.html", "Graph Reference for 2.x"),
		("https://elastic.co/guide/en/elasticsearch/client/groovy-api/current/index.html", "Groovy API"),
		("https://elastic.co/guide/en/infrastructure/guide/current/index.html", "Infrastructure Monitoring Guide for 6.5-7.4"),
		("https://elastic.co/guide/en/beats/journalbeat/current/index.html", "Journalbeat Reference for 6.5-7.15"),
		("https://elastic.co/guide/get-started/current/index.html", "Legacy APM Overview"),
		("https://elastic.co/guide/server/current/index.html", "Legacy APM Server Reference"),
		("https://elastic.co/guide/en/logs/guide/current/index.html", "Logs Monitoring Guide for 7.5-7.9"),
		("https://elastic.co/guide/en/marvel/current/index.html", "Marvel Reference for 2.x and 1.x"),
		("https://elastic.co/guide/en/metrics/guide/current/index.html", "Metrics Monitoring Guide for 7.5-7.9"),
		("https://elastic.co/guide/en/reporting/current/index.html", "Reporting Reference for 2.x"),
		("https://elastic.co/guide/en/sense/current/index.html", "Sense Editor for 4.x"),
		("https://elastic.co/guide/en/shield/current/index.html", "Shield Reference for 2.x and 1.x"),
		("https://elastic.co/guide/en/siem/guide/current/index.html", "SIEM Guide"),
		("https://elastic.co/guide/en/swiftype/sitesearch/current/index.html", "Site Search Reference"),
		("https://elastic.co/guide/en/elastic-stack-overview/current/index.html", "Stack Overview [7.4] — other versions"),
		("https://elastic.co/guide/en/beats/topbeat/current/index.html", "Topbeat Reference"),
		("https://elastic.co/guide/en/uptime/current/index.html", "Uptime Monitoring Guide for 7.2-7.9"),
		("https://elastic.co/guide/en/watcher/current/index.html", "Watcher Reference for 2.x and 1.x"),
		("https://elastic.co/guide/en/x-pack/current/index.html", "X-Pack Reference for 6.0-6.2 and 5.x"),
		("https://elastic.co/guide/cn/index.html", "简体中文"),
		("https://elastic.co/guide/jp/index.html", "日本語"),
		("https://elastic.co/guide/kr/index.html", "한국어"),
	];
}
