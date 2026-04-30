// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;

namespace Elastic.Documentation.Configuration.Toc.CliReference;

public record ArghCliSchema(
	int SchemaVersion,
	string EntryAssembly,
	string? Description,
	List<CliParamSchema> GlobalOptions,
	CliDefaultSchema? RootDefault,
	List<CliCommandSchema> Commands,
	List<CliNamespaceSchema> Namespaces
)
{
	public static ArghCliSchema Load(IFileInfo schemaFile)
	{
		var json = schemaFile.FileSystem.File.ReadAllText(schemaFile.FullName);
		return JsonSerializer.Deserialize(json, ArghSchemaJsonContext.Default.ArghCliSchema)
			?? throw new InvalidOperationException($"Failed to deserialize CLI schema from {schemaFile.FullName}");
	}
}

public record CliCommandSchema(
	string[] Path,
	string Name,
	string? Summary,
	string? Notes,
	string? Usage,
	string[]? Examples,
	List<CliParamSchema> Parameters
);

public record CliNamespaceSchema(
	string Segment,
	string? Summary,
	string? Notes,
	List<CliParamSchema> Options,
	CliDefaultSchema? DefaultCommand,
	List<CliCommandSchema> Commands,
	List<CliNamespaceSchema> Namespaces
);

public record CliParamSchema(
	string Role,
	string Name,
	string? ShortName,
	string Kind,
	bool Required,
	string? Summary
);

public record CliDefaultSchema(
	string? Summary,
	string? Notes,
	string? Usage,
	string[]? Examples,
	List<CliParamSchema> Parameters
);
