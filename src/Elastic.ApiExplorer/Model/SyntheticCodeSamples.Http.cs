// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

public static partial class SyntheticCodeSamples
{
	public static CodeSample CreateHttp(SyntheticSampleInput input, string? mediaType)
	{
		var variables = new HttpVariableNames();
		var serverPrefix = HttpServerPrefix(input, variables);
		// One entry per scheme, so a credential's variable keeps the number of its scheme.
		var credentials = input.Security.Select(SampleCredential.ForHttpFile).ToArray();
		var request = input with { Parameters = [.. input.Parameters.Where(p => !credentials.Any(c => c?.Replaces(p) == true))] };
		var path = HttpPath(request, credentials, variables);
		var headers = HttpHeaders(request, variables);
		path = HttpCredentials(path, credentials, headers);
		if (input.Body is not null)
			headers["Content-Type"] = mediaType ?? "application/json";

		var lines = new List<string> { "###", $"{input.Method.Method.ToUpperInvariant()} {serverPrefix}{path}" };
		lines.AddRange(headers.Select(static h => $"{h.Key}: {HttpLine(h.Value)}"));
		var source = string.Join('\n', lines) + "\n";
		if (input.Body is { } body)
			source += "\n" + body + (body.EndsWith('\n') ? "" : "\n");
		return CodeSample.Generate("HTTP", source);
	}

	private static string HttpServerPrefix(SyntheticSampleInput input, HttpVariableNames variables)
	{
		var server = input.Servers?.FirstOrDefault();
		var url = server?.Url?.Trim().TrimEnd('/');
		if (string.IsNullOrEmpty(url))
			return HttpEnvironmentVariable(input.HostVariable);
		url = HttpLine(url);
		if (WholeHostVariable().Match(url) is { Success: true } host)
			return HttpEnvironmentVariable(host.Groups[1].Value);
		if (url.StartsWith("//", StringComparison.Ordinal))
			url = "https:" + url;
		if (
			!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
			&& !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
		)
			return HttpEnvironmentVariable(input.HostVariable) + "/" + variables.Replace(url.TrimStart('/'), "SERVER", server);
		return variables.Replace(url, "SERVER", server);
	}

	private static string HttpEnvironmentVariable(string name) => "{{" + Placeholder(name.Trim('$', '{', '}'), forShell: false) + "}}";

	private static string HttpPath(SyntheticSampleInput input, SampleCredential?[] credentials, HttpVariableNames variables)
	{
		var target = input.Route;
		foreach (var credential in credentials.Where(static c => c?.In == ParameterLocation.Query))
			target = WithoutHttpQueryKey(target, credential!.Name);
		var built = BuildPath(target, [.. input.Parameters.Where(static p => p.In == ParameterLocation.Path)]);
		// Placeholders are named before the query splits on `&`, which a parameter name may contain.
		var split = built.IndexOf('?', StringComparison.Ordinal);
		var path = split < 0
			? variables.Replace(built, "PATH")
			: variables.Replace(built[..split], "PATH") + "?" + variables.Replace(built[(split + 1)..], "QUERY");
		var existing = QueryNames(path);
		var pairs = input
			.Parameters
			.Where(p => p.In == ParameterLocation.Query && p.Required && !string.IsNullOrEmpty(p.Name) && !existing.Contains(p.Name))
			.DistinctBy(static p => p.Name, StringComparer.Ordinal)
			.SelectMany(static p => QueryPairs(p, ParameterExample.Of(p)))
			.Where(pair => !existing.Contains(HttpQueryName(pair)))
			.Select(pair => variables.Replace(pair, "QUERY"))
			.ToArray();
		return AppendQuery(path, pairs);
	}

	private static Dictionary<string, string> HttpHeaders(SyntheticSampleInput input, HttpVariableNames variables) =>
		RequiredHeaders(input.Parameters)
			.DistinctBy(static h => h.Name, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(
				static h => h.Name,
				h => h.FromSpec || h.Name.Equals("kbn-xsrf", StringComparison.OrdinalIgnoreCase)
					? h.Value
					: variables.Reference("HEADER", h.Name),
				StringComparer.OrdinalIgnoreCase
			);

	private static string HttpCredentials(string path, SampleCredential?[] credentials, Dictionary<string, string> headers)
	{
		foreach (var (credential, index) in credentials.Select(static (c, i) => (c, i)))
		{
			if (credential is null)
				continue;
			var suffix = index == 0 ? "" : $"_{index + 1}";
			var value = credential.Kind switch
			{
				CredentialKind.Basic => "Basic " + HttpEnvironmentVariable("BASIC_CREDENTIALS" + suffix),
				CredentialKind.Bearer => "Bearer " + HttpEnvironmentVariable("TOKEN" + suffix),
				_ => (credential.IsAuthorizationHeader ? "ApiKey " : "") + HttpEnvironmentVariable("API_KEY" + suffix)
			};
			if (credential.In == ParameterLocation.Query)
				path = WithHttpQueryCredential(path, credential.Name, value);
			else
				headers[credential.Name] = value;
		}
		return path;
	}

	private static HashSet<string> QueryNames(string path) => [.. SplitQuery(path).Pairs.Select(HttpQueryName)];

	private static (string Route, string[] Pairs) SplitQuery(string path)
	{
		var split = path.IndexOf('?', StringComparison.Ordinal);
		return split < 0 ? (path, []) : (path[..split], path[(split + 1)..].Split('&'));
	}

	private static string HttpQueryName(string pair) => Uri.UnescapeDataString(pair.Split('=', 2)[0]);

	private static string WithHttpQueryCredential(string path, string name, string value) =>
		AppendQuery(WithoutHttpQueryKey(path, name), [$"{Uri.EscapeDataString(name)}={value}"]);

	private static string WithoutHttpQueryKey(string path, string name)
	{
		var (route, pairs) = SplitQuery(path);
		return AppendQuery(route, [.. pairs.Where(pair => HttpQueryName(pair) != name)]);
	}

	private static string HttpLine(string value) => value.Replace('\r', ' ').Replace('\n', ' ');

	private sealed class HttpVariableNames
	{
		private readonly Dictionary<string, string> _names = [];

		public string Reference(string location, string name)
		{
			var key = location + ":" + name;
			if (_names.TryGetValue(key, out var known))
				return "{{" + known + "}}";
			var root = location + "_" + Placeholder(name, forShell: false);
			var variable = root;
			for (var suffix = 2; _names.ContainsValue(variable); suffix++)
				variable = root + "_" + suffix;
			_names[key] = variable;
			return "{{" + variable + "}}";
		}

		public string Replace(string text, string location, OpenApiServer? server = null) =>
			Template().Replace(text, match =>
			{
				var name = match.Groups[1].Value;
				var value = server?.Variables?.TryGetValue(name, out var variable) == true ? variable.Default : null;
				return value is not null ? HttpLine(value) : Reference(location, name);
			});
	}
}
