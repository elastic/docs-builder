// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Tests;

/// <summary>Answers every request from <paramref name="responder"/> and records the paths it was asked for.</summary>
internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
	public List<string> RequestedPaths { get; } = [];

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.RequestUri is { IsAbsoluteUri: true } uri)
			RequestedPaths.Add(uri.AbsolutePath);
		return Task.FromResult(responder(request));
	}
}
