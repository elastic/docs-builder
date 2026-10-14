// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>What <see cref="SyntheticCodeSamples.Create"/> builds the samples from.</summary>
public sealed record SyntheticSampleInput(HttpMethod Method, string Route, OpenApiOperation Operation)
{
	public IList<OpenApiServer>? Servers { get; init; }

	/// <summary>An example request body; the samples carry it instead of leaving it in a block of its own.</summary>
	public string? Body { get; init; }

	/// <summary>The schemes the operation's curl authenticates with.</summary>
	public IReadOnlyList<IOpenApiSecurityScheme> Security { get; init; } = [];

	/// <summary>What Console puts before the route: empty for Elasticsearch, <c>kbn:</c> for Kibana. Null when Console can't call the API, so there is no Console sample.</summary>
	public string? ConsolePrefix { get; init; } = "";
}

/// <summary>
/// Builds minimal Console/curl samples from an operation when the OpenAPI document
/// does not declare <c>x-codeSamples</c>, so the examples rail is never empty. Every value comes from the
/// spec: parameter examples, the security schemes and the request example. With a request
/// example body the samples carry it, so the body sits in the request like a spec sample's would
/// instead of in a block of its own.
/// </summary>
public static class SyntheticCodeSamples
{
	public static IReadOnlyList<CodeSample> Create(SyntheticSampleInput input)
	{
		var operation = input.Operation;
		var methodLabel = input.Method.Method.ToUpperInvariant();
		var body = string.IsNullOrWhiteSpace(input.Body) ? null : input.Body.Trim();
		var headers = RequiredHeaders(operation).ToArray();
		var (authFlags, authQuery) = AuthParts(input.Security, headers);
		var path = BuildPath(input.Route, operation);

		var samples = new List<CodeSample>();
		if (input.ConsolePrefix is { } prefix)
		{
			var requestLine = $"{methodLabel} {prefix}{path}";
			samples.Add(CodeSample.Generate("Console", body is null ? requestLine : $"{requestLine}\n{body}"));
		}

		var curlPath = authQuery.Count == 0 ? path : $"{path}{(path.Contains('?') ? '&' : '?')}{string.Join('&', authQuery)}";
		var curl = BuildCurl(methodLabel, BuildRequestUrl(curlPath, input.Servers), operation, (headers, authFlags), body);
		samples.Add(CodeSample.Generate("curl", curl));
		return samples;
	}

	/// <summary>The path with each parameter that has an example filled in, then the required query parameters. A parameter without an example keeps its <c>{name}</c>.</summary>
	private static string BuildPath(string route, OpenApiOperation operation)
	{
		var path = string.IsNullOrEmpty(route) ? "/" : route.StartsWith('/') ? route : "/" + route;
		var parameters = operation.Parameters ?? [];

		foreach (var parameter in parameters.Where(static p => p.In == ParameterLocation.Path && !string.IsNullOrEmpty(p.Name)))
		{
			if (ExampleOf(parameter) is { } value)
				path = path.Replace($"{{{parameter.Name}}}", Uri.EscapeDataString(value), StringComparison.Ordinal);
		}

		var query = parameters
			.Where(static p => p.In == ParameterLocation.Query && p.Required && !string.IsNullOrEmpty(p.Name))
			.DistinctBy(static p => p.Name, StringComparer.Ordinal)
			.Select(static p => $"{p.Name}={(ExampleOf(p) is { } value ? Uri.EscapeDataString(value) : $"{{{p.Name}}}")}")
			.ToArray();

		return query.Length == 0 ? path : $"{path}?{string.Join('&', query)}";
	}

