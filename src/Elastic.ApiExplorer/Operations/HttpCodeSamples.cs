// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;

namespace Elastic.ApiExplorer.Operations;

public static class HttpCodeSamples
{
	public static IReadOnlyList<ExampleScenario> Fill(
		IReadOnlyList<ExampleScenario> scenarios,
		SyntheticSampleInput input,
		OperationEndpoint endpoint,
		SchemaAnalyzer analyzer,
		HttpSampleSecurity security
	) => [.. scenarios.Select(scenario => FillScenario(scenario, input, endpoint, analyzer, security))];

	private static ExampleScenario FillScenario(
		ExampleScenario scenario,
		SyntheticSampleInput input,
		OperationEndpoint endpoint,
		SchemaAnalyzer analyzer,
		HttpSampleSecurity security
	)
	{
		if (scenario.CodeSamples.Any(static c => c.IsHttp))
			return scenario;
		if (
			security.Value is not HttpSecuritySupported supported
			|| scenario.FromUnmatchedSamples
			|| !string.IsNullOrEmpty(scenario.RequestExternalValue)
		)
			return scenario;
		var content = OperationPageModel.PrimaryRequestContent(input.Operation.RequestBody);
		var declared = scenario.MediaType ?? content?.Key;
		var body = scenario.RequestJson ?? RequestBodyExample.Build(analyzer, declared, content?.Value?.Schema);
		if (input.Operation.RequestBody is not null && body is null)
			return scenario;
		var mediaType = body is null ? null : BodyMediaType(body, declared);
		if (body is not null && mediaType is null)
			return scenario;
		var (method, route) = GeneratedCodeSamples.RequestOf(scenario, endpoint);
		var sample = SyntheticCodeSamples.CreateHttp(
			input with { Method = new HttpMethod(method), Route = route, Body = body, Security = supported.Schemes },
			mediaType
		);
		var samples = scenario.CodeSamples.ToList();
		var curlIndex = samples.FindIndex(static c => c.IsCurl);
		samples.Insert(curlIndex < 0 ? samples.Count : curlIndex + 1, sample);
		return scenario with
		{
			CodeSamples = samples,
			// The flag covers every language, so HTTP takes the request out of the rail only when it is the only sample.
			CodeSamplesIncludeRequest = scenario.CodeSamplesIncludeRequest
				|| (scenario.CodeSamples.Count == 0 && scenario.RequestJson is not null && body == scenario.RequestJson)
		};
	}

	/// <summary>
	/// The type the body is sent as: a JSON type for one JSON document, a newline-delimited type for one JSON value per line.
	/// Specs often list only <c>application/json</c> for a newline-delimited body, as Elasticsearch's <c>_msearch</c> does, so
	/// within those two the body's shape decides. Null for any other declared type or body, which a sample cannot reproduce.
	/// </summary>
	private static string? BodyMediaType(string body, string? declared)
	{
		if (RequestBodyExample.IsNdjson(declared))
			return RequestBodyExample.IsNdjsonDocument(body) ? declared : null;
		if (declared is not null && !RequestBodyExample.IsJson(declared))
			return null;
		if (RequestBodyExample.IsJsonDocument(body))
			return declared ?? "application/json";
		return RequestBodyExample.IsNdjsonDocument(body) ? "application/x-ndjson" : null;
	}
}
