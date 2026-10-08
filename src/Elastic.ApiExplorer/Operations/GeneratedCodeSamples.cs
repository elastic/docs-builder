// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Model;

namespace Elastic.ApiExplorer.Operations;

/// <summary>
/// Console and curl samples for example scenarios that only ship a JSON body, so Console (the default
/// language, runnable in Kibana Dev Tools) exists for every example.
/// </summary>
public static partial class GeneratedCodeSamples
{
	/// <summary>Adds generated samples to every scenario that has a request body but no code samples.</summary>
	public static IReadOnlyList<ExampleScenario> Fill(
		IReadOnlyList<ExampleScenario> scenarios,
		IReadOnlyList<CodeSample> specSamples,
		OperationEndpoint endpoint
	)
	{
		var console = specSamples.FirstOrDefault(static s => s.IsConsole);
		var curl = specSamples.FirstOrDefault(static s => s.IsCurl);
		var prefix = ConsolePrefix(console?.Source);
		var fallbackMethod = endpoint.Rows.First(r => r.Route == endpoint.ShortestRoute).Method.ToUpperInvariant();

		return [
			.. scenarios.Select(scenario =>
			{
				if (scenario.CodeSamples.Count > 0 || string.IsNullOrWhiteSpace(scenario.RequestJson))
					return scenario;

				var (method, path) = scenario.RequestLine is { } line ? line : (fallbackMethod, endpoint.ShortestRoute);
				method = SampleMethods.Dominant(method, path, endpoint) ?? method;
				var samples = new List<CodeSample>
				{
					new("Console", $"{method} {prefix}{path}\n{scenario.RequestJson.Trim()}", CodeSample.GetHighlightClass("Console"))
					{
						Generated = true
					}
				};
				if (Curl(curl?.Source, method, path, scenario.RequestJson) is { } curlSource)
					samples.Add(new CodeSample("curl", curlSource, CodeSample.GetHighlightClass("curl")) { Generated = true });

				return scenario with { CodeSamples = samples, CodeSamplesIncludeRequest = true };
			})
		];
	}

	/// <summary><c>Run `GET /my-index/_search?from=40` to …</c> in an example description names its request.</summary>
	public static (string Method, string Path)? ParseRequestLine(string? description)
	{
		if (string.IsNullOrWhiteSpace(description) || RunRequestLine().Match(description.Trim()) is not { Success: true } match)
			return null;
		return (match.Groups[1].Value.ToUpperInvariant(), match.Groups[2].Value.Trim());
	}

	/// <summary>The request line a Console sample opens with, e.g. <c>PUT my-index/_doc/1</c>, and the body that follows it.</summary>
	internal static ((string Method, string Path) Line, string Body)? SplitConsole(string? consoleSource)
	{
		if (string.IsNullOrWhiteSpace(consoleSource) || ConsoleRequestLine().Match(consoleSource) is not { Success: true } match)
			return null;
		return ((match.Groups[1].Value.ToUpperInvariant(), match.Groups[2].Value), consoleSource[(match.Index + match.Length)..]);
	}

	/// <summary>The target prefix Console samples put before the route, e.g. <c>kbn:</c> for Kibana. Empty for Elasticsearch.</summary>
	private static string ConsolePrefix(string? consoleSource)
	{
		if (SplitConsole(consoleSource) is not { Line.Path: var target })
			return "";
		var colon = target.IndexOf(':');
		return colon < 0 ? "" : target[..(colon + 1)];
	}

	/// <summary>Reuses the spec's curl sample (host, auth headers) with this example's method, path, and body.</summary>
	private static string? Curl(string? curlSource, string method, string path, string body)
	{
		if (string.IsNullOrWhiteSpace(curlSource) || !CurlBody().IsMatch(curlSource) || !CurlHostPath().IsMatch(curlSource))
			return null;

		var escapedBody = body.Trim().Replace("'", "'\\''", StringComparison.Ordinal);
		var withBody = CurlBody().Replace(curlSource, _ => $"-d '{escapedBody}'", 1);
		var withMethod = CurlMethod().Replace(withBody, $"-X {method}", 1);
		var route = path.StartsWith('/') ? path : "/" + path;
		return CurlHostPath().Replace(withMethod, match => match.Groups[1].Value + route, 1);
	}

	[GeneratedRegex(@"^Run\s+`(GET|POST|PUT|PATCH|DELETE|HEAD)\s+([^`]+)`", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex RunRequestLine();

	[GeneratedRegex(@"^\s*(GET|POST|PUT|PATCH|DELETE|HEAD)\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex ConsoleRequestLine();

	[GeneratedRegex(@"-d\s+'(?:[^']|'\\'')*'", RegexOptions.CultureInvariant)]
	private static partial Regex CurlBody();

	[GeneratedRegex(@"-X\s+[A-Z]+", RegexOptions.CultureInvariant)]
	private static partial Regex CurlMethod();

	/// <summary>The host of the request URL and the path after it: <c>$ELASTICSEARCH_URL/x</c>, <c>${KIBANA_URL}/x</c>, or <c>https://localhost:9200/x</c>.</summary>
	[GeneratedRegex(@"(\$\{?[A-Za-z0-9_]+\}?|https?://[^/""'\s]+)/[^""'\s]*", RegexOptions.CultureInvariant)]
	private static partial Regex CurlHostPath();
}
