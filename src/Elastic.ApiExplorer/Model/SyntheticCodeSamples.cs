// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>What <see cref="SyntheticCodeSamples.Create"/> builds the samples from.</summary>
public sealed record SyntheticSampleInput(HttpMethod Method, string Route, OpenApiOperation Operation)
{
	/// <summary>The parameters the request takes; by default the operation's own, without those its path item declares.</summary>
	public IReadOnlyList<IOpenApiParameter> Parameters { get; init; } = [.. Operation.Parameters ?? []];

	public IList<OpenApiServer>? Servers { get; init; }

	/// <summary>The host the curl calls when the spec names no server, e.g. <c>$ELASTICSEARCH_URL</c>.</summary>
	public string HostVariable { get; init; } = "$API_URL";

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
public static partial class SyntheticCodeSamples
{
	/// <summary>The shell variable that stands for an API key in a sample.</summary>
	internal const string ApiKeyVariable = "$API_KEY";

	public static IReadOnlyList<CodeSample> Create(SyntheticSampleInput input)
	{
		var operation = input.Operation;
		var methodLabel = input.Method.Method.ToUpperInvariant();
		var body = string.IsNullOrWhiteSpace(input.Body) ? null : input.Body.Trim();
		var requiredHeaders = RequiredHeaders(input.Parameters).ToArray();
		var (authFlags, authQuery, authHeaders) = AuthParts(input.Security, requiredHeaders);
		var headers = requiredHeaders.Where(h => h.Name is null || !authHeaders.Contains(h.Name)).ToArray();
		var path = BuildPath(input.Route, input.Parameters, forShell: false);

		var samples = new List<CodeSample>();
		if (input.ConsolePrefix is { } prefix)
		{
			var requestLine = $"{methodLabel} {prefix}{path}";
			samples.Add(CodeSample.Generate("Console", body is null ? requestLine : $"{requestLine}\n{body}"));
		}

		var shellPath = BuildPath(input.Route, input.Parameters, forShell: true);
		var curlPath = authQuery.Count == 0
			? shellPath
			: $"{shellPath}{(shellPath.Contains('?') ? '&' : '?')}{string.Join('&', authQuery)}";
		var curl = BuildCurl(
			methodLabel,
			BuildRequestUrl(curlPath, input.Servers, input.HostVariable),
			operation,
			(headers, authFlags),
			body
		);
		samples.Add(CodeSample.Generate("curl", curl));
		return samples;
	}

	/// <summary>
	/// The path with each parameter that has an example filled in, then the required query parameters. A parameter without an
	/// example shows its name as a placeholder (see <see cref="Placeholder"/>), as a shell variable in the curl.
	/// </summary>
	private static string BuildPath(string route, IReadOnlyList<IOpenApiParameter> parameters, bool forShell)
	{
		var path = string.IsNullOrEmpty(route) ? "/" : route.StartsWith('/') ? route : "/" + route;

		foreach (var parameter in parameters.Where(static p => p.In == ParameterLocation.Path && !string.IsNullOrEmpty(p.Name)))
		{
			if (UrlExample(parameter) is { } value)
				path = path.Replace($"{{{parameter.Name}}}", value, StringComparison.Ordinal);
		}

		path = NamePlaceholders(path, forShell);

		var query = parameters
			.Where(static p => p.In == ParameterLocation.Query && p.Required && !string.IsNullOrEmpty(p.Name))
			.DistinctBy(static p => p.Name, StringComparer.Ordinal)
			.SelectMany(p => QueryPairs(p, forShell))
			.ToArray();

		return query.Length == 0 ? path : $"{path}{(path.Contains('?') ? '&' : '?')}{string.Join('&', query)}";
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
		parts.AddRange(headers.Select(static h => $"-H \"{DoubleQuoted(h.Name ?? "")}: {DoubleQuoted(HeaderExampleValue(h))}\""));
		parts.AddRange(authFlags);

		if (body is not null && !headers.Any(static h => string.Equals(h.Name, "Content-Type", StringComparison.OrdinalIgnoreCase)))
			parts.Add($"-H \"Content-Type: {DoubleQuoted(operation.RequestBody?.Content?.Keys.FirstOrDefault() ?? "application/json")}\"");

		// A single quote ends the quoted argument, so it becomes '\'' as the shell expects. The body's other lines are
		// indented under the flag, the layout CurlSourceFormatter gives spec samples.
		if (body is not null)
			parts.Add($"-d '{body.Replace("'", "'\\''", StringComparison.Ordinal).Replace("\n", "\n  ", StringComparison.Ordinal)}'");

		return string.Join(" \\\n  ", parts);
	}

