// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Argh;

namespace Elastic.Documentation;

public enum Exporter
{
	Html,
	[EnumValue("llm")]
	LLMText,
	Elasticsearch,
	Configuration,
	DocumentationState,
	LinkMetadata,
	Redirects
}

public static class ExportOptions
{
	public static HashSet<Exporter> Default { get; } = [Exporter.Html, Exporter.LLMText, Exporter.Configuration, Exporter.DocumentationState, Exporter.LinkMetadata, Exporter.Redirects];
	public static HashSet<Exporter> MetadataOnly { get; } = [Exporter.Configuration, Exporter.DocumentationState, Exporter.LinkMetadata, Exporter.Redirects];
}
