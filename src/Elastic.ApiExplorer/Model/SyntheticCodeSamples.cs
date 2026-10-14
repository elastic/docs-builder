// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
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

	/// <summary>The host the curl calls when the spec names no server, e.g. <c>${ELASTICSEARCH_URL}</c>.</summary>
	public string HostVariable { get; init; } = "${API_URL}";

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
	internal const string ApiKeyVariable = "${API_KEY}";

	public static IReadOnlyList<CodeSample> Create(SyntheticSampleInput input)
	{
		var methodLabel = input.Method.Method.ToUpperInvariant();
		var body = string.IsNullOrWhiteSpace(input.Body) ? null : input.Body.Trim();
		var auth = CurlAuth.From(input.Security, RequiredHeaders(input.Parameters));
		// The path keeps `{name}` where the spec gives no value; each sample then names the placeholder its own way.
		var path = BuildPath(input.Route, input.Parameters);

		var samples = new List<CodeSample>();
		if (input.ConsolePrefix is { } prefix)
		{
			var requestLine = $"{methodLabel} {prefix}{NamePlaceholders(path, forShell: false)}";
			samples.Add(CodeSample.Generate("Console", body is null ? requestLine : $"{requestLine}\n{body}"));
		}

		var url = BuildRequestUrl(AppendQuery(NamePlaceholders(path, forShell: true), auth.Query), input.Servers, input.HostVariable);
		var contentType = input.Operation.RequestBody?.Content?.Keys.FirstOrDefault() ?? "application/json";
		samples.Add(CodeSample.Generate("curl", BuildCurl($"curl -X {methodLabel} \"{url}\"", auth, contentType, body)));
		return samples;
	}

	/// <summary>The path with each parameter that has an example filled in, then the required query parameters.</summary>
	private static string BuildPath(string route, IReadOnlyList<IOpenApiParameter> parameters)
	{
		var path = string.IsNullOrEmpty(route) ? "/" : route.StartsWith('/') ? route : "/" + route;

		foreach (var parameter in parameters.Where(static p => p.In == ParameterLocation.Path && !string.IsNullOrEmpty(p.Name)))
		{
			if (PathValue(parameter, ParameterExample.Of(parameter)) is { } value)
				path = path.Replace($"{{{parameter.Name}}}", value, StringComparison.Ordinal);
		}

		var query = parameters
			.Where(static p => p.In == ParameterLocation.Query && p.Required && !string.IsNullOrEmpty(p.Name))
			.DistinctBy(static p => p.Name, StringComparer.Ordinal)
			.SelectMany(static p => QueryPairs(p, ParameterExample.Of(p)))
			.ToArray();

		return AppendQuery(path, query);
	}

	/// <summary><paramref name="pairs"/> joined onto the URL's query, whether or not it has one yet.</summary>
	internal static string AppendQuery(string url, IReadOnlyCollection<string> pairs) =>
		pairs.Count == 0 ? url : $"{url}{(url.Contains('?') ? '&' : '?')}{string.Join('&', pairs)}";

	/// <summary>One flag per continuation line, the layout <see cref="CurlSourceFormatter"/> gives spec samples.</summary>
	private static string BuildCurl(string request, CurlAuth auth, string contentType, string? body)
	{
		var parts = new List<string> { request };
		parts.AddRange(auth.Headers.Select(static h => $"-H \"{DoubleQuoted(h.Name)}: {DoubleQuoted(h.Value)}\""));
		parts.AddRange(auth.Flags);

		if (body is not null && !auth.Headers.Any(static h => string.Equals(h.Name, "Content-Type", StringComparison.OrdinalIgnoreCase)))
			parts.Add($"-H \"Content-Type: {DoubleQuoted(contentType)}\"");
		if (body is not null)
			parts.Add(SingleQuotedBody(body));

		return string.Join(" \\\n  ", parts);
	}

	/// <summary>
	/// The <c>-d</c> flag with its body in single quotes. A single quote ends the quoted argument, so it becomes <c>'\''</c> as
	/// the shell expects. A JSON document's other lines are indented under the flag, as <see cref="CurlSourceFormatter"/> lays
	/// out spec samples; any other body stays as written, because the added spaces reach the server and would break YAML
	/// or newline-delimited JSON.
	/// </summary>
	internal static string SingleQuotedBody(string body)
	{
		var trimmed = body.Trim();
		var laidOut = IsJsonDocument(trimmed) ? trimmed.Replace("\n", "\n  ", StringComparison.Ordinal) : trimmed;
		return $"-d '{laidOut.Replace("'", "'\\''", StringComparison.Ordinal)}'";
	}

	private static bool IsJsonDocument(string text)
	{
		try
		{
			_ = JsonNode.Parse(text);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	/// <summary>
	/// Spec text inside a double-quoted shell argument. The shell reads <c>\</c>, <c>"</c>, <c>$</c> and <c>`</c> there, so each
	/// is escaped, and a line break would end the command, so it becomes a space.
	/// </summary>
	private static string DoubleQuoted(string text) =>
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
	/// the product's own samples use (<c>${KIBANA_URL}</c>); any other server variable is named the same way in place.
	/// </summary>
	private static string BuildRequestUrl(string pathWithQuery, IList<OpenApiServer>? servers, string hostVariable)
	{
		var serverUrl = servers?.FirstOrDefault()?.Url?.Trim().TrimEnd('/');
		if (string.IsNullOrEmpty(serverUrl))
			return hostVariable + pathWithQuery;
		if (WholeHostVariable().Match(serverUrl) is { Success: true } host)
			return Placeholder(host.Groups[1].Value, forShell: true) + pathWithQuery;
		return NamePlaceholders(serverUrl, forShell: true) + pathWithQuery;
	}

	/// <summary>
	/// A value the reader must supply, named after its parameter the way GitHub's API docs do: <c>{agentPolicyId}</c> is
	/// <c>AGENT_POLICY_ID</c>, and in a curl the shell variable <c>${AGENT_POLICY_ID}</c>, like the host and credential beside
	/// it. Bare braces are never kept, because curl reads them as a glob and would silently send the bare name.
	/// </summary>
	private static string Placeholder(string name, bool forShell)
	{
		var snake = CamelBoundary().Replace(name, "$1_$2");
		var upper = NotWord().Replace(snake, "_").Trim('_').ToUpperInvariant();
		return forShell ? $"${{{upper}}}" : upper;
	}

	/// <summary>Every <c>{name}</c> still in a path becomes its placeholder.</summary>
	internal static string NamePlaceholders(string path, bool forShell) =>
		forShell ? Template().Replace(path, ShellPlaceholder) : Template().Replace(path, ConsolePlaceholder);

	private static readonly MatchEvaluator ShellPlaceholder = static m => Placeholder(m.Groups[1].Value, forShell: true);
	private static readonly MatchEvaluator ConsolePlaceholder = static m => Placeholder(m.Groups[1].Value, forShell: false);

	[GeneratedRegex(@"\{([^{}]+)\}", RegexOptions.CultureInvariant)]
	private static partial Regex Template();

	[GeneratedRegex(@"^https?://\{([^{}/]+)\}$", RegexOptions.CultureInvariant)]
	private static partial Regex WholeHostVariable();

	[GeneratedRegex(@"([a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
	private static partial Regex CamelBoundary();

	[GeneratedRegex(@"[^A-Za-z0-9]+", RegexOptions.CultureInvariant)]
	private static partial Regex NotWord();

	/// <summary>The required headers with the value each sends: its example, else <c>true</c> for <c>kbn-xsrf</c>, else a placeholder.</summary>
	private static IReadOnlyList<Header> RequiredHeaders(IReadOnlyList<IOpenApiParameter> parameters) =>
		[
			.. parameters
				.Where(static p => p.In == ParameterLocation.Header && p.Required && !string.IsNullOrEmpty(p.Name))
				.OrderBy(static p => p.Name, StringComparer.OrdinalIgnoreCase)
				.Select(
					static p => ParameterExample.Of(p).Scalars is { } example
						? new Header(p.Name!, string.Join(',', example), FromSpec: true)
						: new Header(
							p.Name!,
							string.Equals(p.Name, "kbn-xsrf", StringComparison.OrdinalIgnoreCase) ? "true" : "string",
							FromSpec: false
						)
				)
		];

	private sealed record Header(string Name, string Value, bool FromSpec);

	/// <summary>
	/// The value the spec gives a parameter, read once: as plain strings (one per array item, one for a scalar), or as an
	/// object's entries. Both are null when the spec gives none, or a value that is not plain.
	/// </summary>
	private sealed record ParameterExample(string[]? Scalars, (string Key, string Value)[]? Entries)
	{
		public static ParameterExample Of(IOpenApiParameter parameter)
		{
			var node = (SchemaHelpers.IsValue(parameter.Example) ? parameter.Example : null)
				?? parameter.Examples?.Values.Select(static e => e.Value).FirstOrDefault(SchemaHelpers.IsValue)
				?? SchemaHelpers.StatedValue(parameter.Schema);
			return node switch
			{
				JsonArray items => new ParameterExample(PlainStrings(items), null),
				JsonObject properties => new ParameterExample(null, EntriesOf(properties)),
				null => new ParameterExample(null, null),
				_ => new ParameterExample(PlainStrings([node]), null)
			};
		}

		private static (string Key, string Value)[]? EntriesOf(JsonObject properties) =>
			properties.Count > 0 && PlainStrings(properties.Select(static p => p.Value)) is { } values
				? [.. properties.Select((p, i) => (p.Key, values[i]))]
				: null;

		/// <summary>Every node as text, or null when any is missing, empty, or itself an array or object.</summary>
		private static string[]? PlainStrings(IEnumerable<JsonNode?> nodes)
		{
			var parts = nodes.Select(static n => n is JsonValue ? n.ToString() : null).ToArray();
			return parts.Length > 0 && parts.All(static p => !string.IsNullOrEmpty(p)) ? [.. parts.OfType<string>()] : null;
		}
	}

	/// <summary>
	/// A path parameter's example in its style: <c>simple</c> joins with commas (<c>a,b</c>); <c>label</c> leads with a dot
	/// (<c>.a.b</c> exploded, <c>.a,b</c> not); <c>matrix</c> leads with the name (<c>;id=a;id=b</c> exploded, <c>;id=a,b</c> not).
	/// An object is its keys and values in turn, or <c>key=value</c> pairs when exploded.
	/// </summary>
	private static string? PathValue(IOpenApiParameter parameter, ParameterExample example)
	{
		var items = example.Entries is { } entries
			? parameter.Explode
				? entries.Select(static e => $"{Uri.EscapeDataString(e.Key)}={Uri.EscapeDataString(e.Value)}").ToArray()
				: entries.SelectMany(static e => new[] { Uri.EscapeDataString(e.Key), Uri.EscapeDataString(e.Value) }).ToArray()
			: example.Scalars?.Select(Uri.EscapeDataString).ToArray();
		if (items is null)
			return null;

		var name = Uri.EscapeDataString(parameter.Name ?? "");
		return parameter.Style switch
		{
			ParameterStyle.Label => "." + string.Join(parameter.Explode ? "." : ",", items),
			ParameterStyle.Matrix when parameter.Explode && example.Entries is not null => ";" + string.Join(";", items),
			ParameterStyle.Matrix when parameter.Explode => ";" + string.Join(";", items.Select(i => $"{name}={i}")),
			ParameterStyle.Matrix => $";{name}={string.Join(',', items)}",
			_ => string.Join(',', items)
		};
	}

	/// <summary>A query example as it goes into the URL, escaped and joined with the delimiter the parameter's style names. An object is its keys and values in turn, or <c>key=value</c> pairs when exploded.</summary>
	private static string? UrlExample(IOpenApiParameter parameter, ParameterExample example)
	{
		var delimiter = parameter.Style switch
		{
			ParameterStyle.SpaceDelimited => "%20",
			ParameterStyle.PipeDelimited => "%7C",
			_ => ","
		};
		if (example.Entries is { } entries)
		{
			var items = parameter.Explode
				? entries.Select(static e => $"{Uri.EscapeDataString(e.Key)}={Uri.EscapeDataString(e.Value)}")
				: entries.SelectMany(static e => new[] { Uri.EscapeDataString(e.Key), Uri.EscapeDataString(e.Value) });
			return string.Join(delimiter, items);
		}

		return example.Scalars is { } parts ? string.Join(delimiter, parts.Select(Uri.EscapeDataString)) : null;
	}

	/// <summary>
	/// The default form style repeats the name for each array item (<c>ids=a&amp;ids=b</c>) and sends an object as one pair
	/// per property (<c>a=1&amp;b=2</c>); <c>deepObject</c> names each property (<c>filter[a]=1</c>); other styles send one delimited
	/// value. Without an example the value stays <c>{name}</c> for <see cref="NamePlaceholders"/>.
	/// </summary>
	private static IEnumerable<string> QueryPairs(IOpenApiParameter parameter, ParameterExample example)
	{
		var isForm = (parameter.Style ?? ParameterStyle.Form) == ParameterStyle.Form;
		var name = Uri.EscapeDataString(parameter.Name ?? "");
		if (example.Entries is { } entries)
		{
			// Brackets are escaped because curl would read them as a glob.
			if (parameter.Style == ParameterStyle.DeepObject)
				return entries.Select(e => $"{name}%5B{Uri.EscapeDataString(e.Key)}%5D={Uri.EscapeDataString(e.Value)}");
			if (parameter.Explode && isForm)
				return entries.Select(static e => $"{Uri.EscapeDataString(e.Key)}={Uri.EscapeDataString(e.Value)}");
		}

		if (parameter.Explode && isForm && example.Scalars is { Length: > 1 } parts)
			return parts.Select(part => $"{name}={Uri.EscapeDataString(part)}");

		return [$"{name}={UrlExample(parameter, example) ?? $"{{{parameter.Name}}}"}"];
	}

	/// <summary>Whether a curl has a form for a scheme's credential: a cookie key or mutual TLS has none.</summary>
	internal static bool Sends(IOpenApiSecurityScheme scheme) => QueryKey(scheme) is not null || AuthFlag(scheme) is not null;

	/// <summary>The query pair for an API key that travels in the URL.</summary>
	private static string? QueryKey(IOpenApiSecurityScheme scheme) =>
		scheme.Type == SecuritySchemeType.ApiKey && scheme.In == ParameterLocation.Query && !string.IsNullOrEmpty(scheme.Name)
			? $"{scheme.Name}={ApiKeyVariable}"
			: null;

	/// <summary>The curl flag that sends a scheme's credential, and the header it fills.</summary>
	private static (string Header, string Flag)? AuthFlag(IOpenApiSecurityScheme scheme) => scheme.Type switch
	{
		// An API key in the Authorization header travels behind the ApiKey scheme word, as every Elastic API expects it.
		SecuritySchemeType.ApiKey when scheme.In == ParameterLocation.Header
			&& string.Equals(scheme.Name, "Authorization", StringComparison.OrdinalIgnoreCase) =>
			("Authorization", $"-H \"Authorization: ApiKey {ApiKeyVariable}\""),
		SecuritySchemeType.ApiKey when scheme.In == ParameterLocation.Header && !string.IsNullOrEmpty(scheme.Name) =>
			(scheme.Name, $"-H \"{DoubleQuoted(scheme.Name)}: {ApiKeyVariable}\""),
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "basic", StringComparison.OrdinalIgnoreCase) =>
			("Authorization", "-u \"${USERNAME}:${PASSWORD}\""),
		// Digest is a challenge and response, which curl runs itself; a token header could not stand in for it.
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "digest", StringComparison.OrdinalIgnoreCase) =>
			("Authorization", "--digest -u \"${USERNAME}:${PASSWORD}\""),
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase) =>
			("Authorization", "-H \"Authorization: Bearer ${TOKEN}\""),
		SecuritySchemeType.OAuth2 or SecuritySchemeType.OpenIdConnect => ("Authorization", "-H \"Authorization: Bearer ${TOKEN}\""),
		// Any other HTTP scheme (negotiate, hoba, ...) has no form curl can send from a sample.
		_ => null
	};

	/// <summary>
	/// What the curl sends to authenticate: flags, query pairs for keys that travel in the URL, and the required headers
	/// still to send once a credential has taken the place of a placeholder one.
	/// </summary>
	private sealed record CurlAuth(IReadOnlyList<string> Flags, IReadOnlyList<string> Query, IReadOnlyList<Header> Headers)
	{
		public static CurlAuth From(IReadOnlyList<IOpenApiSecurityScheme> schemes, IReadOnlyList<Header> requiredHeaders)
		{
			var flags = new List<string>();
			var query = new List<string>();
			var replaced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var scheme in schemes)
			{
				if (QueryKey(scheme) is { } pair)
					query.Add(pair);
				// A required header with an example of its own keeps it; one without is a placeholder the credential replaces.
				else if (
					AuthFlag(scheme) is { } flag
					&& !requiredHeaders.Any(h => h.FromSpec && string.Equals(h.Name, flag.Header, StringComparison.OrdinalIgnoreCase))
				)
				{
					flags.Add(flag.Flag);
					_ = replaced.Add(flag.Header);
				}
			}

			return new CurlAuth(flags, query, [.. requiredHeaders.Where(h => !replaced.Contains(h.Name))]);
		}
	}
}
