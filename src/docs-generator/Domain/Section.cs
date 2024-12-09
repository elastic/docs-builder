// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Documentation.Generator.Domain;

public record Section
{
	public required string Header { get; init; }

	public required int Level { get; init; }

	public required string Paragraphs { get; set; }

}
