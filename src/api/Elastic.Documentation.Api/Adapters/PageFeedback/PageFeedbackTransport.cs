// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Transport;

namespace Elastic.Documentation.Api.Adapters.PageFeedback;

internal sealed class PageFeedbackTransport(DocumentationEndpoints endpoints) : IDisposable
{
	public ITransport Transport { get; } = ElasticsearchTransportFactory.Create(endpoints.Elasticsearch);

	public void Dispose() => (Transport as IDisposable)?.Dispose();
}
