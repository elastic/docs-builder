// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation.Diagnostics;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Supplemental;

internal static class ApiSupplementalValidator
{
	public static void Validate(
		ApiSupplementalDiscoveryResult discovery,
		OpenApiDocument document,
		IDiagnosticsCollector collector,
		string moniker)
	{
		var (operationsById, tagNames) = ApiSupplementalDiscovery.CollectEntities(document);
		if (moniker == "main")
			EmitUnmatched(discovery.Unmatched, collector, "the latest spec");
		else if (int.TryParse(moniker, out var major))
		{
			var tagSlugs = new HashSet<string>(tagNames.Select(ApiUrlBuilder.TagSlug), StringComparer.Ordinal);
			ValidateVersionSuffixed(discovery.VersionSuffixed, major, operationsById, tagSlugs, document, collector);
		}
		else
			return;

		ValidateOperationOverrides(discovery.Operations, operationsById, document, collector);
	}

	private static void EmitUnmatched(
		IReadOnlyList<IFileInfo> unmatched,
		IDiagnosticsCollector collector,
		string specLabel)
	{
		foreach (var file in unmatched)
			EmitUnmatchedFile(file, collector, specLabel);
	}

	private static void EmitUnmatchedFile(IFileInfo file, IDiagnosticsCollector collector, string specLabel)
	{
		var kind = file.Name.StartsWith("op-", StringComparison.OrdinalIgnoreCase)
			? "operationId"
			: "tag";
		collector.EmitError(file, $"API supplemental file '{file.Name}' does not match any {kind} in {specLabel}");
	}

	private static void ValidateVersionSuffixed(
		IReadOnlyList<ApiSupplementalVersionedFile> versionSuffixed,
		int major,
		IReadOnlyDictionary<string, OpenApiOperation> operationsById,
		IReadOnlySet<string> tagSlugs,
		OpenApiDocument document,
		IDiagnosticsCollector collector)
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

				ValidateFileOverrides(versioned.File, operation, analyzer, collector);
				continue;
			}

			if (!tagSlugs.Contains(versioned.Name.Stem))
				EmitUnmatchedFile(versioned.File, collector, specLabel);
		}
	}

	private static void ValidateOperationOverrides(
		IReadOnlyDictionary<string, IFileInfo> operationFiles,
		IReadOnlyDictionary<string, OpenApiOperation> operationsById,
		OpenApiDocument document,
		IDiagnosticsCollector collector)
	{
		var analyzer = new SchemaAnalyzer(document);
		foreach (var (operationId, file) in operationFiles)
		{
			if (!operationsById.TryGetValue(operationId, out var operation))
				continue;

			ValidateFileOverrides(file, operation, analyzer, collector);
		}
	}

	private static void ValidateFileOverrides(
		IFileInfo file,
		OpenApiOperation operation,
		SchemaAnalyzer analyzer,
		IDiagnosticsCollector collector)
	{
		var doc = ApiSupplementalDoc.Parse(file.FileSystem.File.ReadAllText(file.FullName));
		if (doc is null)
			return;

		ValidateOverrideKeys(file, operation, analyzer, collector, doc);
	}

	private static void ValidateOverrideKeys(
		IFileInfo file,
		OpenApiOperation operation,
		SchemaAnalyzer analyzer,
		IDiagnosticsCollector collector,
		ApiSupplementalDoc doc)
	{
		var operationId = operation.OperationId ?? "";
		if (doc.ParameterOverrides.Count > 0)
		{
			var parameterNames = ParameterNames(operation);
			foreach (var key in doc.ParameterOverrides.Keys)
			{
				if (!parameterNames.Contains(key))
					collector.EmitError(file, $"API supplemental: Parameter '{key}' not found in operation '{operationId}'");
			}
		}

		if (doc.RequestBodyOverrides.Count > 0)
		{
			var bodyNames = RequestBodyFieldNames(analyzer, operation);
			foreach (var key in doc.RequestBodyOverrides.Keys)
			{
				if (!bodyNames.Contains(key))
					collector.EmitError(file, $"API supplemental: Request body field '{key}' not found in operation '{operationId}'");
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
		var schema = operation.RequestBody?.Content?.FirstOrDefault().Value?.Schema;
		var properties = analyzer.GetSchemaProperties(schema);
		return new HashSet<string>(properties?.Keys ?? [], StringComparer.OrdinalIgnoreCase);
	}
}