	/// <summary>One flag per continuation line, the layout <see cref="CurlSourceFormatter"/> gives spec samples.</summary>
	private static string BuildCurl(
		string methodLabel,
		string url,
		OpenApiOperation operation,
		(IOpenApiParameter[] Headers, IReadOnlyList<string> AuthFlags) flags,
		string? body
	)
	{
		var (headers, authFlags) = flags;
		var parts = new List<string> { $"curl -X {methodLabel} \"{url}\"" };
		parts.AddRange(headers.Select(static h => $"-H \"{h.Name}: {HeaderExampleValue(h)}\""));
		parts.AddRange(authFlags);

		// A body-less request still states the type it accepts when the spec declares one.
		var contentType = operation.RequestBody?.Content?.Keys.FirstOrDefault();
		if (contentType is null && body is not null)
			contentType = "application/json";
		if (contentType is not null && !headers.Any(static h => string.Equals(h.Name, "Content-Type", StringComparison.OrdinalIgnoreCase)))
			parts.Add($"-H \"Content-Type: {contentType}\"");

		// A single quote ends the quoted argument, so it becomes '\'' as the shell expects.
		if (body is not null)
			parts.Add($"-d '{body.Replace("'", "'\\''", StringComparison.Ordinal)}'");

		return string.Join(" \\\n  ", parts);
	}

	private static string BuildRequestUrl(string pathWithQuery, IList<OpenApiServer>? servers)
	{
		var serverUrl = servers?.FirstOrDefault()?.Url?.Trim().TrimEnd('/');
		if (string.IsNullOrEmpty(serverUrl))
			return pathWithQuery;

		return serverUrl + pathWithQuery;
	}

	private static IEnumerable<IOpenApiParameter> RequiredHeaders(OpenApiOperation operation) =>
		(operation.Parameters ?? []).Where(static p => p.In == ParameterLocation.Header && p.Required).OrderBy(
			static p => p.Name,
			StringComparer.OrdinalIgnoreCase
		);

	private static string HeaderExampleValue(IOpenApiParameter header)
	{
		if (ExampleOf(header) is { } example)
			return example;

		if (string.Equals(header.Name, "kbn-xsrf", StringComparison.OrdinalIgnoreCase))
			return "true";

		return "string";
	}

	/// <summary>The example the spec gives a parameter: its own, then its schema's. Null when it gives none.</summary>
	private static string? ExampleOf(IOpenApiParameter parameter)
	{
		var node = parameter.Example
			?? parameter.Examples?.Values.Select(static e => e.Value).FirstOrDefault(static v => v is not null)
			?? parameter.Schema?.Examples?.FirstOrDefault(static v => v is not null);
		var text = node?.ToString().Trim('"');
		return string.IsNullOrEmpty(text) ? null : text;
	}

	/// <summary>What the operation's security schemes add to the curl: flags, and query parameters for schemes that travel in the URL.</summary>
	private static (IReadOnlyList<string> Flags, IReadOnlyList<string> Query) AuthParts(
		IReadOnlyList<IOpenApiSecurityScheme> schemes,
		IOpenApiParameter[] requiredHeaders
	)
	{
		if (schemes.Count == 0)
			return ([], []);

		// A header the operation already requires carries its own example, so the scheme doesn't repeat it.
		var required = requiredHeaders.Select(static h => h.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var flags = new List<string>();
		var query = new List<string>();
		foreach (var scheme in schemes)
		{
			if (scheme.Type == SecuritySchemeType.ApiKey && scheme.In == ParameterLocation.Query && !string.IsNullOrEmpty(scheme.Name))
				query.Add($"{scheme.Name}=$API_KEY");
			else if (AuthFlag(scheme) is { } flag && !required.Contains(flag.Header))
				flags.Add(flag.Flag);
		}

		return (flags, query);
	}

	private static (string Header, string Flag)? AuthFlag(IOpenApiSecurityScheme scheme) => scheme.Type switch
	{
		SecuritySchemeType.ApiKey when scheme.In == ParameterLocation.Header && !string.IsNullOrEmpty(scheme.Name) =>
			(scheme.Name, $"-H \"{scheme.Name}: $API_KEY\""),
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "basic", StringComparison.OrdinalIgnoreCase) =>
			("Authorization", "-u \"$USERNAME:$PASSWORD\""),
		SecuritySchemeType.Http when !string.IsNullOrEmpty(scheme.Scheme)
			&& !string.Equals(scheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase) =>
			("Authorization", $"-H \"Authorization: {scheme.Scheme} $TOKEN\""),
		SecuritySchemeType.Http or SecuritySchemeType.OAuth2 or SecuritySchemeType.OpenIdConnect =>
			("Authorization", "-H \"Authorization: Bearer $TOKEN\""),
		_ => null
	};
}
