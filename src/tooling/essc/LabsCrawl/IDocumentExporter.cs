// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.SiteSearch.Cli.LabsCrawl;

/// <summary>Vendored exporter contract for labs crawl (independent of crawler package).</summary>
public interface IDocumentExporter<in TDocument>
{
	Task ExportAsync(TDocument document, CancellationToken ct);
}
