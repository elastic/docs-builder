// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using static Elastic.Documentation.Exporter;

namespace Elastic.Documentation;

public enum Exporter
{
	Html,
	LLMText,
	Elasticsearch,
	SemanticElasticsearch,
	Configuration,
	DocumentationState,
	LinkMetadata,
	Redirects,
}
public static class ExportOptions
{
	public static HashSet<Exporter> Default { get; } = [Html, LLMText, Configuration, DocumentationState, LinkMetadata, Redirects];
	public static HashSet<Exporter> MetadataOnly { get; } = [Configuration, DocumentationState, LinkMetadata, Redirects];
}
