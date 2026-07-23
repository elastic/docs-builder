// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Transport;

namespace Elastic.Documentation.Api.Adapters.PageFeedback;

internal sealed class PageFeedbackTransport : IDisposable
{
	public PageFeedbackTransport(DocumentationEndpoints endpoints)
		: this(ElasticsearchTransportFactory.Create(endpoints.Elasticsearch))
	{
	}

	internal PageFeedbackTransport(ITransport transport) => Transport = transport;

	public ITransport Transport { get; }

	public void Dispose() => (Transport as IDisposable)?.Dispose();
}