	/// <summary>
	/// Spec text inside a double-quoted shell argument. The shell reads <c>\</c>, <c>"</c>, <c>$</c> and <c>`</c> there, so each
	/// is escaped, and a line break would end the command, so it becomes a space.
	/// </summary>
	internal static string DoubleQuoted(string text) =>
		text
			.Replace("\\", "\\\\", StringComparison.Ordinal)
			.Replace("\"", "\\\"", StringComparison.Ordinal)
			.Replace("$", "\\$", StringComparison.Ordinal)
			.Replace("`", "\\`", StringComparison.Ordinal)
			.Replace("\r\n", " ", StringComparison.Ordinal)
			.Replace('\r', ' ')
			.Replace('\n', ' ');

	/// <summary>
	/// The first server's URL before the path; a spec that names no server gets the product's host variable, so the curl has
	/// a host at all. A server URL that is one variable (<c>https://{kibana_url}</c>) becomes that variable as the shell variable
	/// the product's own samples use (<c>$KIBANA_URL</c>); any other server variable is named the same way in place.
	/// </summary>
	private static string BuildRequestUrl(string pathWithQuery, IList<OpenApiServer>? servers, string hostVariable)
	{
		var serverUrl = servers?.FirstOrDefault()?.Url?.Trim().TrimEnd('/');
		if (string.IsNullOrEmpty(serverUrl))
			return hostVariable + pathWithQuery;
		if (WholeHostVariable().Match(serverUrl) is { Success: true } host)
			return "$" + Placeholder(host.Groups[1].Value) + pathWithQuery;
		return Template().Replace(serverUrl, static m => "$" + Placeholder(m.Groups[1].Value)) + pathWithQuery;
	}

	/// <summary>
	/// A value the reader must supply, named after its parameter the way GitHub's API docs do: <c>{agentPolicyId}</c> is
	/// <c>AGENT_POLICY_ID</c>, and in a curl the shell variable <c>${AGENT_POLICY_ID}</c>, like the host and credential beside
	/// it. Bare braces are never kept, because curl reads them as a glob and would silently send the bare name.
	/// </summary>
	internal static string Placeholder(string name, bool forShell = false)
	{
		var snake = CamelBoundary().Replace(name.Trim('{', '}'), "$1_$2");
		var upper = NotWord().Replace(snake, "_").Trim('_').ToUpperInvariant();
		return forShell ? $"${{{upper}}}" : upper;
	}

	/// <summary>Every <c>{name}</c> still in a path becomes its placeholder.</summary>
	internal static string NamePlaceholders(string path, bool forShell) =>
		Template().Replace(path, m => Placeholder(m.Groups[1].Value, forShell));

	[GeneratedRegex(@"\{([^{}]+)\}", RegexOptions.CultureInvariant)]
	private static partial Regex Template();

	[GeneratedRegex(@"^https?://\{([^{}/]+)\}$", RegexOptions.CultureInvariant)]
	private static partial Regex WholeHostVariable();

