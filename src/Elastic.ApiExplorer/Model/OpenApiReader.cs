// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Model;

public static class OpenApiReader
{
	public static async Task<OpenApiDocument?> Create(IFileInfo openApiSpecification)
	{
		if (!openApiSpecification.Exists)
			return null;

		await using var fs = openApiSpecification.OpenRead();
		return await CreateFromStream(fs);
	}

	/// <summary>
	/// Parses an OpenAPI document from an already-open stream, e.g. one fetched remotely through
	/// <see cref="VersionIndexClient.FetchSpecStreamAsync"/>. Closes <paramref name="stream"/> when done.
	/// </summary>
	public static async Task<OpenApiDocument?> CreateFromStream(Stream stream)
	{
		var settings = new OpenApiReaderSettings
		{
			LeaveStreamOpen = false,
			RuleSet = ValidationRuleSet.GetEmptyRuleSet()
		};
		var openApiDocument = await OpenApiDocument.LoadAsync(stream, settings: settings);
		return openApiDocument.Document;
	}
}
