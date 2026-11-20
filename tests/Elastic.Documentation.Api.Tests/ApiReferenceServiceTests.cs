// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search.ApiReference;
using FakeItEasy;

namespace Elastic.Documentation.Api.Tests;

public class ApiReferenceServiceTests
{
	[Test]
	[Arguments("everything")]
	[Arguments("FULL")]
	[Arguments("")]
	public async Task QueryAsync_UnknownView_RejectsTheQueryWithoutReadingTheIndex(string view)
	{
		var gateway = A.Fake<IApiGateway>();

		var reply = await new ApiReferenceService(gateway).QueryAsync(
			new ApiOperationQuery { Api = "elasticsearch", Endpoint = "GET /_cluster/health", View = view },
			TestContext.Current!.Execution.CancellationToken
		);

		(reply is ApiReferenceRejection rejected ? rejected.Message : null).Should().Be("view is 'basic' or 'full'.");
		A.CallTo(gateway).MustNotHaveHappened();
	}

	[Test]
	[Arguments(null, false)]
	[Arguments("basic", false)]
	[Arguments("full", true)]
	public void Full_IsSetOnlyByTheFullView(string? view, bool full) => new ApiOperationQuery { View = view }.Full.Should().Be(full);
}