	[GeneratedRegex(@"([a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
	private static partial Regex CamelBoundary();

	[GeneratedRegex(@"[^A-Za-z0-9]+", RegexOptions.CultureInvariant)]
	private static partial Regex NotWord();

	private static IEnumerable<IOpenApiParameter> RequiredHeaders(IReadOnlyList<IOpenApiParameter> parameters) =>
		parameters.Where(static p => p.In == ParameterLocation.Header && p.Required).OrderBy(
			static p => p.Name,
			StringComparer.OrdinalIgnoreCase
		);

	private static string HeaderExampleValue(IOpenApiParameter header)
	{
		if (ExampleParts(header) is { } example)
			return string.Join(',', example);

		if (string.Equals(header.Name, "kbn-xsrf", StringComparison.OrdinalIgnoreCase))
			return "true";

		return "string";
	}

	/// <summary>The value the spec gives a parameter: its example, then its schema's example, default or first enum member.</summary>
	private static JsonNode? ExampleNode(IOpenApiParameter parameter) =>
		(SchemaHelpers.IsValue(parameter.Example) ? parameter.Example : null)
			?? parameter.Examples?.Values.Select(static e => e.Value).FirstOrDefault(SchemaHelpers.IsValue)
			?? SchemaHelpers.FirstExample(parameter.Schema)
			?? (SchemaHelpers.IsValue(parameter.Schema?.Default) ? parameter.Schema!.Default : null)
			?? parameter.Schema?.Enum?.FirstOrDefault(SchemaHelpers.IsValue);

	/// <summary>One part per array item, one part for a scalar. Null when the spec gives no example, and for an object (see <see cref="ExampleEntries"/>).</summary>
	private static string[]? ExampleParts(IOpenApiParameter parameter)
	{
		var node = ExampleNode(parameter);
		if (node is JsonArray items)
		{
			var parts = items.Select(static i => i is JsonValue ? i.ToString() : null).ToArray();
			return parts.Length > 0 && parts.All(static p => !string.IsNullOrEmpty(p)) ? [.. parts.OfType<string>()] : null;
		}

		if (node is null or JsonObject)
			return null;

		var text = node.ToString();
		return string.IsNullOrEmpty(text) ? null : [text];
	}

	/// <summary>The properties of an object example. Null for any other example, and when a property is not a plain value.</summary>
	private static (string Key, string Value)[]? ExampleEntries(IOpenApiParameter parameter)
	{
		if (ExampleNode(parameter) is not JsonObject { Count: > 0 } properties)
			return null;

		var entries = properties.Select(static p => (p.Key, Value: p.Value is JsonValue ? p.Value.ToString() : null)).ToArray();
		return entries.All(static e => !string.IsNullOrEmpty(e.Value)) ? [.. entries.Select(static e => (e.Key, e.Value!))] : null;
	}

	/// <summary>The example as it goes into a URL, escaped and joined with the delimiter the parameter's style names. An object is its keys and values in turn, or <c>key=value</c> pairs when exploded.</summary>
	private static string? UrlExample(IOpenApiParameter parameter)
	{
		var delimiter = parameter.Style switch
		{
			ParameterStyle.SpaceDelimited => "%20",
			ParameterStyle.PipeDelimited => "%7C",
			_ => ","
		};
		if (ExampleEntries(parameter) is { } entries)
		{
			var items = parameter.Explode
				? entries.Select(static e => $"{Uri.EscapeDataString(e.Key)}={Uri.EscapeDataString(e.Value)}")
				: entries.SelectMany(static e => new[] { Uri.EscapeDataString(e.Key), Uri.EscapeDataString(e.Value) });
			return string.Join(delimiter, items);
		}

		return ExampleParts(parameter) is { } parts ? string.Join(delimiter, parts.Select(Uri.EscapeDataString)) : null;
	}

	/// <summary>
	/// The default form style repeats the name for each array item (<c>ids=a&amp;ids=b</c>) and sends an object as one pair
	/// per property (<c>a=1&amp;b=2</c>); <c>deepObject</c> names each property (<c>filter[a]=1</c>); other styles send one delimited value.
	/// </summary>
	private static IEnumerable<string> QueryPairs(IOpenApiParameter parameter, bool forShell)
	{
		var isForm = (parameter.Style ?? ParameterStyle.Form) == ParameterStyle.Form;
		var name = Uri.EscapeDataString(parameter.Name ?? "");
		if (ExampleEntries(parameter) is { } entries)
		{
			// Brackets are escaped because curl would read them as a glob.
			if (parameter.Style == ParameterStyle.DeepObject)
				return entries.Select(e => $"{name}%5B{Uri.EscapeDataString(e.Key)}%5D={Uri.EscapeDataString(e.Value)}");
			if (parameter.Explode && isForm)
				return entries.Select(static e => $"{Uri.EscapeDataString(e.Key)}={Uri.EscapeDataString(e.Value)}");
		}

		if (parameter.Explode && isForm && ExampleParts(parameter) is { Length: > 1 } parts)
			return parts.Select(part => $"{name}={Uri.EscapeDataString(part)}");

		return [$"{name}={UrlExample(parameter) ?? Placeholder(parameter.Name ?? "", forShell)}"];
	}

	/// <summary>
	/// What the operation's security schemes add to the curl: flags, query parameters for schemes that travel in the URL,
	/// and the required headers a flag replaces.
	/// </summary>
	private static (IReadOnlyList<string> Flags, IReadOnlyList<string> Query, HashSet<string> Replaced) AuthParts(
		IReadOnlyList<IOpenApiSecurityScheme> schemes,
		IOpenApiParameter[] requiredHeaders
	)
	{
		var replaced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (schemes.Count == 0)
			return ([], [], replaced);

		// A required header with an example of its own keeps it. One without is a placeholder, so the credential replaces it.
		var exampled = requiredHeaders
			.Where(static h => ExampleParts(h) is not null)
			.Select(static h => h.Name)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var flags = new List<string>();
		var query = new List<string>();
		foreach (var scheme in schemes)
		{
			if (scheme.Type == SecuritySchemeType.ApiKey && scheme.In == ParameterLocation.Query && !string.IsNullOrEmpty(scheme.Name))
				query.Add($"{scheme.Name}={ApiKeyVariable}");
			else if (AuthFlag(scheme) is { } flag && !exampled.Contains(flag.Header))
			{
				flags.Add(flag.Flag);
				_ = replaced.Add(flag.Header);
			}
		}

		return (flags, query, replaced);
	}

	private static (string Header, string Flag)? AuthFlag(IOpenApiSecurityScheme scheme) => scheme.Type switch
	{
		// An API key in the Authorization header travels behind the ApiKey scheme word, as every Elastic API expects it.
		SecuritySchemeType.ApiKey when scheme.In == ParameterLocation.Header
			&& string.Equals(scheme.Name, "Authorization", StringComparison.OrdinalIgnoreCase) =>
			("Authorization", $"-H \"Authorization: ApiKey {ApiKeyVariable}\""),
		SecuritySchemeType.ApiKey when scheme.In == ParameterLocation.Header && !string.IsNullOrEmpty(scheme.Name) =>
			(scheme.Name, $"-H \"{DoubleQuoted(scheme.Name)}: {ApiKeyVariable}\""),
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
