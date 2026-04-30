// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;

namespace Elastic.Documentation.Configuration.Toc.CliReference;

// Schema v2: entryAssembly renamed to name, kind replaced by type (JSON Schema primitives)
public record ArghCliSchema(
	int SchemaVersion,
	string Name,
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
	List<CliParamSchema> Parameters,
	string[]? Aliases = null,
	bool Hidden = false
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
	// v2: type uses JSON Schema primitives ("string","integer","number","boolean","array","enum")
	string Type,
	bool Required,
	string? Summary,
	string? DefaultValue = null,
	string[]? EnumValues = null,
	string? ElementType = null,
	bool Repeatable = false,
	string? Separator = null,
	string[]? Aliases = null,
	bool Hidden = false
);

public record CliDefaultSchema(
	string? Summary,
	string? Notes,
	string? Usage,
	string[]? Examples,
	List<CliParamSchema> Parameters
);
