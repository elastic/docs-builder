// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// Builds minimal Console/curl samples from an operation when the OpenAPI document
/// does not declare <c>x-codeSamples</c>, so the examples rail is never empty.
/// </summary>
public static class SyntheticCodeSamples
{
	public static IReadOnlyList<CodeSample> Create(
		HttpMethod method,
		string route,
		OpenApiOperation operation,
		IList<OpenApiServer>? servers)
	{
		var pathWithQuery = BuildPathWithRequiredQuery(route, operation);
		var methodLabel = method.Method.ToUpperInvariant();

		var consoleSource = $"{methodLabel} {pathWithQuery}";
		var curlSource = BuildCurl(methodLabel, pathWithQuery, operation, servers);

		return
		[
			new CodeSample("Console", consoleSource, CodeSample.GetHighlightClass("Console")),
			new CodeSample("curl", CurlSourceFormatter.Format(curlSource), CodeSample.GetHighlightClass("curl"))
		];
	}

	private static string BuildPathWithRequiredQuery(string route, OpenApiOperation operation)
	{
		var path = string.IsNullOrEmpty(route)
			? "/"
			: route.StartsWith('/')
				? route
				: "/" + route;

		var requiredQuery = (operation.Parameters ?? [])
			.Where(static p => p.In == ParameterLocation.Query && p.Required)
			.Select(static p => p.Name)
			.Where(static name => !string.IsNullOrEmpty(name))
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		if (requiredQuery.Length == 0)
			return path;

		var query = string.Join('&', requiredQuery.Select(static name => $"{name}={{{name}}}"));
		return $"{path}?{query}";
	}

	private static string BuildCurl(
		string methodLabel,
		string pathWithQuery,
		OpenApiOperation operation,
		IList<OpenApiServer>? servers)
	{
		var url = BuildRequestUrl(pathWithQuery, servers);
		var sb = new StringBuilder();
		_ = sb.Append("curl -X ").Append(methodLabel).Append(" \"").Append(url).Append('"');

		foreach (var header in RequiredHeaders(operation))
			_ = sb.Append(" -H \"").Append(header.Name).Append(": ").Append(HeaderExampleValue(header)).Append('"');

		return sb.ToString();
	}

	private static string BuildRequestUrl(string pathWithQuery, IList<OpenApiServer>? servers)
	{
		var serverUrl = servers?.FirstOrDefault()?.Url?.Trim().TrimEnd('/');
		if (string.IsNullOrEmpty(serverUrl))
			return pathWithQuery;

		return serverUrl + pathWithQuery;
	}

	private static IEnumerable<IOpenApiParameter> RequiredHeaders(OpenApiOperation operation) =>
		(operation.Parameters ?? [])
			.Where(static p => p.In == ParameterLocation.Header && p.Required)
			.OrderBy(static p => p.Name, StringComparer.OrdinalIgnoreCase);

	private static string HeaderExampleValue(IOpenApiParameter header)
	{
		if (header.Schema?.Example is { } example)
		{
			var text = example.ToString()?.Trim('"');
			if (!string.IsNullOrEmpty(text))
				return text;
		}

		if (string.Equals(header.Name, "kbn-xsrf", StringComparison.OrdinalIgnoreCase))
			return "true";

		return "string";
	}
}
