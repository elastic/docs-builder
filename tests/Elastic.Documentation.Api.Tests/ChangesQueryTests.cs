// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Clients.Elasticsearch;
using Elastic.Documentation.Search;
using Elastic.Transport;
using Elastic.Transport.Extensions;

namespace Elastic.Documentation.Api.Tests;

public class ChangesQueryTests
{
	private static readonly ElasticsearchClient Client = new(new ElasticsearchClientSettings(new InMemoryRequestInvoker()));

	[Test]
	public void ChangedSince_Date_ListsVisibleDocumentsChangedAfterIt() =>
		Client
			.RequestResponseSerializer
			.SerializeToString(ChangesService.ChangedSince(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero)))
			.Should()
			.Be(
				"""{"bool":{"filter":{"range":{"content_last_updated":{"gt":"2026-11-01T00:00:00.0000000\u002B00:00"}}},"must_not":{"term":{"hidden":{"value":true}}}}}"""
			);
}
