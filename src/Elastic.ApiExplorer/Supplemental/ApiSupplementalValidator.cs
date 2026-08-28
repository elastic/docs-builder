// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation.Diagnostics;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Supplemental;

internal sealed record ApiSupplementalValidationRequest(
	OpenApiDocument Document,
	IDiagnosticsCollector Collector,
	string Moniker,
	bool EmitUnmatchedBaseFiles
);

internal static class ApiSupplementalValidator
{
	public static void Validate(ApiSupplementalDiscoveryResult discovery, ApiSupplementalValidationRequest request)
	{
		if (request.EmitUnmatchedBaseFiles)
			EmitUnmatched(discovery.Unmatched, request.Collector, "the latest spec");

		var (operationsById, tagNames) = ApiSupplementalDiscovery.CollectEntities(request.Document);
		if (int.TryParse(request.Moniker, out var major))
		{
			var (uniqueBySlug, _) = ApiSupplementalDiscovery.IndexTags(tagNames);
			var tagSlugs = new HashSet<string>(uniqueBySlug.Keys, StringComparer.Ordinal);
			ValidateVersionSuffixed(discovery.VersionSuffixed, major, operationsById, tagSlugs, request.Document, request.Collector);
		}

		ValidateOperationOverrides(discovery.Operations, operationsById, request);
	}

	private static void EmitUnmatched(IReadOnlyList<IFileInfo> unmatched, IDiagnosticsCollector collector, string specLabel)
	{
		foreach (var file in unmatched)
			EmitUnmatchedFile(file, collector, specLabel);
	}

	private static void EmitUnmatchedFile(IFileInfo file, IDiagnosticsCollector collector, string specLabel)
	{
		var kind = file.Name.StartsWith("op-", StringComparison.OrdinalIgnoreCase) ? "operationId" : "tag";
		collector.EmitError(file, $"API supplemental file '{file.Name}' does not match any {kind} in {specLabel}");
	}

	private static void ValidateVersionSuffixed(
		IReadOnlyList<ApiSupplementalVersionedFile> versionSuffixed,
		int major,
		IReadOnlyDictionary<string, OpenApiOperation> operationsById,
		IReadOnlySet<string> tagSlugs,
		OpenApiDocument document,
		IDiagnosticsCollector collector
	)
	{
		var analyzer = new SchemaAnalyzer(document);
		var specLabel = $"version {major}";
		foreach (var versioned in versionSuffixed)
		{
			if (versioned.Name.VersionMajor != major)
				continue;

			if (versioned.Name.Kind == ApiSupplementalKind.Operation)
			{
				if (!operationsById.TryGetValue(versioned.Name.Stem, out var operation))
				{
					EmitUnmatchedFile(versioned.File, collector, specLabel);
					continue;
				}

				ValidateFileOverrides(versioned.File, operation, analyzer, collector, specLabel);
				continue;
			}

			if (!tagSlugs.Contains(versioned.Name.Stem))
				EmitUnmatchedFile(versioned.File, collector, specLabel);
		}
	}

	private static void ValidateOperationOverrides(
		IReadOnlyDictionary<string, IFileInfo> operationFiles,
		IReadOnlyDictionary<string, OpenApiOperation> operationsById,
		ApiSupplementalValidationRequest request
	)
	{
		var analyzer = new SchemaAnalyzer(request.Document);
		var specLabel = SpecLabel(request.Moniker);
		foreach (var (operationId, file) in operationFiles)
		{
			if (!operationsById.TryGetValue(operationId, out var operation))
				continue;

			ValidateFileOverrides(file, operation, analyzer, request.Collector, specLabel);
		}
	}

	private static string SpecLabel(string moniker) => int.TryParse(moniker, out var major) ? $"version {major}" : "the latest spec";

	private static void ValidateFileOverrides(
		IFileInfo file,
		OpenApiOperation operation,
		SchemaAnalyzer analyzer,
		IDiagnosticsCollector collector,
		string specLabel
	)
	{
		var doc = ApiSupplementalDoc.Parse(file.FileSystem.File.ReadAllText(file.FullName));
		if (doc is null)
			return;

		ValidateOverrideKeys(file, operation, analyzer, collector, doc, specLabel);
	}

	private static void ValidateOverrideKeys(
		IFileInfo file,
		OpenApiOperation operation,
		SchemaAnalyzer analyzer,
		IDiagnosticsCollector collector,
		ApiSupplementalDoc doc,
		string specLabel
	)
	{
		var operationId = operation.OperationId ?? "";
		if (doc.ParameterOverrides.Count > 0)
		{
			var parameterNames = ParameterNames(operation);
			foreach (var key in doc.ParameterOverrides.Keys)
			{
				if (!parameterNames.Contains(key))
					collector.EmitError(file, $"API supplemental: Parameter '{key}' not found in operation '{operationId}' in {specLabel}");
			}
		}

		if (doc.RequestBodyOverrides.Count > 0)
		{
			var bodyNames = RequestBodyFieldNames(analyzer, operation);
			foreach (var key in doc.RequestBodyOverrides.Keys)
			{
				if (!bodyNames.Contains(key))
					collector.EmitError(
						file,
						$"API supplemental: Request body field '{key}' not found in operation '{operationId}' in {specLabel}"
					);
			}
		}
	}

	private static HashSet<string> ParameterNames(OpenApiOperation operation)
	{
		var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var parameter in operation.Parameters ?? [])
		{
			if (!string.IsNullOrEmpty(parameter.Name))
				_ = names.Add(parameter.Name);
		}

		return names;
	}

	private static HashSet<string> RequestBodyFieldNames(SchemaAnalyzer analyzer, OpenApiOperation operation)
	{
		var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var schema = operation.RequestBody?.Content?.FirstOrDefault().Value?.Schema;
		CollectFieldNames(analyzer, schema, names, []);
		return names;
	}

	private static void CollectFieldNames(SchemaAnalyzer analyzer, IOpenApiSchema? schema, HashSet<string> names, HashSet<object> visited)
	{
		var resolved = analyzer.ResolveSchema(schema);
		if (resolved is null || !visited.Add(resolved))
			return;

		var properties = analyzer.GetSchemaProperties(resolved);
		if (properties is not null)
		{
			foreach (var (name, child) in properties)
			{
				_ = names.Add(name);
				CollectFieldNames(analyzer, child, names, visited);
			}
		}

		if (resolved.Items is not null)
			CollectFieldNames(analyzer, resolved.Items, names, visited);

		if (resolved.AdditionalProperties is IOpenApiSchema additional)
			CollectFieldNames(analyzer, additional, names, visited);

		foreach (var option in resolved.OneOf ?? [])
			CollectFieldNames(analyzer, option, names, visited);
		foreach (var option in resolved.AnyOf ?? [])
			CollectFieldNames(analyzer, option, names, visited);
	}
}
