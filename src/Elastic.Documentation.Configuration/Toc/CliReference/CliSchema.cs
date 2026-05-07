// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;

namespace Elastic.Documentation.Configuration.Toc.CliReference;

// Language-agnostic CLI schema spec (https://cli-schema.org)
// Schema v2: type uses JSON Schema primitives; v1 used kind-style strings
public record CliSchema(
	int SchemaVersion,
	string Name,
	string? Description,
	List<CliParamSchema> GlobalOptions,
	CliDefaultSchema? RootDefault,
	List<CliCommandSchema> Commands,
	List<CliNamespaceSchema> Namespaces,
	string? Version = null,
	string[]? ReservedMetaCommands = null,
	string[]? Tags = null,
	bool? RequiresAuth = null,
	string[]? AuthCommands = null,
	CliEnvironmentSchema? Environment = null
)
{
	public static CliSchema Load(IFileInfo schemaFile)
	{
		var json = schemaFile.FileSystem.File.ReadAllText(schemaFile.FullName);
		return JsonSerializer.Deserialize(json, CliSchemaJsonContext.Default.CliSchema)
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
	bool Hidden = false,
	string[]? Tags = null,
	CliDeprecatedSchema? Deprecated = null,
	CliIntentSchema? Intent = null,
	CliOutputSchema? Output = null,
	bool Streaming = false,
	bool LongRunning = false
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
	bool Hidden = false,
	bool Variadic = false,
	CliDeprecatedSchema? Deprecated = null,
	List<CliValidationSchema>? Validations = null
);

public record CliDefaultSchema(
	string? Summary,
	string? Notes,
	string? Usage,
	string[]? Examples,
	List<CliParamSchema> Parameters,
	string Kind = "",
	bool Hidden = false
);

public record CliValidationSchema(
	string Kind,
	string[]? Values = null,
	string? Min = null,
	string? Max = null,
	string? Pattern = null
);

public record CliDeprecatedSchema(
	string? Message = null,
	string? Since = null,
	string? RemovedIn = null
);

public record CliIntentSchema(
	bool? Destructive = null,
	bool? Idempotent = null,
	string? Scope = null,
	bool? RequiresConfirmation = null,
	bool? RequiresAuth = null
);

public record CliOutputSchema(
	string[]? Formats = null,
	string? FormatFlag = null
);

public record CliEnvironmentSchema(
	List<CliEnvVarSchema>? Variables = null,
	List<CliConfigFileSchema>? ConfigFiles = null
);

public record CliEnvVarSchema(
	string Name,
	string? Description = null,
	bool Required = false,
	string? DefaultValue = null
);

public record CliConfigFileSchema(
	string Path,
	string? Description = null,
	bool Required = false
);
